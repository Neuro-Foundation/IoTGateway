﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Collections;

namespace Waher.Persistence.Files
{
	internal class KeyCollection : ICollection<string>
	{
		private readonly StringDictionary dictionary;

		public KeyCollection(StringDictionary Dictionary)
		{
			this.dictionary = Dictionary;
		}

		public int Count => this.dictionary.Count;

		public bool IsReadOnly => true;

		public void Add(string item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}

		public void Clear()
		{
			throw new NotSupportedException("Collection is read-only.");
		}

		public bool Contains(string item)
		{
			return this.dictionary.ContainsKey(item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			Task Task = this.CopyToAsync(array, arrayIndex);
			FilesProvider.Wait(Task, this.dictionary.DictionaryFile.TimeoutMilliseconds);
		}

		/// <summary>
		/// Copies the keys of the dicitionary to an array.
		/// </summary>
		/// <param name="array">Array</param>
		/// <param name="arrayIndex">Start index</param>
		public async Task CopyToAsync(string[] array, int arrayIndex)
		{
			await this.dictionary.DictionaryFile.BeginRead();
			try
			{
				ObjectBTreeFileCursor<KeyValuePair<string, object>> e = await this.dictionary.GetEnumeratorLocked();

				while (await e.MoveNextAsyncLocked())
					array[arrayIndex++] = e.Current.Key;
			}
			finally
			{
				await this.dictionary.DictionaryFile.EndRead();
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			return new KeyEnumeration(this.dictionary.GetEnumerator());
		}

		public bool Remove(string item)
		{
			return this.dictionary.Remove(item);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new KeyEnumeration(this.dictionary.GetEnumerator());
		}

		/// <summary>
		/// Gets all keys.
		/// </summary>
		/// <returns>Array of keys.</returns>
		public async Task<string[]> GetKeysAsync()
		{
			ChunkedList<string> Result = new ChunkedList<string>();

			await this.dictionary.DictionaryFile.BeginRead();
			try
			{
				ObjectBTreeFileCursor<KeyValuePair<string, object>> e = await this.dictionary.GetEnumeratorLocked();

				while (await e.MoveNextAsyncLocked())
					Result.Add(e.Current.Key);
			}
			finally
			{
				await this.dictionary.DictionaryFile.EndRead();
			}

			return Result.ToArray();
		}

	}
}
