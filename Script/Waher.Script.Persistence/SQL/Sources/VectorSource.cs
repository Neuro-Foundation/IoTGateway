﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;
using Waher.Script.Objects.VectorSpaces;
using Waher.Script.Order;
using Waher.Script.Persistence.Functions;
using Waher.Script.Persistence.SQL.Enumerators;

namespace Waher.Script.Persistence.SQL.Sources
{
    /// <summary>
    /// Data Source defined by a vector.
    /// </summary>
    public class VectorSource : IDataSource
	{
		private readonly Dictionary<Type, bool> types = new Dictionary<Type, bool>();
		private readonly Dictionary<string, bool> isLabel = new Dictionary<string, bool>();
		private readonly IVector vector;
		private readonly ScriptNode node;
		private readonly string name;
		private readonly string alias;

		/// <summary>
		/// Data Source defined by a vector.
		/// </summary>
		/// <param name="Name">Name of source.</param>
		/// <param name="Alias">Alias for source.</param>
		/// <param name="Vector">Vector</param>
		/// <param name="Node">Node defining the vector.</param>
		public VectorSource(string Name, string Alias, IVector Vector, ScriptNode Node)
		{
			this.vector = Vector;
			this.node = Node;
			this.name = Name;
			this.alias = Alias;

			Type LastType = null;
			Type T;

			foreach (IElement E in Vector.ChildElements)
			{
				T = E.AssociatedObjectValue?.GetType();
				if (T is null || T == LastType)
					continue;

				LastType = T;
				this.types[T] = true;
			}
		}

		/// <summary>
		/// Vector
		/// </summary>
		public IVector Vector => this.vector;

		/// <summary>
		/// Finds objects matching filter conditions in <paramref name="Where"/>.
		/// </summary>
		/// <param name="Offset">Offset at which to return elements.</param>
		/// <param name="Top">Maximum number of elements to return.</param>
		/// <param name="Generic">If objects of type <see cref="GenericObject"/> should be returned.</param>
		/// <param name="Where">Filter conditions.</param>
		/// <param name="Variables">Current set of variables.</param>
		/// <param name="Order">Order at which to order the result set.</param>
		/// <param name="Node">Script node performing the evaluation.</param>
		/// <returns>Enumerator.</returns>
		public Task<IResultSetEnumerator> Find(int Offset, int Top, bool Generic, ScriptNode Where, Variables Variables,
			KeyValuePair<VariableReference, bool>[] Order, ScriptNode Node)
		{
			return Find(this.vector, Offset, Top, Generic, Where, Variables, Order, Node);
		}

		internal static async Task<IResultSetEnumerator> Find(IVector Vector, int Offset, int Top, bool Generic, ScriptNode Where, Variables Variables,
			KeyValuePair<VariableReference, bool>[] Order, ScriptNode Node)
		{
			IResultSetEnumerator e = new SynchEnumerator(Vector.VectorElements.GetEnumerator());
			int i, c;

			if (!(Where is null))
				e = new ConditionalEnumerator(e, Variables, Where);

			if ((c = Order?.Length ?? 0) > 0)
			{
				List<IElement> Items = new List<IElement>();

				while (await e.MoveNextAsync())
				{
					if (!(e.Current is IElement E))
						E = Expression.Encapsulate(e.Current);

					Items.Add(Generic ? await Generalize.EvaluateAsync(E) : E);
				}

				IComparer<IElement> Order2;

				if (c == 1)
					Order2 = ToPropertyOrder(Node, Order[0]);
				else
				{
					IComparer<IElement>[] Orders = new IComparer<IElement>[c];

					for (i = 0; i < c; i++)
						Orders[i] = ToPropertyOrder(Node, Order[i]);

					Order2 = new CompoundOrder(Orders);
				}

				Items.Sort(Order2);

				e = new SynchEnumerator(Items.GetEnumerator());
			}

			if (Offset > 0)
				e = new OffsetEnumerator(e, Offset);

			if (Top != int.MaxValue)
				e = new MaxCountEnumerator(e, Top);

			return e;
		}

		private static PropertyOrder ToPropertyOrder(ScriptNode Node, KeyValuePair<VariableReference, bool> Order)
		{
			return new PropertyOrder(Node, Order.Key.VariableName, Order.Value ? 1 : -1);
		}

		/// <summary>
		/// Updates a set of objects.
		/// </summary>
		/// <param name="Lazy">If operation can be completed at next opportune time.</param>
		/// <param name="Objects">Objects to update</param>
		public Task Update(bool Lazy, IEnumerable<object> Objects)
		{
			return Task.CompletedTask;  // Do nothing.
		}

		private Exception InvalidOperation()
		{
			return new ScriptRuntimeException("Operation not permitted on joined sources.", this.node);
		}

