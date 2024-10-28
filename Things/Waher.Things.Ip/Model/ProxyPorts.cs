﻿using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Security;

namespace Waher.Things.Ip.Model
{
	/// <summary>
	/// Static class managing Port Proxies.
	/// </summary>
	public static class ProxyPorts
	{
		private static readonly Dictionary<string, ProxyPort> proxies = new Dictionary<string, ProxyPort>();

		/// <summary>
		/// Gets sort key for Port Proxy
		/// </summary>
		/// <param name="Host">Host address</param>
		/// <param name="Port">Port number</param>
		/// <param name="Tls">If TLS is used</param>
		/// <param name="TrustServer">If server is to be trusteed.</param>
		/// <param name="ListeningPort">Post number to listen on.</param>
		/// <param name="AuthorizedAccess">If only authorized access (mTLS) is permitted.</param>
		/// <param name="RemoteIps">Remote IP Address restrictions</param>
		/// <returns>Hash Digest, as key</returns>
		public static string GetKey(string Host, int Port, bool Tls, bool TrustServer, int ListeningPort, bool AuthorizedAccess, IpCidr[] RemoteIps)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(Host);
			sb.AppendLine(Port.ToString());
			sb.AppendLine(Tls.ToString());
			sb.AppendLine(TrustServer.ToString());
			sb.AppendLine(ListeningPort.ToString());
			sb.AppendLine(AuthorizedAccess.ToString());

			if (!(RemoteIps is null))
			{
				foreach (IpCidr Range in RemoteIps)
				{
					sb.AppendLine(Range.Address.ToString());
					sb.AppendLine(Range.Range.ToString());
				}
			}

			return Hashes.ComputeSHA1HashString(Encoding.UTF8.GetBytes(sb.ToString()));
		}

		/// <summary>
		/// Gets a TCP/IP proxy node
		/// </summary>
		/// <param name="Node">IP Host Port Proxy node.</param>
		/// <param name="Key">Sort key</param>
		/// <param name="Host">Remote host</param>
		/// <param name="Port">Remote port</param>
		/// <param name="Tls">If TLS should be used</param>
		/// <param name="TrustServer">If remote server should be trusted</param>
		/// <param name="ListeningPort">What port to open for listening.</param>
		/// <param name="AuthorizedAccess">If remote certificate is used to authorize access.</param>
		/// <param name="RemoteIps">Array of allowed remote IP ranges.</param>
		/// <returns>Proxy node.</returns>
		public static async Task<ProxyPort> GetProxy(IpHostPortProxy Node, string Key, string Host, int Port, bool Tls, bool TrustServer, int ListeningPort, 
			bool AuthorizedAccess, IpCidr[] RemoteIps)
		{
			ProxyPort Proxy;

			lock (proxies)
			{
				if (!proxies.TryGetValue(Key, out Proxy))
					Proxy = null;
			}

			if (!(Proxy is null))
				return Proxy;
			else
			{
				Proxy = await ProxyPort.Create(Node, Host, Port, Tls, TrustServer, ListeningPort, AuthorizedAccess, RemoteIps);

				lock (proxies)
				{
					if (proxies.ContainsKey(Key))
					{
						Proxy.Dispose();
						return proxies[Key];
					}
					else
					{
						proxies[Key] = Proxy;
						return Proxy;
					}
				}
			}
		}

		/// <summary>
		/// Destroys a proxy node
		/// </summary>
		/// <param name="Key">Key of node.</param>
		public static void DestroyProxy(string Key)
		{
			ProxyPort Proxy;

			lock (proxies)
			{
				if (!proxies.TryGetValue(Key, out Proxy))
					return;

				proxies.Remove(Key);
			}

			Proxy.Dispose();
		}

	}
}
