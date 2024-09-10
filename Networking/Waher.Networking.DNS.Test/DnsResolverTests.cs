using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.DNS.Communication;
using Waher.Networking.DNS.Enumerations;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Console;
using Waher.Runtime.Inventory;

namespace Waher.Networking.DNS.Test
{
	[TestClass]
	public class DnsResolverTests
	{
		private static FilesProvider filesProvider = null;

		[AssemblyInitialize]
		public static async Task AssemblyInitialize(TestContext _)
		{
			Types.Initialize(
				typeof(Database).Assembly,
				typeof(FilesProvider).Assembly,
				typeof(ObjectSerializer).Assembly,
				typeof(DnsResolver).Assembly);

			filesProvider = await FilesProvider.CreateAsync("Data", "Default", 8192, 10000, 8192, Encoding.UTF8, 10000, true);
			Database.Register(filesProvider);
		}

		[AssemblyCleanup]
		public static void AssemblyCleanup()
		{
			filesProvider?.Dispose();
			filesProvider = null;
		}

		[TestMethod]
		public void Test_01_DNS_Server_Addresses()
		{
			foreach (IPAddress Address in DnsResolver.DnsServerAddresses)
				ConsoleOut.WriteLine(Address.ToString());
		}

		[TestMethod]
		public async Task Test_02_Resolve_IPv4()
		{
			IPAddress[] Addresses = await DnsResolver.LookupIP4Addresses("google.com");
			foreach (IPAddress Address in Addresses)
				ConsoleOut.WriteLine(Address);
		}

		[TestMethod]
		public async Task Test_03_Resolve_IPv6()
		{
			IPAddress[] Addresses = await DnsResolver.LookupIP6Addresses("google.com");
			foreach (IPAddress Address in Addresses)
				ConsoleOut.WriteLine(Address);
		}

		[TestMethod]
		[ExpectedException(typeof(GenericException))]
		public async Task Test_04_NonexistantName()
		{
			IPAddress[] Addresses = await DnsResolver.LookupIP4Addresses("dettanamnfinnsinte.se");
			foreach (IPAddress Address in Addresses)
				ConsoleOut.WriteLine(Address);
		}

		[TestMethod]
		public async Task Test_05_Resolve_Mail_Exchange()
		{
			await TestExchange("hotmail.com");
		}

		private static async Task TestExchange(string Domain)
		{
			string[] ExchangeHosts = await DnsResolver.LookupMailExchange(Domain);
			foreach (string ExchangeHost in ExchangeHosts)
			{
				ConsoleOut.WriteLine(ExchangeHost);

				IPAddress[] Addresses = await DnsResolver.LookupIP4Addresses(ExchangeHost);
				foreach (IPAddress Address in Addresses)
					ConsoleOut.WriteLine(Address);
			}
		}

		[TestMethod]
		public async Task Test_06_Resolve_Mail_Exchange_2()
		{
			await TestExchange("gmail.com");
		}

		[TestMethod]
		public async Task Test_07_Resolve_Reverse_IP4_Lookup()
		{
			string[] DomainNames = await DnsResolver.LookupDomainName(IPAddress.Parse("172.217.21.174"));
			foreach (string DomainName in DomainNames)
				ConsoleOut.WriteLine(DomainName);
		}

		[TestMethod]
		public async Task Test_08_Resolve_Reverse_IP6_Lookup()
		{
			string[] DomainNames = await DnsResolver.LookupDomainName(IPAddress.Parse("2a00:1450:400f:80a::200e"));
			foreach (string DomainName in DomainNames)
				ConsoleOut.WriteLine(DomainName);
		}

		[TestMethod]
		public async Task Test_09_Resolve_Service_Endpoint()
		{
			SRV Endpoint = await DnsResolver.LookupServiceEndpoint("jabber.org", "xmpp-client", "tcp");
			ConsoleOut.WriteLine(Endpoint.ToString());
		}

		[TestMethod]
		public async Task Test_10_Resolve_Service_Endpoint_2()
		{
			SRV Endpoint = await DnsResolver.LookupServiceEndpoint("cibernotar.io", "xmpp-client", "tcp");
			ConsoleOut.WriteLine(Endpoint.ToString());
		}

