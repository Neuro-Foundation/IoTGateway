﻿using System.Text;
using Waher.Content.Xml;

namespace Waher.Networking.XMPP.Contracts.Search
{
	/// <summary>
	/// Abstract base class for String-valued Smart Contract Search filter operands.
	/// </summary>
	public abstract class SearchFilterStringOperand : SearchFilterOperand
	{
		private readonly string value;

		/// <summary>
		/// Abstract base class for String-valued Smart Contract Search filter operands.
		/// </summary>
		/// <param name="Value">String value</param>
		public SearchFilterStringOperand(string Value)
			: base()
		{
			this.value = Value;
		}

		/// <summary>
		/// String value.
		/// </summary>
		public string Value => this.value;

		/// <summary>
		/// Local XML element suffix.
		/// </summary>
		public override string ElementSuffix => "Str";

		/// <summary>
		/// Serializes the search filter to XML.
		/// </summary>
		/// <param name="Xml">XML output.</param>
		/// <param name="ElementSuffix">Suffix, to append to element name during serialization.</param>
		public override void Serialize(StringBuilder Xml, string ElementSuffix)
		{
			Xml.Append('<');
			Xml.Append(this.OperandName);
			Xml.Append(ElementSuffix);
			Xml.Append('>');
			Xml.Append(XML.Encode(this.value));
			Xml.Append("</");
			Xml.Append(this.OperandName);
			Xml.Append(ElementSuffix);
			Xml.Append('>');
		}

	}
}
