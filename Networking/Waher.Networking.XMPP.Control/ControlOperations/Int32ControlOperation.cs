﻿using System.Threading.Tasks;
using Waher.Things.ControlParameters;
using Waher.Things;
using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP.Control.ControlOperations
{
	/// <summary>
	/// Int32 control operation.
	/// </summary>
	public class Int32ControlOperation : ControlOperation
	{
		private readonly Int32ControlParameter parameter;
		private readonly int value;

		/// <summary>
		/// Int32 control operation.
		/// </summary>
		/// <param name="Node">Node on which operation is to be performed.</param>
		/// <param name="Parameter">Control parameter.</param>
		/// <param name="Value">Value to set.</param>
		/// <param name="request">Original request.</param>
		public Int32ControlOperation(IThingReference Node, Int32ControlParameter Parameter, int Value, IqEventArgs request)
			: base(Node, request, Parameter)
		{
			this.parameter = Parameter;
			this.value = Value;
		}

		/// <summary>
		/// Control parameter
		/// </summary>
		public Int32ControlParameter Parameter => this.parameter;

		/// <summary>
		/// Value to set.
		/// </summary>
		public int Value => this.value;

		/// <summary>
		/// Performs the control operation.
		/// </summary>
		/// <returns>If the operation was successful or not.</returns>
		public override async Task<bool> Set()
		{
			bool Result = await this.parameter.Set(this.Node, this.value);

			if (!Result)
				await ControlServer.ParameterValueInvalid(this.parameter.Name, this.Request);

			return Result;
		}
	}
}
