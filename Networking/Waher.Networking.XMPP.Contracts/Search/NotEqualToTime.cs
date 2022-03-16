﻿using System;

namespace Waher.Networking.XMPP.Contracts.Search
{
	/// <summary>
	/// Limits searches to items with values not equal to this value.
	/// </summary>
	public class NotEqualToTime : SearchFilterTimeOperand
	{
		/// <summary>
		/// Limits searches to items with values not equal to this value.
		/// </summary>
		/// <param name="Value">Time value</param>
		public NotEqualToTime(TimeSpan Value)
			: base(Value)
		{
		}

		/// <summary>
		/// Local XML element name of string operand.
		/// </summary>
		public override string OperandName => "neq";
	}
}
