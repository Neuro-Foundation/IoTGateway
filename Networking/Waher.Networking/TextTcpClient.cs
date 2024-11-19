﻿using System.Text;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;
#endif
using Waher.Events;
using Waher.Networking.Sniffers;

namespace Waher.Networking
{
	/// <summary>
	/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
	/// </summary>
	public class TextTcpClient : BinaryTcpClient, ITextTransportLayer
	{
		private Encoding encoding;
		private readonly bool sniffText;
		private int lastReceivedBytes = 0;
		private int lastTransmittedBytes = 0;

		/// <summary>
		/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
		/// </summary>
		/// <param name="Encoding">Text encoding to use.</param>
		/// <param name="DecoupledEvents">If events raised from the communication 
		/// layer are decoupled, i.e. executed in parallel with the source that raised 
		/// them.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public TextTcpClient(Encoding Encoding, bool DecoupledEvents, params ISniffer[] Sniffers)
			: this(Encoding, true, DecoupledEvents, Sniffers)
		{
		}

		/// <summary>
		/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
		/// </summary>
		/// <param name="Encoding">Text encoding to use.</param>
		/// <param name="SniffText">If text communication is to be forwarded to registered sniffers.</param>
		/// <param name="DecoupledEvents">If events raised from the communication 
		/// layer are decoupled, i.e. executed in parallel with the source that raised 
		/// them.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public TextTcpClient(Encoding Encoding, bool SniffText, bool DecoupledEvents, params ISniffer[] Sniffers)
			: base(false, DecoupledEvents, Sniffers)
		{
			this.encoding = Encoding;
			this.sniffText = SniffText;
		}

#if WINDOWS_UWP
		/// <summary>
		/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
		/// </summary>
		/// <param name="Client">Encapsulate this <see cref="StreamSocket"/> connection.</param>
		/// <param name="Encoding">Text encoding to use.</param>
		/// <param name="DecoupledEvents">If events raised from the communication 
		/// layer are decoupled, i.e. executed in parallel with the source that raised 
		/// them.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public TextTcpClient(StreamSocket Client, Encoding Encoding, bool DecoupledEvents, params ISniffer[] Sniffers)
			: this(Client, Encoding, true, DecoupledEvents, Sniffers)
		{
		}

		/// <summary>
		/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
		/// </summary>
		/// <param name="Client">Encapsulate this <see cref="StreamSocket"/> connection.</param>
		/// <param name="Encoding">Text encoding to use.</param>
		/// <param name="SniffText">If text communication is to be forwarded to registered sniffers.</param>
		/// <param name="DecoupledEvents">If events raised from the communication 
		/// layer are decoupled, i.e. executed in parallel with the source that raised 
		/// them.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public TextTcpClient(StreamSocket Client, Encoding Encoding, bool SniffText, bool DecoupledEvents, params ISniffer[] Sniffers)
			: base(Client, false, DecoupledEvents, Sniffers)
		{
			this.encoding = Encoding;
			this.sniffText = SniffText;
		}
#else
		/// <summary>
		/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
		/// </summary>
		/// <param name="Client">Encapsulate this <see cref="TcpClient"/> connection.</param>
		/// <param name="Encoding">Text encoding to use.</param>
		/// <param name="DecoupledEvents">If events raised from the communication 
		/// layer are decoupled, i.e. executed in parallel with the source that raised 
		/// them.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public TextTcpClient(TcpClient Client, Encoding Encoding, bool DecoupledEvents, params ISniffer[] Sniffers)
			: this(Client, Encoding, true, DecoupledEvents, Sniffers)
		{
		}

		/// <summary>
		/// Implements a text-based TCP Client, by using the thread-safe full-duplex <see cref="BinaryTcpClient"/>.
		/// </summary>
		/// <param name="Client">Encapsulate this <see cref="TcpClient"/> connection.</param>
		/// <param name="Encoding">Text encoding to use.</param>
		/// <param name="SniffText">If text communication is to be forwarded to registered sniffers.</param>
		/// <param name="DecoupledEvents">If events raised from the communication 
		/// layer are decoupled, i.e. executed in parallel with the source that raised 
		/// them.</param>
		/// <param name="Sniffers">Sniffers.</param>
		public TextTcpClient(TcpClient Client, Encoding Encoding, bool SniffText, bool DecoupledEvents, params ISniffer[] Sniffers)
			: base(Client, false, DecoupledEvents, Sniffers)
		{
			this.encoding = Encoding;
			this.sniffText = SniffText;
		}
#endif

		/// <summary>
		/// Text encoding to use.
		/// </summary>
		public Encoding Encoding
		{
			get => this.encoding;
			set => this.encoding = value;
		}

		/// <summary>
		/// Method called when binary data has been received.
		/// </summary>
		/// <param name="Buffer">Binary Data Buffer</param>
		/// <param name="Offset">Start index of first byte read.</param>
		/// <param name="Count">Number of bytes read.</param>
		/// <returns>If the process should be continued.</returns>
		protected override Task<bool> BinaryDataReceived(byte[] Buffer, int Offset, int Count)
		{
			this.lastReceivedBytes = Count;
			string Text = this.encoding.GetString(Buffer, Offset, Count);
			return this.TextDataReceived(Text);
		}

		/// <summary>
		/// Number of bytes of current (or last) text received. Can be used in event handlers to <see cref="OnReceived"/>.
		/// </summary>
		public int LastReceivedBytes => this.lastReceivedBytes;

		/// <summary>
		/// Method called when text data has been received.
		/// </summary>
		/// <param name="Data">Text data received.</param>
		protected virtual async Task<bool> TextDataReceived(string Data)
		{
			if (this.sniffText && this.HasSniffers)
				await this.ReceiveText(Data);

			TextEventHandler h = this.OnReceived;
			if (h is null)
				return true;
			else
				return await h(this, Data);
		}

		/// <summary>
		/// Event received when text data has been received.
		/// </summary>
		public new event TextEventHandler OnReceived;

		/// <summary>
		/// Sends a text packet.
		/// </summary>
		/// <param name="Text">Text packet.</param>
		/// <returns>If data was sent.</returns>
		public virtual Task<bool> SendAsync(string Text)
		{
			return this.SendAsync(Text, null, null);
		}

		/// <summary>
		/// Sends a text packet.
		/// </summary>
		/// <param name="Text">Text packet.</param>
		/// <param name="Callback">Method to call when packet has been sent.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		/// <returns>If data was sent.</returns>
		public async virtual Task<bool> SendAsync(string Text, EventHandlerAsync<DeliveryEventArgs> Callback, object State)
		{
			byte[] Data = this.encoding.GetBytes(Text);
			this.lastTransmittedBytes = Data.Length;
			bool Result = await base.SendAsync(Data, Callback, State);
			await this.TextDataSent(Text);
			return Result;
		}

		/// <summary>
		/// Number of bytes of current (or last) text transmitted. Can be used in event handlers to <see cref="OnSent"/>.
		/// </summary>
		public int LastTransmittedBytes => this.lastTransmittedBytes;

		/// <summary>
		/// Method called when text data has been sent.
		/// </summary>
		/// <param name="Text">Text data sent.</param>
		protected virtual async Task TextDataSent(string Text)
		{
			if (this.sniffText && this.HasSniffers)
				await this.TransmitText(Text);

			TextEventHandler h = this.OnSent;
			if (!(h is null))
				await h(this, Text);
		}

		/// <summary>
		/// Event raised when a packet has been sent.
		/// </summary>
		public new event TextEventHandler OnSent;

	}
}
