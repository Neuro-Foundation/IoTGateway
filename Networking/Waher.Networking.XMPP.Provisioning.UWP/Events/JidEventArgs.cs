﻿using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP.Provisioning.Events
{
	/// <summary>
	/// Event arguments for JID callbacks.
	/// </summary>
	public class JidEventArgs : IqResultEventArgs
	{
		private readonly string jid;

		internal JidEventArgs(IqResultEventArgs e, object State, string JID)
			: base(e)
		{
			this.State = State;
			this.jid = JID;
		}

		/// <summary>
		/// JID.
		/// </summary>
		public string JID => this.jid;
	}
}
