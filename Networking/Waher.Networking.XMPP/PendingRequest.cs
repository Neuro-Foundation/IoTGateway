﻿using System;
using Waher.Events;
using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP
{
	/// <summary>
	/// Contains information about a pending IQ request.
	/// </summary>
	internal class PendingRequest
	{
		private readonly EventHandlerAsync<IqResultEventArgs> iqCallback;
		private readonly EventHandlerAsync<PresenceEventArgs> presenceCallback;
		private DateTime timeout;
		private readonly string to;
		private string xml;
		private readonly object state;
		private int retryTimeout;
		private int nrRetries;
		private readonly int maxRetryTimeout;
		private readonly uint seqNr;
		private readonly bool dropOff;

		internal PendingRequest(uint SeqNr, EventHandlerAsync<IqResultEventArgs> Callback, object State, int RetryTimeout, int NrRetries, bool DropOff, int MaxRetryTimeout, 
			string To)
		{
			this.seqNr = SeqNr;
			this.iqCallback = Callback;
			this.presenceCallback = null;
			this.state = State;
			this.retryTimeout = RetryTimeout;
			this.nrRetries = NrRetries;
			this.maxRetryTimeout = MaxRetryTimeout;
			this.dropOff = DropOff;
			this.to = To;

			this.timeout = DateTime.Now.AddMilliseconds(RetryTimeout);
		}

		internal PendingRequest(uint SeqNr, EventHandlerAsync<PresenceEventArgs> Callback, object State, int RetryTimeout, int NrRetries, bool DropOff, int MaxRetryTimeout,
			string To)
		{
			this.seqNr = SeqNr;
			this.iqCallback = null;
			this.presenceCallback = Callback;
			this.state = State;
			this.retryTimeout = RetryTimeout;
			this.nrRetries = NrRetries;
			this.maxRetryTimeout = MaxRetryTimeout;
			this.dropOff = DropOff;
			this.to = To;

			this.timeout = DateTime.Now.AddMilliseconds(RetryTimeout);
		}

		/// <summary>
		/// Sequence number.
		/// </summary>
		public uint SeqNr => this.seqNr;

		/// <summary>
		/// To
		/// </summary>
		public string To => this.to;

		/// <summary>
		/// Request XML
		/// </summary>
		public string Xml
		{
			get => this.xml;
			internal set => this.xml = value;
		}

		/// <summary>
		/// Callback method (for IQ stanzas) to call when a result or error is returned.
		/// </summary>
		public EventHandlerAsync<IqResultEventArgs> IqCallback => this.iqCallback;

		/// <summary>
		/// Callback method (for Presence stanzas) to call when a result or error is returned.
		/// </summary>
		public EventHandlerAsync<PresenceEventArgs> PresenceCallback => this.presenceCallback;

		/// <summary>
		/// State object passed in the original request.
		/// </summary>
		public object State => this.state;

		/// <summary>
		/// Retry Timeout, in milliseconds.
		/// </summary>
		public int RetryTimeout => this.retryTimeout;

		/// <summary>
		/// Number of retries (left).
		/// </summary>
		public int NrRetries => this.nrRetries;

		/// <summary>
		/// Maximum retry timeout. Used if <see cref="DropOff"/> is true.
		/// </summary>
		public int MaxRetryTimeout => this.maxRetryTimeout;

		/// <summary>
		/// If the retry timeout should be doubled between retries (true), or if the same retry timeout should be used for all retries.
		/// The retry timeout will never exceed <see cref="MaxRetryTimeout"/>.
		/// </summary>
		public bool DropOff => this.dropOff;

		/// <summary>
		/// When the requests times out.
		/// </summary>
		public DateTime Timeout 
		{
			get => this.timeout;
			internal set => this.timeout = value; 
		}

		/// <summary>
		/// Checks if the request can be retried.
		/// </summary>
		/// <returns>If the request can be retried.</returns>
		public bool CanRetry()
		{
			if (this.nrRetries-- <= 0)
				return false;

			if (this.dropOff)
			{
				int i = this.retryTimeout * 2;
				if (i < this.retryTimeout || this.retryTimeout > this.maxRetryTimeout)
					this.retryTimeout = this.maxRetryTimeout;
				else
					this.retryTimeout = i;
			}

			this.timeout = this.timeout.AddMilliseconds(this.retryTimeout);

			return true;
		}

	}
}
