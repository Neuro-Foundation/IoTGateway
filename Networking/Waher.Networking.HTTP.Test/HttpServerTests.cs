﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Text;
using Waher.Networking.HTTP.Authentication;

namespace Waher.Networking.HTTP.Test
{
	public abstract class HttpServerTests : HttpServerTestsBase
	{
		public abstract Version ProtocolVersion { get; }

		[TestMethod]
		public async Task Test_01_GET_HTTP_ContentLength()
		{
			server.Register("/test01.txt", (req, resp) => resp.Return("hej på dej"));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/test01.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[DataTestMethod]
		[DataRow(100000)]
		[DataRow(1000000)]
		[DataRow(10000000)]
		[DataRow(100000000)]
		[DataRow(1000000000)]
		public async Task Test_02_GET_HTTP_Chunked(int TotalSize)
		{
			HttpResource Resource = server.Register("/test02.txt", async (req, resp) =>
			{
				int i = 0;
				int j;

				resp.ContentType = PlainTextCodec.DefaultContentType;
				while (i < TotalSize)
				{
					j = Math.Min(2000, TotalSize - i);
					await resp.Write(new string('a', j));
					i += j;
				}
			});

			try
			{
				using CookieWebClient Client = new(this.ProtocolVersion);
				byte[] Data = await Client.DownloadData("http://localhost:8081/test02.txt");
				string s = Encoding.UTF8.GetString(Data);
				Assert.AreEqual(TotalSize, s.Length);
				Assert.AreEqual(new string('a', TotalSize), s);
			}
			finally
			{
				server.Unregister(Resource);
			}
		}

		[TestMethod]
		public async Task Test_03_GET_HTTP_Encoding()
		{
			server.Register("/test03.png", async (req, resp) =>
			{
				await resp.Return(new SKBitmap(320, 200));
			});

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/test03.png");
			SKBitmap Bmp = SKBitmap.Decode(Data);
			Assert.AreEqual(320, Bmp.Width);
			Assert.AreEqual(200, Bmp.Height);
		}

		[TestMethod]
		public async Task Test_04_GET_HTTPS()
		{
			server.Register("/test04.txt", (req, resp) => resp.Return("hej på dej"));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("https://localhost:8088/test04.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_05_Authentication_Basic()
		{
			server.Register("/test05.txt", (req, resp) => resp.Return("hej på dej"), new BasicAuthentication("Test05", this));

			using CookieWebClient Client = new(this.ProtocolVersion);
			Client.Credentials = new NetworkCredential("User", "Password");
			byte[] Data = await Client.DownloadData("http://localhost:8081/test05.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_06_Authentication_Digest()
		{
			server.Register("/test06.txt", (req, resp) => resp.Return("hej på dej"), new DigestAuthentication("Test06", this));

			using CookieWebClient Client = new(this.ProtocolVersion);
			Client.Credentials = new NetworkCredential("User", "Password");
			byte[] Data = await Client.DownloadData("http://localhost:8081/test06.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_07_EmbeddedResource()
		{
			server.Register(new HttpEmbeddedResource("/test07.png", "Waher.Networking.HTTP.Test.Data.Frog-300px.png", typeof(HttpServerTests).Assembly));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/test07.png");
			SKBitmap Bmp = SKBitmap.Decode(Data);
			Assert.AreEqual(300, Bmp.Width);
			Assert.AreEqual(184, Bmp.Height);
		}

		[TestMethod]
		public async Task Test_08_FolderResource_GET()
		{
			server.Register(new HttpFolderResource("/Test08", "Data", false, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Test08/BarnSwallowIsolated-300px.png");
			SKBitmap Bmp = SKBitmap.Decode(Data);
			Assert.AreEqual(300, Bmp.Width);
			Assert.AreEqual(264, Bmp.Height);
		}

		[DataTestMethod]
		[DataRow(10 * 1024)]            // 10 kB	(k=1024)
		[DataRow(100 * 1024)]           // 100 kB	(k=1024)
		[DataRow(1024 * 1024)]          // 1 MB		(k=1024)
		[DataRow(10 * 1024 * 1024)]     // 10 MB	(k=1024)
		[DataRow(100 * 1024 * 1024)]    // 100 MB	(k=1024)
		[DataRow(500 * 1024 * 1024)]	// 500 MB	(k=1024)
		[DataRow(10 * 1000)]            // 10 kB	(k=1000)
		[DataRow(100 * 1000)]           // 100 kB	(k=1000)
		[DataRow(1024 * 1000)]          // 1 MB		(k=1000)
		[DataRow(10 * 1000 * 1000)]     // 10 MB	(k=1000)
		[DataRow(100 * 1000 * 1000)]    // 100 MB	(k=1000)
		[DataRow(500 * 1000 * 1000)]	// 500 MB	(k=1000)
		public async Task Test_09_FolderResource_PUT_File(int FileSize)
		{
			if (!server.TryGetResource("/Test09", false, out _, out _))
				server.Register(new HttpFolderResource("/Test09", "Data", true, false, true, false));

			StringBuilder sb = new();
			int i = 0;

			while (sb.Length < FileSize)
			{
				if (i > 0)
					sb.Append('_');

				sb.Append(i);
				i++;
			}

			if (sb.Length > FileSize)
				sb.Length = FileSize;

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			string s1 = sb.ToString();
			string FileName = "string_" + FileSize.ToString() + ".txt";
			byte[] Data0 = Utf8.GetBytes(s1);
			await Client.UploadData("http://localhost:8081/Test09/" + FileName, HttpMethod.Put, Data0);

			byte[] Data = await Client.DownloadData("http://localhost:8081/Test09/" + FileName);
			string s2 = Utf8.GetString(Data);

			string r1 = ToRows(s1);
			string r2 = ToRows(s2);

			File.WriteAllText("Data\\s1_" + FileSize.ToString() + ".txt", r1);
			File.WriteAllText("Data\\s2_" + FileSize.ToString() + ".txt", r2);

			if (s1 != s2)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("First diff");
				Console.Out.WriteLine("===============");

				string[] Rows1 = r1.Split("\r\n", StringSplitOptions.None);
				string[] Rows2 = r2.Split("\r\n", StringSplitOptions.None);
				int c = Math.Min(Rows1.Length, Rows2.Length);

				for (i = 0; i < c; i++)
				{
					if (Rows1[i] != Rows2[i])
					{
						Console.Out.WriteLine("orignal:");
						Console.Out.WriteLine(Rows1[i]);
						Console.Out.WriteLine("vs:");
						Console.Out.WriteLine(Rows2[i]);
						break;
					}
				}

				Assert.AreEqual(s1.Length, s2.Length);
				Assert.AreEqual(Data0.Length, Data.Length);
				Assert.AreEqual(s1, s2);
				Assert.AreEqual(Convert.ToBase64String(Data0), Convert.ToBase64String(Data));
			}
		}

		private static string ToRows(string s)
		{
			StringBuilder sb = new();
			int i = 0, c = s.Length;
			int j;

			while (i < c)
			{
				j = Math.Min(c - i, 76);
				sb.AppendLine(s.Substring(i, j));
				i += j;
			}

			return sb.ToString();
		}

		[TestMethod]
		[ExpectedException(typeof(HttpRequestException))]
		public async Task Test_10_FolderResource_PUT_File_NotAllowed()
		{
			server.Register(new HttpFolderResource("/Test10", "Data", false, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			byte[] Data = await Client.UploadData("http://localhost:8081/Test10/string.txt", HttpMethod.Put, Utf8.GetBytes(new string('Ω', 100000)));
		}

		[TestMethod]
		public async Task Test_11_FolderResource_DELETE_File()
		{
			server.Register(new HttpFolderResource("/Test11", "Data", true, true, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			await Client.UploadData("http://localhost:8081/Test11/string.txt", HttpMethod.Put, Utf8.GetBytes(new string('Ω', 100000)));

			await Client.UploadData("http://localhost:8081/Test11/string.txt", HttpMethod.Delete, []);
		}

		[TestMethod]
		[ExpectedException(typeof(HttpRequestException))]
		public async Task Test_12_FolderResource_DELETE_File_NotAllowed()
		{
			server.Register(new HttpFolderResource("/Test12", "Data", true, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			await Client.UploadData("http://localhost:8081/Test12/string.txt", HttpMethod.Put, Utf8.GetBytes(new string('Ω', 100000)));

			await Client.UploadData("http://localhost:8081/Test12/string.txt", HttpMethod.Delete, []);
		}

		[TestMethod]
		public async Task Test_13_FolderResource_PUT_CreateFolder()
		{
			server.Register(new HttpFolderResource("/Test13", "Data", true, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			string s1 = new('Ω', 100000);
			await Client.UploadData("http://localhost:8081/Test13/Folder/string.txt", HttpMethod.Put, Utf8.GetBytes(s1));

			byte[] Data = await Client.DownloadData("http://localhost:8081/Test13/Folder/string.txt");
			string s2 = Utf8.GetString(Data);

			Assert.AreEqual(s1, s2);
		}

		[TestMethod]
		public async Task Test_14_FolderResource_DELETE_Folder()
		{
			server.Register(new HttpFolderResource("/Test14", "Data", true, true, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			await Client.UploadData("http://localhost:8081/Test14/Folder/string.txt", HttpMethod.Put, Utf8.GetBytes(new string('Ω', 100000)));

			await Client.UploadData("http://localhost:8081/Test14/Folder", HttpMethod.Delete, []);
		}

		[TestMethod]
		public async Task Test_15_GET_Single_Closed_Range()
		{
			server.Register(new HttpFolderResource("/Test15", "Data", false, false, true, false));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Get, "http://localhost:8081/Test15/Text.txt");
			Request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(100, 119);

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			byte[] Data = await Response.Content.ReadAsByteArrayAsync();

			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("89012345678901234567", s);
		}

		[TestMethod]
		public async Task Test_16_GET_Single_Open_Range1()
		{
			server.Register(new HttpFolderResource("/Test16", "Data", false, false, true, false));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Get, "http://localhost:8081/Test16/Text.txt");
			Request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(980, null);

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			byte[] Data = await Response.Content.ReadAsByteArrayAsync();

			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("89012345678901234567890", s);
		}

		[TestMethod]
		public async Task Test_17_GET_Single_Open_Range2()
		{
			server.Register(new HttpFolderResource("/Test17", "Data", false, false, true, false));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Get, "http://localhost:8081/Test17/Text.txt");
			Request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(null, 20);

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			byte[] Data = await Response.Content.ReadAsByteArrayAsync();

			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("12345678901234567890", s);
		}

		[TestMethod]
		public async Task Test_18_GET_MultipleRanges()
		{
			server.Register(new HttpFolderResource("/Test18", "Data", false, false, true, false));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Get, "http://localhost:8081/Test18/Text.txt");
			Request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(100, 199);
			Request.Headers.Range.Ranges.Add(new System.Net.Http.Headers.RangeItemHeaderValue(null, 100));

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			byte[] Data = await Response.Content.ReadAsByteArrayAsync();
			string s = Encoding.UTF8.GetString(Data);
			string s2 = await Resources.ReadAllTextAsync("Data/MultiRangeResponse.txt");

			int i = s.IndexOf("--");
			int j = s.IndexOf("\r\n", i);
			string Boundary = s.Substring(i + 2, j - i - 2);

			Assert.AreEqual(s2, s.Replace(Boundary, "463d71b7a34048709e1bb217940feea6"));
		}

		[TestMethod]
		public async Task Test_19_PUT_Range()
		{
			server.Register(new HttpFolderResource("/Test19", "Data", true, false, true, false));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Put, "http://localhost:8081/Test19/String2.txt");

			byte[] Data = new byte[20];
			int i;

			for (i = 0; i < 20; i++)
				Data[i] = (byte)'1';

			Request.Content = new ByteArrayContent(Data);
			Request.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(20, 39, 40);

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			for (i = 0; i < 20; i++)
				Data[i] = (byte)'2';

			using HttpRequestMessage Request2 = new(HttpMethod.Put, "http://localhost:8081/Test19/String2.txt");

			Request2.Content = new ByteArrayContent(Data);
			Request2.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, 19, 40);

			using HttpResponseMessage Response2 = await Client.SendAsync(Request2);
			Response2.EnsureSuccessStatusCode();

			using HttpRequestMessage Request3 = new(HttpMethod.Get, "http://localhost:8081/Test19/String2.txt");

			using HttpResponseMessage Response3 = await Client.SendAsync(Request3);
			Response3.EnsureSuccessStatusCode();

			Data = await Response3.Content.ReadAsByteArrayAsync();

			string s = Encoding.ASCII.GetString(Data);

			Assert.AreEqual("2222222222222222222211111111111111111111", s);
		}

		[TestMethod]
		public async Task Test_20_PATCH_Range()
		{
			server.Register(new HttpFolderResource("/Test20", "Data", true, false, true, false));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Patch, "http://localhost:8081/Test20/String2.txt");

			byte[] Data = new byte[20];
			int i;

			for (i = 0; i < 20; i++)
				Data[i] = (byte)'1';

			Request.Content = new ByteArrayContent(Data);
			Request.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(20, 39, 40);

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			for (i = 0; i < 20; i++)
				Data[i] = (byte)'2';

			using HttpRequestMessage Request2 = new(HttpMethod.Patch, "http://localhost:8081/Test20/String2.txt");

			Request2.Content = new ByteArrayContent(Data);
			Request2.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, 19, 40);

			using HttpResponseMessage Response2 = await Client.SendAsync(Request2);
			Response2.EnsureSuccessStatusCode();

			using HttpRequestMessage Request3 = new(HttpMethod.Get, "http://localhost:8081/Test20/String2.txt");

			using HttpResponseMessage Response3 = await Client.SendAsync(Request3);
			Response3.EnsureSuccessStatusCode();

			Data = await Response3.Content.ReadAsByteArrayAsync();

			string s = Encoding.ASCII.GetString(Data);

			Assert.AreEqual("2222222222222222222211111111111111111111", s);
		}

		[TestMethod]
		public async Task Test_21_HEAD()
		{
			server.Register("/test21.png", async (req, resp) =>
			{
				await resp.Return(new SKBitmap(320, 200));
			});

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};

			using HttpRequestMessage Request = new(HttpMethod.Head, "http://localhost:8081/test21.png");

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			Assert.IsTrue(Response.Content.Headers.ContentLength > 0);

			byte[] Data = await Response.Content.ReadAsByteArrayAsync();

			Assert.AreEqual(0, Data.Length);
		}

		[TestMethod]
		public async Task Test_22_Cookies()
		{
			server.Register("/test22_1.txt", async (req, resp) =>
			{
				resp.SetCookie(new Cookie("word1", "hej", "localhost", "/"));
				resp.SetCookie(new Cookie("word2", "på", "localhost", "/"));
				resp.SetCookie(new Cookie("word3", "dej", "localhost", "/"));

				await resp.Return("hejsan");
			});

			server.Register("/test22_2.txt", async (req, resp) =>
			{
				await resp.Return(req.Header.Cookie["word1"] + " " + req.Header.Cookie["word2"] + " " + req.Header.Cookie["word3"]);
			});

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/test22_1.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hejsan", s);

			Data = await Client.DownloadData("http://localhost:8081/test22_2.txt");
			s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		[ExpectedException(typeof(HttpRequestException))]
		public async Task Test_23_Conditional_GET_IfModifiedSince_1()
		{
			DateTime LastModified = File.GetLastWriteTime("Data\\BarnSwallowIsolated-300px.png");

			server.Register(new HttpFolderResource("/Test23", "Data", false, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			Client.IfModifiedSince = LastModified.AddMinutes(1);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Test23/BarnSwallowIsolated-300px.png");
		}

		[TestMethod]
		public async Task Test_24_Conditional_GET_IfModifiedSince_2()
		{
			DateTime LastModified = File.GetLastWriteTime("Data\\BarnSwallowIsolated-300px.png");

			server.Register(new HttpFolderResource("/Test24", "Data", false, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			Client.IfModifiedSince = LastModified.AddMinutes(-1);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Test24/BarnSwallowIsolated-300px.png");
			SKBitmap Bmp = SKBitmap.Decode(Data);
			Assert.AreEqual(300, Bmp.Width);
			Assert.AreEqual(264, Bmp.Height);
		}

		[TestMethod]
		[ExpectedException(typeof(HttpRequestException))]
		public async Task Test_25_Conditional_PUT_IfUnmodifiedSince_1()
		{
			DateTime LastModified = File.GetLastWriteTime("Data\\Temp.txt");

			server.Register(new HttpFolderResource("/Test25", "Data", true, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			string s1 = new('Ω', 100000);
			Client.IfUnmodifiedSince = LastModified.AddMinutes(-1);
			await Client.UploadData("http://localhost:8081/Test25/Temp.txt", HttpMethod.Put, Utf8.GetBytes(s1));
		}

		[TestMethod]
		public async Task Test_26_Conditional_PUT_IfUnmodifiedSince_2()
		{
			DateTime LastModified = File.GetLastWriteTime("Data\\Temp.txt");

			server.Register(new HttpFolderResource("/Test26", "Data", true, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			UTF8Encoding Utf8 = new(true);
			string s1 = new('Ω', 100000);
			Client.IfUnmodifiedSince = LastModified.AddMinutes(1);
			await Client.UploadData("http://localhost:8081/Test26/Temp.txt", HttpMethod.Put, Utf8.GetBytes(s1));
		}

		[TestMethod]
		[ExpectedException(typeof(HttpRequestException))]
		public async Task Test_27_NotAcceptable()
		{
			server.Register(new HttpFolderResource("/Test27", "Data", false, false, true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			Client.Accept = "text/x-test4";
			byte[] Data = await Client.DownloadData("http://localhost:8081/Test27/Text.txt");
		}

		[TestMethod]
		public async Task Test_28_Content_Conversion()
		{
			HttpFolderResource Resource = new("/Test28", "Data", false, false, true, false);
			Resource.AllowTypeConversion(PlainTextCodec.DefaultContentType, "text/x-test1", "text/x-test2", "text/x-test3");

			server.Register(Resource);

			using CookieWebClient Client = new(this.ProtocolVersion);
			Client.Accept = "text/x-test3";
			byte[] Data = await Client.DownloadData("http://localhost:8081/Test28/Text.txt");
			MemoryStream ms = new(Data);
			StreamReader r = new(ms);
			string s = r.ReadToEnd();
			Assert.AreEqual("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\r\nConverter 1 was here.\r\nConverter 2 was here.\r\nConverter 3 was here.", s);
		}

		[TestMethod]
		public async Task Test_29_ReverseProxy()
		{
			server.Register("/Remote/test29.txt", (req, resp) => resp.Return("hej på dej"));
			server.Register(new HttpReverseProxyResource("/Proxy29", "localhost", 8081, "/Remote", false, TimeSpan.FromSeconds(10)));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Proxy29/test29.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_30_ReverseProxy_WithQuery()
		{
			server.Register("/Remote/test30.txt", async (req, resp) =>
			{
				if (!req.Header.TryGetQueryParameter("A", out string A))
					throw new BadRequestException();

				if (!req.Header.TryGetQueryParameter("B", out string B))
					throw new BadRequestException();

				if (!req.Header.TryGetQueryParameter("C", out string C))
					throw new BadRequestException();

				await resp.Return(WebUtility.UrlDecode(A) + " " + WebUtility.UrlDecode(B) + " " +
					WebUtility.UrlDecode(C));
			});
			server.Register(new HttpReverseProxyResource("/Proxy30", "localhost", 8081, "/Remote", false, TimeSpan.FromSeconds(10)));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Proxy30/test30.txt?A=" +
				WebUtility.UrlEncode("hej") + "&B=" + WebUtility.UrlEncode("på") + "&C=" +
				WebUtility.UrlEncode("dej"));
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_31_ReverseProxy_Cookies()
		{
			server.Register("/Remote/test31/SetA", null, async (req, resp) =>
			{
				req.Session["A"] = (await req.DecodeDataAsync()).Decoded;
			}, true, false, true);
			server.Register("/Remote/test31/SetB", null, async (req, resp) =>
			{
				req.Session["B"] = (await req.DecodeDataAsync()).Decoded;
			}, true, false, true);
			server.Register("/Remote/test31/SetC", null, async (req, resp) =>
			{
				req.Session["C"] = (await req.DecodeDataAsync()).Decoded;
			}, true, false, true);
			server.Register("/Remote/test31.txt", async (req, resp) =>
			{
				string A = req.Session["A"]?.ToString();
				string B = req.Session["B"]?.ToString();
				string C = req.Session["C"]?.ToString();

				await resp.Return(A + " " + B + " " + C);
			}, true, false, true);
			server.Register(new HttpReverseProxyResource("/Proxy31", "localhost", 8081, "/Remote", false, TimeSpan.FromSeconds(10)));

			using HttpClient Client = new()
			{
				DefaultRequestVersion = this.ProtocolVersion,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};
			using HttpRequestMessage Request = new(HttpMethod.Post, "http://localhost:8081/Proxy31/test31/SetA")
			{
				Content = new StringContent("hej")
			};

			using HttpResponseMessage Response = await Client.SendAsync(Request);
			Response.EnsureSuccessStatusCode();

			using HttpRequestMessage Request2 = new(HttpMethod.Post, "http://localhost:8081/Proxy31/test31/SetB")
			{
				Content = new StringContent("på")
			};

			using HttpResponseMessage Response2 = await Client.SendAsync(Request2);
			Response2.EnsureSuccessStatusCode();

			using HttpRequestMessage Request3 = new(HttpMethod.Post, "http://localhost:8081/Proxy31/test31/SetC")
			{
				Content = new StringContent("dej")
			};

			using HttpResponseMessage Response3 = await Client.SendAsync(Request3);
			Response3.EnsureSuccessStatusCode();

			using HttpRequestMessage Request4 = new(HttpMethod.Get, "http://localhost:8081/Proxy31/test31.txt");
			using HttpResponseMessage Response4 = await Client.SendAsync(Request4);
			Response4.EnsureSuccessStatusCode();

			byte[] Data = await Response4.Content.ReadAsByteArrayAsync();
			string s = Encoding.UTF8.GetString(Data);

			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_32_TemporaryRedirect()
		{
			server.Register("/New32/test.txt", (req, resp) => resp.Return("hej på dej"));
			server.Register(new HttpRedirectionResource("/Old32", "/New32", true, false));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Old32/test.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}

		[TestMethod]
		public async Task Test_33_PermanentRedirect()
		{
			server.Register("/New33/test.txt", (req, resp) => resp.Return("hej på dej"));
			server.Register(new HttpRedirectionResource("/Old33", "/New33", true, true));

			using CookieWebClient Client = new(this.ProtocolVersion);
			byte[] Data = await Client.DownloadData("http://localhost:8081/Old33/test.txt");
			string s = Encoding.UTF8.GetString(Data);
			Assert.AreEqual("hej på dej", s);
		}
	}
}
