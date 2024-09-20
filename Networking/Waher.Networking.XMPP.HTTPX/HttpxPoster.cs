﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Posters;
using Waher.Networking.HTTP;
using Waher.Runtime.Inventory;

namespace Waher.Networking.XMPP.HTTPX
{
	/// <summary>
	/// Content Poster, posting content using the HTTPX URI Scheme.
	/// 
	/// For the poster to work, the application needs to register a <see cref="HttpxProxy"/> object instance
	/// as a Model Parameter named HTTPX.
	/// </summary>
	public class HttpxPoster : PosterBase
	{
		/// <summary>
		/// Content Poster, posting content using the HTTPX URI Scheme.
		/// 
		/// For the poster to work, the application needs to register a <see cref="HttpxProxy"/> object instance
		/// as a Model Parameter named HTTPX.
		/// </summary>
		public HttpxPoster()
		{
		}

		/// <summary>
		/// Supported URI schemes.
		/// </summary>
		public override string[] UriSchemes => new string[] { HttpxGetter.HttpxUriScheme };

		/// <summary>
		/// If the poster is able to post to a resource, given its URI.
		/// </summary>
		/// <param name="Uri">URI</param>
		/// <param name="Grade">How well the poster would be able to post to a resource given the indicated URI.</param>
		/// <returns>If the poster can post to a resource with the indicated URI.</returns>
		public override bool CanPost(Uri Uri, out Grade Grade)
		{
			switch (Uri.Scheme)
			{
				case HttpxGetter.HttpxUriScheme:
					Grade = Grade.Ok;
					return true;

				default:
					Grade = Grade.NotAtAll;
					return false;
			}
		}

