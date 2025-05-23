﻿using System;

namespace Waher.Networking.XMPP.InBandBytestreams
{
	/// <summary>
	/// Event arguments for stream close callbacks.
	/// </summary>
	public class StreamClosedEventArgs : EventArgs
	{
		private readonly CloseReason reason;
		private readonly object state;

		/// <summary>
		/// Event arguments for stream close callbacks.
		/// </summary>
		/// <param name="Reason">Reason for closing stream.</param>
		/// <param name="State">State object.</param>
		public StreamClosedEventArgs(CloseReason Reason, object State)
		{
			this.reason = Reason;
			this.state = State;
		}

		/// <summary>
		/// Reason for closing stream.
		/// </summary>
		public CloseReason Reason => this.reason;

		/// <summary>
		/// State object.
		/// </summary>
		public object State => this.state;
	}
}
