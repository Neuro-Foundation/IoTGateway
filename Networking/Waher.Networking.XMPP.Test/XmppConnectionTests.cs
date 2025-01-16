﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Events;
using Waher.Events.Console;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP.AuthenticationErrors;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppConnectionTests
	{
		private static ConsoleEventSink sink = null;
		protected AutoResetEvent connected = new(false);
		protected AutoResetEvent error = new(false);
		protected AutoResetEvent offline = new(false);
		protected XmppClient client;
		protected Exception ex = null;
		protected Type permittedExceptionType = null;

		public XmppConnectionTests()
		{
		}

		[ClassInitialize]
		public static void ClassInitialize(TestContext _)
		{
			sink = new ConsoleEventSink();
			Log.Register(sink);
		}

		[ClassCleanup]
		public static async Task ClassCleanup()
		{
			if (sink is not null)
			{
				Log.Unregister(sink);
				await sink.DisposeAsync();
				sink = null;
			}
		}

		[TestInitialize]
		public void Setup()
		{
			this.connected.Reset();
			this.error.Reset();
			this.offline.Reset();

			this.ex = null;
			this.permittedExceptionType = null;

			this.client = new XmppClient(this.GetCredentials(), "en", typeof(CommunicationTests).Assembly)
			{
				DefaultNrRetries = 2,
				DefaultRetryTimeout = 1000,
				DefaultMaxRetryTimeout = 5000,
				DefaultDropOff = true
			};

			this.client.SetTag("ShowE2E", true);
			this.client.Add(new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount, LineEnding.NewLine));

			this.client.OnConnectionError += this.Client_OnConnectionError;
			this.client.OnError += this.Client_OnError;
			this.client.OnStateChanged += this.Client_OnStateChanged;
			this.client.Connect();
		}

		public virtual XmppCredentials GetCredentials()
		{
			return new XmppCredentials()
			{
				Host = "waher.se",
				Port = 5222,
				Account = "xmppclient.test01",
				Password = "testpassword"
			};
		}

		private Task Client_OnStateChanged(object Sender, XmppState NewState)
		{
			switch (NewState)
			{
				case XmppState.Connected:
					this.connected.Set();
					break;

				case XmppState.Error:
					this.error.Set();
					break;

				case XmppState.Offline:
					this.offline.Set();
					break;
			}

			return Task.CompletedTask;
		}

		Task Client_OnError(object Sender, Exception Exception)
		{
			this.ex = Exception;
			return Task.CompletedTask;
		}

		Task Client_OnConnectionError(object Sender, Exception Exception)
		{
			this.ex = Exception;
			return Task.CompletedTask;
		}

		private int Wait(int Timeout)
		{
			return WaitHandle.WaitAny(new WaitHandle[] { this.connected, this.error, this.offline }, Timeout);
		}

		private void WaitConnected(int Timeout)
		{
			switch (this.Wait(Timeout))
			{
				default:
					Assert.Fail("Unable to connect. Timeout occurred.");
					break;

				case 0:
					break;

				case 1:
					Assert.Fail("Unable to connect. Error occurred.");
					break;

				case 2:
					Assert.Fail("Unable to connect. Client turned offline.");
					break;
			}
		}

		private void WaitError(int Timeout)
		{
			switch (this.Wait(Timeout))
			{
				default:
					Assert.Fail("Expected error. Timeout occurred.");
					break;

				case 0:
					Assert.Fail("Expected error. Connection successful.");
					break;

				case 1:
					break;

				case 2:
					Assert.Fail("Expected error. Client turned offline.");
					break;
			}

			this.permittedExceptionType = typeof(NotAuthorizedException);
			this.ex = null;
		}

		private void WaitOffline(int Timeout)
		{
			switch (this.Wait(Timeout))
			{
				default:
					Assert.Fail("Expected offline. Timeout occurred.");
					break;

				case 0:
					Assert.Fail("Expected offline. Connection successful.");
					break;

				case 1:
					Assert.Fail("Expected offline. Error occured.");
					break;

				case 2:
					break;
			}
		}

		[TestCleanup]
		public async Task TearDown()
		{
			if (this.client is not null)
			{
				await this.client.OfflineAndDisposeAsync(false);
				this.client = null;
			}

			if (this.ex is not null && this.ex.GetType() != this.permittedExceptionType)
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(this.ex).Throw();
		}

		[TestMethod]
		public void Connection_Test_01_Connect_AutoCreate()
		{
			this.client.AllowRegistration();
			this.WaitConnected(10000);
		}

		[TestMethod]
		public void Connection_Test_02_Connect()
		{
			this.WaitConnected(10000);
		}

		[TestMethod]
		public async Task Connection_Test_03_Disconnect()
		{
			this.WaitConnected(10000);

			if (this.client is not null)
			{
				await this.client.OfflineAndDisposeAsync(false);
				this.client = null;
			}

			this.WaitOffline(10000);
			this.ex = null;
		}

		[TestMethod]
		public async Task Connection_Test_04_NotAuthorized()
		{
			await this.Connection_Test_03_Disconnect();

			this.client = new XmppClient("waher.se", 5222, "xmppclient.test01", "abc", "en", typeof(CommunicationTests).Assembly);
			this.client.SetTag("ShowE2E", true);
			this.client.OnConnectionError += this.Client_OnConnectionError;
			this.client.OnError += this.Client_OnError;
			this.client.OnStateChanged += this.Client_OnStateChanged;
			await this.client.Connect();

			this.WaitError(10000);
		}

		[TestMethod]
		public async Task Connection_Test_05_Reconnect()
		{
			this.WaitConnected(10000);

			this.permittedExceptionType = typeof(OperationCanceledException);

			await this.client.HardOffline();
			this.WaitOffline(10000);

			Thread.Sleep(100);

			this.offline.Reset();
			this.error.Reset();
			this.connected.Reset();

			await this.client.Reconnect();
			this.WaitConnected(10000);
		}

		[TestMethod]
		[Ignore("Feature not supported on server.")]
		public void Connection_Test_06_ChangePassword()
		{
			AutoResetEvent Changed = new(false);

			this.WaitConnected(10000);

			this.client.OnPasswordChanged += (Sender, e) =>
			{
				Changed.Set();
				return Task.CompletedTask;
			};

			this.client.ChangePassword("newtestpassword");
			Assert.IsTrue(Changed.WaitOne(10000), "Unable to change password.");

			this.client.ChangePassword("testpassword");
			Assert.IsTrue(Changed.WaitOne(10000), "Unable to change password back.");
		}
	}
}
