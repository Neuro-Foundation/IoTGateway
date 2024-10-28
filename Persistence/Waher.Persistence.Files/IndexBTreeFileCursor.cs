﻿using System;
using System.Collections;
using System.Threading.Tasks;
using Waher.Persistence.Exceptions;
using Waher.Persistence.Serialization;
using Waher.Persistence.Files.Storage;
using System.Runtime.ExceptionServices;

namespace Waher.Persistence.Files
{
	/// <summary>
	/// Enumerates object in a <see cref="ObjectBTreeFile"/> in GUID order. You can use the enumerator to enumerate objects
	/// forwards and backwards, as well as skip a given number of objects.
	/// </summary>
	public class IndexBTreeFileCursor<T> : ICursor<T>
	{
		private ObjectBTreeFileCursor<object> e;
		private IObjectSerializer currentSerializer;
		private FilesProvider provider;
		private IndexBTreeFile file;
		private IndexRecords recordHandler;
		private Guid currentObjectId;
		private T current;
		private bool hasCurrent;
		private bool currentTypeCompatible;

		internal static async Task<IndexBTreeFileCursor<T>> CreateLocked(IndexBTreeFile File, IndexRecords RecordHandler)
		{
			return new IndexBTreeFileCursor<T>()
			{
				file = File,
				recordHandler = RecordHandler,
				provider = File.Provider,
				hasCurrent = false,
				currentObjectId = Guid.Empty,
				current = default,
				currentSerializer = null,
				e = await File.GetCursor(RecordHandler)
			};
		}

		internal void SetStartingPoint(BlockInfo StartingPoint)
		{
			this.e.SetStartingPoint(StartingPoint);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.e?.Dispose();
			this.e = null;
		}

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public T Current
		{
			get
			{
				if (this.hasCurrent)
					return this.current;
				else
					throw new InvalidOperationException("Enumeration not started. Call MoveNext() first.");
			}
		}

		/// <summary>
		/// If the curent object is type compatible with <typeparamref name="T"/> or not. If not compatible, <see cref="Current"/> 
		/// will be null, even if there exists an object at the current position.
		/// </summary>
		public bool CurrentTypeCompatible
		{
			get
			{
				if (this.hasCurrent)
					return this.currentTypeCompatible;
				else
					throw new InvalidOperationException("Enumeration not started. Call MoveNext() first.");
			}
		}

		/// <summary>
		/// Gets the Object ID of the current object.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public Guid CurrentObjectId
		{
			get
			{
				if (this.hasCurrent)
					return this.currentObjectId;
				else
					throw new InvalidOperationException("Enumeration not started. Call MoveNext() first.");
			}
		}

		/// <summary>
		/// Gets the rank of the current object.
		/// </summary>
		public Task<ulong> GetCurrentRankLocked()
		{
			return this.e.GetCurrentRankLocked();
		}

		/// <summary>
		/// Serializer used to deserialize <see cref="Current"/>.
		/// </summary>
		public IObjectSerializer CurrentSerializer => this.currentSerializer;

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
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public async Task<bool> MoveNextAsyncLocked()
		{
			if (!await this.e.MoveNextAsyncLocked())
			{
				this.Reset();
				return false;
			}

			await this.LoadObject();

			return true;
		}

		private async Task LoadObject()
		{
			byte[] Key = (byte[])this.e.CurrentObjectId;
			BinaryDeserializer Reader = new BinaryDeserializer(this.file.CollectionName, this.file.Encoding, Key, this.file.BlockLimit);
			this.recordHandler.SkipKey(Reader, true);
			this.currentObjectId = this.recordHandler.ObjectId;
			object Obj;

			try
			{
				if (this.currentSerializer is null)
					this.currentSerializer = await this.provider.GetObjectSerializer(typeof(T));

				Obj = await this.file.TryLoadObjectLocked(this.currentObjectId, this.currentSerializer);

				if (Obj is null)
				{
					this.current = default;
					this.currentTypeCompatible = false;

					// TODO: Delete records pointing to objects that do not exist, after lock has been released.
				}
				else if (Obj is T T2)
				{
					this.current = T2;
					this.currentTypeCompatible = true;
				}
				else
				{
					this.current = default;
					this.currentTypeCompatible = false;
				}
			}
			catch (InconsistencyException ex)
			{
				this.current = default;
				this.currentTypeCompatible = false;

				ExceptionDispatchInfo.Capture(ex).Throw();
			}
			catch (Exception)
			{
				this.current = default;
				this.currentTypeCompatible = false;
			}

			this.hasCurrent = true;
		}

