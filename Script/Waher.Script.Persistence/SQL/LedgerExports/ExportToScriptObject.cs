﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Serialization;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Objects;
using Waher.Script.Objects.VectorSpaces;

namespace Waher.Script.Persistence.SQL.LedgerExports
{
	/// <summary>
	/// Export is serialized into object form.
	/// </summary>
	public class ExportToScriptObject : ILedgerExport
	{
		private readonly List<IElement> events = new List<IElement>();
		private Dictionary<string, IElement> currentProperties = null;
		private string currentCollection = null;
		private string currentBlock = null;

		/// <summary>
		/// Export is serialized into object form.
		/// </summary>
		public ExportToScriptObject()
		{
		}

		/// <summary>
		/// Returns an array of exported objects.
		/// </summary>
		/// <returns>Exported objects.</returns>
		public IVector ToVector()
		{
			return new ObjectVector(this.events.ToArray());
		}

		/// <summary>
		/// Is called when export of ledger is started.
		/// </summary>
		/// <returns>If export can continue.</returns>
		public Task<bool> StartLedger()
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when export of ledger is finished.
		/// </summary>
		/// <returns>If export can continue.</returns>
		public Task<bool> EndLedger()
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when a collection is started.
		/// </summary>
		/// <param name="CollectionName">Name of collection</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> StartCollection(string CollectionName)
		{
			this.currentCollection = CollectionName;
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when a collection is finished.
		/// </summary>
		/// <returns>If export can continue.</returns>
		public Task<bool> EndCollection()
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when a block in a collection is started.
		/// </summary>
		/// <param name="BlockID">Block ID</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> StartBlock(string BlockID)
		{
			this.currentBlock = BlockID;
			return Task.FromResult(true);
		}

		/// <summary>
		/// Reports block meta-data.
		/// </summary>
		/// <param name="Key">Meta-data key.</param>
		/// <param name="Value">Meta-data value.</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> BlockMetaData(string Key, object Value)
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when a block in a collection is finished.
		/// </summary>
		/// <returns>If export can continue.</returns>
		public Task<bool> EndBlock()
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when an entry is started.
		/// </summary>
		/// <param name="ObjectId">ID of object.</param>
		/// <param name="TypeName">Type name of object.</param>
		/// <param name="EntryType">Type of entry</param>
		/// <param name="EntryTimestamp">Timestamp of entry</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> StartEntry(string ObjectId, string TypeName, EntryType EntryType, DateTimeOffset EntryTimestamp)
		{
			this.currentProperties = new Dictionary<string, IElement>();

			this.events.Add(new ObjectValue(new Dictionary<string, IElement>()
			{
				{ "Collection", new ObjectValue(this.currentCollection) },
				{ "BlockId", new ObjectValue(this.currentBlock) },
				{ "ObjectId", new ObjectValue(ObjectId) },
				{ "TypeName", new ObjectValue(TypeName) },
				{ "EntryType", new ObjectValue(EntryType) },
				{ "Timestamp", new ObjectValue(EntryTimestamp) },
				{ "this", new ObjectValue(this.currentProperties) }
			}));

			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when an entry is finished.
		/// </summary>
		/// <returns>If export can continue.</returns>
		public Task<bool> EndEntry()
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when the collection has been cleared.
		/// </summary>
		/// <param name="EntryTimestamp">Timestamp of entry</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> CollectionCleared(DateTimeOffset EntryTimestamp)
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when a property is reported.
		/// </summary>
		/// <param name="PropertyName">Property name.</param>
		/// <param name="PropertyValue">Property value.</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> ReportProperty(string PropertyName, object PropertyValue)
		{
			this.currentProperties[PropertyName] = Expression.Encapsulate(PropertyValue);
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when an error is reported.
		/// </summary>
		/// <param name="Message">Error message.</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> ReportError(string Message)
		{
			return Task.FromResult(true);
		}

		/// <summary>
		/// Is called when an exception has occurred.
		/// </summary>
		/// <param name="Exception">Exception object.</param>
		/// <returns>If export can continue.</returns>
		public Task<bool> ReportException(Exception Exception)
		{
			return Task.FromResult(true);
		}
	}
}
