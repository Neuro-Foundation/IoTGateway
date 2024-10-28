﻿using System;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using Waher.Runtime.Profiling;

namespace Waher.Persistence
{
	/// <summary>
	/// Interface for ledger providers that can be plugged into the static <see cref="Ledger"/> class.
	/// </summary>
	public interface ILedgerProvider
	{
		/// <summary>
		/// Registers a recipient of external events.
		/// </summary>
		/// <param name="ExternalEvents">Interface for recipient of external events.</param>
		/// <exception cref="Exception">If another recipient has been previously registered.</exception>
		void Register(ILedgerExternalEvents ExternalEvents);

		/// <summary>
		/// Unregisters a recipient of external events.
		/// </summary>
		/// <param name="ExternalEvents">Interface for recipient of external events.</param>
		/// <exception cref="Exception">If the recipient is not the currently registered recipient.</exception>
		void Unregister(ILedgerExternalEvents ExternalEvents);

		/// <summary>
		/// Adds an entry to the ledger.
		/// </summary>
		/// <param name="Object">New object.</param>
		Task NewEntry(object Object);

		/// <summary>
		/// Updates an entry in the ledger.
		/// </summary>
		/// <param name="Object">Updated object.</param>
		Task UpdatedEntry(object Object);

		/// <summary>
		/// Deletes an entry in the ledger.
		/// </summary>
		/// <param name="Object">Deleted object.</param>
		Task DeletedEntry(object Object);

		/// <summary>
		/// Clears a collection in the ledger.
		/// </summary>
		/// <param name="Collection">Cleared collection.</param>
		Task ClearedCollection(string Collection);

		/// <summary>
		/// Gets an eumerator for objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Type of object entries to enumerate.</typeparam>
		/// <returns>Enumerator object.</returns>
		Task<ILedgerEnumerator<T>> GetEnumerator<T>();

		/// <summary>
		/// Gets an eumerator for objects in a collection.
		/// </summary>
		/// <param name="CollectionName">Collection to enumerate.</param>
		/// <returns>Enumerator object.</returns>
		Task<ILedgerEnumerator<object>> GetEnumerator(string CollectionName);

		/// <summary>
		/// Called when processing starts.
		/// </summary>
		Task Start();

		/// <summary>
		/// Called when processing ends.
		/// </summary>
		Task Stop();

		/// <summary>
		/// Persists any pending changes.
		/// </summary>
		Task Flush();

		/// <summary>
		/// Gets an array of available collections.
		/// </summary>
		/// <returns>Array of collections.</returns>
		Task<string[]> GetCollections();

		/// <summary>
		/// Performs an export of the entire ledger.
		/// </summary>
		/// <param name="Output">Ledger will be output to this interface.</param>
		/// <param name="Restriction">Optional restrictions to apply.
		/// If null, all information available in the ledger will be exported.</param>
		/// <returns>If export process was completed (true), or terminated by <paramref name="Output"/> (false).</returns>
		Task<bool> Export(ILedgerExport Output, LedgerExportRestriction Restriction);

		/// <summary>
		/// Performs an export of the entire ledger.
		/// </summary>
		/// <param name="Output">Ledger will be output to this interface.</param>
		/// <param name="Restriction">Optional restrictions to apply.
		/// If null, all information available in the ledger will be exported.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>If export process was completed (true), or terminated by <paramref name="Output"/> (false).</returns>
		Task<bool> Export(ILedgerExport Output, LedgerExportRestriction Restriction, ProfilerThread Thread);
	}
}
