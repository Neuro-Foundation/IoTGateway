﻿using System;

namespace Waher.Persistence
{
	/// <summary>
	/// Event arguments for database object events.
	/// </summary>
	public class ObjectEventArgs : EventArgs
	{
		private readonly object obj;

		/// <summary>
		/// Event arguments for database object events.
		/// </summary>
		/// <param name="Object">Object</param>
		public ObjectEventArgs(object Object)
		{
			this.obj = Object;
		}

		/// <summary>
		/// Object
		/// </summary>
		public object Object => this.obj;
	}
}
