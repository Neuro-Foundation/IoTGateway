﻿namespace Waher.Networking.XMPP.Contracts.Search
{
	/// <summary>
	/// Limits searches to items with values lesser than or equal to this value.
	/// </summary>
	public class LesserThanOrEqualToNumber : SearchFilterNumberOperand
	{
		/// <summary>
		/// Limits searches to items with values lesser than or equal to this value.
		/// </summary>
		/// <param name="Value">Number value</param>
		public LesserThanOrEqualToNumber(decimal Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Local XML element name of string operand.
		/// </summary>
		public override string OperandName => "lte";
	}
}
