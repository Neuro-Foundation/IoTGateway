﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content.Binary;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Runtime.Console;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppFileUploadTests
	{
		protected XmppClient client1;
		protected Exception ex1 = null;
		protected ManualResetEvent connected1 = new(false);
		protected ManualResetEvent error1 = new(false);
		protected ManualResetEvent offline1 = new(false);
		private HttpFileUploadClient httpUpload = null;

		[TestInitialize]
		public void TestInitialize()
		{
			this.connected1.Reset();
			this.error1.Reset();
			this.offline1.Reset();

			this.ex1 = null;

			this.client1 = new XmppClient("xmpp.is", 5222, "unit.test", "testpassword", "en", typeof(CommunicationTests).Assembly)
			{
				DefaultNrRetries = 2,
				DefaultRetryTimeout = 1000,
				DefaultMaxRetryTimeout = 5000,
				DefaultDropOff = true
			};

			this.client1.Add(new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount, LineEnding.NewLine));
			this.client1.OnConnectionError += this.Client_OnConnectionError1;
			this.client1.OnError += this.Client_OnError1;
			this.client1.OnStateChanged += this.Client_OnStateChanged1;
			this.client1.SetPresence(Availability.Chat, new KeyValuePair<string, string>("en", "Live and well"));
			this.client1.Connect();

			this.WaitConnected1(5000);
		}

		private Task Client_OnStateChanged1(object _, XmppState NewState)
		{
			switch (NewState)
			{
				case XmppState.Connected:
					this.connected1.Set();
					break;

				case XmppState.Error:
					this.error1.Set();
					break;

				case XmppState.Offline:
					this.offline1.Set();
					break;

				case XmppState.Connecting:
					break;
			}

			return Task.CompletedTask;
		}

		Task Client_OnError1(object Sender, Exception Exception)
		{
			this.ex1 = Exception;
			return Task.CompletedTask;
		}

		Task Client_OnConnectionError1(object Sender, Exception Exception)
		{
			this.ex1 = Exception;
			return Task.CompletedTask;
		}

		private int Wait1(int Timeout)
		{
			return WaitHandle.WaitAny(new WaitHandle[] { this.connected1, this.error1, this.offline1 }, Timeout);
		}

		private void WaitConnected1(int Timeout)
		{
			AssertWaitConnected(this.Wait1(Timeout));
		}

		private static void AssertWaitConnected(int Event)
		{
			switch (Event)
			{
				case -1:
				case WaitHandle.WaitTimeout:
					Assert.Fail("Unable to connect. Timeout occurred.");
					break;

				case 0: // Connected
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
		public void TestCleanup()
		{
			this.client1?.Dispose();

			if (this.ex1 is not null)
				throw new TargetInvocationException(this.ex1);
		}

		[TestMethod]
		public void FileUpload_Test_01_Discovery()
		{
			ManualResetEvent Done = new(false);
			ManualResetEvent Error = new(false);

			this.httpUpload = new HttpFileUploadClient(this.client1);
			this.httpUpload.Discover((sender, e) =>
			{
				if (this.httpUpload.HasSupport)
					Done.Set();
				else
					Error.Set();
			});

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }), 5000);

			ConsoleOut.WriteLine("JID: " + this.httpUpload.FileUploadJid);
			ConsoleOut.WriteLine("Max File Size: " + this.httpUpload.MaxFileSize.ToString());
		}

		[TestMethod]
		public void FileUpload_Test_02_RequestUploadSlot()
		{
			this.FileUpload_Test_01_Discovery();

			string FileName = Guid.NewGuid().ToString().Replace("-", string.Empty) + ".bin";
			string ContentType = BinaryCodec.DefaultContentType;
			Random Rnd = new();
			byte[] Bin = new byte[1024];
			Rnd.NextBytes(Bin);

			ManualResetEvent Done = new(false);
			ManualResetEvent Error = new(false);
			HttpFileUploadEventArgs e = null;

			this.httpUpload.RequestUploadSlot(FileName, ContentType, Bin.Length, (sender, e2) =>
			{
				e = e2;
				if (e2.Ok)
					Done.Set();
				else
					Error.Set();

				return Task.CompletedTask;

			}, null);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }), 5000);

			ConsoleOut.WriteLine("GET URL:" + e.GetUrl);
			ConsoleOut.WriteLine("PUT URL:" + e.PutUrl);
			ConsoleOut.WriteLine("PUT Headers:");

			if (e.PutHeaders is not null)
			{
				foreach (KeyValuePair<string, string> Header in e.PutHeaders)
					ConsoleOut.WriteLine(Header.Key + ": " + Header.Value);
			}
		}

	}
}
