﻿using System.Threading.Tasks;

namespace Waher.Networking.XMPP.InBandBytestreams
{
	/// <summary>
	/// Delegate for stream validation events.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate Task ValidateStreamEventHandler(object Sender, ValidateStreamEventArgs e);

	/// <summary>
	/// Event argument for stream validation events.
	/// </summary>
	public class ValidateStreamEventArgs : IqEventArgs
	{
		private DataReceivedEventHandler dataCallback = null;
		private StreamClosedEventHandler closeCallback = null;
		private readonly XmppClient client;
		private object state = null;
		private readonly string streamId;
		private readonly int blockSize;

		internal ValidateStreamEventArgs(XmppClient Client, IqEventArgs e, string StreamId, int BlockSize)
			: base(e)
		{
			this.client = Client;
			this.streamId = StreamId;
			this.blockSize = BlockSize;
		}

		/// <summary>
		/// XMPP Client
		/// </summary>
		public XmppClient Client => this.client;

		/// <summary>
		/// Stream ID
		/// </summary>
		public string StreamId => this.streamId;

		/// <summary>
		/// Block Size
		/// </summary>
		public int BlockSize => this.blockSize;

		internal DataReceivedEventHandler DataCallback => this.dataCallback;

		internal StreamClosedEventHandler CloseCallback => this.closeCallback;

		internal object State => this.state;

		/// <summary>
		/// Call this method to accept the incoming stream.
		/// </summary>
		/// <param name="DataCallback">Method called when data has been received.</param>
		/// <param name="CloseCallback">Method called when stream has been closed.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the stream acceptance was completed (true), or if somebody else accepted the stream beforehand (false).</returns>
		public bool AcceptStream(DataReceivedEventHandler DataCallback, StreamClosedEventHandler CloseCallback, object State)
		{
			if (this.dataCallback is null)
			{
				this.dataCallback = DataCallback;
				this.closeCallback = CloseCallback;
				this.state = State;

				return true;
			}
			else
				return false;
		}

	}
}
