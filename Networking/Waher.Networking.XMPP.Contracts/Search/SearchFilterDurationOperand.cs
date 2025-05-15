﻿using System.Text;
using Waher.Content;

namespace Waher.Networking.XMPP.Contracts.Search
{
	/// <summary>
	/// Abstract base class for <see cref="Duration"/>-valued Smart Contract Search filter operands.
	/// </summary>
	public abstract class SearchFilterDurationOperand : SearchFilterOperand
	{
		private readonly Duration value;

		/// <summary>
		/// Abstract base class for <see cref="Duration"/>-valued Smart Contract Search filter operands.
		/// </summary>
		/// <param name="Value">String value</param>
		public SearchFilterDurationOperand(Duration Value)
			: base()
		{
			this.value = Value;
		}

		/// <summary>
		/// <see cref="Duration"/> value.
		/// </summary>
		public Duration Value => this.value;

		/// <summary>
		/// Local XML element suffix.
		/// </summary>
		public override string ElementSuffix => "Dr";

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
			Xml.Append(this.value.ToString());
			Xml.Append("</");
			Xml.Append(this.OperandName);
			Xml.Append(ElementSuffix);
			Xml.Append('>');
		}

	}
}
