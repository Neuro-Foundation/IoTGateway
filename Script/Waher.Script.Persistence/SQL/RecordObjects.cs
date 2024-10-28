﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Serialization;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators;
using Waher.Script.Persistence.SQL.Parsers;

namespace Waher.Script.Persistence.SQL
{
	/// <summary>
	/// Executes an RECORD ... OBJECT[S] ... statement against the ledger.
	/// </summary>
	public class RecordObjects : ScriptNode, IEvaluateAsync
	{
		private readonly RecordOperation operation;
		private SourceDefinition source;
		private ElementList objects;

		/// <summary>
		/// Executes an RECORD ... OBJECT[S] ... statement against the ledger.
		/// </summary>
		/// <param name="Source">Source to update objects from.</param>
		/// <param name="Operation">Ledger event operation.</param>
		/// <param name="Objects">Objects</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public RecordObjects(SourceDefinition Source, RecordOperation Operation,
			ElementList Objects, int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
			this.source = Source;
			this.source?.SetParent(this);

			this.objects = Objects;
			this.objects?.SetParent(this);

			this.operation = Operation;
		}

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			return this.EvaluateAsync(Variables).Result;
		}

		/// <summary>
		/// Evaluates the node asynchronously, using the variables provided in 
		/// the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(Variables Variables)
		{
			IDataSource Source = await this.source.GetSource(Variables);
			IEnumerable<IElement> Objects;
			IElement E;
			long Count = 0;
			object Item;

			foreach (ScriptNode Object in this.objects.Elements)
			{
				E = await Object.EvaluateAsync(Variables);
				if (E is IVector V)
					Objects = V.ChildElements;
				else if (E is ISet S)
					Objects = S.ChildElements;
				else
					Objects = new IElement[] { E };

				foreach (IElement E2 in Objects)
				{
					Item = E2.AssociatedObjectValue;

					if (Item is Dictionary<string, IElement> ObjExNihilo)
					{
						GenericObject Obj2 = new GenericObject(Source.CollectionName, Source.TypeName, Guid.Empty);

						foreach (KeyValuePair<string, IElement> P in ObjExNihilo)
							Obj2[P.Key] = P.Value.AssociatedObjectValue;

						Item = Obj2;
					}
					else if (Item is Dictionary<string, object> ObjExNihilo2)
					{
						GenericObject Obj2 = new GenericObject(Source.CollectionName, Source.TypeName, Guid.Empty);

						foreach (KeyValuePair<string, object> P in ObjExNihilo2)
							Obj2[P.Key] = P.Value;

						Item = Obj2;
					}

					switch (this.operation)
					{
						case RecordOperation.New:
						default:
							await Ledger.NewEntry(Item);
							break;

						case RecordOperation.Update:
							await Ledger.UpdatedEntry(Item);
							break;

						case RecordOperation.Delete:
							await Ledger.DeletedEntry(Item);
							break;
					}
					Count++;
				}
			}

			return new DoubleNumber(Count);
		}

		/// <summary>
		/// Calls the callback method for all child nodes.
		/// </summary>
		/// <param name="Callback">Callback method to call.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <param name="Order">Order to traverse the nodes.</param>
		/// <returns>If the process was completed.</returns>
		public override bool ForAllChildNodes(ScriptNodeEventHandler Callback, object State, SearchMethod Order)
		{
			if (Order == SearchMethod.DepthFirst)
			{
				if (!(this.source?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				if (!(this.objects?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;
			}

			ScriptNode NewNode;
			bool b;

			if (!(this.source is null))
			{
				b = !Callback(this.source, out NewNode, State);
				if (!(NewNode is null) && NewNode is SourceDefinition Source2)
				{
					this.source = Source2;
					this.source.SetParent(this);
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.source.ForAllChildNodes(Callback, State, Order)))
					return false;
			}

			if (!(this.objects is null))
			{
				b = !Callback(this.objects, out NewNode, State);
				if (!(NewNode is null) && NewNode is ElementList NewObjects)
				{
					this.objects = NewObjects;
					this.objects.SetParent(this);
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.objects.ForAllChildNodes(Callback, State, Order)))
					return false;
			}

			if (Order == SearchMethod.BreadthFirst)
			{
				if (!(this.source?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				if (!(this.objects?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is RecordObjects O &&
				this.operation == O.operation &&
				AreEqual(this.source, O.source) &&
				AreEqual(this.objects, O.objects) &&
				base.Equals(obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = base.GetHashCode();
			Result ^= Result << 5 ^ this.operation.GetHashCode();
			Result ^= Result << 5 ^ GetHashCode(this.source);
			Result ^= Result << 5 ^ GetHashCode(this.objects);
			return Result;
		}

	}
}
