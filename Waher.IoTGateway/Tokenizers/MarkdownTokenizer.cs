﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Rendering;
using Waher.Persistence.FullTextSearch;
using Waher.Persistence.FullTextSearch.Files;
using Waher.Persistence.FullTextSearch.Tokenizers;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.IO;

namespace Waher.IoTGateway.Tokenizers
{
	/// <summary>
	/// Tokenizes contents defined in a Markdown document.
	/// </summary>
	public class MarkdownTokenizer : ITokenizer, IFileTokenizer, IPropertyEvaluator
	{
		private string definition = string.Empty;

		/// <summary>
		/// Tokenizes contents defined in a Markdown document.
		/// </summary>
		public MarkdownTokenizer()
		{
		}

		/// <summary>
		/// If the interface understands objects such as <paramref name="Type"/>.
		/// </summary>
		/// <param name="Type">Object</param>
		/// <returns>How well objects of this type are supported.</returns>
		public Grade Supports(Type Type)
		{
			if (Type == typeof(MarkdownDocument))
				return Grade.Ok;
			else
				return Grade.NotAtAll;
		}

		/// <summary>
		/// How well the file tokenizer supports files of a given extension.
		/// </summary>
		/// <param name="Extension">File extension (in lower case).</param>
		/// <returns>How well the tokenizer supports files having this extension.</returns>
		public Grade Supports(string Extension)
		{
			if (Extension == "md")
				return Grade.Ok;
			else
				return Grade.NotAtAll;
		}

		/// <summary>
		/// Tokenizes an object.
		/// </summary>
		/// <param name="Value">Object to tokenize.</param>
		/// <param name="Process">Current tokenization process.</param>
		public async Task Tokenize(object Value, TokenizationProcess Process)
		{
			if (Value is MarkdownDocument Doc)
				await Tokenize(Doc, Process);
		}

		/// <summary>
		/// Tokenizes a Markdown document.
		/// </summary>
		/// <param name="Doc">Document to tokenize.</param>
		/// <param name="Process">Current tokenization process.</param>
		public static async Task Tokenize(MarkdownDocument Doc, TokenizationProcess Process)
		{
			StringBuilder sb = new StringBuilder();

			Append(sb, Doc.Description);
			Append(sb, Doc.Author);
			Append(sb, Doc.Keywords);
			Append(sb, Doc.Subtitle);
			Append(sb, Doc.Title);

			using (TextRenderer Renderer = new TextRenderer(sb))
			{
				await Renderer.RenderDocument(Doc, true);
			}

			StringTokenizer.Tokenize(sb.ToString(), Process);
		}

		private static void Append(StringBuilder sb, string[] Strings)
		{
			if (!(Strings is null))
			{
				foreach (string s in Strings)
					sb.AppendLine(s);
			}
		}

		/// <summary>
		/// Tokenizes an object.
		/// </summary>
		/// <param name="Reference">Reference to file to tokenize.</param>
		/// <param name="Process">Current tokenization process.</param>
		public async Task Tokenize(FileReference Reference, TokenizationProcess Process)
		{
			string Text = await Files.ReadAllTextAsync(Reference.FileName);
			MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Text);

			await Tokenize(Doc, Process);
		}

		#region IPropertyEvaluator

		/// <summary>
		/// Prepares the evaluator with its definition.
		/// </summary>
		/// <param name="Definition">Property definition</param>
		public Task Prepare(string Definition)
		{
			this.definition = Definition;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Evaluates the property evaluator, on an object instance.
		/// </summary>
		/// <param name="Instance">Object instance being indexed.</param>
		/// <returns>Property value.</returns>
		public async Task<object> Evaluate(object Instance)
		{
			if (Instance is GenericObject Obj)
			{
				if (Obj.TryGetFieldValue(this.definition, out object Obj2) &&
					Obj2 is string Markdown)
				{
					MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown);

					if (MarkdownDocument.HeaderEndPosition(Markdown).HasValue)
						return Doc;
					else
						return await Doc.GeneratePlainText();
				}
			}

			return null;
		}

		#endregion
	}
}
