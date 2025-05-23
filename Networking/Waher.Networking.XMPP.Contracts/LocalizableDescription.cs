﻿using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts.HumanReadable;

namespace Waher.Networking.XMPP.Contracts
{
	/// <summary>
	/// Abstract base class for contractual parameters
	/// </summary>
	public abstract class LocalizableDescription
	{
		private HumanReadableText[] descriptions = null;

		/// <summary>
		/// Discriptions of the object, in different languages.
		/// </summary>
		public HumanReadableText[] Descriptions
		{
			get => this.descriptions;
			set => this.descriptions = value;
		}

		/// <summary>
		/// Creates a human-readable Markdown document for the contract.
		/// </summary>
		/// <param name="Language">Desired language</param>
		/// <param name="Contract">Contract hosting the object.</param>
		/// <returns>Markdown</returns>
		public Task<string> ToMarkdown(string Language, Contract Contract)
		{
			return this.ToMarkdown(Language, Contract, MarkdownType.ForRendering);
		}

		/// <summary>
		/// Creates a human-readable Markdown document for the contract.
		/// </summary>
		/// <param name="Language">Desired language</param>
		/// <param name="Contract">Contract hosting the object.</param>
		/// <param name="Type">Type of Markdown being generated. (Default=<see cref="MarkdownType.ForRendering"/>)</param>
		/// <returns>Markdown</returns>
		public Task<string> ToMarkdown(string Language, Contract Contract, MarkdownType Type)
		{
			return Contract.ToMarkdown(this.descriptions, Language, Type);
		}

		/// <summary>
		/// Creates a human-readable HTML document for the contract.
		/// </summary>
		/// <param name="Language">Desired language</param>
		/// <param name="Contract">Contract hosting the object.</param>
		/// <returns>Markdown</returns>
		public Task<string> ToHTML(string Language, Contract Contract)
		{
			return Contract.ToHTML(this.descriptions, Language);
		}

		/// <summary>
		/// Creates a human-readable Plain Trext document for the contract.
		/// </summary>
		/// <param name="Language">Desired language</param>
		/// <param name="Contract">Contract hosting the object.</param>
		/// <returns>Markdown</returns>
		public Task<string> ToPlainText(string Language, Contract Contract)
		{
			return Contract.ToPlainText(this.descriptions, Language);
		}
	}
}
