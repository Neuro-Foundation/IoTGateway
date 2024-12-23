﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking.Sniffers;
using Waher.Security;

namespace Waher.Networking.XMPP.P2P.SOCKS5
{
    /// <summary>
    /// SOCKS5 connection state.
    /// </summary>
    public enum Socks5State
	{
		/// <summary>
		/// Offline
		/// </summary>
		Offline,

		/// <summary>
		/// Connecting
		/// </summary>
		Connecting,

		/// <summary>
		/// Initializing
		/// </summary>
		Initializing,

		/// <summary>
		/// Authenticating
		/// </summary>
		Authenticating,

		/// <summary>
		/// Authenticated
		/// </summary>
		Authenticated,

		/// <summary>
		/// Connected
		/// </summary>
		Connected,

		/// <summary>
		/// Error state
		/// </summary>
		Error
	}

	/// <summary>
	/// Client used for SOCKS5 communication.
	/// 
	/// SOCKS5 is defined in RFC 1928.
	/// </summary>
	public class Socks5Client : CommunicationLayer, IDisposable, IHostReference
	{
		private BinaryTcpClient client;
		private Socks5State state = Socks5State.Offline;
		private readonly object synchObj = new object();
		private readonly string host;
		private readonly int port;
		private readonly string jid;
		private bool closeWhenDone = false;
		private bool disposed = false;
		private object callbackState;
		private object tag = null;
		private bool isWriting = false;

