﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.DNS;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;

namespace Waher.Security.SPF.Test
{
	[TestClass]
	public class SpfEvaluationTests
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
		public static async Task AssemblyCleanup()
		{
			if (filesProvider is not null)
			{
				await filesProvider.DisposeAsync();
				filesProvider = null;
			}
		}

		[TestMethod]
		public async Task Test_01_SPF_Evaluation_1()
		{
			KeyValuePair<SpfResult, string> Result = await SpfResolver.CheckHost(
				IPAddress.Parse("20.105.198.113"), "cybercity.online",
				"testaccount@cybercity.online", "smtp.outgoing.loopia.se", "cybercity.online");
			Assert.AreEqual(SpfResult.Pass, Result.Key, Result.Value);
		}

		[TestMethod]
		public async Task Test_02_SPF_Evaluation_2()
		{
			await TestSpfString("v=spf1 include:_spf.google.com ~all",
				"mobilgirot.com", "testaccount@mobilgirot.com",
				IPAddress.Parse("209.85.221.49"), "mail-wr1-f49.google.com",
                "cybercity.online");
		}

		[TestMethod]
		public async Task Test_03_SPF_Evaluation_3()
		{
			await TestSpfString("v=spf1 ptr:yahoo.com ~all",
				"att.net", "testaccount@att.net",
				IPAddress.Parse("74.6.128.85"), "sonic312-23.consmr.mail.bf2.yahoo.com",
                "cybercity.online");
		}

		private static async Task TestSpfString(string SpfString, string Domain,
			string Sender, IPAddress Address, string CallerHost, string Host)
		{
			KeyValuePair<SpfResult, string> Result = await SpfResolver.CheckHost(
				Address, Domain, Sender, CallerHost, Host, 
				new SpfExpression(Domain, false, SpfString));

			Assert.AreEqual(SpfResult.Pass, Result.Key, Result.Value);
		}
	}
}
