﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Vectors;

namespace Waher.Script.Operators
{
	/// <summary>
	/// Creates an object from nothing.
	/// </summary>
	public class ObjectExNihilo : ScriptNode
	{
		private readonly LinkedList<KeyValuePair<string, ScriptNode>> members;
		private readonly bool hasWildcards = false;
		private Dictionary<string, ScriptNode> quick = null;
		private bool isAsync;

		/// <summary>
		/// Creates an object from nothing.
		/// </summary>
		/// <param name="Members">Members</param>.
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ObjectExNihilo(LinkedList<KeyValuePair<string, ScriptNode>> Members, int Start, int Length, Expression Expression)
			: this(Members, false, Start,Length, Expression)
		{
		}

		/// <summary>
		/// Creates an object from nothing.
		/// </summary>
		/// <param name="Members">Members</param>
		/// <param name="HasWildcards">If the object definition includes wildcards.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ObjectExNihilo(LinkedList<KeyValuePair<string, ScriptNode>> Members, 
			bool HasWildcards, int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
			this.members = Members;
			this.hasWildcards = HasWildcards;

			foreach (KeyValuePair<string, ScriptNode> P in Members)
				P.Value?.SetParent(this);

			this.CalcIsAsync();
		}

		/// <summary>
		/// If the object definition includes wildcards.
		/// </summary>
		public bool HasWildcards => this.hasWildcards;

		private void CalcIsAsync()
		{
			this.isAsync = false;

			foreach (KeyValuePair<string, ScriptNode> P in this.members)
			{
				if (P.Value?.IsAsynchronous ?? false)
				{
					this.isAsync = true;
					break;
				}
			}
		}

		/// <summary>
		/// Members, in order of definition.
		/// </summary>
		public LinkedList<KeyValuePair<string, ScriptNode>> Members => this.members;

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => this.isAsync;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			Dictionary<string, IElement> Result = new Dictionary<string, IElement>();

			foreach (KeyValuePair<string, ScriptNode> P in this.members)
				Result[P.Key] = P.Value.Evaluate(Variables);