		/// <summary>
		/// Goes to the first object.
		/// </summary>
		/// <returns>If a first object was found.</returns>
		public Task<bool> GoToFirstLocked()
		{
			return this.e.GoToFirstLocked();
		}

		/// <summary>
		/// Advances the enumerator to the previous element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the previous element; false if
		/// the enumerator has passed the beginning of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public async Task<bool> MovePreviousAsyncLocked()
		{
			if (!await this.e.MovePreviousAsyncLocked())
			{
				this.Reset();
				return false;
			}

			await this.LoadObject();

			return true;
		}

		/// <summary>
		/// Goes to the last object.
		/// </summary>
		/// <returns>If a last object was found.</returns>
		public Task<bool> GoToLastLocked()
		{
			return this.e.GoToLastLocked();
		}

		/// <summary>
		/// Finds the object given its order in the underlying database.
		/// </summary>
		/// <param name="ObjectIndex">Order of object in database.</param>
		/// <returns>If the corresponding object was found. If so, the <see cref="Current"/> property will contain the corresponding
		/// object.</returns>
		public async Task<bool> GoToObjectLocked(ulong ObjectIndex)
		{
			if (!await this.e.GoToObjectLocked(ObjectIndex))
			{
				this.Reset();
				return false;
			}

			await this.LoadObject();

			return true;
		}

		/// <summary>
		/// <see cref="IEnumerator.Reset()"/>
		/// </summary>
		public void Reset()
		{
			this.hasCurrent = false;
			this.currentObjectId = Guid.Empty;
			this.current = default;
			this.currentSerializer = null;

			this.e.Reset();
		}

		/// <summary>
		/// Resets the enumerator, and sets the starting point to a given starting point.
		/// </summary>
		/// <param name="StartingPoint">Starting point to start enumeration.</param>
		public void Reset(Bookmark StartingPoint)
		{
			this.hasCurrent = false;
			this.currentObjectId = Guid.Empty;
			this.current = default;
			this.currentSerializer = null;

			this.e.Reset(StartingPoint);
		}

		/// <summary>
		/// Skips a certain number of objects forward (positive <paramref name="NrObjects"/>) or backward (negative <paramref name="NrObjects"/>).
		/// </summary>
		/// <param name="NrObjects">Number of objects to skip forward (positive) or backward (negative).</param>
		/// <returns>If the skip operation was successful and a new object is available in <see cref="Current"/>.</returns>
		internal async Task<bool> SkipLocked(long NrObjects)
		{
			long Rank = (long)await this.GetCurrentRankLocked();

			Rank += NrObjects;
			if (Rank < 0)
				return false;

			if (!await this.GoToObjectLocked((ulong)Rank))
				return false;

			return true;
		}

		/// <summary>
		/// Gets a bookmark for the current position. You can set the current position of the enumerator, calling the
		/// <see cref="Reset(Bookmark)"/> method.
		/// </summary>
		/// <returns>Bookmark</returns>
		internal Task<Bookmark> GetBookmarkLocked()
		{
			return this.e.GetBookmarkLocked();
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
			return this.file.SameSortOrder(ConstantFields, SortOrder);
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
			return this.file.ReverseSortOrder(ConstantFields, SortOrder);
		}

		/// <summary>
		/// Continues operating after a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public async Task ContinueAfterLocked(T LastItem)
		{
			if (this.currentSerializer is null)
				this.currentSerializer = await this.provider.GetObjectSerializer(typeof(T));

			byte[] Bin = await this.recordHandler.Serialize(IndexBTreeFile.GuidMax, LastItem, this.currentSerializer, MissingFieldAction.Last);

			await this.e.ContinueAfterLocked(Bin);
		}

		/// <summary>
		/// Continues operating before a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public async Task ContinueBeforeLocked(T LastItem)
		{
			if (this.currentSerializer is null)
				this.currentSerializer = await this.provider.GetObjectSerializer(typeof(T));

			byte[] Bin = await this.recordHandler.Serialize(Guid.Empty, LastItem, this.currentSerializer, MissingFieldAction.First);

			await this.e.ContinueBeforeLocked(Bin);
		}
	}
}
