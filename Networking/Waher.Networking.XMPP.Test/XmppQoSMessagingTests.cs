﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppQoSMessagingTests : CommunicationTests
	{
		[ClassInitialize]
		public static void ClassInitialize(TestContext _)
		{
			SetupSnifferAndLog();
		}

		[ClassCleanup]
		public static async Task ClassCleanup()
		{
			await DisposeSnifferAndLog();
		}

		[TestMethod]
		public async Task QoS_Test_01_Unacknowledged_Service()
		{
			await this.QoSTest(QoSLevel.Unacknowledged);
		}

		[TestMethod]
		public async Task QoS_Test_02_Acknowledged_Service()
		{
			await this.QoSTest(QoSLevel.Acknowledged);
		}

		[TestMethod]
		public async Task QoS_Test_03_Assured_Service()
		{
			await this.QoSTest(QoSLevel.Assured);
		}

		private async Task QoSTest(QoSLevel Level)
		{
			await this.ConnectClients();

			ManualResetEvent Received = new(false);
			ManualResetEvent Delivered = new(false);

			this.client2.OnNormalMessage += (Sender, e) => { Received.Set(); return Task.CompletedTask; };

			await this.client1.SendMessage(Level, MessageType.Normal, this.client2.FullJID, string.Empty, "Hello", string.Empty, "en",
				string.Empty, string.Empty, (Sender, e) => { Delivered.Set(); return Task.CompletedTask; }, null);

			Assert.IsTrue(Delivered.WaitOne(10000), "Message not delivered properly.");
			Assert.IsTrue(Received.WaitOne(10000), "Message not received properly.");
		}

		[TestMethod]
		public async Task QoS_Test_04_Timeout()
		{
			ManualResetEvent Done = new(false);
			IqResultEventArgs e2 = null;

			await this.ConnectClients();

			this.client2.RegisterIqGetHandler("test", "test", (Sender, e) =>
			{
				// Do nothing. Do not return result or error.
				return Task.CompletedTask;
			}, false);

			await this.client1.SendIqGet(this.client2.FullJID, "<test:test xmlns:test='test'/>", (Sender, e) =>
			{
				e2 = e;
				Done.Set();
				return Task.CompletedTask;
			}, null, 1000, 3, true, int.MaxValue);

			Assert.IsTrue(Done.WaitOne(20000), "Retry function not working properly.");
			Assert.IsFalse(e2.Ok, "Request not properly cancelled.");
		}
	}
}
