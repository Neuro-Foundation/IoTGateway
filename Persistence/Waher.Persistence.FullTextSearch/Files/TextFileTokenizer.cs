﻿using System.Threading.Tasks;
using Waher.Persistence.FullTextSearch.Tokenizers;
using Waher.Runtime.Inventory;

namespace Waher.Persistence.FullTextSearch.Files
{
	/// <summary>
	/// Tokenizes text files.
	/// </summary>
	public class TextFileTokenizer : IFileTokenizer
	{
		/// <summary>
		/// Tokenizes text files.
		/// </summary>
		public TextFileTokenizer()
		{
		}

		/// <summary>
		/// How well the file tokenizer supports files of a given extension.
		/// </summary>
		/// <param name="Extension">File extension (in lower case).</param>
		/// <returns>How well the tokenizer supports files having this extension.</returns>
		public Grade Supports(string Extension)
		{
			if (Extension == "txt")
				return Grade.Ok;
			else
				return Grade.NotAtAll;
		}

		/// <summary>
		/// Tokenizes an object.
		/// </summary>
		/// <param name="Reference">Reference to file to tokenize.</param>
		/// <param name="Process">Current tokenization process.</param>
		public async Task Tokenize(FileReference Reference, TokenizationProcess Process)
		{
			string Text = await Runtime.IO.Files.ReadAllTextAsync(Reference.FileName);
			StringTokenizer.Tokenize(Text, Process);
		}
	}
}
