﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Events;
using Waher.Events.Console;
using Waher.Networking.Sniffers;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppComponentTests
	{
		private static ConsoleEventSink sink = null;
		protected AutoResetEvent clientConnected = new(false);
		protected AutoResetEvent clientError = new(false);
		protected AutoResetEvent clientOffline = new(false);
		protected AutoResetEvent componentConnected = new(false);
		protected AutoResetEvent componentError = new(false);
		protected AutoResetEvent componentOffline = new(false);
		protected XmppClient client;
		protected XmppComponent component;
		protected Exception clientEx = null;
		protected Exception componentEx = null;

		public XmppComponentTests()
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
		public virtual void Setup()
		{
			this.clientConnected.Reset();
			this.clientError.Reset();
			this.clientOffline.Reset();

			this.componentConnected.Reset();
			this.componentError.Reset();
			this.componentOffline.Reset();
			
			this.clientEx = null;
			this.componentEx = null;

			this.client = new XmppClient("localhost", 5222, "testuser", "testpass", "en", typeof(CommunicationTests).Assembly)
			{
				TrustServer = true,
				DefaultNrRetries = 2,
				DefaultRetryTimeout = 1000,
				DefaultMaxRetryTimeout = 5000,
				DefaultDropOff = true
			};
			//this.client.Add(new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount));

			this.client.SetTag("ShowE2E", true);
			this.client.OnConnectionError += this.Client_OnConnectionError;
			this.client.OnError += this.Client_OnError;
			this.client.OnStateChanged += this.Client_OnStateChanged;

			this.client.SetPresence(Availability.Chat, new KeyValuePair<string, string>("en", "Live and well"));
			this.client.Connect();

			this.component = new XmppComponent("localhost", 5275, "provisioning.peterwaher-hp14", "provisioning", "collaboration", "provisioning", "Provisioning service")
			{
				DefaultNrRetries = 2,
				DefaultRetryTimeout = 1000,
				DefaultMaxRetryTimeout = 5000,
				DefaultDropOff = true
			};

			this.component.Add(new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount, LineEnding.NewLine));

			this.component.OnConnectionError += this.Component_OnConnectionError;
			this.component.OnError += this.Component_OnError;
			this.component.OnStateChanged += this.Component_OnStateChanged;

			this.WaitConnected(10000);
		}

		private Task Client_OnStateChanged(object Sender, XmppState NewState)
		{
			switch (NewState)
			{
				case XmppState.Connected:
					this.clientEx = null;
					this.clientConnected.Set();
					break;

				case XmppState.Error:
					this.clientError.Set();
					break;

				case XmppState.Offline:
					this.clientOffline.Set();
					break;
			}

			return Task.CompletedTask;
		}

		private Task Component_OnStateChanged(object Sender, XmppState NewState)
		{
			switch (NewState)
			{
				case XmppState.Connected:
					this.componentConnected.Set();
					break;

				case XmppState.Error:
					this.componentError.Set();
					break;

				case XmppState.Offline:
					this.componentOffline.Set();
					break;
			}
		
			return Task.CompletedTask;
		}

		Task Client_OnError(object Sender, Exception Exception)
		{
			this.clientEx = Exception;
			return Task.CompletedTask;
		}

		Task Component_OnError(object Sender, Exception Exception)
		{
			this.componentEx = Exception;
			return Task.CompletedTask;
		}

		Task Client_OnConnectionError(object Sender, Exception Exception)
		{
			this.clientEx = Exception;
			return Task.CompletedTask;
		}

		Task Component_OnConnectionError(object Sender, Exception Exception)
		{
			this.componentEx = Exception;
			return Task.CompletedTask;
		}

		private int WaitClient(int Timeout)
		{
			return WaitHandle.WaitAny(new WaitHandle[] { this.clientConnected, this.clientError, this.clientOffline }, Timeout);
		}

		private int WaitComponent(int Timeout)
		{
			return WaitHandle.WaitAny(new WaitHandle[] { this.componentConnected, this.componentError, this.componentOffline }, Timeout);
		}

		private void WaitClientConnected(int Timeout)
		{
			AssertWaitConnected(this.WaitClient(Timeout));
		}

		private void WaitComponentConnected(int Timeout)
		{
			AssertWaitConnected(this.WaitComponent(Timeout));
		}

		private void WaitConnected(int Timeout)
		{
			this.WaitClientConnected(Timeout);
			this.WaitComponentConnected(Timeout);
		}

		private static void AssertWaitConnected(int Event)
		{
			switch (Event)
			{
				case -1:
					Assert.Fail("Unable to connect. Timeout occurred.");
					break;

				case 1:
					Assert.Fail("Unable to connect. Error occurred.");
					break;

				case 2:
					Assert.Fail("Unable to connect. Client turned offline.");
					break;
			}
		}

		[TestCleanup]
		public virtual async Task TearDown()
		{
			if (this.client is not null)
			{
				await this.client.OfflineAndDisposeAsync(false);
				this.client = null;
			}

			if (this.component is not null)
			{
				await this.component.DisposeAsync();
				this.component = null;
			}

			if (this.clientEx is not null)
				throw new TargetInvocationException(this.clientEx);

			if (this.componentEx is not null)
				throw new TargetInvocationException(this.componentEx);
		}

		[TestMethod]
		[Ignore]
		public void Component_Test_01_Connect()
		{
		}
	}
}
