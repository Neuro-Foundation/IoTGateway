﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP.P2P.SOCKS5;
using Waher.Runtime.Console;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppSocks5Tests : CommunicationTests
	{
		[TestMethod]
		public void Socks5_Test_01_FindProxy()
		{
			this.ConnectClients();
			ManualResetEvent Done = new(false);
			Socks5Proxy Proxy = new(this.client1);
			
			Proxy.StartSearch((sender, e) =>
			{
				Done.Set();
			});

			Assert.IsTrue(Done.WaitOne(30000), "Search not complete.");
			Assert.IsTrue(Proxy.HasProxy, "No SOCKS5 proxy found.");

			ConsoleOut.WriteLine("JID: " + Proxy.JID);
			ConsoleOut.WriteLine("Port: " + Proxy.Port.ToString());
			ConsoleOut.WriteLine("Host: " + Proxy.Host);
		}

		[TestMethod]
		public void Socks5_Test_02_ConnectSOCKS5()
		{
			this.ConnectClients();
			ManualResetEvent Error = new(false);
			ManualResetEvent Done = new(false);
			Socks5Client Client = new("waher.se", 1080, "socks5.waher.se",
				new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount, LineEnding.NewLine));

			Client.OnStateChange += (sender, e) =>
			{
				switch (Client.State)
				{
					case Socks5State.Authenticated:
						Done.Set();
						break;

					case Socks5State.Error:
					case Socks5State.Offline:
						Error.Set();
						break;
				}

				return Task.CompletedTask;
			};

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Unable to connect.");
		}

		[TestMethod]
		public void Socks5_Test_03_ConnectStream()
		{
			this.ConnectClients();
			ManualResetEvent Error = new(false);
			ManualResetEvent Done = new(false);
			Socks5Client Client = new("waher.se", 1080, "socks5.waher.se",
				new ConsoleOutSniffer(BinaryPresentationMethod.Hexadecimal, LineEnding.NewLine));

			Client.OnStateChange += (sender, e) =>
			{
				switch (Client.State)
				{
					case Socks5State.Authenticated:
						Client.CONNECT("Stream0001", this.client1.FullJID, this.client2.FullJID);
						break;

					case Socks5State.Connected:
						Done.Set();
						break;

					case Socks5State.Error:
					case Socks5State.Offline:
						Error.Set();
						break;
				}

				return Task.CompletedTask;
			};

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Unable to connect.");
		}

		[TestMethod]
		public void Socks5_Test_04_ActivateStream()
		{
			this.ConnectClients();
			ManualResetEvent Error1 = new(false);
			ManualResetEvent Done1 = new(false);
			Socks5Client Client1 = new("waher.se", 1080, "socks5.waher.se",
				new ConsoleOutSniffer(BinaryPresentationMethod.Hexadecimal, LineEnding.NewLine));

			Client1.OnStateChange += (sender, e) =>
			{
				switch (Client1.State)
				{
					case Socks5State.Authenticated:
						Client1.CONNECT("Stream0001", this.client1.FullJID, this.client2.FullJID);
						break;

					case Socks5State.Connected:
						Done1.Set();
						break;

					case Socks5State.Error:
					case Socks5State.Offline:
						Error1.Set();
						break;
				}

				return Task.CompletedTask;
			};

			ManualResetEvent Error2 = new(false);
			ManualResetEvent Done2 = new(false);
			Socks5Client Client2 = new("waher.se", 1080, "socks5.waher.se",
				new ConsoleOutSniffer(BinaryPresentationMethod.Hexadecimal, LineEnding.NewLine));

			Client2.OnStateChange += (sender, e) =>
			{
				switch (Client2.State)
				{
					case Socks5State.Authenticated:
						Client2.CONNECT("Stream0001", this.client1.FullJID, this.client2.FullJID);
						break;

					case Socks5State.Connected:
						Done2.Set();
						break;

					case Socks5State.Error:
					case Socks5State.Offline:
						Error2.Set();
						break;
				}

				return Task.CompletedTask;
			};

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done1, Error1 }, 10000), "Unable to connect.");
			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done2, Error2 }, 10000), "Unable to connect.");

			ManualResetEvent Done = new(false);
			ManualResetEvent Error = new(false);

			Done1.Reset();
			Error1.Reset();

			Done2.Reset();
			Error2.Reset();

			Client1.OnDataReceived += (sender, e) =>
			{
				if (Encoding.UTF8.GetString(e.Buffer, e.Offset, e.Count) == "Hello2")
					Done1.Set();
				else
					Error1.Set();

				return Task.CompletedTask;
			};

			Client2.OnDataReceived += (sender, e) =>
			{
				if (Encoding.UTF8.GetString(e.Buffer, e.Offset, e.Count) == "Hello1")
					Done2.Set();
				else
					Error2.Set();

				return Task.CompletedTask;
			};

			this.client1.SendIqSet("socks5.waher.se", "<query xmlns='http://jabber.org/protocol/bytestreams' sid='Stream0001'>" +
				"<activate>" + this.client2.FullJID + "</activate></query>", (sender, e) =>
			{
				if (e.Ok)
				{
					Client1.Send(Encoding.UTF8.GetBytes("Hello1"));
					Client2.Send(Encoding.UTF8.GetBytes("Hello2"));

					Done.Set();
				}
				else
					Error.Set();

				return Task.CompletedTask;

			}, null);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Unable to activate stream.");
			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done1, Error1 }, 10000), "Did not receive message.");
			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done2, Error2 }, 10000), "Did not receive message.");
		}

		[TestMethod]
		public void Socks5_Test_05_InitiateSession()
		{
			this.ConnectClients();

			ConsoleOut.WriteLine();
			ConsoleOut.WriteLine("Searching for SOCKS5 proxy.");
			ConsoleOut.WriteLine();

			ManualResetEvent Done1 = new(false);
			Socks5Proxy Proxy1 = new(this.client1);

			Proxy1.StartSearch((sender, e) =>
			{
				Done1.Set();
			});

			ManualResetEvent Done2 = new(false);
			Socks5Proxy Proxy2 = new(this.client2);

			Proxy2.StartSearch((sender, e) =>
			{
				Done2.Set();
			});

			Assert.IsTrue(Done1.WaitOne(30000), "Search not complete.");
			Assert.IsTrue(Proxy1.HasProxy, "No SOCKS5 proxy found.");

			Assert.IsTrue(Done2.WaitOne(30000), "Search not complete.");
			Assert.IsTrue(Proxy2.HasProxy, "No SOCKS5 proxy found.");

			Done1.Reset();
			Done2.Reset();

			ManualResetEvent Error1 = new(false);
			ManualResetEvent Error2 = new(false);
			ManualResetEvent Closed1 = new(false);
			ManualResetEvent Closed2 = new(false);

			ConsoleOut.WriteLine();
			ConsoleOut.WriteLine("Start of session initiation.");
			ConsoleOut.WriteLine();

			Proxy2.OnOpen += (sender, e) =>
			{
				e.AcceptStream((sender2, e2) =>
				{
					if (Encoding.ASCII.GetString(e2.Buffer, e2.Offset, e2.Count) == "Hello1")
					{
						Done2.Set();
						e2.Stream.Send(Encoding.ASCII.GetBytes("Hello2"));
						e2.Stream.CloseWhenDone();
					}
					else
						Error2.Set();

					return Task.CompletedTask;

				}, (sender2, e2) =>
				{
					Closed2.Set();
					return Task.CompletedTask;
				}, null);

				return Task.CompletedTask;
			};

			Proxy1.InitiateSession(this.client2.FullJID, (sender, e) =>
			{
				if (e.Ok)
				{
					e.Stream.OnDataReceived += (sender2, e2) =>
					{
						if (Encoding.ASCII.GetString(e2.Buffer, e2.Offset, e2.Count) == "Hello2")
							Done1.Set();
						else
							Error1.Set();

						return Task.CompletedTask;
					};

					e.Stream.OnStateChange += (sender2, e2) =>
					{
						if (e.Stream.State == Socks5State.Offline)
							Closed1.Set();

						return Task.CompletedTask;
					};

					e.Stream.Send(Encoding.ASCII.GetBytes("Hello1"));
				}
				return Task.CompletedTask;
			}, null);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done1, Error1 }, 10000), "Did not receive message 1.");
			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done2, Error2 }, 10000), "Did not receive message 2.");
			Assert.IsTrue(Closed1.WaitOne(10000), "Client 1 did not close properly.");
			Assert.IsTrue(Closed2.WaitOne(10000), "Client 2 did not close properly.");
		}

	}
}
