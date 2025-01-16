﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Waher.Content.Xml;

namespace Waher.Content.Html
{
	/// <summary>
	/// In-line text.
	/// </summary>
    public class HtmlText : HtmlNode
    {
		private readonly string text;
		private bool? isWhiteSpace;

		/// <summary>
		/// In-line text.
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent node.</param>
		/// <param name="StartPosition">Start position.</param>
		/// <param name="EndPosition">End position.</param>
		/// <param name="Text">Text.</param>
		public HtmlText(HtmlDocument Document, HtmlNode Parent, int StartPosition, int EndPosition, string Text)
			: base(Document, Parent, StartPosition, EndPosition)
		{
			this.text = Text;
		}

		/// <summary>
		/// Inline text.
		/// </summary>
		public string InlineText => this.text;

		/// <summary>
		/// If the text consists only of white-space.
		/// </summary>
		public bool IsWhiteSpace
		{
			get
			{
				if (!this.isWhiteSpace.HasValue)
					this.isWhiteSpace = string.IsNullOrEmpty(this.text.Trim());

				return this.isWhiteSpace.Value;
			}
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.text;
		}

		/// <summary>
		/// Exports the HTML document to XML.
		/// </summary>
		/// <param name="Namespaces">Namespaces defined, by prefix.</param>
		/// <param name="Output">XML Output</param>
		public override void Export(XmlWriter Output, Dictionary<string, string> Namespaces)
		{
			Output.Flush();
			Output.WriteRaw(XML.Encode(this.text));
		}

        /// <summary>
        /// Exports the HTML document to XML.
        /// </summary>
        /// <param name="Output">XML Output</param>
        public override void Export(StringBuilder Output)
        {
            Output.Append(XML.Encode(this.text));
        }
    }
}
