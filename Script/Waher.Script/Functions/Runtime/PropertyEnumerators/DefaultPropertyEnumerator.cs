﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;

namespace Waher.Script.Functions.Runtime.PropertyEnumerators
{
	/// <summary>
	/// Enumerates any type of object.
	/// </summary>
	public class DefaultPropertyEnumerator : IPropertyEnumerator
	{
		/// <summary>
		/// Enumerates any type of object.
		/// </summary>
		public DefaultPropertyEnumerator()
		{
		}

		/// <summary>
		/// Enumerates the properties of an object (of a type it supports).
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>Property enumeration as a script element.</returns>
		public async Task<IElement> EnumerateProperties(object Object)
		{
			ChunkedList<IElement> Elements = new ChunkedList<IElement>();
			Type T = Object.GetType();
			IElement Value;

			foreach (PropertyInfo PI in T.GetRuntimeProperties())
			{
				if (!PI.CanRead || !PI.GetMethod.IsPublic || PI.GetIndexParameters().Length > 0)
					continue;

				Elements.Add(new StringValue(PI.Name));

				try
				{
					Value = Expression.Encapsulate(await ScriptNode.WaitPossibleTask(PI.GetValue(Object)));
				}
				catch (Exception ex)
				{
					Value = new ObjectValue(ex);
				}

				Elements.Add(Value);
			}

			ObjectMatrix M = new ObjectMatrix(Elements.Count / 2, 2, Elements)
			{
				ColumnNames = new string[] { "Name", "Value" }
			};

			return M;
		}

		/// <summary>
		/// If the interface understands objects such as <paramref name="Object"/>.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>How well objects of this type are supported.</returns>
		public Grade Supports(Type Object)
		{
			return Grade.Barely;
		}
	}
}
