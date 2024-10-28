﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using Waher.Persistence.Files.Storage;
using System.Collections;

namespace Waher.Persistence.Files.Searching
{
	/// <summary>
	/// Provides a cursor into a sorted set of objects.
	/// </summary>
	/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
	internal class SortedCursor<T> : ICursor<T>
	{
		private readonly SortedDictionary<SortedReference<T>, bool> sortedObjects;
		private readonly IndexRecords recordHandler;
		private SortedDictionary<SortedReference<T>, bool>.KeyCollection.Enumerator e;
		private bool initialized = false;

		/// <summary>
		/// Provides a cursor into a sorted set of objects.
		/// </summary>
		/// <param name="SortedObjects">Sorted set of objects.</param>
		/// <param name="RecordHandler">Record handler.</param>
		internal SortedCursor(SortedDictionary<SortedReference<T>, bool> SortedObjects, IndexRecords RecordHandler)
		{
			this.sortedObjects = SortedObjects;
			this.recordHandler = RecordHandler;
		}

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public T Current => this.e.Current.Value;

		/// <summary>
		/// Serializer used to deserialize <see cref="Current"/>.
		/// </summary>
		public IObjectSerializer CurrentSerializer => this.e.Current.Serializer;

		/// <summary>
		/// If the curent object is type compatible with <typeparamref name="T"/> or not. If not compatible, <see cref="Current"/> 
		/// will be null, even if there exists an object at the current position.
		/// </summary>
		public bool CurrentTypeCompatible => this.e.Current.Value != null;

		/// <summary>
		/// Gets the Object ID of the current object.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public Guid CurrentObjectId => this.e.Current.ObjectId;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.e.Dispose();
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
			this.e = this.sortedObjects.Keys.GetEnumerator();
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public Task<bool> MoveNextAsyncLocked()
		{
			if (!this.initialized)
			{
				this.e = this.sortedObjects.Keys.GetEnumerator();
				this.initialized = true;
			}

			return Task.FromResult(this.e.MoveNext());
		}

		/// <summary>
		/// Advances the enumerator to the previous element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the previous element; false if
		/// the enumerator has passed the beginning of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public Task<bool> MovePreviousAsyncLocked()
		{
			return this.MoveNextAsyncLocked();	// Ordering only in one direction.
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
			return this.recordHandler.SameSortOrder(ConstantFields, SortOrder);
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
			return this.recordHandler.ReverseSortOrder(ConstantFields, SortOrder);
		}

		/// <summary>
		/// Continues operating after a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueAfterLocked(T LastItem)
		{
			throw new NotSupportedException("Paginated search is not supported for query with post-load sort process. Consider using an index.");
		}

		/// <summary>
		/// Continues operating before a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueBeforeLocked(T LastItem)
		{
			throw new NotSupportedException("Paginated search is not supported for query with post-load sort process. Consider using an index.s");
		}
	}
}
