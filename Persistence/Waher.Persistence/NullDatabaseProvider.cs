﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Waher.Persistence.Filters;
using Waher.Persistence.Serialization;
using Waher.Runtime.Profiling;

namespace Waher.Persistence
{
	/// <summary>
	/// A NULL database.
	/// </summary>
	public class NullDatabaseProvider : IDatabaseProvider
	{
		/// <summary>
		/// A NULL database.
		/// </summary>
		public NullDatabaseProvider()
		{
		}

		/// <summary>
		/// Inserts an object into the database.
		/// </summary>
		/// <param name="Object">Object to insert.</param>
		public Task Insert(object Object) => Task.CompletedTask;

		/// <summary>
		/// Inserts a collection of objects into the database.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		public Task Insert(params object[] Objects) => Task.CompletedTask;

		/// <summary>
		/// Inserts a collection of objects into the database.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		public Task Insert(IEnumerable<object> Objects) => Task.CompletedTask;

		/// <summary>
		/// Inserts an object into the database, if unlocked. If locked, object will be inserted at next opportunity.
		/// </summary>
		/// <param name="Object">Object to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task InsertLazy(object Object, ObjectCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Inserts an object into the database, if unlocked. If locked, object will be inserted at next opportunity.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task InsertLazy(object[] Objects, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Inserts an object into the database, if unlocked. If locked, object will be inserted at next opportunity.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task InsertLazy(IEnumerable<object> Objects, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Finds objects of a given class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<T>> Find<T>(int Offset, int MaxCount, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IEnumerable<T>>(new T[0]);
		}

