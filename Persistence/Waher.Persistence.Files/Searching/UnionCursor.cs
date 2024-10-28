﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence.Filters;
using Waher.Persistence.Serialization;

namespace Waher.Persistence.Files.Searching
{
	/// <summary>
	/// Provides a cursor that joins results from multiple cursors. It only returns an object once, regardless of how many times
	/// it appears in the different child cursors.
	/// </summary>
	/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
	internal class UnionCursor<T> : ICursor<T>
	{
		private readonly Dictionary<Guid, bool> returned = new Dictionary<Guid, bool>();
		private ICursor<T> currentCursor;
		private readonly Filter[] childFilters;
		private readonly ObjectBTreeFile file;
		private int currentCursorPosition = 0;
		private readonly int nrCursors;

		/// <summary>
		/// Provides a cursor that joins results from multiple cursors. It only returns an object once, regardless of how many times
		/// it appears in the different child cursors.
		/// </summary>
		/// <param name="ChildFilters">Child filters.</param>
		/// <param name="File">File being searched.</param>
		public UnionCursor(Filter[] ChildFilters, ObjectBTreeFile File)
		{
			this.childFilters = ChildFilters;
			this.nrCursors = this.childFilters.Length;
			this.file = File;
			this.currentCursor = null;
		}

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public T Current => this.CurrentCursor.Current;

		private ICursor<T> CurrentCursor
		{
			get
			{
				if (this.currentCursor is null)
					throw new InvalidOperationException("Enumeration not started or has already ended.");
				else
					return this.currentCursor;
			}
		}

		/// <summary>
		/// Serializer used to deserialize <see cref="Current"/>.
		/// </summary>
		public IObjectSerializer CurrentSerializer => this.CurrentCursor.CurrentSerializer;

		/// <summary>
		/// If the curent object is type compatible with <typeparamref name="T"/> or not. If not compatible, <see cref="Current"/> 
		/// will be null, even if there exists an object at the current position.
		/// </summary>
		public bool CurrentTypeCompatible => this.CurrentCursor.CurrentTypeCompatible;

		/// <summary>
		/// Gets the Object ID of the current object.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public Guid CurrentObjectId => this.CurrentCursor.CurrentObjectId;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.currentCursor = null;
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
		public void Reset()
		{
			this.currentCursor = null;
			this.currentCursorPosition = 0;
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public async Task<bool> MoveNextAsyncLocked()
		{
			Guid ObjectId;

			while (true)
			{
				if (this.currentCursor is null)
				{
					if (this.currentCursorPosition >= this.nrCursors)
						return false;

					this.currentCursor = await this.file.ConvertFilterToCursorLocked<T>(this.childFilters[this.currentCursorPosition++], null, true);
				}

				if (!await this.currentCursor.MoveNextAsyncLocked())
				{
					this.currentCursor = null;
					continue;
				}

				if (!this.currentCursor.CurrentTypeCompatible)
					continue;

				ObjectId = this.currentCursor.CurrentObjectId;
				if (this.returned.ContainsKey(ObjectId))
					continue;

				this.returned[ObjectId] = true;
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
			return this.MoveNextAsyncLocked();    // Union operator not ordered.
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
			return false;
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
			return false;
		}

		/// <summary>
		/// Continues operating after a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueAfterLocked(T LastItem)
		{
			throw new NotSupportedException("Paginated search is not supported for union queries.");
		}

		/// <summary>
		/// Continues operating before a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueBeforeLocked(T LastItem)
		{
			throw new NotSupportedException("Paginated search is not supported for union queries.");
		}
	}
}