		/// <summary>
		/// Posts to a resource, using a Uniform Resource Identifier (or Locator).
		/// </summary>
		/// <param name="Uri">URI</param>
		/// <param name="EncodedData">Encoded data to be posted.</param>
		/// <param name="ContentType">Content-Type of encoded data in <paramref name="EncodedData"/>.</param>
		/// <param name="Certificate">Optional client certificate to use in a Mutual TLS session.</param>
		/// <param name="RemoteCertificateValidator">Optional validator of remote certificates.</param>
		/// <param name="TimeoutMs">Timeout, in milliseconds. (Default=60000)</param>
		/// <param name="Headers">Optional headers. Interpreted in accordance with the corresponding URI scheme.</param>
		/// <returns>Encoded response.</returns>
		/// <exception cref="InvalidOperationException">No <see cref="HttpxProxy"/> set in the HTTPX <see cref="Types"/> module parameter.</exception>
		/// <exception cref="ArgumentException">If the <paramref name="Uri"/> parameter is invalid.</exception>
		/// <exception cref="ArgumentException">If the object response be decoded.</exception>
		/// <exception cref="ConflictException">If an approved presence subscription with the remote entity does not exist.</exception>
		/// <exception cref="ServiceUnavailableException">If the remote entity is not online.</exception>
		/// <exception cref="TimeoutException">If the request times out.</exception>
		/// <exception cref="OutOfMemoryException">If resource too large to decode.</exception>
		/// <exception cref="IOException">If unable to read from temporary file.</exception>
		public override async Task<KeyValuePair<byte[], string>> PostAsync(Uri Uri, byte[] EncodedData, string ContentType, 
			X509Certificate Certificate, RemoteCertificateEventHandler RemoteCertificateValidator, int TimeoutMs, 
			params KeyValuePair<string, string>[] Headers)
		{
			HttpxClient HttpxClient;
			string BareJid;
			string FullJid;
			string LocalUrl;

			if (Types.TryGetModuleParameter("HTTPX", out object Obj) && Obj is HttpxProxy Proxy)
			{
				if (Proxy.DefaultXmppClient.Disposed || Proxy.ServerlessMessaging.Disposed)
					throw new InvalidOperationException("Service is being shut down.");

				GetClientResponse Rec = await Proxy.GetClientAsync(Uri);

				BareJid = Rec.BareJid;
				FullJid = Rec.FullJid;
				HttpxClient = Rec.HttpxClient;
				LocalUrl = Rec.LocalUrl;
			}
			else if (Types.TryGetModuleParameter("XMPP", out Obj) && Obj is XmppClient XmppClient)
			{
				if (XmppClient.Disposed)
					throw new InvalidOperationException("Service is being shut down.");

				if (!XmppClient.TryGetExtension(out HttpxClient HttpxClient2))
					throw new InvalidOperationException("No HTTPX Extesion has been registered on the XMPP Client.");

				HttpxClient = HttpxClient2;

				if (string.IsNullOrEmpty(Uri.UserInfo))
					FullJid = BareJid = Uri.Authority;
				else
				{
					BareJid = Uri.UserInfo + "@" + Uri.Authority;

					RosterItem Item = XmppClient.GetRosterItem(BareJid);

					if (Item is null)
						throw new ConflictException("No approved presence subscription with " + BareJid + ".");
					else if (!Item.HasLastPresence || !Item.LastPresence.IsOnline)
						throw new ServiceUnavailableException(BareJid + " is not online.");
					else
						FullJid = Item.LastPresenceFullJid;
				}

				LocalUrl = Uri.PathAndQuery + Uri.Fragment;
			}
			else
				throw new InvalidOperationException("An HTTPX Proxy or XMPP Client Module Parameter has not been registered.");

			List<HttpField> Headers2 = new List<HttpField>();
			bool HasContentType = false;
			bool HasHost = false;

			foreach (KeyValuePair<string, string> Header in Headers)
			{
				switch (Header.Key.ToLower())
				{
					case "host":
						Headers2.Add(new HttpField("Host", BareJid));
						HasHost = true;
						break;

					case "cookie":
					case "set-cookie":
						// Do not forward cookies.
						break;

					case "content-type":
						Headers2.Add(new HttpField(Header.Key, Header.Value));
						HasContentType = true;
						break;

					default:
						Headers2.Add(new HttpField(Header.Key, Header.Value));
						break;
				}
			}

			if (!HasContentType)
				Headers2.Add(new HttpField("Content-Type", ContentType));

			if (!HasHost)
				Headers2.Add(new HttpField("Host", Uri.Authority));

			MemoryStream Data = new MemoryStream(EncodedData);
			State State = null;
			Timer Timer = null;

			try
			{
				State = new State();
				Timer = new Timer((P) =>
				{
					State.Done.TrySetResult(false);
				}, null, TimeoutMs, Timeout.Infinite);

				// TODO: Transport public part of Client certificate, if provided.

				await HttpxClient.Request(FullJid, "POST", LocalUrl, 1.1, Headers2, Data, async (sender, e) =>
					{
						if (e.Ok)
						{
							State.HttpResponse = e.HttpResponse;
							State.StatusCode = e.StatusCode;
							State.StatusMessage = e.StatusMessage;

							if (e.HasData)
							{
								if (State.Data is null)
									State.Data = new MemoryStream();

								if (!(e.Data is null))
								{
									await State.Data.WriteAsync(e.Data, 0, e.Data.Length);
									State.Done.TrySetResult(true);
								}
							}
							else
								State.Done.TrySetResult(true);
						}
						else
							State.Done.TrySetException(e.StanzaError ?? new Exception("Unable to get resource."));

					}, async (sender, e) =>
					{
						if (State.Data is null)
							State.Data = new MemoryStream();

						await State.Data.WriteAsync(e.Data, 0, e.Data.Length);
						if (e.Last)
							State.Done.TrySetResult(true);

					}, State);

				if (!await State.Done.Task)
					throw new TimeoutException("Request timed out.");

				Timer.Dispose();
				Timer = null;

				if (State.StatusCode >= 200 && State.StatusCode < 300)
					return new KeyValuePair<byte[], string>(State.Data?.ToArray(), State.HttpResponse?.ContentType);
				else
				{
					ContentType = string.Empty;
					EncodedData = State.Data?.ToArray();

					throw HttpxGetter.GetExceptionObject(State.StatusCode, State.StatusMessage,
						State.HttpResponse, EncodedData, ContentType);
				}
			}
			finally
			{
				State.Data?.Dispose();
				State.Data = null;

				State.HttpResponse?.Dispose();
				State.HttpResponse = null;

				Timer?.Dispose();
				Timer = null;

				Data.Dispose();
			}
		}

		private class State
		{
			public HttpResponse HttpResponse = null;
			public MemoryStream Data = null;
			public TaskCompletionSource<bool> Done = new TaskCompletionSource<bool>();
			public string StatusMessage = string.Empty;
			public int StatusCode = 0;
		}

	}
}
