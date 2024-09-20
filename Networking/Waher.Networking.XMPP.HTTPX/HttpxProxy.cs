﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking.HTTP;
using Waher.Networking.XMPP.P2P;
using Waher.Runtime.Temporary;

namespace Waher.Networking.XMPP.HTTPX
{
	/// <summary>
	/// Implements a Proxy resource that allows Web clients to fetch HTTP-based resources over HTTPX.
	/// </summary>
	public partial class HttpxProxy : HttpAsynchronousResource, IDisposable, IHttpGetMethod, IHttpGetRangesMethod,
		IHttpPostMethod, IHttpPostRangesMethod, IHttpPutMethod, IHttpPutRangesMethod, IHttpPatchMethod, IHttpPatchRangesMethod, 
		IHttpTraceMethod, IHttpDeleteMethod
	{
		private readonly XmppClient defaultXmppClient;
		private HttpxClient httpxClient;
		private XmppServerlessMessaging serverlessMessaging;
		private IHttpxCache httpxCache;
		private IPostResource postResource = null;
		private InBandBytestreams.IbbClient ibbClient = null;
		private P2P.SOCKS5.Socks5Proxy socks5Proxy = null;
		private bool disposed = false;

		/// <summary>
		/// Implements a Proxy resource that allows Web clients to fetch HTTP-based resources over HTTPX.
		/// </summary>
		/// <param name="ResourceName">Resource name of proxy resource.</param>
		/// <param name="DefaultXmppClient">Default XMPP client.</param>
		/// <param name="MaxChunkSize">Max Chunk Size to use.</param>
		public HttpxProxy(string ResourceName, XmppClient DefaultXmppClient, int MaxChunkSize)
			: this(ResourceName, DefaultXmppClient, MaxChunkSize, null, null)
		{
		}

		/// <summary>
		/// Implements a Proxy resource that allows Web clients to fetch HTTP-based resources over HTTPX.
		/// </summary>
		/// <param name="ResourceName">Resource name of proxy resource.</param>
		/// <param name="DefaultXmppClient">Default XMPP client.</param>
		/// <param name="MaxChunkSize">Max Chunk Size to use.</param>
		/// <param name="ServerlessMessaging">Serverless messaging manager.</param>
		public HttpxProxy(string ResourceName, XmppClient DefaultXmppClient, int MaxChunkSize, XmppServerlessMessaging ServerlessMessaging)
			: this(ResourceName, DefaultXmppClient, MaxChunkSize, ServerlessMessaging, null)
		{
		}

		/// <summary>
		/// Implements a Proxy resource that allows Web clients to fetch HTTP-based resources over HTTPX.
		/// </summary>
		/// <param name="ResourceName">Resource name of proxy resource.</param>
		/// <param name="DefaultXmppClient">Default XMPP client.</param>
		/// <param name="MaxChunkSize">Max Chunk Size to use.</param>
		/// <param name="ServerlessMessaging">Serverless messaging manager.</param>
		/// <param name="HttpxCache">HTTPX cache object.</param>
		public HttpxProxy(string ResourceName, XmppClient DefaultXmppClient, int MaxChunkSize, XmppServerlessMessaging ServerlessMessaging,
			IHttpxCache HttpxCache) : base(ResourceName)
		{
			this.defaultXmppClient = DefaultXmppClient;
			this.serverlessMessaging = ServerlessMessaging;
			this.httpxCache = HttpxCache;

			this.httpxClient = new HttpxClient(this.defaultXmppClient, MaxChunkSize)
			{
				PostResource = this.postResource
			};
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.httpxClient?.Dispose();
			this.httpxClient = null;
			this.disposed = true;
		}

		/// <summary>
		/// If the proxy has been disposed.
		/// </summary>
		public bool Disposed => this.disposed;

		/// <summary>
		/// Post resource for responses.
		/// </summary>
		public IPostResource PostResource
		{
			get => this.postResource;
			set
			{
				this.postResource = value;

				if (!(this.httpxClient is null))
					this.httpxClient.PostResource = value;
			}
		}

		/// <summary>
		/// Serverless messaging manager.
		/// </summary>
		public XmppServerlessMessaging ServerlessMessaging
		{
			get => this.serverlessMessaging;
			set
			{
				if (!(this.serverlessMessaging is null) && this.serverlessMessaging != value)
					throw new Exception("Property already set.");

				this.serverlessMessaging = value;
			}
		}

		/// <summary>
		/// Reference to the HTTPX Cache manager.
		/// </summary>
		public IHttpxCache HttpxCache
		{
			get => this.httpxCache;
			set
			{
				if (!(this.httpxCache is null) && this.httpxCache != value)
					throw new Exception("Property already set.");

				this.httpxCache = value;
			}
		}

		/// <summary>
		/// Default XMPP client.
		/// </summary>
		public XmppClient DefaultXmppClient => this.defaultXmppClient;

		/// <summary>
		/// Default HTTPX client.
		/// </summary>
		public HttpxClient DefaultHttpxClient => this.httpxClient;

		/// <summary>
		/// In-band bytestream client, if supported.
		/// </summary>
		public InBandBytestreams.IbbClient IbbClient
		{
			get => this.ibbClient;
			set
			{
				this.ibbClient = value;

				if (!(this.httpxClient is null))
					this.httpxClient.IbbClient = value;
			}
		}

		/// <summary>
		/// SOCKS5 proxy, if supported.
		/// </summary>
		public P2P.SOCKS5.Socks5Proxy Socks5Proxy
		{
			get => this.socks5Proxy;
			set
			{
				this.socks5Proxy = value;

				if (!(this.httpxClient is null))
					this.httpxClient.Socks5Proxy = value;
			}
		}

		/// <summary>
		/// If the resource handles sub-paths.
		/// </summary>
		public override bool HandlesSubPaths
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// If the resource uses user sessions.
		/// </summary>
		public override bool UserSessions
		{
			get
			{
				return false;
			}
		}

		private async Task Request(string Method, HttpRequest Request, HttpResponse Response)
		{
			try
			{
				string Url = Request.SubPath;
				if (Url.StartsWith("/"))
					Url = Url.Substring(1);

				if (!Url.StartsWith("httpx://", StringComparison.OrdinalIgnoreCase))
					throw new BadRequestException("Invalid URI. Must use httpx URI scheme.");

				int i = Url.IndexOf('/', 8);
				if (i < 0)
					throw new BadRequestException("Invalid URI.");

				string BareJID = Url.Substring(8, i - 8);
				string LocalUrl = Url.Substring(i);

				IHttpxCachedResource CachedResource;

				if (Method == "GET" && !(this.httpxCache is null))
				{
					if (!((CachedResource = await this.httpxCache.TryGetCachedResource(BareJID, LocalUrl)) is null))
					{
						if (!(Request.Header.IfNoneMatch is null))
						{
							if (!(CachedResource.ETag is null) && Request.Header.IfNoneMatch.Value == CachedResource.ETag)
								throw new NotModifiedException();
						}
						else if (!(Request.Header.IfModifiedSince is null))
						{
							DateTimeOffset? Limit;

							if ((Limit = Request.Header.IfModifiedSince.Timestamp).HasValue &&
								HttpFolderResource.LessOrEqual(CachedResource.LastModified.UtcDateTime, Limit.Value.ToUniversalTime()))
							{
								throw new NotModifiedException();
							}
						}

						await HttpFolderResource.SendResponse(CachedResource.FileName, CachedResource.ContentType, CachedResource.ETag,
							CachedResource.LastModified.UtcDateTime, Response, Request);

						return;
					}
				}

				RosterItem Item = this.defaultXmppClient.GetRosterItem(BareJID);
				if (Item is null)
				{
					if (!XmppClient.BareJidRegEx.IsMatch(BareJID))
						throw new BadRequestException("Invalid Bare JID.");

					// TODO: Request presence subscription, if user authenticated and request valid.

					throw new ConflictException("No approved presence subscription with " + BareJID + ".");
				}
				else
				{
					foreach (PresenceEventArgs e in Item.Resources)
					{
						// TODO: Select one based on features.

						if (!(this.serverlessMessaging is null))
						{
							this.serverlessMessaging.GetPeerConnection(e.From, this.SendP2P, new SendP2pRec()
							{
								item = Item,
								method = Method,
								fullJID = e.From,
								localUrl = LocalUrl,
								request = Request,
								response = Response
							});
						}
						else
							await this.SendRequest(this.httpxClient, e.From, Method, BareJID, LocalUrl, Request, Response);

						return;
					}

					throw new ServiceUnavailableException(BareJID + " not online.");
				}
			}
			catch (Exception ex)
			{
				await Response.SendResponse(ex);
			}
		}

		/// <summary>
		/// Gets a corresponding <see cref="HttpxClient"/> appropriate for a given request.
		/// </summary>
		/// <param name="Uri">URI</param>
		/// <returns>Contains details of the <paramref name="Uri"/> and the corresponding <see cref="HttpxClient"/> to use
		/// for requesting the resource from the entity.</returns>
		/// <exception cref="ArgumentException">If the <paramref name="Uri"/> parameter is invalid.</exception>
		/// <exception cref="ConflictException">If an approved presence subscription with the remote entity does not exist.</exception>
		/// <exception cref="ServiceUnavailableException">If the remote entity is not online.</exception>
		public async Task<GetClientResponse> GetClientAsync(Uri Uri)
		{
			if (string.Compare(Uri.Scheme, HttpxGetter.HttpxUriScheme, true) != 0)
				throw new ArgumentException("URI must use URI Scheme HTTPX.", nameof(Uri));

			string BareJID = Uri.UserInfo + "@" + Uri.Authority;
			string LocalUrl = Uri.PathAndQuery + Uri.Fragment;

			RosterItem Item = this.defaultXmppClient.GetRosterItem(BareJID);
			if (Item is null)
			{
				if (BareJID.IndexOf('@') < 0)	// Server or component hosts HTTPX interface
				{
					return new GetClientResponse()
					{
						BareJid = BareJID,
						FullJid = BareJID,
						HttpxClient = this.DefaultHttpxClient,
						LocalUrl = LocalUrl
					};
				}

				if (!XmppClient.BareJidRegEx.IsMatch(BareJID))
					throw new BadRequestException("Invalid Bare JID.");

				// TODO: Request presence subscription, if user authenticated and request valid.

				throw new ConflictException("No approved presence subscription with " + BareJID + ".");
			}
			else
			{
				TaskCompletionSource<HttpxClient> Result = new TaskCompletionSource<HttpxClient>();

				foreach (PresenceEventArgs e in Item.Resources)
				{
					if (!(this.serverlessMessaging is null))
					{
						this.serverlessMessaging.GetPeerConnection(e.From, (sender, e2) =>
						{
							if (e2.Client is null)
								Result.TrySetResult(this.httpxClient);
							else
							{
								if (e2.Client.SupportsFeature(HttpxClient.Namespace) &&
									e2.Client.TryGetTag("HttpxClient", out object Obj) &&
									Obj is HttpxClient Client)
								{
									Result.TrySetResult(Client);
								}
								else
									Result.TrySetResult(this.httpxClient);
							}
						}, null);
					}
					else
						Result.TrySetResult(this.httpxClient);

					HttpxClient Client2 = await Result.Task;

					return new GetClientResponse()
					{
						FullJid = e.From,
						BareJid = BareJID,
						LocalUrl = LocalUrl,
						HttpxClient = Client2
					};
				}

				throw new ServiceUnavailableException(BareJID + " not online.");
			}
		}

		private class SendP2pRec
		{
			public RosterItem item;
			public string method;
			public string fullJID;
			public string localUrl;
			public HttpRequest request;
			public HttpResponse response;
		}

		private void SendP2P(object Sender, PeerConnectionEventArgs e)
		{
			SendP2pRec Rec = (SendP2pRec)e.State;

			try
			{
				if (e.Client is null)
				{
					this.SendRequest(this.httpxClient, Rec.fullJID, Rec.method, XmppClient.GetBareJID(Rec.fullJID),
						Rec.localUrl, Rec.request, Rec.response);
				}
				else
				{
					if (e.Client.SupportsFeature(HttpxClient.Namespace) &&
						e.Client.TryGetTag("HttpxClient", out object Obj) &&
						Obj is HttpxClient Client)
					{
						this.SendRequest(Client, Rec.fullJID, Rec.method, XmppClient.GetBareJID(Rec.fullJID),
							Rec.localUrl, Rec.request, Rec.response);
					}
					else
					{
						this.SendRequest(this.httpxClient, Rec.fullJID, Rec.method, XmppClient.GetBareJID(Rec.fullJID),
							Rec.localUrl, Rec.request, Rec.response);
					}
				}
			}
			catch (Exception ex)
			{
				Task _ = Rec.response.SendResponse(ex);
			}
		}

		private Task SendRequest(HttpxClient HttpxClient, string To, string Method, string BareJID, string LocalUrl,
			HttpRequest Request, HttpResponse Response)
		{
			LinkedList<HttpField> Headers = new LinkedList<HttpField>();

			foreach (HttpField Header in Request.Header)
			{
				switch (Header.Key.ToLower())
				{
					case "host":
						Headers.AddLast(new HttpField("Host", BareJID));
						break;

					case "cookie":
					case "set-cookie":
						// Do not forward cookies.
						break;

					default:
						Headers.AddLast(Header);
						break;
				}
			}

			ReadoutState State = new ReadoutState(Response, BareJID, LocalUrl)
			{
				Cacheable = (Method == "GET" && !(this.httpxCache is null))
			};

			string s = LocalUrl;
			int i = s.IndexOf('.');
			if (i > 0)
			{
				s = s.Substring(i + 1);
				i = s.IndexOfAny(new char[] { '?', '#' });
				if (i > 0)
					s = s.Substring(0, i);

				if (this.httpxCache.CanCache(BareJID, LocalUrl, InternetContent.GetContentType(s)))
				{
					LinkedListNode<HttpField> Loop = Headers.First;
					LinkedListNode<HttpField> Next;

					while (!(Loop is null))
					{
						Next = Loop.Next;

						switch (Loop.Value.Key.ToLower())
						{
							case "if-match":
							case "if-modified-since":
							case "if-none-match":
							case "if-range":
							case "if-unmodified-since":
								Headers.Remove(Loop);
								break;
						}

						Loop = Next;
					}
				}
			}

			return HttpxClient.Request(To, Method, LocalUrl, Request.Header.HttpVersion, Headers, Request.HasData ? Request.DataStream : null,
				this.RequestResponse, this.ResponseData, State);
		}

		private async Task RequestResponse(object Sender, HttpxResponseEventArgs e)
		{
			ReadoutState State2 = (ReadoutState)e.State;

			State2.Response.StatusCode = e.StatusCode;
			State2.Response.StatusMessage = e.StatusMessage;

			if (!(e.HttpResponse is null))
			{
				foreach (KeyValuePair<string, string> Field in e.HttpResponse.GetHeaders())
				{
					switch (Field.Key.ToLower())
					{
						case "cookie":
						case "set-cookie":
							// Do not forward cookies.
							break;

						case "content-type":
							State2.ContentType = Field.Value;
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;

						case "etag":
							State2.ETag = Field.Value;
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;

						case "last-modified":
							DateTimeOffset TP;
							if (CommonTypes.TryParseRfc822(Field.Value, out TP))
								State2.LastModified = TP;
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;

						case "expires":
							if (CommonTypes.TryParseRfc822(Field.Value, out TP))
								State2.Expires = TP;
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;

						case "cache-control":
							State2.CacheControl = Field.Value;
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;

						case "pragma":
							State2.Pragma = Field.Value;
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;

						default:
							State2.Response.SetHeader(Field.Key, Field.Value);
							break;
					}
				}
			}

			if (!e.HasData)
				await State2.Response.SendResponse();
			else
			{
				if (e.StatusCode == 200 && State2.Cacheable && State2.CanCache &&
					this.httpxCache.CanCache(State2.BareJid, State2.LocalResource, State2.ContentType))
				{
					State2.TempOutput = new TemporaryStream();
				}

				if (!(e.Data is null))
					await this.BinaryDataReceived(State2, true, e.Data);
			}
		}

		private Task ResponseData(object Sender, HttpxResponseDataEventArgs e)
		{
			ReadoutState State2 = (ReadoutState)e.State;

			return this.BinaryDataReceived(State2, e.Last, e.Data);
		}

		private async Task BinaryDataReceived(ReadoutState State2, bool Last, byte[] Data)
		{
			try
			{
				await State2.Response.Write(Data);
			}
			catch (Exception)
			{
				State2.Dispose();
				return;
			}

			State2.TempOutput?.Write(Data, 0, Data.Length);

			if (Last)
			{
				await State2.Response.SendResponse();
				this.AddToCacheAsync(State2);
			}
		}

		private async void AddToCacheAsync(ReadoutState State)
		{
			try
			{
				if (!(State.TempOutput is null))
				{
					State.TempOutput.Position = 0;

					await this.httpxCache.AddToCache(State.BareJid, State.LocalResource, State.ContentType, State.ETag,
						State.LastModified.Value, State.Expires, State.TempOutput);
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
			finally
			{
				try
				{
					State.Dispose();
				}
				catch (Exception ex2)
				{
					Log.Exception(ex2);
				}
			}
		}

		private class ReadoutState : IDisposable
		{
			public bool Cacheable = false;
			public HttpResponse Response;
			public string ETag = null;
			public string BareJid = null;
			public string LocalResource = null;
			public string ContentType = null;
			public string CacheControl = null;
			public string Pragma = null;
			public DateTimeOffset? Expires = null;
			public DateTimeOffset? LastModified = null;
			public TemporaryStream TempOutput = null;

			public ReadoutState(HttpResponse Response, string BareJid, string LocalResource)
			{
				this.Response = Response;
				this.BareJid = BareJid;
				this.LocalResource = LocalResource;
			}

			public bool CanCache
			{
				get
				{
					if (this.ETag is null || !this.LastModified.HasValue)
						return false;

					if (!(this.CacheControl is null))
					{
						if ((this.CacheControl.Contains("no-cache") || this.CacheControl.Contains("no-store")))
							return false;

						if (!this.Expires.HasValue)
						{
							string s = this.CacheControl;
							int i = s.IndexOf("max-age");
							int c = s.Length;
							char ch;

							while (i < c && ((ch = s[i]) <= ' ' || ch == '=' || ch == 160))
								i++;

							int j = i;

							while (j < c && (ch = s[j]) >= '0' && ch <= '9')
								j++;

							if (j > i && int.TryParse(s.Substring(i, j - i), out j))
								this.Expires = DateTimeOffset.UtcNow.AddSeconds(j);
						}
					}

					if (!(this.Pragma is null) && this.Pragma.Contains("no-cache"))
						return false;

					return true;
				}
			}

			public void Dispose()
			{
				if (!(this.TempOutput is null))
				{
					this.TempOutput.Dispose();
					this.TempOutput = null;
				}
			}
		}

		/// <summary>
		/// If the GET method is allowed.
		/// </summary>
		public bool AllowsGET
		{
			get { return true; }
		}

		/// <summary>
		/// Executes the GET method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task GET(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("GET", Request, Response);
		}

		/// <summary>
		/// Executes the ranged GET method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <param name="FirstInterval">First byte range interval.</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task GET(HttpRequest Request, HttpResponse Response, ByteRangeInterval FirstInterval)
		{
			return this.Request("GET", Request, Response);
		}

		/// <summary>
		/// Executes the OPTIONS method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public override Task OPTIONS(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("OPTIONS", Request, Response);
		}

		/// <summary>
		/// If the POST method is allowed.
		/// </summary>
		public bool AllowsPOST
		{
			get { return true; }
		}

		/// <summary>
		/// Executes the POST method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task POST(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("POST", Request, Response);
		}