		[TestMethod]
		public async Task Test_11_Resolve_Service_Endpoint_3()
		{
			SRV Endpoint = await DnsResolver.LookupServiceEndpoint("cibernotar.io", "xmpp-server", "tcp");
			ConsoleOut.WriteLine(Endpoint.ToString());
		}

		[TestMethod]
		public async Task Test_12_Resolve_Service_Endpoints()
		{
			SRV[] Endpoints = await DnsResolver.LookupServiceEndpoints("jabber.org", "xmpp-client", "tcp");
			foreach (SRV SRV in Endpoints)
				ConsoleOut.WriteLine(SRV.ToString());
		}

		[TestMethod]
		public async Task Test_13_International_Domain_Names()
		{
			IPAddress[] Addresses = await DnsResolver.LookupIP4Addresses("b�cher.com");
			foreach (IPAddress Address in Addresses)
				ConsoleOut.WriteLine(Address);
		}

		[TestMethod]
		public async Task Test_14_Resolve_Mail_Exchange_3()
		{
			await TestExchange("waher.se");
		}

		[TestMethod]
		public async Task Test_15_Resolve_Mail_Exchange_4()
		{
			await TestExchange("cybercity.online");
		}

		[TestMethod]
		[Ignore]
		public async Task Test_16_Resolve_Mail_Exchange_5()
		{
			await TestExchange("littlesister.se");
		}

		[TestMethod]
		[Ignore]
		public async Task Test_17_Resolve_DNSBL_Lookup_OK_IP()
		{
			string[] Reasons = await DnsResolver.LookupBlackList(IPAddress.Parse("194.9.95.112"), "zen.spamhaus.org");
			Assert.IsNull(Reasons);
		}

		[TestMethod]
		[Ignore]
		public async Task Test_18_Resolve_DNSBL_Lookup_Spam_IP()
		{
			string[] Reasons = await DnsResolver.LookupBlackList(IPAddress.Parse("179.49.7.95"), "zen.spamhaus.org");
			Assert.IsNotNull(Reasons);
			foreach (string Reason in Reasons)
				ConsoleOut.WriteLine(Reason);
		}

		[TestMethod]
		public async Task Test_19_Resolve_Reverse_IP4_Lookup()
		{
			string[] DomainNames = await DnsResolver.LookupDomainName(IPAddress.Parse("90.224.165.60"));
			foreach (string DomainName in DomainNames)
				ConsoleOut.WriteLine(DomainName);
		}

		[TestMethod]
		public async Task Test_20_Resolve_TXT()
		{
			string[] Text = await DnsResolver.LookupText("hotmail.com");
			foreach (string Row in Text)
				ConsoleOut.WriteLine(Row);
		}

		[TestMethod]
		public async Task Test_21_Resolve_TXT_2()
		{
			string[] Text = await DnsResolver.LookupText("waher.se");
			foreach (string Row in Text)
				ConsoleOut.WriteLine(Row);
		}

		[TestMethod]
		public async Task Test_22_Resolve_TXT_3()
		{
			string[] Text = await DnsResolver.LookupText("cybercity.online");
			foreach (string Row in Text)
				ConsoleOut.WriteLine(Row);
		}

		[TestMethod]
		public async Task Test_23_Query()
		{
			DnsResponse Response;

			Response = await DnsResolver.Query("lab.tagroot.io", QTYPE.A, QCLASS.IN);
			ConsoleOut.WriteLine(Response.ToString());

			Response = await DnsResolver.Query("lab.tagroot.io", QTYPE.MX, QCLASS.IN);
			ConsoleOut.WriteLine(Response.ToString());

			Response = await DnsResolver.Query("lab.tagroot.io", QTYPE.TXT, QCLASS.IN);
			ConsoleOut.WriteLine(Response.ToString());

			Response = await DnsResolver.Query("_xmpp-client._tcp.lab.tagroot.io", QTYPE.SRV, QCLASS.IN);
			ConsoleOut.WriteLine(Response.ToString());

			Response = await DnsResolver.Query("_xmpp-server._tcp.lab.tagroot.io", QTYPE.SRV, QCLASS.IN);
			ConsoleOut.WriteLine(Response.ToString());
		}
	}
}
