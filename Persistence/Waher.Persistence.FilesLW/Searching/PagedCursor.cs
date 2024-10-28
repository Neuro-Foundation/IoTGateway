﻿using System;
using System.Collections;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;

namespace Waher.Persistence.Files.Searching
{
	/// <summary>
	/// Provides a cursor into a paged set of objects.
	/// </summary>
	/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
	internal class PagesCursor<T> : ICursor<T>
	{
		private int offset;
		private int maxCount;
		private readonly ICursor<T> cursor;

		/// <summary>
		/// Provides a cursor into a paged set of objects.
		/// </summary>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Cursor">Cursor to underlying result set.</param>
		internal PagesCursor(int Offset, int MaxCount, ICursor<T> Cursor)
		{
			this.offset = Offset;
			this.maxCount = MaxCount;
			this.cursor = Cursor;
		}

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public T Current => this.cursor.Current;

		/// <summary>
		/// Serializer used to deserialize <see cref="Current"/>.
		/// </summary>
		public IObjectSerializer CurrentSerializer => this.cursor.CurrentSerializer;

		/// <summary>
		/// If the curent object is type compatible with <typeparamref name="T"/> or not. If not compatible, <see cref="Current"/> 
		/// will be null, even if there exists an object at the current position.
		/// </summary>
		public bool CurrentTypeCompatible => this.cursor.CurrentTypeCompatible;

		/// <summary>
		/// Gets the Object ID of the current object.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public Guid CurrentObjectId => this.cursor.CurrentObjectId;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// Note: Enumerator only works if object is locked.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		Task<bool> IAsyncEnumerator.MoveNextAsync() => this.MoveNextAsyncLocked();

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		object IEnumerator.Current => this.Current;

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// Note: Enumerator only works if object is locked.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public bool MoveNext() => this.MoveNextAsyncLocked().Result;

		/// <summary>
		/// Resets the enumerator.
		/// </summary>
		public void Reset() => this.cursor.Reset();

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public async Task<bool> MoveNextAsyncLocked()
		{
			while (true)
			{
				if (this.maxCount <= 0)
					return false;

				if (!await this.cursor.MoveNextAsyncLocked())
					return false;

				if (!this.cursor.CurrentTypeCompatible)
					continue;

				if (this.offset > 0)
				{
					this.offset--;
					continue;
				}

				this.maxCount--;
				return true;
			}
		}

		/// <summary>
		/// Advances the enumerator to the previous element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the previous element; false if
		/// the enumerator has passed the beginning of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public Task<bool> MovePreviousAsyncLocked()
		{
			return this.MoveNextAsyncLocked();    // Ordering only in one direction.
		}

		/// <summary>
		/// If the index ordering corresponds to a given sort order.
		/// </summary>
		/// <param name="ConstantFields">Optional array of names of fields that will be constant during the enumeration.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>If the index matches the sort order. (The index ordering is allowed to be more specific.)</returns>
		public bool SameSortOrder(string[] ConstantFields, string[] SortOrder)
		{
			return this.cursor.SameSortOrder(ConstantFields, SortOrder);
		}

		/// <summary>
		/// If the index ordering is a reversion of a given sort order.
		/// </summary>
		/// <param name="ConstantFields">Optional array of names of fields that will be constant during the enumeration.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>If the index matches the sort order. (The index ordering is allowed to be more specific.)</returns>
		public bool ReverseSortOrder(string[] ConstantFields, string[] SortOrder)
		{
			return this.cursor.ReverseSortOrder(ConstantFields, SortOrder);
		}

		/// <summary>
		/// Continues operating after a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueAfterLocked(T LastItem)
		{
			return this.cursor.ContinueAfterLocked(LastItem);
		}

		/// <summary>
		/// Continues operating before a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueBeforeLocked(T LastItem)
		{
			return this.cursor.ContinueBeforeLocked(LastItem);
		}
	}
}
