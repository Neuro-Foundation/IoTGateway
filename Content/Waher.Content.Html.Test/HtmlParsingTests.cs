﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Html.Elements;
using Waher.Content.Xml;
using Waher.Runtime.Console;
using Waher.Runtime.Inventory;

namespace Waher.Content.Html.Test
{
	[TestClass]
	public class HtmlParsingTests
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext _)
		{
			Types.Initialize(typeof(InternetContent).Assembly,
				typeof(HtmlDocument).Assembly);
		}

		private static async Task LoadAndParse(string Url)
		{
			using HttpClient Client = new();
			Client.Timeout = TimeSpan.FromMilliseconds(30000);
			Client.DefaultRequestHeaders.ExpectContinue = false;
			Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.61 Safari/537.36 Edg/94.0.992.31");

			HttpResponseMessage Response = await Client.GetAsync(Url);
			if (!Response.IsSuccessStatusCode)
				await Getters.WebGetter.ProcessResponse(Response, new Uri(Url));

			byte[] Data = await Response.Content.ReadAsByteArrayAsync();
			string ContentType = Response.Content.Headers.ContentType.ToString();

			HtmlDocument Doc = await InternetContent.DecodeAsync(ContentType, Data, new Uri(Url)) as HtmlDocument;
			Assert.IsNotNull(Doc);

			Assert.IsNotNull(Doc.Root);
			Assert.IsNotNull(Doc.Html);
			Assert.IsNotNull(Doc.Head);
			Assert.IsNotNull(Doc.Body);
			Assert.IsNotNull(Doc.Title);

			List<HtmlNode> Todo = new()
			{
				Doc.Root
			};

			string s;
			HtmlNode N;
			int i = 0;
			int Last = -1;

			while (i < Todo.Count)
			{
				N = Todo[i++];

				if (Last >= 0)
					s = "\r\n\r\n" + Doc.HtmlText[(Last + 1)..];
				else
					s = string.Empty;

				Assert.IsTrue(N.StartPosition > Last, "Start position not set properly. Start=" + N.StartPosition.ToString() + ", Last=" + Last.ToString() + s);
				Assert.IsTrue(N.EndPosition >= N.StartPosition, "End position not set.\r\n\r\n" + Doc.HtmlText[N.StartPosition..]);
				Assert.IsTrue(!string.IsNullOrEmpty(N.OuterHtml), "OuterHTML not set properly.\r\n\r\n" + Doc.HtmlText[N.StartPosition..]);

				if (N is HtmlElement E)
				{
					Last = E.EndPositionOfStartTag;

					if (E.HasChildren)
						Todo.InsertRange(i, E.Children);

					Assert.IsTrue(E.InnerHtml is not null, "InnerHTML not set properly.\r\n\r\n" + Doc.HtmlText[N.StartPosition..]);
				}
				else
					Last = N.EndPosition;
			}

			PageMetaData MetaData = Doc.GetMetaData();

			if (Doc.Meta is not null)
			{
				foreach (Meta Meta in Doc.Meta)
					ConsoleOut.WriteLine(Meta.OuterHtml);
			}

			XmlWriterSettings Settings = XML.WriterSettings(true, true);
			using XmlWriter Output = XmlWriter.Create(ConsoleOut.Writer, Settings);
			
			Doc.Export(Output);
			Output.Flush();
		}

		[TestMethod]
		public async Task HtmlParseTest_01_Google()
		{
			await LoadAndParse("http://google.com/");
		}

		[TestMethod]
		public async Task HtmlParseTest_02_Trocadero()
		{
			await LoadAndParse("http://www.kristianstadsbladet.se/tt-ekonomi/folkstorm-nar-trocadero-forsvinner/");
		}

		[TestMethod]
		public async Task HtmlParseTest_03_TheGuardian()
		{
			await LoadAndParse("https://www.theguardian.com/technology/2018/mar/04/has-dopamine-got-us-hooked-on-tech-facebook-apps-addiction");
		}

		[TestMethod]
		public async Task HtmlParseTest_04_Cnbc()
		{
			await LoadAndParse("https://www.cnbc.com/2021/09/10/epic-games-v-apple-judge-reaches-decision-.html");
		}

		[TestMethod]
		public async Task HtmlParseTest_05_Amgreatness()
		{
			await LoadAndParse("https://amgreatness.com/2021/09/24/over-3000-doctors-and-scientists-sign-declaration-accusing-covid-policy-makers-of-crimes-against-humanity/");
		}
	}
}
