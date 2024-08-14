﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.HTTP;
using Waher.Networking.XMPP.HTTPX;
using Waher.Runtime.Cache;

namespace Waher.Networking.XMPP.Test.E2eTests.HttpxTests
{
	internal class PostBack : HttpSynchronousResource, IPostResource, IHttpPostMethod
	{
		private Cache<string, KeyValuePair<PostBackEventHandler, object>> queries = null;
		private readonly object synchObj = new();
		private readonly RandomNumberGenerator rnd = RandomNumberGenerator.Create();

		public PostBack()
			: base("/PostBack")
		{
		}

		public override bool HandlesSubPaths => true;
		public override bool UserSessions => false;
		public bool AllowsPOST => true;

		public string GetUrl(PostBackEventHandler Callback, object State)
		{
			byte[] Bin = new byte[32];
			string Key;

			lock (this.synchObj)
			{
				this.rnd.GetBytes(Bin);
				Key = Base64Url.Encode(Bin);

				if (this.queries is null)
				{
					this.queries = new Cache<string, KeyValuePair<PostBackEventHandler, object>>(int.MaxValue, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
					this.queries.Removed += this.Queries_Removed;
				}

				this.queries[Key] = new KeyValuePair<PostBackEventHandler, object>(Callback, State);
				this.queries[string.Empty] = new KeyValuePair<PostBackEventHandler, object>(null, null);    // Keep cache active to avoid multiple recreation when a series of requests occur in sequence.
			}

			return "http://localhost:8081" + this.ResourceName + "/" + Key;
		}

		private void Queries_Removed(object Sender, CacheItemEventArgs<string, KeyValuePair<PostBackEventHandler, object>> e)
		{
			lock (this.synchObj)
			{
				if (this.queries is not null && this.queries.Count == 0)
				{
					this.queries.Dispose();
					this.queries = null;
				}
			}
		}

		public Task POST(HttpRequest Request, HttpResponse Response)
		{
			if (!Request.HasData)
				throw new BadRequestException("Missing data.");

			string ContentType = Request.Header.ContentType?.Value;
			if (string.IsNullOrEmpty(ContentType) || Array.IndexOf(Content.Binary.BinaryCodec.BinaryContentTypes, ContentType) < 0)
				throw new BadRequestException("Expected Binary data.");

			string From = Request.Header.From?.Value;
			if (string.IsNullOrEmpty(From))
				throw new BadRequestException("No From header.");

			string To = Request.Header["Origin"];
			if (string.IsNullOrEmpty(To))
				throw new BadRequestException("No Origin header.");

			string Referer = Request.Header.Referer?.Value;
			string EndpointReference;
			string SymmetricCipherReference;
			int i;

			if (!string.IsNullOrEmpty(Referer) && (i = Referer.IndexOf(':')) >= 0)
			{
				EndpointReference = Referer[..i];
				SymmetricCipherReference = Referer[(i + 1)..];
			}
			else
			{
				EndpointReference = string.Empty;
				SymmetricCipherReference = string.Empty;
			}

			string Key = Request.SubPath;
			if (string.IsNullOrEmpty(Key))
				throw new BadRequestException("No sub-path provided.");

			Key = Key[1..];

			KeyValuePair<PostBackEventHandler, object> Rec;

			lock (this.synchObj)
			{
				if (this.queries is null || !this.queries.TryGetValue(Key, out Rec))
					throw new NotFoundException("Resource sub-key not found.");
			}

			Request.DataStream.Position = 0;

			return Rec.Key.Invoke(this, new PostBackEventArgs(Request.DataStream, Rec.Value, From, To, EndpointReference, SymmetricCipherReference));
		}
	}
}