		/// <summary>
		/// Finds and Deletes a set of objects.
		/// </summary>
		/// <param name="Lazy">If operation can be completed at next opportune time.</param>
		/// <param name="Offset">Offset at which to return elements.</param>
		/// <param name="Top">Maximum number of elements to return.</param>
		/// <param name="Where">Filter conditions.</param>
		/// <param name="Variables">Current set of variables.</param>
		/// <param name="Order">Order at which to order the result set.</param>
		/// <param name="Node">Script node performing the evaluation.</param>
		/// <returns>Number of objects deleted, if known.</returns>
		public Task<int?> FindDelete(bool Lazy, int Offset, int Top, ScriptNode Where, Variables Variables,
			KeyValuePair<VariableReference, bool>[] Order, ScriptNode Node)
		{
			throw this.InvalidOperation();
		}

		/// <summary>
		/// Inserts an object.
		/// </summary>
		/// <param name="Lazy">If operation can be completed at next opportune time.</param>
		/// <param name="Object">Object to insert.</param>
		public Task Insert(bool Lazy, object Object)
		{
			throw this.InvalidOperation();
		}

		/// <summary>
		/// Name of corresponding collection.
		/// </summary>
		public string CollectionName
		{
			get { throw new ScriptRuntimeException("Collection not defined.", this.node); }
		}

		/// <summary>
		/// Name of corresponding type.
		/// </summary>
		public string TypeName
		{
			get { throw new ScriptRuntimeException("Type not defined.", this.node); }
		}

		/// <summary>
		/// Collection name or alias.
		/// </summary>
		public string Name
		{
			get => string.IsNullOrEmpty(this.alias) ? this.name : this.alias;
		}

		/// <summary>
		/// Checks if the name refers to the source.
		/// </summary>
		/// <param name="Name">Name to check.</param>
		/// <returns>If the name refers to the source.</returns>
		public bool IsSource(string Name)
		{
			return
				string.Compare(this.name, Name, true) == 0 ||
				string.Compare(this.alias, Name, true) == 0;
		}

		/// <summary>
		/// Checks if the label is a label in the source.
		/// </summary>
		/// <param name="Label">Label</param>
		/// <returns>If the label is a label in the source.</returns>
		public Task<bool> IsLabel(string Label)
		{
			lock (this.isLabel)
			{
				if (!this.isLabel.TryGetValue(Label, out bool Result))
				{
					Result = false;

					foreach (Type T in this.types.Keys)
					{
						PropertyInfo PI = T.GetRuntimeProperty(Label);

						if (PI is null)
						{
							FieldInfo FI = T.GetRuntimeField(Label);

							if (!(FI is null))
							{
								Result = FI.IsPublic;
								break;
							}
						}
						else
						{
							Result = PI.CanRead && PI.GetMethod.IsPublic;
							break;
						}
					}

					this.isLabel[Label] = Result;
				}

				return Task.FromResult<bool>(Result);
			}
		}

		/// <summary>
		/// Creates an index in the source.
		/// </summary>
		/// <param name="Name">Name of index.</param>
		/// <param name="Fields">Field names. Prefix with hyphen (-) to define descending order.</param>
		public Task CreateIndex(string Name, string[] Fields)
		{
			throw this.InvalidOperation();
		}

		/// <summary>
		/// Drops an index from the source.
		/// </summary>
		/// <param name="Name">Name of index.</param>
		/// <returns>If an index was found and dropped.</returns>
		public Task<bool> DropIndex(string Name)
		{
			throw InvalidOperation();
		}

		/// <summary>
		/// Drops the collection from the source.
		/// </summary>
		public Task DropCollection()
		{
			throw InvalidOperation();
		}

		/// <summary>
		/// Converts an object matrix, with named columns, to a vector of
		/// objects ex nihilo.
		/// </summary>
		/// <param name="ResultSet">Result set</param>
		/// <returns>Object vector.</returns>
		public static ObjectVector ToGenericObjectVector(ObjectMatrix ResultSet)
		{
			if (!ResultSet.HasColumnNames)
				throw new ArgumentException("Result Set lacks named columns.", nameof(ResultSet));

			int Rows = ResultSet.Rows;
			int Columns = ResultSet.Columns;
			string[] Names = ResultSet.ColumnNames;
			IElement[] Objects = new IElement[Rows];
			Dictionary<string, object> Object;
			int x, y;

			for (y = 0; y < Rows; y++)
			{
				Object = new Dictionary<string, object>();
				Objects[y] = new ObjectValue(Object);

				for (x = 0; x < Columns; x++)
					Object[Names[x]] = ResultSet.GetElement(x, y).AssociatedObjectValue;
			}

			return new ObjectVector(Objects);
		}

	}
}
