﻿using System;
using System.Collections.Generic;
using Waher.Content;
using Waher.Runtime.Inventory;
using Waher.Things.Ieee1451.Ieee1451_0.Messages;
using Waher.Things.SensorData;

namespace Waher.Things.Ieee1451.Ieee1451_0.TEDS.FieldTypes.TransducerChannelTeds
{
	/// <summary>
	/// TEDS TransducerChannel write setup time (§6.5.2.35)
	/// </summary>
	public class ReadSetupTime : TedsRecord
	{
		/// <summary>
		/// TEDS TransducerChannel write setup time (§6.5.2.35)
		/// </summary>
		public ReadSetupTime()
			: base()
		{
		}

		/// <summary>
		/// This field contains the maximum time (trs), expressed in seconds, between 
		/// the receipt of the trigger by the transducer module and the time that the 
		/// data is available to be read.
		/// </summary>
		public float Value { get; set; }

		/// <summary>
		/// How well the class supports a specific TEDS field type.
		/// </summary>
		/// <param name="RecordTypeId">Record Type identifier.</param>
		/// <returns>Suppoer grade.</returns>
		public override Grade Supports(ClassTypePair RecordTypeId)
		{
			return RecordTypeId.Class == 3 && RecordTypeId.Type == 22 ? Grade.Perfect : Grade.NotAtAll;
		}

		/// <summary>
		/// Parses a TEDS record.
		/// </summary>
		/// <param name="RecordTypeId">Record Type identifier.</param>
		/// <param name="RawValue">Raw Value of record</param>
		/// <param name="State">Current parsing state.</param>
		/// <returns>Parsed TEDS record.</returns>
		public override TedsRecord Parse(ClassTypePair RecordTypeId, Binary RawValue, ParsingState State)
		{
			return new ReadSetupTime()
			{
				Class = RecordTypeId.Class,
				Type = RecordTypeId.Type,
				RawValue = RawValue.Body,
				Value = RawValue.NextSingle()
			};
		}

		/// <summary>
		/// Adds fields to a collection of fields.
		/// </summary>
		/// <param name="Thing">Thing associated with fields.</param>
		/// <param name="Timestamp">Timestamp of fields.</param>
		/// <param name="Fields">Parsed fields.</param>
		/// <param name="Teds">TEDS containing records.</param>
		public override void AddFields(ThingReference Thing, DateTime Timestamp, List<Field> Fields, Teds Teds)
		{
			Fields.Add(new QuantityField(Thing, Timestamp, "Read Setup Time", this.Value,
				Math.Min(CommonTypes.GetNrDecimals(this.Value), (byte)2), "s",
				FieldType.Status, FieldQoS.AutomaticReadout));
		}
	}
}
