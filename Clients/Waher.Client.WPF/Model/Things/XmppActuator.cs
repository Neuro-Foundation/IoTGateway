﻿using System;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.Sensor;
using Waher.Things.SensorData;

namespace Waher.Client.WPF.Model.Things
{
	/// <summary>
	/// Represents a simple XMPP actuator.
	/// </summary>
	public class XmppActuator : XmppContact
	{
		private readonly bool isSensor;
		private readonly bool suportsEvents;

		public XmppActuator(TreeNode Parent, XmppClient Client, string BareJid, bool IsSensor, bool SupportsEventSubscripton, bool SupportsRdp)
			: base(Parent, Client, BareJid, SupportsRdp)
		{
			this.isSensor = IsSensor;
			this.suportsEvents = SupportsEventSubscripton;
		}

		public override string TypeName
		{
			get { return "Actuator"; }
		}

		public override bool CanReadSensorData => this.isSensor;
		public override bool CanSubscribeToSensorData => this.suportsEvents;

		public override async Task<SensorDataClientRequest> StartSensorDataMomentaryReadout()
		{
			if (this.isSensor)
			{
				XmppAccountNode XmppAccountNode = this.XmppAccountNode;
				SensorClient SensorClient;

				if (!(XmppAccountNode is null) && !((SensorClient = XmppAccountNode.SensorClient) is null))
					return await SensorClient.RequestReadout(this.RosterItem.LastPresenceFullJid, FieldType.Momentary);
				else
					return null;
			}
			else
				throw new NotSupportedException();
		}

		public override async Task<SensorDataClientRequest> StartSensorDataFullReadout()
		{
			if (this.isSensor)
			{
				XmppAccountNode XmppAccountNode = this.XmppAccountNode;
				SensorClient SensorClient;

				if (!(XmppAccountNode is null) && !((SensorClient = XmppAccountNode.SensorClient) is null))
					return await SensorClient.RequestReadout(this.RosterItem.LastPresenceFullJid, FieldType.All);
				else
					return null;
			}
			else
				throw new NotSupportedException();
		}

		public override async Task<SensorDataSubscriptionRequest> SubscribeSensorDataMomentaryReadout(FieldSubscriptionRule[] Rules)
		{
			if (this.isSensor)
			{
				XmppAccountNode XmppAccountNode = this.XmppAccountNode;
				SensorClient SensorClient;

				if (!(XmppAccountNode is null) && !((SensorClient = XmppAccountNode.SensorClient) is null))
				{
					return await SensorClient.Subscribe(this.RosterItem.LastPresenceFullJid, FieldType.Momentary, Rules,
						Duration.FromSeconds(1), Duration.FromMinutes(1), false);
				}
				else
					return null;
			}
			else
				throw new NotSupportedException();
		}

		public override bool CanConfigure => true;

		public override void Configure()
		{
			base.Configure();
		}

		protected override bool UseActuatorControl => true;

		public override async Task GetConfigurationForm(EventHandlerAsync<DataFormEventArgs> Callback, object State)
		{
			XmppAccountNode XmppAccountNode = this.XmppAccountNode;
			ControlClient ControlClient;

			if (!(XmppAccountNode is null) && !((ControlClient = XmppAccountNode.ControlClient) is null))
				await ControlClient.GetForm(this.RosterItem.LastPresenceFullJid, "en", Callback, State);
			else
				throw new NotSupportedException();
		}

	}
}
