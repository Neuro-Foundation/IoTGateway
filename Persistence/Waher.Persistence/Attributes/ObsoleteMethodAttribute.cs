﻿using System;
using System.Collections.Generic;

namespace Waher.Persistence.Attributes
{
	/// <summary>
	/// This attribute defines a method for obsolete properties and field values found in serializations of older versions of the class.
	/// The method should take one parameter, a <see cref="Dictionary{String, Object}"/>, that represents property or field values
	/// with no matching properties or fields in the object. The return type of the method should be either void or Task. If a Task,
	/// the deserialization of the object waits for the task to complete.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class ObsoleteMethodAttribute : Attribute
	{
		private readonly string methodName;

		/// <summary>
		/// This attribute defines a method for obsolete properties and field values found in serializations of older versions of the class.
		/// </summary>
		/// <param name="MethodName">Name of method that will be called if deserializing objects with obsolete property or field values.
		/// The method should take one parameter, a <see cref="Dictionary{String, Object}"/>, that represents property or field values
		/// with no matching properties or fields in the object. The return type of the method should be either void or Task. If a Task,
		/// the deserialization of the object waits for the task to complete.
		/// </param>
		public ObsoleteMethodAttribute(string MethodName)
		{
			this.methodName = MethodName;
		}

		/// <summary>
		/// Name of method that will be called if deserializing objects with obsolete property or field values.
		/// The method should take one parameter, a <see cref="Dictionary{String, Object}"/>, that represents property or field values
		/// with no matching properties or fields in the object. The return type of the method should be either void or Task. If a Task,
		/// the deserialization of the object waits for the task to complete.
		/// </summary>
		public string MethodName => this.methodName;
	}
}
