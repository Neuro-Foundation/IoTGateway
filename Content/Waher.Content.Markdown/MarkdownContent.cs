﻿using Waher.Content.Json;
using Waher.Runtime.Inventory;

namespace Waher.Content.Markdown
{
    /// <summary>
    /// Class that can be used to encapsulate Markdown to be returned from a Web Service, bypassing any encoding protections,
    /// and avoiding doubly parsing the Markdown.
    /// </summary>
    public class MarkdownContent : IJsonEncodingHint
	{
		private readonly string markdown;
		private readonly MarkdownSettings settings;

		/// <summary>
		/// Class that can be used to encapsulate Markdown to be returned from a Web Service, bypassing any encoding protections,
		/// and avoiding doubly parsing the Markdown.
		/// </summary>
		/// <param name="Markdown">Markdown content to return.</param>
		public MarkdownContent(string Markdown)
			: this(Markdown, null)
		{
		}

		/// <summary>
		/// Class that can be used to encapsulate Markdown to be returned from a Web Service, bypassing any encoding protections,
		/// and avoiding doubly parsing the Markdown.
		/// </summary>
		/// <param name="Markdown">Markdown content to return.</param>
		/// <param name="Settings">Markdown Settings.</param>
		public MarkdownContent(string Markdown, MarkdownSettings Settings)
		{
			this.markdown = Markdown;
			this.settings = Settings;
		}

		/// <summary>
		/// Markdown content.
		/// </summary>
		public string Markdown => this.markdown;

		/// <summary>
		/// Markdown settings, if provided.
		/// </summary>
		public MarkdownSettings Settings => this.settings;

		/// <summary>
		/// To what extent the object supports JSON encoding.
		/// </summary>
		public Grade CanEncodeJson => Grade.NotAtAll;
	}
}
