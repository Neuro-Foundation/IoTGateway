﻿using System.Text;
using System.Threading.Tasks;
using Waher.Content.Xml;

namespace Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements
{
	/// <summary>
	/// Text
	/// </summary>
	public class Text : InlineElement
	{
		private string @value;

		/// <summary>
		/// Text
		/// </summary>
		public string Value
		{
			get => this.@value;
			set => this.@value = value;
		}

		/// <summary>
		/// Checks if the element is well-defined.
		/// </summary>
		/// <returns>Returns first failing element, if found.</returns>
		public override Task<HumanReadableElement> IsWellDefined()
		{
			return Task.FromResult<HumanReadableElement>(string.IsNullOrEmpty(this.@value) ? this : null);
		}

		/// <summary>
		/// Serializes the element in normalized form.
		/// </summary>
		/// <param name="Xml">XML Output.</param>
		public override void Serialize(StringBuilder Xml)
		{
			Xml.Append("<text>");
			Xml.Append(XML.Encode(this.@value));
			Xml.Append("</text>");
		}

		/// <summary>
		/// Generates markdown for the human-readable text.
		/// </summary>
		/// <param name="Markdown">Markdown output.</param>
		/// <param name="SectionLevel">Current section level.</param>
		/// <param name="Indentation">Current indentation.</param>
		/// <param name="Settings">Settings used for Markdown generation of human-readable text.</param>
		public override Task GenerateMarkdown(MarkdownOutput Markdown, int SectionLevel, int Indentation, MarkdownSettings Settings)
		{
			Markdown.Append(MarkdownEncode(this.@value, Settings.SimpleEscape));
			return Task.CompletedTask;
		}
	}
}
