﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Persistence.Filters;

namespace Waher.Runtime.Language
{
	/// <summary>
	/// Contains information about a namespace in a language.
	/// </summary>
	[CollectionName("LanguageNamespaces")]
	[Index("LanguageId", "Name")]
	[TypeName(TypeNameSerialization.None)]
	public class Namespace
	{
		private readonly SortedDictionary<string, LanguageString> stringsById = new SortedDictionary<string, LanguageString>();
		private readonly object synchObject = new object();
		private Guid objectId = Guid.Empty;
		private Guid languageId = Guid.Empty;
		private string name = string.Empty;
		private bool stringsLoaded = false;

		/// <summary>
		/// Contains information about a namespace in a language.
		/// </summary>
		public Namespace()
		{
		}

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public Guid ObjectId
		{
			get => this.objectId;
			set => this.objectId = value;
		}

		/// <summary>
		/// Language ID.
		/// </summary>
		public Guid LanguageId
		{
			get => this.languageId;
			set => this.languageId = value;
		}

		/// <summary>
		/// Namespace.
		/// </summary>
		public string Name
		{
			get => this.name;
			set => this.name = value;
		}

		/// <summary>
		/// Gets the string object, given its ID, if available.
		/// </summary>
		/// <param name="Id">String ID.</param>
		/// <returns>String object, if found, or null if not found.</returns>
		public Task<LanguageString> GetStringAsync(int Id)
		{
			return this.GetStringAsync(Id.ToString());
		}

		/// <summary>
		/// Gets the string object, given its ID, if available.
		/// </summary>
		/// <param name="Id">String ID.</param>
		/// <returns>String object, if found, or null if not found.</returns>
		public async Task<LanguageString> GetStringAsync(string Id)
		{
			lock (this.synchObject)
			{
				if (this.stringsById.TryGetValue(Id, out LanguageString Result))
					return Result;
			}

			LanguageString LanguageString = await Database.FindFirstIgnoreRest<LanguageString>(new FilterAnd(
				new FilterFieldEqualTo("NamespaceId", this.objectId), new FilterFieldEqualTo("Id", Id)));

			if (!(LanguageString is null))
			{
				lock (this.synchObject)
				{
					if (this.stringsById.TryGetValue(Id, out LanguageString Result))
						return Result;
					else
						this.stringsById[LanguageString.Id] = LanguageString;
				}
			}

			return LanguageString;
		}

		/// <summary>
		/// Gets the string value of a string ID. If no such string exists, a string is created with the default value.
		/// </summary>
		/// <param name="Id">String ID</param>
		/// <param name="Default">Default (untranslated) string.</param>
		/// <returns>Localized string.</returns>
		public Task<string> GetStringAsync(int Id, string Default)
		{
			return this.GetStringAsync(Id.ToString(), Default);
		}

		/// <summary>
		/// Gets the string value of a string ID. If no such string exists, a string is created with the default value.
		/// </summary>
		/// <param name="Id">String ID</param>
		/// <param name="Default">Default (untranslated) string.</param>
		/// <returns>Localized string.</returns>
		public async Task<string> GetStringAsync(string Id, string Default)
		{
			LanguageString StringObj = await this.GetStringAsync(Id);
			if (!(StringObj is null))
				return StringObj.Value;

			StringObj = await this.CreateStringAsync(Id, Default, TranslationLevel.Untranslated);

			return StringObj.Value;
		}

		/// <summary>
		/// Gets available strings.
		/// </summary>
		/// <returns>Strings.</returns>
		public async Task<LanguageString[]> GetStringsAsync()
		{
			if (!this.stringsLoaded)
			{
				IEnumerable<LanguageString> Strings = await Database.Find<LanguageString>(
					new FilterFieldEqualTo("NamespaceId", this.objectId));

				lock (this.synchObject)
				{
					foreach (LanguageString LanguageString in Strings)
					{
						if (!this.stringsById.ContainsKey(LanguageString.Id))
							this.stringsById[LanguageString.Id] = LanguageString;
					}
				}

				this.stringsLoaded = true;
			}

			lock (this.synchObject)
			{
				LanguageString[] Result = new LanguageString[this.stringsById.Count];
				this.stringsById.Values.CopyTo(Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Creates a new language string, or updates an existing language string, if one exist with the same properties.
		/// </summary>
		/// <param name="Id">String ID</param>
		/// <param name="Value">Localized value.</param>
		/// <param name="Level">Translation level of the string.</param>
		/// <returns>Namespace object.</returns>
		public async Task<LanguageString> CreateStringAsync(string Id, string Value, TranslationLevel Level)
		{
			LanguageString Result = await this.GetStringAsync(Id);
			if (!(Result is null))
			{
				if (Result.Value != Value && (Level >= Result.Level))
				{
					Result.Value = Value;
					Result.Level = Level;

					await Database.Update(Result);
				}

				return Result;
			}
			else
			{
				bool Insert = false;

				lock (this.synchObject)
				{
					if (this.stringsById.TryGetValue(Id, out Result))
					{
						if (Result.Value == Value || Result.Level > Level)
							return Result;

						Result.Value = Value;
						Result.Level = Level;
					}
					else
					{
						Result = new LanguageString()
						{
							NamespaceId = this.objectId,
							Id = Id,
							Value = Value,
							Level = Level
						};

						this.stringsById[Id] = Result;
						Insert = true;
					}
				}

				if (Insert)
					await Database.Insert(Result);
				else
					await Database.Update(Result);

				return Result;
			}
		}

		/// <summary>
		/// Deletes a string from the namespace.
		/// </summary>
		/// <param name="Id">String ID.</param>
		public async Task DeleteStringAsync(string Id)
		{
			LanguageString s = await this.GetStringAsync(Id);
			if (!(s is null))
			{
				await Database.Delete(s);

				lock (this.synchObject)
				{
					this.stringsById.Remove(Id);
				}
			}
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.name;
		}

	}
}
