﻿using Waher.Things;
using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP.Provisioning.Events
{
	/// <summary>
	/// Event argument base class for node information events.
	/// </summary>
	public class NodeEventArgs : IqEventArgs
	{
		private ThingReference node;

		internal NodeEventArgs(IqEventArgs e, ThingReference Node)
			: base(e)
		{
			this.node = Node;
		}

		/// <summary>
		/// Node reference.
		/// </summary>
		public ThingReference Node
		{
			get => this.node;
			set => this.node = value;
		}
	}
}