		/// <summary>
		/// Executes the ranged POST method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <param name="Interval">Content byte range.</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task POST(HttpRequest Request, HttpResponse Response, ContentByteRangeInterval Interval)
		{
			return this.Request("POST", Request, Response);
		}

		/// <summary>
		/// If the PUT method is allowed.
		/// </summary>
		public bool AllowsPUT
		{
			get { return true; }
		}

		/// <summary>
		/// Executes the PUT method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task PUT(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("PUT", Request, Response);
		}

		/// <summary>
		/// Executes the ranged PUT method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <param name="Interval">Content byte range.</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task PUT(HttpRequest Request, HttpResponse Response, ContentByteRangeInterval Interval)
		{
			return this.Request("PUT", Request, Response);
		}

		/// <summary>
		/// If the PATCH method is allowed.
		/// </summary>
		public bool AllowsPATCH
		{
			get { return true; }
		}

		/// <summary>
		/// Executes the PATCH method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task PATCH(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("PATCH", Request, Response);
		}

		/// <summary>
		/// Executes the ranged PATCH method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <param name="Interval">Content byte range.</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task PATCH(HttpRequest Request, HttpResponse Response, ContentByteRangeInterval Interval)
		{
			return this.Request("PATCH", Request, Response);
		}

		/// <summary>
		/// If the TRACE method is allowed.
		/// </summary>
		public bool AllowsTRACE
		{
			get { return true; }
		}

		/// <summary>
		/// Executes the TRACE method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task TRACE(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("TRACE", Request, Response);
		}

		/// <summary>
		/// If the DELETE method is allowed.
		/// </summary>
		public bool AllowsDELETE
		{
			get { return true; }
		}

		/// <summary>
		/// Executes the DELETE method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public Task DELETE(HttpRequest Request, HttpResponse Response)
		{
			return this.Request("DELETE", Request, Response);
		}
	}
}
