﻿using System;
using System.Collections.Generic;
using Waher.Runtime.Inventory;
using Waher.Things.Ieee1451.Ieee1451_0.Messages;
using Waher.Things.SensorData;

namespace Waher.Things.Ieee1451.Ieee1451_0.TEDS.FieldTypes.TransducerChannelTeds
{
	/// <summary>
	/// Transducer Channel Type
	/// </summary>
	public enum TransducerChannelType
	{
		/// <summary>
		/// Sensor (0)
		/// </summary>
		Sensor = 0,

		/// <summary>
		/// Actuator (1)
		/// </summary>
		Actuator = 1,

		/// <summary>
		/// Event Sensor (2)
		/// </summary>
		EventSensor = 2
	}

	/// <summary>
	/// TEDS TransducerChannel type key (§6.5.2.5)
	/// </summary>
	public class ChannelType : TedsRecord
	{
		/// <summary>
		/// TEDS TransducerChannel type key (§6.5.2.5)
		/// </summary>
		public ChannelType()
			: base()
		{
		}

		/// <summary>
		/// Transducer Channel type key
		/// </summary>
		public TransducerChannelType TransducerType { get; set; }

		/// <summary>
		/// How well the class supports a specific TEDS field type.
		/// </summary>
		/// <param name="RecordTypeId">Record Type identifier.</param>
		/// <returns>Suppoer grade.</returns>
		public override Grade Supports(ClassTypePair RecordTypeId)
		{
			return RecordTypeId.Class == 3 && RecordTypeId.Type == 11 ? Grade.Perfect : Grade.NotAtAll;
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
			return new ChannelType()
			{
				Class = RecordTypeId.Class,
				Type = RecordTypeId.Type,
				RawValue = RawValue.Body,
				TransducerType = (TransducerChannelType)RawValue.NextUInt8()
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
			Fields.Add(new EnumField(Thing, Timestamp, "Transducer Channel Type", this.TransducerType,
				FieldType.Status, FieldQoS.AutomaticReadout));
		}
	}
}