			return new ObjectValue(Result);
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(Variables Variables)
		{
			if (!this.isAsync)
				return this.Evaluate(Variables);

			Dictionary<string, IElement> Result = new Dictionary<string, IElement>();

			foreach (KeyValuePair<string, ScriptNode> P in this.members)
				Result[P.Key] = await P.Value.EvaluateAsync(Variables);

			return new ObjectValue(Result);
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
			LinkedListNode<KeyValuePair<string, ScriptNode>> Loop;

			if (Order == SearchMethod.DepthFirst)
			{
				Loop = this.members.First;

				while (!(Loop is null))
				{
					if (!(Loop.Value.Value?.ForAllChildNodes(Callback, State, Order) ?? true))
						return false;

					Loop = Loop.Next;
				}
			}

			Loop = this.members.First;

			ScriptNode Node;
			bool RecalcIsAsync = false;

			while (!(Loop is null))
			{
				Node = Loop.Value.Value;
				if (!(Node is null))
				{
					bool Result = Callback(Node, out ScriptNode NewNode, State);
					if (!(NewNode is null))
					{
						Loop.Value = new KeyValuePair<string, ScriptNode>(Loop.Value.Key, NewNode);
						NewNode.SetParent(this);
						Node = NewNode;

						RecalcIsAsync = true;
					}

					if (!Result || (Order == SearchMethod.TreeOrder && !Node.ForAllChildNodes(Callback, State, Order)))
					{
						if (RecalcIsAsync)
							this.CalcIsAsync();

						return false;
					}
				}

				Loop = Loop.Next;
			}

			if (RecalcIsAsync)
				this.CalcIsAsync();

			if (Order == SearchMethod.BreadthFirst)
			{
				Loop = this.members.First;

				while (!(Loop is null))
				{
					if (!(Loop.Value.Value?.ForAllChildNodes(Callback, State, Order) ?? true))
						return false;

					Loop = Loop.Next;
				}
			}

			return true;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (!(obj is ObjectExNihilo O) ||
				this.hasWildcards != O.hasWildcards ||
				!base.Equals(obj))
			{
				return false;
			}

			LinkedList<KeyValuePair<string, ScriptNode>>.Enumerator e1 = this.members.GetEnumerator();
			LinkedList<KeyValuePair<string, ScriptNode>>.Enumerator e2 = O.members.GetEnumerator();

			while (true)
			{
				bool b1 = e1.MoveNext();
				bool b2 = e2.MoveNext();

				if (b1 ^ b2)
					return false;

				if (!b1)
					return true;

				KeyValuePair<string, ScriptNode> Item1 = e1.Current;
				KeyValuePair<string, ScriptNode> Item2 = e2.Current;

				if (!Item1.Key.Equals(Item2.Key) ||
					!Item1.Value.Equals(Item2.Value))
				{
					return false;
				}
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = base.GetHashCode();

			Result ^= Result << 5 ^ this.hasWildcards.GetHashCode();

			foreach (KeyValuePair<string, ScriptNode> P in this.members)
			{
				Result ^= Result << 5 ^ P.Key.GetHashCode();
				Result ^= Result << 5 ^ P.Value.GetHashCode();
			}

			return Result;
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			PatternMatchResult Result;

			if (CheckAgainst.AssociatedObjectValue is IDictionary<string, IElement> Object)
			{
				this.CheckQuick();

				if (!this.hasWildcards)
				{
					foreach (KeyValuePair<string, IElement> P in Object)
					{
						if (!this.quick.ContainsKey(P.Key))
							return PatternMatchResult.NoMatch;
					}
				}

				foreach (KeyValuePair<string, ScriptNode> P in this.members)
				{
					if (Object.TryGetValue(P.Key, out IElement E))
						Result = P.Value.PatternMatch(E, AlreadyFound);
					else
						Result = P.Value.PatternMatch(ObjectValue.Null, AlreadyFound);

					if (Result != PatternMatchResult.Match)
						return Result;
				}
			}
			else if (CheckAgainst.AssociatedObjectValue is IDictionary<string, object> Object2)
			{
				this.CheckQuick();

				if (!this.hasWildcards)
				{
					foreach (KeyValuePair<string, object> P in Object2)
					{
						if (!this.quick.ContainsKey(P.Key))
							return PatternMatchResult.NoMatch;
					}
				}

				foreach (KeyValuePair<string, ScriptNode> P in this.members)
				{
					if (Object2.TryGetValue(P.Key, out object E))
					{
						if (E is Array A)
							Result = P.Value.PatternMatch(VectorDefinition.Encapsulate(A, false, this), AlreadyFound);
						else
							Result = P.Value.PatternMatch(Expression.Encapsulate(E), AlreadyFound);
					}
					else
						Result = P.Value.PatternMatch(ObjectValue.Null, AlreadyFound);

					if (Result != PatternMatchResult.Match)
						return Result;
				}
			}
			else if (CheckAgainst.AssociatedObjectValue is IDictionary<string, string> Object3)
			{
				this.CheckQuick();

				if (!this.hasWildcards)
				{
					foreach (KeyValuePair<string, string> P in Object3)
					{
						if (!this.quick.ContainsKey(P.Key))
							return PatternMatchResult.NoMatch;
					}
				}

				foreach (KeyValuePair<string, ScriptNode> P in this.members)
				{
					if (Object3.TryGetValue(P.Key, out string s))
						Result = P.Value.PatternMatch(new StringValue(s), AlreadyFound);
					else
						Result = P.Value.PatternMatch(ObjectValue.Null, AlreadyFound);

					if (Result != PatternMatchResult.Match)
						return Result;
				}
			}
			else
				return PatternMatchResult.NoMatch;

			return PatternMatchResult.Match;
		}

		private void CheckQuick()
		{
			if (this.quick is null)
			{
				Dictionary<string, ScriptNode> Quick = new Dictionary<string, ScriptNode>();

				foreach (KeyValuePair<string, ScriptNode> N in this.members)
					Quick[N.Key] = N.Value;

				this.quick = Quick;
			}
		}

	}
}
