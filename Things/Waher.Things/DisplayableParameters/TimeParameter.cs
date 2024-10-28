﻿using System;
using System.Text;

namespace Waher.Things.DisplayableParameters
{
	/// <summary>
	/// TimeSpan-valued parameter.
	/// </summary>
	public class TimeSpanParameter : Parameter
	{
		private TimeSpan value;

		/// <summary>
		/// TimeSpan-valued parameter.
		/// </summary>
		public TimeSpanParameter()
			: base()
		{
			this.value = TimeSpan.Zero;
		}

		/// <summary>
		/// TimeSpan-valued parameter.
		/// </summary>
		/// <param name="Id">Parameter ID.</param>
		/// <param name="Name">Parameter Name.</param>
		/// <param name="Value">Parameter Value</param>
		public TimeSpanParameter(string Id, string Name, TimeSpan Value)
			: base(Id, Name)
		{
			this.value = Value;
		}

		/// <summary>
		/// Parameter Value.
		/// </summary>
		public TimeSpan Value
		{
			get => this.value;
			set { this.value = value; }
		}

		/// <summary>
		/// Untyped parameter value
		/// </summary>
		public override object UntypedValue => this.value;

		/// <summary>
		/// Exports the parameters to XML.
		/// </summary>
		/// <param name="Xml">XML Output.</param>
		public override void Export(StringBuilder Xml)
		{
			Xml.Append("<time");
			base.Export(Xml);
			Xml.Append(" value='");
			Xml.Append(this.value.ToString());
			Xml.Append("'/>");
		}
	}
}
