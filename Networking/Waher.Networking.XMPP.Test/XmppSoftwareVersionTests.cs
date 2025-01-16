﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Waher.Networking.XMPP.SoftwareVersion;
using Waher.Runtime.Console;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppSoftwareVersionTests : CommunicationTests
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
		public async Task SoftwareVersion_Test_01_Server()
		{
			await this.ConnectClients();
			SoftwareVersionEventArgs e = this.client1.SoftwareVersion(this.client1.Domain, 10000);
			Print(e);
		}

		private static void Print(SoftwareVersionEventArgs e)
		{
			ConsoleOut.WriteLine();
			ConsoleOut.WriteLine("Name: " + e.Name);
			ConsoleOut.WriteLine("Version: " + e.Version);
			ConsoleOut.WriteLine("OS: " + e.OS);
		}

		[TestMethod]
		public async Task SoftwareVersion_Test_02_Client()
		{
			await this.ConnectClients();
			SoftwareVersionEventArgs e = this.client1.SoftwareVersion(this.client1.FullJID, 10000);
			Print(e);
		}

	}
}