		/// <summary>
		/// Finds objects of a given class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<T>> Find<T>(int Offset, int MaxCount, Filter Filter, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IEnumerable<T>>(new T[0]);
		}

		/// <summary>
		/// Finds objects in a given collection.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<object>> Find(string Collection, int Offset, int MaxCount, params string[] SortOrder)
		{
			return Task.FromResult<IEnumerable<object>>(new object[0]);
		}

		/// <summary>
		/// Finds objects in a given collection.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<object>> Find(string Collection, int Offset, int MaxCount, Filter Filter, params string[] SortOrder)
		{
			return Task.FromResult<IEnumerable<object>>(new object[0]);
		}

		/// <summary>
		/// Finds objects in a given collection.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<T>> Find<T>(string Collection, int Offset, int MaxCount, Filter Filter, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IEnumerable<T>>(new T[0]);
		}

		/// <summary>
		/// Finds the first page of objects of a given class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="PageSize">Number of items on a page.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>First page of objects.</returns>
		public Task<IPage<T>> FindFirst<T>(int PageSize, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IPage<T>>(new EmptyPage<T>());
		}

		/// <summary>
		/// Finds the first page of objects of a given class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="PageSize">Number of items on a page.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>First page of objects.</returns>
		public Task<IPage<T>> FindFirst<T>(int PageSize, Filter Filter, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IPage<T>>(new EmptyPage<T>());
		}

		/// <summary>
		/// Finds the first page of objects in a given collection.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="PageSize">Number of items on a page.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>First page of objects.</returns>
		public Task<IPage<object>> FindFirst(string Collection, int PageSize, params string[] SortOrder)
		{
			return Task.FromResult<IPage<object>>(new EmptyPage<object>());
		}

		/// <summary>
		/// Finds the first page of objects in a given collection.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="PageSize">Number of items on a page.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>First page of objects.</returns>
		public Task<IPage<object>> FindFirst(string Collection, int PageSize, Filter Filter, params string[] SortOrder)
		{
			return Task.FromResult<IPage<object>>(new EmptyPage<object>());
		}

		/// <summary>
		/// Finds the first page of objects in a given collection.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="PageSize">Number of items on a page.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>First page of objects.</returns>
		public Task<IPage<T>> FindFirst<T>(string Collection, int PageSize, Filter Filter, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IPage<T>>(new EmptyPage<T>());
		}

		/// <summary>
		/// Finds the next page of objects of a given class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Page">Page reference.</param>
		/// <returns>Next page, directly following <paramref name="Page"/>.</returns>
		public Task<IPage<T>> FindNext<T>(IPage<T> Page)
			where T : class
		{
			return Task.FromResult<IPage<T>>(new EmptyPage<T>());
		}

		/// <summary>
		/// Finds the next page of objects in a given collection.
		/// </summary>
		/// <param name="Page">Page reference.</param>
		/// <returns>Next page, directly following <paramref name="Page"/>.</returns>
		public Task<IPage<object>> FindNext(IPage<object> Page)
		{
			return Task.FromResult<IPage<object>>(new EmptyPage<object>());
		}

		/// <summary>
		/// Updates an object in the database.
		/// </summary>
		/// <param name="Object">Object to insert.</param>
		public Task Update(object Object) => Task.CompletedTask;

		/// <summary>
		/// Updates a collection of objects in the database.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		public Task Update(params object[] Objects) => Task.CompletedTask;

		/// <summary>
		/// Updates a collection of objects in the database.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		public Task Update(IEnumerable<object> Objects) => Task.CompletedTask;

		/// <summary>
		/// Updates an object in the database, if unlocked. If locked, object will be updated at next opportunity.
		/// </summary>
		/// <param name="Object">Object to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task UpdateLazy(object Object, ObjectCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Updates a collection of objects in the database, if unlocked. If locked, objects will be updated at next opportunity.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task UpdateLazy(object[] Objects, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Updates a collection of objects in the database, if unlocked. If locked, objects will be updated at next opportunity.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task UpdateLazy(IEnumerable<object> Objects, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Deletes an object in the database.
		/// </summary>
		/// <param name="Object">Object to insert.</param>
		public Task Delete(object Object) => Task.CompletedTask;

		/// <summary>
		/// Deletes a collection of objects in the database.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		public Task Delete(params object[] Objects) => Task.CompletedTask;

		/// <summary>
		/// Deletes a collection of objects in the database.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		public Task Delete(IEnumerable<object> Objects) => Task.CompletedTask;

		/// <summary>
		/// Deletes an object in the database, if unlocked. If locked, object will be deleted at next opportunity.
		/// </summary>
		/// <param name="Object">Object to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy(object Object, ObjectCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Deletes a collection of objects in the database, if unlocked. If locked, objects will be deleted at next opportunity.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy(object[] Objects, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Deletes a collection of objects in the database, if unlocked. If locked, objects will be deleted at next opportunity.
		/// </summary>
		/// <param name="Objects">Objects to insert.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy(IEnumerable<object> Objects, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Finds objects of a given class <typeparamref name="T"/> and deletes them in the same atomic operation.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<T>> FindDelete<T>(int Offset, int MaxCount, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IEnumerable<T>>(new T[0]);
		}

		/// <summary>
		/// Finds objects of a given class <typeparamref name="T"/> and deletes them in the same atomic operation.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<T>> FindDelete<T>(int Offset, int MaxCount, Filter Filter, params string[] SortOrder)
			where T : class
		{
			return Task.FromResult<IEnumerable<T>>(new T[0]);
		}

		/// <summary>
		/// Finds objects in a given collection and deletes them in the same atomic operation.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<object>> FindDelete(string Collection, int Offset, int MaxCount, params string[] SortOrder)
		{
			return Task.FromResult<IEnumerable<object>>(new object[0]);
		}

		/// <summary>
		/// Finds objects in a given collection and deletes them in the same atomic operation.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>Objects found.</returns>
		public Task<IEnumerable<object>> FindDelete(string Collection, int Offset, int MaxCount, Filter Filter, params string[] SortOrder)
		{
			return Task.FromResult<IEnumerable<object>>(new object[0]);
		}

		/// <summary>
		/// Finds objects of a given class <typeparamref name="T"/> and deletes them in the same atomic operation.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy<T>(int Offset, int MaxCount, string[] SortOrder, ObjectsCallback Callback)
			where T : class => Task.CompletedTask;

		/// <summary>
		/// Finds objects of a given class <typeparamref name="T"/> and deletes them in the same atomic operation.
		/// </summary>
		/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy<T>(int Offset, int MaxCount, Filter Filter, string[] SortOrder, ObjectsCallback Callback)
			where T : class => Task.CompletedTask;

		/// <summary>
		/// Finds objects in a given collection and deletes them in the same atomic operation.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy(string Collection, int Offset, int MaxCount, string[] SortOrder, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Finds objects in a given collection and deletes them in the same atomic operation.
		/// </summary>
		/// <param name="Collection">Name of collection to search.</param>
		/// <param name="Offset">Result offset.</param>
		/// <param name="MaxCount">Maximum number of objects to return.</param>
		/// <param name="Filter">Optional filter. Can be null.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <param name="Callback">Method to call when operation completed.</param>
		public Task DeleteLazy(string Collection, int Offset, int MaxCount, Filter Filter, string[] SortOrder, ObjectsCallback Callback) => Task.CompletedTask;

		/// <summary>
		/// Tries to load an object given its Object ID <paramref name="ObjectId"/> and its class type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Base type.</typeparam>
		/// <param name="ObjectId">Object ID</param>
		/// <returns>Loaded object, or null if not found.</returns>
		public Task<T> TryLoadObject<T>(object ObjectId)
			where T : class
		{
			return default;
		}

		/// <summary>
		/// Tries to load an object given its Object ID <paramref name="ObjectId"/> and its class type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Base type.</typeparam>
		/// <param name="CollectionName">Name of collection in which the object resides.</param>
		/// <param name="ObjectId">Object ID</param>
		/// <returns>Loaded object, or null if not found.</returns>
		public Task<T> TryLoadObject<T>(string CollectionName, object ObjectId)
			where T : class
		{
			return default;
		}

		/// <summary>
		/// Tries to load an object given its Object ID <paramref name="ObjectId"/> and its collection name <paramref name="CollectionName"/>.
		/// </summary>
		/// <param name="CollectionName">Name of collection in which the object resides.</param>
		/// <param name="ObjectId">Object ID</param>
		/// <returns>Loaded object, or null if not found.</returns>
		public Task<object> TryLoadObject(string CollectionName, object ObjectId)
		{
			return null;
		}

		/// <summary>
		/// Performs an export of the database.
		/// </summary>
		/// <param name="Output">Database will be output to this interface.</param>
		/// <param name="CollectionNames">Optional array of collections to export. If null, all collections will be exported.</param>
		/// <returns>If export process was completed (true), or terminated by <paramref name="Output"/> (false).</returns>
		public Task<bool> Export(IDatabaseExport Output, string[] CollectionNames) => Task.FromResult(true);

		/// <summary>
		/// Performs an export of the database.
		/// </summary>
		/// <param name="Output">Database will be output to this interface.</param>
		/// <param name="CollectionNames">Optional array of collections to export. If null, all collections will be exported.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>If export process was completed (true), or terminated by <paramref name="Output"/> (false).</returns>
		public Task<bool> Export(IDatabaseExport Output, string[] CollectionNames, ProfilerThread Thread) => Task.FromResult(true);

		/// <summary>
		/// Performs an iteration of contents of the entire database.
		/// </summary>
		/// <typeparam name="T">Type of objects to iterate.</typeparam>
		/// <param name="Recipient">Recipient of iterated objects.</param>
		/// <param name="CollectionNames">Optional array of collections to export. If null, all collections will be exported.</param>
		/// <returns>Task object for synchronization purposes.</returns>
		public Task Iterate<T>(IDatabaseIteration<T> Recipient, string[] CollectionNames)
			where T : class
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Performs an iteration of contents of the entire database.
		/// </summary>
		/// <typeparam name="T">Type of objects to iterate.</typeparam>
		/// <param name="Recipient">Recipient of iterated objects.</param>
		/// <param name="CollectionNames">Optional array of collections to export. If null, all collections will be exported.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>Task object for synchronization purposes.</returns>
		public Task Iterate<T>(IDatabaseIteration<T> Recipient, string[] CollectionNames, ProfilerThread Thread)
			where T : class
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Clears a collection of all objects.
		/// </summary>
		/// <param name="CollectionName">Name of collection to clear.</param>
		/// <returns>public Task object for synchronization purposes.</returns>
		public Task Clear(string CollectionName) => Task.CompletedTask;

		/// <summary>
		/// Analyzes the database and exports findings to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		/// <param name="XsltPath">Optional XSLT to use to view the output.</param>
		/// <param name="ProgramDataFolder">Program data folder. Can be removed from filenames used, when referencing them in the report.</param>
		/// <param name="ExportData">If data in database is to be exported in output.</param>
		/// <returns>Collections with errors found.</returns>
		public Task<string[]> Analyze(XmlWriter Output, string XsltPath, string ProgramDataFolder, bool ExportData)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Analyzes the database and exports findings to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		/// <param name="XsltPath">Optional XSLT to use to view the output.</param>
		/// <param name="ProgramDataFolder">Program data folder. Can be removed from filenames used, when referencing them in the report.</param>
		/// <param name="ExportData">If data in database is to be exported in output.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>Collections with errors found.</returns>
		public Task<string[]> Analyze(XmlWriter Output, string XsltPath, string ProgramDataFolder, bool ExportData, ProfilerThread Thread)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Analyzes the database and repairs it if necessary. Results are exported to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		/// <param name="XsltPath">Optional XSLT to use to view the output.</param>
		/// <param name="ProgramDataFolder">Program data folder. Can be removed from filenames used, when referencing them in the report.</param>
		/// <param name="ExportData">If data in database is to be exported in output.</param>
		/// <returns>Collections with errors found and repaired.</returns>
		public Task<string[]> Repair(XmlWriter Output, string XsltPath, string ProgramDataFolder, bool ExportData)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Analyzes the database and repairs it if necessary. Results are exported to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		/// <param name="XsltPath">Optional XSLT to use to view the output.</param>
		/// <param name="ProgramDataFolder">Program data folder. Can be removed from filenames used, when referencing them in the report.</param>
		/// <param name="ExportData">If data in database is to be exported in output.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>Collections with errors found and repaired.</returns>
		public Task<string[]> Repair(XmlWriter Output, string XsltPath, string ProgramDataFolder, bool ExportData, ProfilerThread Thread)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Analyzes the database and exports findings to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		/// <param name="XsltPath">Optional XSLT to use to view the output.</param>
		/// <param name="ProgramDataFolder">Program data folder. Can be removed from filenames used, when referencing them in the report.</param>
		/// <param name="ExportData">If data in database is to be exported in output.</param>
		/// <param name="Repair">If files should be repaired if corruptions are detected.</param>
		/// <returns>Collections with errors found, and repaired if <paramref name="Repair"/>=true.</returns>
		public Task<string[]> Analyze(XmlWriter Output, string XsltPath, string ProgramDataFolder, bool ExportData, bool Repair)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Analyzes the database and exports findings to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		/// <param name="XsltPath">Optional XSLT to use to view the output.</param>
		/// <param name="ProgramDataFolder">Program data folder. Can be removed from filenames used, when referencing them in the report.</param>
		/// <param name="ExportData">If data in database is to be exported in output.</param>
		/// <param name="Repair">If files should be repaired if corruptions are detected.</param>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <returns>Collections with errors found, and repaired if <paramref name="Repair"/>=true.</returns>
		public Task<string[]> Analyze(XmlWriter Output, string XsltPath, string ProgramDataFolder, bool ExportData, bool Repair, ProfilerThread Thread)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Repairs a set of collections.
		/// </summary>
		/// <param name="CollectionNames">Set of collections to repair.</param>
		/// <returns>Collections repaired.</returns>
		public Task<string[]> Repair(params string[] CollectionNames)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Repairs a set of collections.
		/// </summary>
		/// <param name="Thread">Optional Profiler thread.</param>
		/// <param name="CollectionNames">Set of collections to repair.</param>
		/// <returns>Collections repaired.</returns>
		public Task<string[]> Repair(ProfilerThread Thread, params string[] CollectionNames)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Adds an index to a collection, if one does not already exist.
		/// </summary>
		/// <param name="CollectionName">Name of collection.</param>
		/// <param name="FieldNames">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		public Task AddIndex(string CollectionName, string[] FieldNames) => Task.CompletedTask;

		/// <summary>
		/// Removes an index from a collection, if one exist.
		/// </summary>
		/// <param name="CollectionName">Name of collection.</param>
		/// <param name="FieldNames">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		public Task RemoveIndex(string CollectionName, string[] FieldNames) => Task.CompletedTask;

		/// <summary>
		/// Starts bulk-proccessing of data. Must be followed by a call to <see cref="EndBulk"/>.
		/// </summary>
		public Task StartBulk() => Task.CompletedTask;

		/// <summary>
		/// Ends bulk-processing of data. Must be called once for every call to <see cref="StartBulk"/>.
		/// </summary>
		public Task EndBulk() => Task.CompletedTask;

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
		/// Number of bytes used by an Object ID.
		/// </summary>
		public int ObjectIdByteCount => 0;

		/// <summary>
		/// Gets a persistent dictionary containing objects in a collection.
		/// </summary>
		/// <param name="Collection">Collection Name</param>
		/// <returns>Persistent dictionary</returns>
		public Task<IPersistentDictionary> GetDictionary(string Collection) => throw new InvalidOperationException("Server is shutting down.");

		/// <summary>
		/// Gets an array of available dictionary collections.
		/// </summary>
		/// <returns>Array of dictionary collections.</returns>
		public Task<string[]> GetDictionaries() => Task.FromResult(new string[0]);

		/// <summary>
		/// Gets an array of available collections.
		/// </summary>
		/// <returns>Array of collections.</returns>
		public Task<string[]> GetCollections()
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Gets the collection corresponding to a given type.
		/// </summary>
		/// <param name="Type">Type</param>
		/// <returns>Collection name.</returns>
		public Task<string> GetCollection(Type Type)
		{
			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Gets the collection corresponding to a given object.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>Collection name.</returns>
		public Task<string> GetCollection(Object Object)
		{
			return Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Checks if a string is a label in a given collection.
		/// </summary>
		/// <param name="Collection">Name of collection.</param>
		/// <param name="Label">Label to check.</param>
		/// <returns>If <paramref name="Label"/> is a label in the collection
		/// defined by <paramref name="Collection"/>.</returns>
		public Task<bool> IsLabel(string Collection, string Label)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Gets an array of available labels for a collection.
		/// </summary>
		/// <returns>Array of labels.</returns>
		public Task<string[]> GetLabels(string Collection)
		{
			return Task.FromResult(new string[0]);
		}

		/// <summary>
		/// Tries to get the Object ID of an object, if it exists.
		/// </summary>
		/// <param name="Object">Object whose Object ID is of interest.</param>
		/// <returns>Object ID, if found, null otherwise.</returns>
		public Task<object> TryGetObjectId(object Object)
		{
			return Task.FromResult<object>(null);
		}

		/// <summary>
		/// Drops a collection, if it exist.
		/// </summary>
		/// <param name="CollectionName">Name of collection.</param>
		public Task DropCollection(string CollectionName) => Task.CompletedTask;

		/// <summary>
		/// Creates a generalized representation of an object.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>Generalized representation.</returns>
		public Task<GenericObject> Generalize(object Object) => Task.FromResult<GenericObject>(null);

		/// <summary>
		/// Creates a specialized representation of a generic object.
		/// </summary>
		/// <param name="Object">Generic object.</param>
		/// <returns>Specialized representation.</returns>
		public Task<object> Specialize(GenericObject Object) => Task.FromResult<object>(null);

		/// <summary>
		/// Gets an array of collections that should be excluded from backups.
		/// </summary>
		/// <returns>Array of excluded collections.</returns>
		public string[] GetExcludedCollections() => new string[0];
	}
}
