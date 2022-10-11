﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.Modbus;
using Waher.Networking.Modbus.Exceptions;
using Waher.Persistence.Attributes;
using Waher.Runtime.Language;
using Waher.Things.Attributes;
using Waher.Things.DisplayableParameters;
using Waher.Things.Metering;
using Waher.Things.SensorData;

namespace Waher.Things.Modbus
{
	/// <summary>
	/// Represents a Unit Device on a Modbus network.
	/// </summary>
	public class ModbusUnitNode : ProvisionedMeteringNode, ISensor
	{
		/// <summary>
		/// Represents a Unit Device on a Modbus network.
		/// </summary>
		public ModbusUnitNode()
			: base()
		{
			this.MaxCoilNr = 65535;
			this.MaxDiscreteInputNr = 65535;
			this.MaxHoldingRegisterNr = 65535;
			this.MaxInputRegisterNr = 65535;
		}

		/// <summary>
		/// If the node is provisioned is not. Property is editable.
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(5, "Unit Address:")]
		[ToolTip(6, "Unit ID on the Modbus network.")]
		[Range(0, 255)]
		[Required]
		public int UnitId { get; set; }

		/// <summary>
		/// Minimum coil number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(30, "Minimum coil number:")]
		[ToolTip(31, "Smallest coil number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(0)]
		public int MinCoilNr { get; set; }

		/// <summary>
		/// Maximum coil number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(32, "Maximum coil number:")]
		[ToolTip(33, "Largest coil number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(65535)]
		public int MaxCoilNr { get; set; }

		/// <summary>
		/// Minimum discrete input number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(34, "Minimum discrete input number:")]
		[ToolTip(35, "Smallest discrete input number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(0)]
		public int MinDiscreteInputNr { get; set; }

		/// <summary>
		/// Maximum discrete input number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(36, "Maximum discrete input number:")]
		[ToolTip(37, "Largest discrete input number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(65535)]
		public int MaxDiscreteInputNr { get; set; }

		/// <summary>
		/// Minimum input register number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(38, "Minimum input register number:")]
		[ToolTip(39, "Smallest input register number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(0)]
		public int MinInputRegisterNr { get; set; }

		/// <summary>
		/// Maximum input register number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(40, "Maximum input register number:")]
		[ToolTip(41, "Largest input register number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(65535)]
		public int MaxInputRegisterNr { get; set; }

		/// <summary>
		/// Minimum holding register number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(42, "Minimum holding register number:")]
		[ToolTip(43, "Smallest holding register number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(0)]
		public int MinHoldingRegisterNr { get; set; }

		/// <summary>
		/// Maximum holding register number
		/// </summary>
		[Page(4, "Modbus", 100)]
		[Header(44, "Maximum holding register number:")]
		[ToolTip(45, "Largest holding register number accepted by the device.")]
		[Range(0, 65535)]
		[DefaultValue(65535)]
		public int MaxHoldingRegisterNr { get; set; }

		/// <summary>
		/// Gets the type name of the node.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <returns>Localized type node.</returns>
		public override Task<string> GetTypeNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(ModbusGatewayNode), 2, "Modbus Unit");
		}

		/// <summary>
		/// Gets displayable parameters.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <param name="Caller">Information about caller.</param>
		/// <returns>Set of displayable parameters.</returns>
		public override async Task<IEnumerable<Parameter>> GetDisplayableParametersAsync(Language Language, RequestOrigin Caller)
		{
			LinkedList<Parameter> Result = await base.GetDisplayableParametersAsync(Language, Caller) as LinkedList<Parameter>;

			Result.AddLast(new Int32Parameter("Address", await Language.GetStringAsync(typeof(ModbusGatewayNode), 7, "Address"), this.UnitId));

			return Result;
		}

		/// <summary>
		/// If the node accepts a presumptive parent, i.e. can be added to that parent (if that parent accepts the node as a child).
		/// </summary>
		/// <param name="Parent">Presumptive parent node.</param>
		/// <returns>If the parent is acceptable.</returns>
		public override Task<bool> AcceptsParentAsync(INode Parent)
		{
			return Task.FromResult<bool>(Parent is ModbusGatewayNode);
		}

		/// <summary>
		/// If the node accepts a presumptive child, i.e. can receive as a child (if that child accepts the node as a parent).
		/// </summary>
		/// <param name="Child">Presumptive child node.</param>
		/// <returns>If the child is acceptable.</returns>
		public override Task<bool> AcceptsChildAsync(INode Child)
		{
			return Task.FromResult<bool>(Child is ModbusUnitChildNode);
		}

		/// <summary>
		/// Modbus Gateway node.
		/// </summary>
		public ModbusGatewayNode Gateway
		{
			get
			{
				if (this.Parent is ModbusGatewayNode GatewayNode)
					return GatewayNode;
				else
					throw new Exception("Modbus Gateway node not found.");
			}
		}

		/// <summary>
		/// Starts the readout of the sensor.
		/// </summary>
		/// <param name="Request">Request object. All fields and errors should be reported to this interface.</param>
		public async Task StartReadout(ISensorReadout Request)
		{
			ModbusTcpClient Client = await this.Gateway.GetTcpIpConnection();
			await Client.Enter();
			try
			{
				LinkedList<Field> Fields = new LinkedList<Field>();
				DateTime TP = DateTime.UtcNow;
				int Offset = this.MinInputRegisterNr;
				int StepSize = 64;
				int i;

				while ((StepSize = Math.Min(StepSize, this.MaxInputRegisterNr - Offset + 1)) > 0 && Offset <= this.MaxInputRegisterNr)
				{
					try
					{
						ushort[] Values = await Client.ReadInputRegisters((byte)this.UnitId, (ushort)Offset, (ushort)StepSize);
						StepSize = Math.Min(StepSize, Values.Length);

						for (i = 0; i < StepSize; i++)
							Fields.AddLast(new Int32Field(this, TP, "Input Register 3" + (Offset + i).ToString("D5"), Values[i], FieldType.Momentary, FieldQoS.AutomaticReadout));

						Request.ReportFields(false, Fields);
						Fields.Clear();
						Offset += StepSize;
					}
					catch (ModbusException)
					{
						StepSize >>= 1;
					}
				}

				Offset = this.MinHoldingRegisterNr;
				StepSize = 64;

				while ((StepSize = Math.Min(StepSize, this.MaxHoldingRegisterNr - Offset + 1)) > 0 && Offset <= this.MaxHoldingRegisterNr)
				{
					try
					{
						ushort[] Values = await Client.ReadMultipleRegisters((byte)this.UnitId, (ushort)Offset, (ushort)StepSize);
						StepSize = Math.Min(StepSize, Values.Length);

						for (i = 0; i < StepSize; i++)
							Fields.AddLast(new Int32Field(this, TP, "Holding Register 4" + (Offset + i).ToString("D5"), Values[i], FieldType.Momentary, FieldQoS.AutomaticReadout));

						Request.ReportFields(false, Fields);
						Fields.Clear();
						Offset += StepSize;
					}
					catch (ModbusException)
					{
						StepSize >>= 1;
					}
				}

				Offset = this.MinCoilNr;
				StepSize = 256;

				while ((StepSize = Math.Min(StepSize, this.MaxCoilNr - Offset + 1)) > 0 && Offset <= this.MaxCoilNr)
				{
					try
					{
						BitArray Bits = await Client.ReadCoils((byte)this.UnitId, (ushort)Offset, (ushort)StepSize);
						StepSize = Math.Min(StepSize, Bits.Length);

						for (i = 0; i < StepSize; i++)
							Fields.AddLast(new BooleanField(this, TP, "Coil 0" + (Offset + i).ToString("D5"), Bits[i], FieldType.Momentary, FieldQoS.AutomaticReadout));

						Request.ReportFields(false, Fields);
						Fields.Clear();
						Offset += StepSize;
					}
					catch (ModbusException)
					{
						StepSize >>= 1;
					}
				}

				Offset = this.MinDiscreteInputNr;
				StepSize = 256;

				while ((StepSize = Math.Min(StepSize, this.MaxDiscreteInputNr - Offset + 1)) > 0 && Offset <= this.MaxDiscreteInputNr)
				{
					try
					{
						BitArray Bits = await Client.ReadInputDiscretes((byte)this.UnitId, (ushort)Offset, (ushort)StepSize);
						StepSize = Math.Min(StepSize, Bits.Length);

						for (i = 0; i < StepSize; i++)
							Fields.AddLast(new BooleanField(this, TP, "Discrete Input 1" + (Offset + i).ToString("D5"), Bits[i], FieldType.Momentary, FieldQoS.AutomaticReadout));

						Request.ReportFields(false, Fields);
						Fields.Clear();
						Offset += StepSize;
					}
					catch (ModbusException)
					{
						StepSize >>= 1;
					}
				}

				Request.ReportFields(true);
			}
			catch (Exception ex)
			{
				Request.ReportErrors(true, new ThingError(this, ex.Message));
			}
			finally
			{
				Client.Leave();
			}
		}

	}
}