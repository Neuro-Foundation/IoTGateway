﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Waher.Persistence.FullTextSearch.Keywords
{
	/// <summary>
	/// Represents a required keyword.
	/// </summary>
	public class RequiredKeyword : Keyword
	{
		/// <summary>
		/// Represents a required keyword.
		/// </summary>
		/// <param name="Keyword">Keyword</param>
		public RequiredKeyword(Keyword Keyword)
			: base()
		{
			this.Keyword = Keyword;
		}

		/// <summary>
		/// Keyword
		/// </summary>
		public Keyword Keyword { get; }

		/// <summary>
		/// If keyword is optional
		/// </summary>
		public override bool Optional => false;

		/// <summary>
		/// If keyword is required
		/// </summary>
		public override bool Required => true;

		/// <summary>
		/// Order category of keyword
		/// </summary>
		public override int OrderCategory => 1;

		/// <summary>
		/// Order complexity (within category) of keyword
		/// </summary>
		public override int OrderComplexity => this.Keyword.OrderComplexity;

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is RequiredKeyword k && this.Keyword.Equals(k.Keyword);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return "+" + this.Keyword.ToString();
		}

		/// <summary>
		/// Gets available token references.
		/// </summary>
		/// <param name="Index">Dictionary containing token references.</param>
		/// <returns>Enumerable set of token references.</returns>
		public override Task<IEnumerable<KeyValuePair<string, TokenReferences>>> GetTokenReferences(IPersistentDictionary Index)
		{
			return this.Keyword.GetTokenReferences(Index);
		}

		/// <summary>
		/// Processes the keyword in a search process.
		/// </summary>
		/// <param name="Process"></param>
		/// <returns>If the process can continue (true) or if an empty result is concluded (false).</returns>
		public override async Task<bool> Process(SearchProcess Process)
		{
			IEnumerable<KeyValuePair<string, TokenReferences>> Records = await this.GetTokenReferences(Process.Index);

			foreach (KeyValuePair<string, TokenReferences> Rec in Records)
			{
				string Token = Rec.Key;
				TokenReferences References = Rec.Value;

				int j, d = References.ObjectReferences.Length;

				for (j = 0; j < d; j++)
				{
					ulong ObjectReference = References.ObjectReferences[j];

					if (Process.IsRestricted)
						Process.Found[ObjectReference] = true;

					if (!Process.ReferencesByObject.TryGetValue(ObjectReference, out LinkedList<TokenReference> ByObjectReference))
					{
						if (Process.IsRestricted)
							continue;

						ByObjectReference = new LinkedList<TokenReference>();
						Process.ReferencesByObject[ObjectReference] = ByObjectReference;
					}

					ByObjectReference.AddLast(new TokenReference()
					{
						Count = References.Counts[j],
						LastBlock = References.LastBlock,
						ObjectReference = ObjectReference,
						Timestamp = References.Timestamps[j],
						Token = Token
					});
				}
			}

			if (Process.IsRestricted)
			{
				LinkedList<ulong> ToRemove = null;

				foreach (ulong ObjectReference in Process.ReferencesByObject.Keys)
				{
					if (!Process.Found.ContainsKey(ObjectReference))
					{
						if (ToRemove is null)
							ToRemove = new LinkedList<ulong>();

						ToRemove.AddLast(ObjectReference);
					}
				}

				if (!(ToRemove is null))
				{
					foreach (ulong ObjectReference in ToRemove)
						Process.ReferencesByObject.Remove(ObjectReference);
				}
			}

			if (Process.ReferencesByObject.Count == 0)
				return false;

			Process.IncRestricted();

			return true;
		}

	}
}
