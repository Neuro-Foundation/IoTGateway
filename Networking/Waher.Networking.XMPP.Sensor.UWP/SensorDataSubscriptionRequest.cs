﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Things;
using Waher.Things.SensorData;

namespace Waher.Networking.XMPP.Sensor
{
	/// <summary>
	/// Manages a sensor data client request.
	/// </summary>
	public class SensorDataSubscriptionRequest : SensorDataClientRequest 
	{
		private readonly FieldSubscriptionRule[] fieldRules;
		private readonly Duration? minInterval;
		private readonly Duration? maxInterval;
		private readonly Duration? maxAge;
		private bool unsubscribed = false;

		/// <summary>
		/// Manages a sensor data client request.
		/// </summary>
		/// <param name="Id">Request identity.</param>
		/// <param name="SensorClient">Sensor client object.</param>
		/// <param name="RemoteJID">JID of the other side of the conversation in the sensor data readout.</param>
		/// <param name="Actor">Actor causing the request to be made.</param>
		/// <param name="Nodes">Array of nodes to read. Can be null or empty, if reading a sensor that is not a concentrator.</param>
		/// <param name="Types">Field Types to read.</param>
		/// <param name="FieldRules">Fields to read.</param>
		/// <param name="MinInterval">Smallest interval for reporting events. Events are not reported more often than this limit.</param>
		/// <param name="MaxInterval">Largest interval for reporting events. Events are not reported less often than this limit.</param>
		/// <param name="MaxAge">Maximum age of historical data.</param>
		/// <param name="ServiceToken">Optional service token.</param>
		/// <param name="DeviceToken">Optional device token.</param>
		/// <param name="UserToken">Optional user token.</param>
		internal SensorDataSubscriptionRequest(string Id, SensorClient SensorClient, string RemoteJID, string Actor, IThingReference[] Nodes, 
			FieldType Types, FieldSubscriptionRule[] FieldRules, Duration? MinInterval, Duration? MaxInterval, Duration? MaxAge, string ServiceToken, 
			string DeviceToken, string UserToken)
			: base(Id, SensorClient, RemoteJID, Actor, Nodes, Types, ExtractFieldNames(FieldRules), 
				  DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, ServiceToken, DeviceToken, UserToken)
		{
			this.minInterval = MinInterval;
			this.maxInterval = MaxInterval;
			this.maxAge = MaxAge;
			this.fieldRules = FieldRules;
		}

		private static string[] ExtractFieldNames(FieldSubscriptionRule[] FieldRules)
		{
			if (FieldRules is null)
				return null;

			int i, c = FieldRules.Length;
			string[] Result = new string[c];

			for (i = 0; i < c; i++)
				Result[i] = FieldRules[i].FieldName;

			return Result;
		}

		/// <summary>
		/// Smallest interval for reporting events. Events are not reported more often than this limit.
		/// </summary>
		public Duration? MinInterval => this.minInterval;

		/// <summary>
		/// Largest interval for reporting events. Events are not reported less often than this limit.
		/// </summary>
		public Duration? MaxInterval => this.maxInterval;

		/// <summary>
		/// Maximum age of historical data.
		/// </summary>
		public Duration? MaxAge => this.maxAge;

		/// <summary>
		/// Field subscription rules.
		/// </summary>
		public FieldSubscriptionRule[] FieldRules => this.fieldRules;

		/// <summary>
		/// Removes the subscription.
		/// </summary>
		public void Unsubscribe()
		{
			if (this.unsubscribed)
				return;

			this.unsubscribed = true;

			if (this.sensorClient.Client.State == XmppState.Connected)
			{
				StringBuilder Xml = new StringBuilder();

				Xml.Append("<unsubscribe xmlns='");
				Xml.Append(SensorClient.NamespaceSensorEventsCurrent);
				Xml.Append("' id='");
				Xml.Append(this.Id);
				Xml.Append("'/>");

				this.sensorClient.Client.SendIqSet(this.RemoteJID, Xml.ToString(), this.UnsubscribeResponse, null);
			}
		}

		private Task UnsubscribeResponse(object Sender, IqResultEventArgs e)
		{
			if (!e.Ok)
				return this.Fail(e.ErrorText);
			else
				return Task.CompletedTask;
		}

		internal override Task LogFields(IEnumerable<Field> Fields)
		{
			if (!this.unsubscribed)
			{
				this.Unsubscribe();
				return Task.CompletedTask;
			}
			else
				return base.LogFields(Fields);
		}

		internal override Task LogErrors(IEnumerable<ThingError> Errors)
		{
			if (!this.unsubscribed)
			{
				this.Unsubscribe();
				return Task.CompletedTask;
			}
			else
				return base.LogErrors(Errors);
		}

	}
}
