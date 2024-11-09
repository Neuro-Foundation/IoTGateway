﻿using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppRosterTests : CommunicationTests
	{
		[ClassInitialize]
		public static void ClassInitialize(TestContext _)
		{
			SetupSnifferAndLog();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			DisposeSnifferAndLog();
		}

		[TestMethod]
		public void Roster_Test_01_GetRoster()
		{
			this.ConnectClients();
			Assert.IsTrue(this.client1.HasRoster);
			Assert.IsTrue(this.client2.HasRoster);
		}

		[TestMethod]
		public void Roster_Test_02_AddRosterItem()
		{
			this.ConnectClients();
			using ManualResetEvent Added = new(false);

			this.client1.AddRosterItem(new RosterItem(this.client2.BareJID, "Test Client 2", "Test Clients"),
				(Sender, e) => { Added.Set(); return Task.CompletedTask; }, null);

			Assert.IsTrue(Added.WaitOne(10000), "Roster item not properly added.");
		}

		[TestMethod]
		public void Roster_Test_03_UpdateRosterItem()
		{
			this.ConnectClients();
			using ManualResetEvent Updated = new(false);
			
			this.client1.UpdateRosterItem(this.client2.BareJID, "Test Client II", new string[] { "Test Clients" },
				(Sender, e) => { Updated.Set(); return Task.CompletedTask; }, null);

			Assert.IsTrue(Updated.WaitOne(10000), "Roster item not properly updated.");
		}

		[TestMethod]
		public void Roster_Test_04_RemoveRosterItem()
		{
			this.ConnectClients();
			using ManualResetEvent Removed = new(false);
			
			this.client1.RemoveRosterItem(this.client2.BareJID, (Sender, e) => { Removed.Set(); return Task.CompletedTask; }, null);

			Assert.IsTrue(Removed.WaitOne(10000), "Roster item not properly removed.");
		}

		[TestMethod]
		public void Roster_Test_05_AcceptPresenceSubscription()
		{
			this.ConnectClients();
			ManualResetEvent Received = new(false);
			ManualResetEvent Done = new(false);

			this.client2.OnPresenceSubscribe += (Sender, e) =>
			{
				Received.Set();
				e.Accept();
				return Task.CompletedTask;
			};

			this.client1.OnPresenceSubscribed += (Sender, e) => { Done.Set(); return Task.CompletedTask; };

			this.client1.RequestPresenceSubscription(this.client2.BareJID);

			Assert.IsTrue(Received.WaitOne(10000), "Presence subscription not received.");
			Assert.IsTrue(Done.WaitOne(10000), "Presence subscription failed.");
		}

		[TestMethod]
		public void Roster_Test_06_AcceptPresenceUnsubscription()
		{
			this.ConnectClients();
			ManualResetEvent Done = new(false);

			this.client2.OnPresenceUnsubscribe += (Sender, e) => { e.Accept(); return Task.CompletedTask; };
			this.client1.OnPresenceUnsubscribed += (Sender, e) => { Done.Set(); return Task.CompletedTask; };

			this.client1.OnPresenceSubscribe += (Sender, e) => { e.Decline(); return Task.CompletedTask; };
			this.client2.OnPresenceSubscribe += (Sender, e) => { e.Decline(); return Task.CompletedTask; };

			this.client1.RequestPresenceUnsubscription(this.client2.BareJID);

			Assert.IsTrue(Done.WaitOne(10000), "Presence unsubscription failed.");
		}

		[TestMethod]
		public void Roster_Test_07_FederatedSubscriptionRequest()
		{
			this.ConnectClients();
			ManualResetEvent Done = new(false);

			this.client1.OnPresenceSubscribed += (Sender, e) => { Done.Set(); return Task.CompletedTask; };
			this.client1.RequestPresenceSubscription("wpfclient@cybercity.online");

			Assert.IsTrue(Done.WaitOne(10000), "Presence subscription failed.");
		}

	}
}
