﻿using System.Text;
using Waher.Content.Xml;

namespace Waher.Things.DisplayableParameters
{
	/// <summary>
	/// String-valued parameter.
	/// </summary>
	public class StringParameter : Parameter
	{
		private string value;

		/// <summary>
		/// String-valued parameter.
		/// </summary>
		public StringParameter()
			: base()
		{
			this.value = null;
		}

		/// <summary>
		/// String-valued parameter.
		/// </summary>
		/// <param name="Id">Parameter ID.</param>
		/// <param name="Name">Parameter Name.</param>
		/// <param name="Value">Parameter Value</param>
		public StringParameter(string Id, string Name, string Value)
			: base(Id, Name)
		{
			this.value = Value;
		}

		/// <summary>
		/// Parameter Value.
		/// </summary>
		public string Value
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
			Xml.Append("<string");
			base.Export(Xml);
			Xml.Append(" value='");
			Xml.Append(XML.Encode(this.value));
			Xml.Append("'/>");
		}
	}
}
