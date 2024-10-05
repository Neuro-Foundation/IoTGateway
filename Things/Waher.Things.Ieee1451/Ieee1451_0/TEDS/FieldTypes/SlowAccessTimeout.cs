﻿using System;
using System.Collections.Generic;
using Waher.Content;
using Waher.Runtime.Inventory;
using Waher.Things.Ieee1451.Ieee1451_0.Messages;
using Waher.Things.SensorData;

namespace Waher.Things.Ieee1451.Ieee1451_0.TEDS.FieldTypes
{
	/// <summary>
	/// TEDS Slow access time-out (§6.4.2.5)
	/// </summary>
	public class SlowAccessTimeout : TedsRecord
	{
		/// <summary>
		/// TEDS Slow access time-out (§6.4.2.5)
		/// </summary>
		public SlowAccessTimeout()
			: base()
		{
		}

		/// <summary>
		/// The slow access time-out field contains the time interval, in seconds, after 
		/// which an action for which the lack of a reply following the receipt of a 
		/// command may be interpreted as a failed operation.
		/// </summary>
		public float Timeout { get; set; }

		/// <summary>
		/// How well the class supports a specific TEDS field type.
		/// </summary>
		/// <param name="FieldType">TEDS field type.</param>
		/// <returns>Suppoer grade.</returns>
		public override Grade Supports(byte FieldType)
		{
			return FieldType == 11 ? Grade.Perfect : Grade.NotAtAll;
		}

		/// <summary>
		/// Parses a TEDS record.
		/// </summary>
		/// <param name="Type">Field Type</param>
		/// <param name="RawValue">Raw Value of record</param>
		/// <returns>Parsed TEDS record.</returns>
		public override TedsRecord Parse(byte Type, Ieee1451_0Binary RawValue)
		{
			return new SlowAccessTimeout()
			{
				Type = Type,
				RawValue = RawValue.Body,
				Timeout = RawValue.NextSingle()
			};
		}

		/// <summary>
		/// Adds fields to a collection of fields.
		/// </summary>
		/// <param name="Thing">Thing associated with fields.</param>
		/// <param name="Timestamp">Timestamp of fields.</param>
		/// <param name="Fields">Parsed fields.</param>
		public override void AddFields(ThingReference Thing, DateTime Timestamp, List<Field> Fields)
		{
			Fields.Add(new QuantityField(Thing, Timestamp, "Slow Access Timeout",
				this.Timeout, Math.Min(CommonTypes.GetNrDecimals(this.Timeout), (byte)2), "s", 
				SensorData.FieldType.Status, FieldQoS.AutomaticReadout));
		}
	}
}
