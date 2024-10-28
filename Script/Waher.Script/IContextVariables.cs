﻿namespace Waher.Script
{
	/// <summary>
	/// Variables available in a specific context.
	/// </summary>
	public interface IContextVariables
	{
		/// <summary>
		/// Tries to get a variable object, given its name.
		/// </summary>
		/// <param name="Name">Variable name.</param>
		/// <param name="Variable">Variable, if found, or null otherwise.</param>
		/// <returns>If a variable with the corresponding name was found.</returns>
		bool TryGetVariable(string Name, out Variable Variable);

		/// <summary>
		/// If the collection contains a variable with a given name.
		/// </summary>
		/// <param name="Name">Variable name.</param>
		/// <returns>If a variable with that name exists.</returns>
		bool ContainsVariable(string Name);

		/// <summary>
		/// Adds a variable to the collection.
		/// </summary>
		/// <param name="Name">Variable name.</param>
		/// <param name="Value">Associated variable object value.</param>
		/// <returns>Reference to variable that was added.</returns>
		Variable Add(string Name, object Value);
	}
}