		/// <summary>
		/// Client used for SOCKS5 communication.
		/// </summary>
		/// <param name="Host">Host of SOCKS5 stream host.</param>
		/// <param name="Port">Port of SOCKS5 stream host.</param>
		/// <param name="JID">JID of SOCKS5 stream host.</param>
		/// <param name="Sniffers">Optional set of sniffers.</param>
		public Socks5Client(string Host, int Port, string JID, params ISniffer[] Sniffers)
			: base(false, Sniffers)
		{
			this.host = Host;
			this.port = Port;
			this.jid = JID;

			Task.Run(async () =>
			{
				try
				{
					await this.SetState(Socks5State.Connecting);
					await this.Information("Connecting to " + this.host + ":" + this.port.ToString());
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			});

			this.client = new BinaryTcpClient(false);
			this.Connect();
		}

		private async void Connect()
		{
			try
			{
				this.client.OnReceived += this.Client_OnReceived;
				this.client.OnSent += this.Client_OnSent;
				this.client.OnError += this.Client_OnError;
				this.client.OnDisconnected += this.Client_OnDisconnected;
				this.client.OnWriteQueueEmpty += this.Client_OnWriteQueueEmpty;

				await this.client.ConnectAsync(this.host, this.port);
				if (this.disposed)
					return;

				await this.Information("Connected to " + this.host + ":" + this.port.ToString());

				this.state = Socks5State.Initializing;
				await this.SendPacket(true, new byte[] { 5, 1, 0 });
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				await this.SetState(Socks5State.Error);
			}
		}

		private async Task Client_OnWriteQueueEmpty(object Sender, EventArgs e)
		{
			bool DoDispose;

			lock (this.synchObj)
			{
				this.isWriting = false;
				DoDispose = this.closeWhenDone;
			}

			if (DoDispose)
				this.Dispose();
			else
				await this.OnWriteQueueEmpty.Raise(this, e);
		}

		private Task Client_OnDisconnected(object Sender, EventArgs e)
		{
			return this.SetState(Socks5State.Offline);
		}

		private Task Client_OnError(object Sender, Exception Exception)
		{
			return this.SetState(Socks5State.Error);
		}

		private Task Client_OnSent(object Sender, byte[] Buffer, int Offset, int Count)
		{
			if (this.HasSniffers)
				return this.TransmitBinary(Buffer, Offset, Count);
			else
				return Task.CompletedTask;
		}

		private async Task<bool> Client_OnReceived(object Sender, byte[] Buffer, int Offset, int Count)
		{
			if (this.HasSniffers)
				await this.ReceiveBinary(Buffer, Offset, Count);

			try
			{
				await this.ParseIncoming(Buffer, Offset, Count);
				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// Current state.
		/// </summary>
		public Socks5State State => this.state;

		internal async Task SetState(Socks5State NewState)
		{
			if (this.state != NewState)
			{
				this.state = NewState;
				await this.Information("State changed to " + this.state.ToString());

				await this.OnStateChange.Raise(this, EventArgs.Empty);
			}
		}

		internal object CallbackState
		{
			get => this.callbackState;
			set => this.callbackState = value;
		}

		/// <summary>
		/// Tag
		/// </summary>
		public object Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		/// <summary>
		/// Event raised whenever the state changes.
		/// </summary>
		public event EventHandlerAsync OnStateChange = null;

		/// <summary>
		/// Host of SOCKS5 stream host.
		/// </summary>
		public string Host => this.host;

		/// <summary>
		/// Port of SOCKS5 stream host.
		/// </summary>
		public int Port => this.port;

		/// <summary>
		/// JID of SOCKS5 stream host.
		/// </summary>
		public string JID => this.jid;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;
				Task _ = this.SetState(Socks5State.Offline);

				this.client?.Dispose();
				this.client = null;
			}
		}

		/// <summary>
		/// Send binary data.
		/// </summary>
		/// <param name="Data">Data</param>
		/// <returns>If data was sent.</returns>
		[Obsolete("Use an overload with a OneTimeBuffer argument. This increases performance, as the buffer will not be unnecessarily cloned if queued.")]
		public Task<bool> Send(byte[] Data)
		{
			return this.Send(false, Data);
		}

		/// <summary>
		/// Send binary data.
		/// </summary>
		/// <param name="OneTimeBuffer">If the buffer is used only for this call (true),
		/// or if it will be used for multiple calls with different data (false).</param>
		/// <param name="Data">Data</param>
		/// <returns>If data was sent.</returns>
		public Task<bool> Send(bool OneTimeBuffer, byte[] Data)
		{
			if (this.state != Socks5State.Connected)
				throw new IOException("SOCKS5 connection not open.");

			return this.SendPacket(OneTimeBuffer, Data);
		}

		private Task<bool> SendPacket(bool OneTimeBuffer, byte[] Data)
		{
			lock (this.synchObj)
			{
				this.isWriting = true;
			}

			return this.client.SendAsync(OneTimeBuffer, Data);
		}

		/// <summary>
		/// Event raised when the write queue is empty.
		/// </summary>
		public event EventHandlerAsync OnWriteQueueEmpty = null;

		/// <summary>
		/// Closes the stream when all bytes have been sent.
		/// </summary>
		public void CloseWhenDone()
		{
			lock (this.synchObj)
			{
				if (this.isWriting)
				{
					this.closeWhenDone = true;
					return;
				}
			}

			this.Dispose();
		}

		private async Task ParseIncoming(byte[] Buffer, int Offset, int Count)
		{
			if (this.state == Socks5State.Connected)
				await this.OnDataReceived.Raise(this, new DataReceivedEventArgs(Buffer, Offset, Count, this, this.callbackState), false);
			else if (this.state == Socks5State.Initializing)
			{
				if (Count < 2 || Buffer[Offset++] < 5)
				{
					await this.ToError();
					return;
				}

				byte Method = Buffer[Offset++];

				switch (Method)
				{
					case 0: // No authentication.
						await this.SetState(Socks5State.Authenticated);
						break;

					default:
						await this.ToError();
						return;
				}
			}
			else
			{
				int c = Offset + Count;

				if (Count < 5 || Buffer[Offset++] < 5)
				{
					await this.ToError();
					return;
				}

				byte REP = Buffer[Offset++];

				switch (REP)
				{
					case 0: // Succeeded
						await this.SetState(Socks5State.Connected);
						break;

					case 1:
						await this.Error("General SOCKS server failure.");
						await this.ToError();
						break;

					case 2:
						await this.Error("Connection not allowed by ruleset.");
						await this.ToError();
						break;

					case 3:
						await this.Error("Network unreachable.");
						await this.ToError();
						break;

					case 4:
						await this.Error("Host unreachable.");
						await this.ToError();
						break;

					case 5:
						await this.Error("Connection refused.");
						await this.ToError();
						break;

					case 6:
						await this.Error("TTL expired.");
						await this.ToError();
						break;

					case 7:
						await this.Error("Command not supported.");
						await this.ToError();
						break;

					case 8:
						await this.Error("Address type not supported.");
						await this.ToError();
						break;

					default:
						await this.Error("Unrecognized error code returned: " + REP.ToString());
						await this.ToError();
						break;
				}

				Offset++;

				byte ATYP = Buffer[Offset++];
				IPAddress Addr = null;
				string DomainName = null;

				switch (ATYP)
				{
					case 1: // IPv4.
						if (Offset + 4 > c)
						{
							await this.Error("Expected more bytes.");
							await this.ToError();
							return;
						}

						byte[] A = new byte[4];
						Array.Copy(Buffer, Offset, A, 0, 4);
						Offset += 4;
						Addr = new IPAddress(A);
						break;

					case 3: // Domain name.
						byte NrBytes = Buffer[Offset++];
						if (Offset + NrBytes > c)
						{
							await this.Error("Expected more bytes.");
							await this.ToError();
							return;
						}

						DomainName = Encoding.ASCII.GetString(Buffer, Offset, NrBytes);
						Offset += NrBytes;
						break;

					case 4: // IPv6.
						if (Offset + 16 > c)
						{
							await this.Error("Expected more bytes.");
							await this.ToError();
							return;
						}

						A = new byte[16];
						Array.Copy(Buffer, Offset, A, 0, 16);
						Offset += 16;
						Addr = new IPAddress(A);
						break;

					default:
						await this.ToError();
						return;
				}

				if (Offset + 2 != c)
				{
					await this.Error("Invalid number of bytes received.");
					await this.ToError();
					return;
				}

				int Port = Buffer[Offset++];
				Port <<= 8;
				Port |= Buffer[Offset++];

				await this.OnResponse.Raise(this, new ResponseEventArgs(REP, Addr, DomainName, Port), false);
			}
		}

		/// <summary>
		/// Event raised when a response has been returned.
		/// </summary>
		public event EventHandlerAsync<ResponseEventArgs> OnResponse = null;

		/// <summary>
		/// Event raised when binary data has been received over an established connection.
		/// </summary>
		public event EventHandlerAsync<DataReceivedEventArgs> OnDataReceived = null;

		private async Task ToError()
		{
			await this.SetState(Socks5State.Error);
			this.client.Dispose();
		}

		private Task Request(Command Command, IPAddress DestinationAddress, int Port)
		{
			using MemoryStream Req = new MemoryStream();

			Req.WriteByte(5);
			Req.WriteByte((byte)Command);
			Req.WriteByte(0);

			if (DestinationAddress.AddressFamily == AddressFamily.InterNetwork)
				Req.WriteByte(1);
			else if (DestinationAddress.AddressFamily == AddressFamily.InterNetworkV6)
				Req.WriteByte(4);
			else
				throw new ArgumentException("Invalid address family.", nameof(DestinationAddress));

			byte[] Addr = DestinationAddress.GetAddressBytes();
			Req.Write(Addr, 0, Addr.Length);
			Req.WriteByte((byte)(Port >> 8));
			Req.WriteByte((byte)Port);

			return this.SendPacket(true, Req.ToArray());
		}

		private Task Request(Command Command, string DestinationDomainName, int Port)
		{
			using MemoryStream Req = new MemoryStream();

			Req.WriteByte(5);
			Req.WriteByte((byte)Command);
			Req.WriteByte(0);
			Req.WriteByte(3);

			byte[] Bytes = Encoding.ASCII.GetBytes(DestinationDomainName);
			int c = Bytes.Length;
			if (c > 255)
				throw new IOException("Domain name too long.");

			Req.WriteByte((byte)c);
			Req.Write(Bytes, 0, Bytes.Length);
			Req.WriteByte((byte)(Port >> 8));
			Req.WriteByte((byte)Port);

			return this.SendPacket(true, Req.ToArray());
		}

		/// <summary>
		/// Connects to the target.
		/// </summary>
		/// <param name="DestinationAddress">Destination Address. Must be a IPv4 or IPv6 address.</param>
		/// <param name="Port">Port number.</param>
		/// <exception cref="IOException">If client not connected (yet).</exception>
		public Task CONNECT(IPAddress DestinationAddress, int Port)
		{
			return this.Request(Command.CONNECT, DestinationAddress, Port);
		}

		/// <summary>
		/// Connects to the target.
		/// </summary>
		/// <param name="DestinationDomainName">Destination Domain Name.</param>
		/// <param name="Port">Port number.</param>
		/// <exception cref="IOException">If client not connected (yet).</exception>
		public Task CONNECT(string DestinationDomainName, int Port)
		{
			return this.Request(Command.CONNECT, DestinationDomainName, Port);
		}

		/// <summary>
		/// XMPP-specific SOCKS5 connection, as described in XEP-0065:
		/// https://xmpp.org/extensions/xep-0065.html
		/// </summary>
		/// <param name="StreamID">Stream ID</param>
		/// <param name="RequesterJID">Requester JID</param>
		/// <param name="TargetJID">Target JID</param>
		public Task CONNECT(string StreamID, string RequesterJID, string TargetJID)
		{
			string s = StreamID + RequesterJID + TargetJID;
			byte[] Hash = Hashes.ComputeSHA1Hash(Encoding.UTF8.GetBytes(s));
			StringBuilder sb = new StringBuilder();

			foreach (byte b in Hash)
				sb.Append(b.ToString("x2"));

			return this.CONNECT(sb.ToString(), 0);
		}

		/// <summary>
		/// Binds to the target.
		/// </summary>
		/// <param name="DestinationAddress">Destination Address. Must be a IPv4 or IPv6 address.</param>
		/// <param name="Port">Port number.</param>
		/// <exception cref="IOException">If client not connected (yet).</exception>
		public Task BIND(IPAddress DestinationAddress, int Port)
		{
			return this.Request(Command.BIND, DestinationAddress, Port);
		}

		/// <summary>
		/// Binds to the target.
		/// </summary>
		/// <param name="DestinationDomainName">Destination Domain Name.</param>
		/// <param name="Port">Port number.</param>
		/// <exception cref="IOException">If client not connected (yet).</exception>
		public Task BIND(string DestinationDomainName, int Port)
		{
			return this.Request(Command.BIND, DestinationDomainName, Port);
		}

		/// <summary>
		/// Establish an association within the UDP relay process.
		/// </summary>
		/// <param name="DestinationAddress">Destination Address. Must be a IPv4 or IPv6 address.</param>
		/// <param name="Port">Port number.</param>
		/// <exception cref="IOException">If client not connected (yet).</exception>
		public Task UDP_ASSOCIATE(IPAddress DestinationAddress, int Port)
		{
			return this.Request(Command.UDP_ASSOCIATE, DestinationAddress, Port);
		}

		/// <summary>
		/// Establish an association within the UDP relay process.
		/// </summary>
		/// <param name="DestinationDomainName">Destination Domain Name.</param>
		/// <param name="Port">Port number.</param>
		/// <exception cref="IOException">If client not connected (yet).</exception>
		public Task UDP_ASSOCIATE(string DestinationDomainName, int Port)
		{
			return this.Request(Command.UDP_ASSOCIATE, DestinationDomainName, Port);
		}

	}
}
