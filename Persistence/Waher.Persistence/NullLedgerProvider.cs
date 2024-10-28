﻿using System;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using Waher.Runtime.Profiling;

namespace Waher.Persistence
{
	/// <summary>
	/// A NULL ledger.
	/// </summary>
	public class NullLedgerProvider : ILedgerProvider
	{
		/// <summary>
		/// A NULL ledger.
		/// </summary>
		public NullLedgerProvider()
		{
		}

		/// <summary>
		/// Adds an entry to the ledger.
		/// </summary>
		/// <param name="Object">New object.</param>
		public Task NewEntry(object Object) => Task.CompletedTask;

		/// <summary>
		/// Updates an entry in the ledger.
		/// </summary>
		/// <param name="Object">Updated object.</param>
		public Task UpdatedEntry(object Object) => Task.CompletedTask;

		/// <summary>
		/// Deletes an entry in the ledger.
		/// </summary>
		/// <param name="Object">Deleted object.</param>
		public Task DeletedEntry(object Object) => Task.CompletedTask;

		/// <summary>
		/// Clears a collection in the ledger.
		/// </summary>
		/// <param name="Collection">Cleared collection.</param>
		public Task ClearedCollection(string Collection) => Task.CompletedTask;

		/// <summary>
		/// Gets an eumerator for objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Type of object entries to enumerate.</typeparam>
		/// <returns>Enumerator object.</returns>
		public Task<ILedgerEnumerator<T>> GetEnumerator<T>() => throw new InvalidOperationException("Server is shutting down.");

		/// <summary>
		/// Gets an eumerator for objects in a collection.
		/// </summary>
		/// <param name="CollectionName">Collection to enumerate.</param>
		/// <returns>Enumerator object.</returns>
		public Task<ILedgerEnumerator<object>> GetEnumerator(string CollectionName) => throw new InvalidOperationException("Server is shutting down.");

		/// <summary>
		/// Called when processing starts.
		/// </summary>
		public Task Start() => Task.CompletedTask;

		/// <summary>
		/// Called when processing ends.
		/// </summary>
		public Task Stop() => Task.CompletedTask;

		/// <summary>
		/// Persists any pending changes.
		/// </summary>
		public Task Flush() => Task.CompletedTask;

		/// <summary>
		/// Gets an array of available collections.
		/// </summary>
		/// <returns>Array of collections.</returns>
		public Task<string[]> GetCollections() => Task.FromResult<string[]>(new string[0]);

		/// <summary>
		/// Performs an export of the entire ledger.
		/// </summary>
		/// <param name="Output">Ledger will be output to this interface.</param>
		/// <param name="Restriction">Optional restrictions to apply.
		/// If null, all information available in the ledger will be exported.</param>
		/// <returns>If export process was completed (true), or terminated by <paramref name="Output"/> (false).</returns>
		public Task<bool> Export(ILedgerExport Output, LedgerExportRestriction Restriction) => Task.FromResult(true);

		/// <summary>
		/// Performs an export of the entire ledger.
		/// </summary>
		/// <param name="Output">Ledger will be output to this interface.</param>
		/// <param name="Restriction">Optional restrictions to apply.
		/// If null, all information available in the ledger will be exported.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>If export process was completed (true), or terminated by <paramref name="Output"/> (false).</returns>
		public Task<bool> Export(ILedgerExport Output, LedgerExportRestriction Restriction, ProfilerThread Thread) => Task.FromResult(true);

		/// <summary>
		/// Registers a recipient of external events.
		/// </summary>
		/// <param name="ExternalEvents">Interface for recipient of external events.</param>
		/// <exception cref="Exception">If another recipient has been previously registered.</exception>
		public void Register(ILedgerExternalEvents ExternalEvents) { }

		/// <summary>
		/// Unregisters a recipient of external events.
		/// </summary>
		/// <param name="ExternalEvents">Interface for recipient of external events.</param>
		/// <exception cref="Exception">If the recipient is not the currently registered recipient.</exception>
		public void Unregister(ILedgerExternalEvents ExternalEvents) { }

	}
}
