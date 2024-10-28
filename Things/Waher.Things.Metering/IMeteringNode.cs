﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Things.DisplayableParameters;
using Waher.Things.SensorData;

namespace Waher.Things.Metering
{
	/// <summary>
	/// Base Interface for all metering nodes.
	/// </summary>
	public interface IMeteringNode : INode, ILifeCycleManagement
	{
		/// <summary>
		/// ID of node.
		/// </summary>
		new string NodeId { get; set; }

		/// <summary>
		/// Parent Node, or null if a root node.
		/// </summary>
		[Obsolete("Use the asynchronous GetParent() method instead.")]
		new INode Parent { get; }

		/// <summary>
		/// Gets the parent of the node.
		/// </summary>
		/// <returns>Parent instance.</returns>
		/// <exception cref="System.Exception">If parent is not found.</exception>
		Task<INode> GetParent();

		/// <summary>
		/// Logs an error message on the node.
		/// </summary>
		/// <param name="Body">Message body.</param>
		Task LogErrorAsync(string Body);

		/// <summary>
		/// Logs an error message on the node.
		/// </summary>
		/// <param name="EventId">Optional Event ID.</param>
		/// <param name="Body">Message body.</param>
		Task LogErrorAsync(string EventId, string Body);

		/// <summary>
		/// Logs an warning message on the node.
		/// </summary>
		/// <param name="Body">Message body.</param>
		Task LogWarningAsync(string Body);

		/// <summary>
		/// Logs an warning message on the node.
		/// </summary>
		/// <param name="EventId">Optional Event ID.</param>
		/// <param name="Body">Message body.</param>
		Task LogWarningAsync(string EventId, string Body);

		/// <summary>
		/// Logs an informational message on the node.
		/// </summary>
		/// <param name="Body">Message body.</param>
		Task LogInformationAsync(string Body);

		/// <summary>
		/// Logs an informational message on the node.
		/// </summary>
		/// <param name="EventId">Optional Event ID.</param>
		/// <param name="Body">Message body.</param>
		Task LogInformationAsync(string EventId, string Body);

		/// <summary>
		/// Logs a message on the node.
		/// </summary>
		/// <param name="Type">Type of message.</param>
		/// <param name="Body">Message body.</param>
		Task LogMessageAsync(MessageType Type, string Body);

		/// <summary>
		/// Logs a message on the node.
		/// </summary>
		/// <param name="Type">Type of message.</param>
		/// <param name="EventId">Optional Event ID.</param>
		/// <param name="Body">Message body.</param>
		Task LogMessageAsync(MessageType Type, string EventId, string Body);

		/// <summary>
		/// Removes error messages with an empty event ID from the node.
		/// </summary>
		Task<bool> RemoveErrorAsync();

		/// <summary>
		/// Removes error messages with a given event ID from the node.
		/// </summary>
		/// <param name="EventId">Optional Event ID.</param>
		Task<bool> RemoveErrorAsync(string EventId);

		/// <summary>
		/// Removes warning messages with an empty event ID from the node.
		/// </summary>
		Task<bool> RemoveWarningAsync();

		/// <summary>
		/// Removes warning messages with a given event ID from the node.
		/// </summary>
		/// <param name="EventId">Optional Event ID.</param>
		Task<bool> RemoveWarningAsync(string EventId);

		/// <summary>
		/// Removes warning messages with an empty event ID from the node.
		/// </summary>
		Task<bool> RemoveInformationAsync();

		/// <summary>
		/// Removes an informational message on the node.
		/// </summary>
		/// <param name="EventId">Optional Event ID.</param>
		Task<bool> RemoveInformationAsync(string EventId);

		/// <summary>
		/// Removes messages with empty event IDs from the node.
		/// </summary>
		/// <param name="Type">Type of message.</param>
		Task<bool> RemoveMessageAsync(MessageType Type);

		/// <summary>
		/// Logs a message on the node.
		/// </summary>
		/// <param name="Type">Type of message.</param>
		/// <param name="EventId">Optional Event ID.</param>
		Task<bool> RemoveMessageAsync(MessageType Type, string EventId);

		#region Momentary values

		/// <summary>
		/// Reports newly measured values.
		/// </summary>
		/// <param name="Values">New momentary values.</param>
		void NewMomentaryValues(params Field[] Values);

		/// <summary>
		/// Reports newly measured values.
		/// </summary>
		/// <param name="Values">New momentary values.</param>
		void NewMomentaryValues(IEnumerable<Field> Values);

		#endregion
	}
}
