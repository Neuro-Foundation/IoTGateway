﻿using System.Text;

namespace Waher.Things.DisplayableParameters
{
	/// <summary>
	/// Int32-valued parameter.
	/// </summary>
	public class Int32Parameter : Parameter
	{
		private int value;

		/// <summary>
		/// Int32-valued parameter.
		/// </summary>
		public Int32Parameter()
			: base()
		{
			this.value = 0;
		}

		/// <summary>
		/// Int32-valued parameter.
		/// </summary>
		/// <param name="Id">Parameter ID.</param>
		/// <param name="Name">Parameter Name.</param>
		/// <param name="Value">Parameter Value</param>
		public Int32Parameter(string Id, string Name, int Value)
			: base(Id, Name)
		{
			this.value = Value;
		}

		/// <summary>
		/// Parameter Value.
		/// </summary>
		public int Value
		{
			get => this.value;
			set => this.value = value;
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
			Xml.Append("<int");
			base.Export(Xml);
			Xml.Append(" value='");
			Xml.Append(this.value.ToString());
			Xml.Append("'/>");
		}
	}
}
