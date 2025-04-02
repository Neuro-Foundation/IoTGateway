﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;

namespace Waher.Script.Xml.Model
{
	/// <summary>
	/// XML Script attribute node, whose value is defined by script.
	/// </summary>
	public class XmlScriptAttributeString : XmlScriptAttribute 
	{
		private readonly string value;

		/// <summary>
		/// XML Script attribute node, whose value is defined by script.
		/// </summary>
		/// <param name="Name">Element name.</param>
		/// <param name="Value">String value.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public XmlScriptAttributeString(string Name, string Value, int Start, int Length, Expression Expression)
			: base(Name, Start, Length, Expression)
		{
			this.value = Value;
		}

		/// <summary>
		/// Calls the callback method for all child nodes.
		/// </summary>
		/// <param name="Callback">Callback method to call.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <param name="Order">Order to traverse the nodes.</param>
		/// <returns>If the process was completed.</returns>
		public override bool ForAllChildNodes(ScriptNodeEventHandler Callback, object State, SearchMethod Order)
		{
			return true;
		}

		/// <summary>
		/// Builds an XML Document object
		/// </summary>
		/// <param name="Document">Document being built.</param>
		/// <param name="Parent">Parent element.</param>
		/// <param name="Variables">Current set of variables.</param>
		internal override void Build(XmlDocument Document, XmlElement Parent, Variables Variables)
		{
			Parent.SetAttribute(this.Name, this.value);
		}

		/// <summary>
		/// Gets the attribute value.
		/// </summary>
		/// <param name="Variables">Current set of variables.</param>
		internal override string GetValue(Variables Variables)
		{
			return this.value;
		}

		/// <summary>
		/// Gets the attribute value.
		/// </summary>
		/// <param name="Variables">Current set of variables.</param>
		internal override Task<string> GetValueAsync(Variables Variables)
		{
			return Task.FromResult<string>(this.value);
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(XmlNode CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			if (CheckAgainst is XmlAttribute)
				return CheckAgainst.Value == this.value ? PatternMatchResult.Match : PatternMatchResult.NoMatch;
			else
				return PatternMatchResult.NoMatch;
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(string CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			return CheckAgainst == this.value ? PatternMatchResult.Match : PatternMatchResult.NoMatch;
		}

		/// <summary>
		/// If the node is applicable in pattern matching against <paramref name="CheckAgainst"/>.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <returns>If the node is applicable for pattern matching.</returns>
		public override bool IsApplicable(string CheckAgainst)
		{
			return CheckAgainst == this.value;
		}
	}
}
