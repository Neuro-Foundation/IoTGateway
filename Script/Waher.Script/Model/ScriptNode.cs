﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Objects;
using Waher.Script.Operators.Arithmetics;

namespace Waher.Script.Model
{
	/// <summary>
	/// Status result of a pattern matching operation.
	/// </summary>
	public enum PatternMatchResult
	{
		/// <summary>
		/// Script branch matches pattern
		/// </summary>
		Match,

		/// <summary>
		/// Script branch does not match pattern
		/// </summary>
		NoMatch,

		/// <summary>
		/// Pattern match could not be evaluated.
		/// </summary>
		Unknown
	}

	/// <summary>
	/// Method to traverse the expression structure
	/// </summary>
	public enum SearchMethod
	{
		/// <summary>
		/// Children are processed before their corresponding parents.
		/// </summary>
		DepthFirst,

		/// <summary>
		/// Siblings are processed before their corresponding children.
		/// </summary>
		BreadthFirst,

		/// <summary>
		/// After each node is processed, their children are recursively processed,
		/// before going on to the next sibling.
		/// </summary>
		TreeOrder
	}

	/// <summary>
	/// Delegate for ScriptNode callback methods.
	/// </summary>
	/// <param name="Node">Node being processed. Change the reference to change the structure of the expression.</param>
	/// <param name="NewNode">A new node to replace the old node, or null if no replacement necessary.</param>
	/// <param name="State">State object.</param>
	/// <returns>true if process is to continue, false if it is completed.</returns>
	public delegate bool ScriptNodeEventHandler(ScriptNode Node, out ScriptNode NewNode, object State);

	/// <summary>
	/// Base class for all nodes in a parsed script tree.
	/// </summary>
	public abstract class ScriptNode
	{
		private readonly Expression expression;
		private ScriptNode parent;
		private int start;
		private int length;

		/// <summary>
		/// Base class for all nodes in a parsed script tree.
		/// </summary>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ScriptNode(int Start, int Length, Expression Expression)
		{
			this.start = Start;
			this.length = Length;
			this.expression = Expression;
		}

		/// <summary>
		/// Start position in script expression.
		/// </summary>
		public int Start
		{
			get => this.start;
			internal set => this.start = value;
		}

		/// <summary>
		/// Length of expression covered by node.
		/// </summary>
		public int Length
		{
			get => this.length;
			internal set => this.length = value;
		}

		/// <summary>
		/// Sets the <see cref="Start"/> and <see cref="Length"/> properties of the node.
		/// </summary>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		protected void SetSubExpression(int Start, int Length)
		{
			if (Start < 0)
				throw new ArgumentOutOfRangeException(nameof(Start), "Cannot be negative.");

			if (Length < 0)
				throw new ArgumentOutOfRangeException(nameof(Length), "Cannot be negative.");

			this.start = Start;
			this.length = Length;
		}

		/// <summary>
		/// Parent node.
		/// </summary>
		public ScriptNode Parent => this.parent;

		/// <summary>
		/// Sets the parent node. Can only be used when expression is being parsed.
		/// </summary>
		/// <param name="Parent">Parent Node</param>
		public void SetParent(ScriptNode Parent)
		{
			if (this.expression?.Root is null)
				this.parent = Parent;
		}

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="EvaluateAsync(Variables)"/>.
		/// </summary>
		public virtual bool IsAsynchronous => false;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is false.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public abstract IElement Evaluate(Variables Variables);

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// This method should be used for nodes whose <see cref="IsAsynchronous"/> is true.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public virtual Task<IElement> EvaluateAsync(Variables Variables)
		{
			return Task.FromResult(this.Evaluate(Variables));
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public virtual PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			return PatternMatchResult.Unknown;
		}

		/// <summary>
		/// Expression of which the node is a part.
		/// </summary>
		public Expression Expression => this.expression;

		/// <summary>
		/// Sub-expression defining the node.
		/// </summary>
		public string SubExpression
		{
			get { return this.expression.Script.Substring(this.start, this.length); }
		}

		/// <summary>
		/// Implements the differentiation chain rule, by differentiating the argument and multiplying it to the differentiation of the main node.
		/// </summary>
		/// <param name="VariableName">Name of variable to differentiate on.</param>
		/// <param name="Variables">Collection of variables.</param>
		/// <param name="Argument">Inner argument</param>
		/// <param name="Differentiation">Differentiation of main node.</param>
		/// <returns><paramref name="Differentiation"/>*D(<paramref name="Argument"/>)</returns>
		protected ScriptNode DifferentiationChainRule(string VariableName, Variables Variables, ScriptNode Argument, ScriptNode Differentiation)
		{
			if (Argument is IDifferentiable Differentiable)
			{
				ScriptNode ChainFactor = Differentiable.Differentiate(VariableName, Variables);

				if (ChainFactor is ConstantElement ConstantElement &&
					ConstantElement.Constant.AssociatedObjectValue is double d)
				{
					if (d == 0)
						return ConstantElement;
					else if (d == 1)
						return Differentiation;
				}

				int Start = this.Start;
				int Len = this.Length;
				Expression Exp = this.Expression;

				if (Differentiation is Invert Invert)
				{
					if (Invert.Operand is Negate Negate)
						return new Negate(new Divide(ChainFactor, Negate.Operand, Start, Len, this.Expression), Start, Len, Exp);
					else
						return new Divide(ChainFactor, Invert.Operand, Start, Len, Exp);
				}
				else if (Differentiation is Negate Negate)
				{
					if (Negate.Operand is Invert Invert2)
						return new Negate(new Divide(ChainFactor, Invert2.Operand, Start, Len, this.Expression), Start, Len, Exp);
					else
						return new Negate(new Multiply(Negate.Operand, ChainFactor, Start, Len, this.Expression), Start, Len, Exp);
				}
				else
					return new Multiply(Differentiation, ChainFactor, Start, Len, Exp);
			}
			else
				throw new ScriptRuntimeException("Argument not differentiable.", this);
		}

		/// <summary>
		/// Calls the callback method for all child nodes.
		/// </summary>
		/// <param name="Callback">Callback method to call.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <param name="DepthFirst">If calls are made depth first (true) or on each node and then its leaves (false).</param>
		/// <returns>If the process was completed.</returns>
		[Obsolete("Use ForAllChildNodes(ScriptNodeEventHandler, object, SearchMethod) instead.")]
		public bool ForAllChildNodes(ScriptNodeEventHandler Callback, object State, bool DepthFirst)
		{
			return this.ForAllChildNodes(Callback, State, DepthFirst ? SearchMethod.DepthFirst : SearchMethod.BreadthFirst);
		}

		/// <summary>
		/// Calls the callback method for all child nodes.
		/// </summary>
		/// <param name="Callback">Callback method to call.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <param name="Order">Order to traverse the nodes.</param>
		/// <returns>If the process was completed.</returns>
		public abstract bool ForAllChildNodes(ScriptNodeEventHandler Callback, object State, SearchMethod Order);

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return this.GetType() == obj.GetType();
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return this.GetType().GetHashCode();
		}

		/// <summary>
		/// Compares if two script nodes are equal.
		/// </summary>
		/// <param name="S1">Node 1. Can be null.</param>
		/// <param name="S2">Node 2. Can be null.</param>
		/// <returns>If the size and contents of the arrays are equal</returns>
		public static bool AreEqual(ScriptNode S1, ScriptNode S2)
		{
			if (S1 is null ^ S2 is null)
				return false;

			if (!(S1 is null) && !S1.Equals(S2))
				return false;

			return true;
		}

		/// <summary>
		/// Compares if the contents of two enumerable sets are equal
		/// </summary>
		/// <param name="A1">Array 1</param>
		/// <param name="A2">Array 2</param>
		/// <returns>If the size and contents of the arrays are equal</returns>
		public static bool AreEqual(IEnumerable A1, IEnumerable A2)
		{
			if (A1 is null ^ A2 is null)
				return false;

			if (A1 is null)
				return true;

			IEnumerator e1 = A1.GetEnumerator();
			IEnumerator e2 = A2.GetEnumerator();

			while (true)
			{
				bool b1 = e1.MoveNext();
				bool b2 = e2.MoveNext();

				if (b1 ^ b2)
					return false;

				if (!b1)
					return true;

				object Item1 = e1.Current;
				object Item2 = e2.Current;

				if (Item1 is null ^ Item2 is null)
					return false;

				if (!(Item1 is null) && !Item1.Equals(Item2))
					return false;
			}
		}

		/// <summary>
		/// Calculates a hash code for the contents of an array.
		/// </summary>
		/// <param name="Node">Node. Can be null.</param>
		/// <returns>Hash code</returns>
		public static int GetHashCode(ScriptNode Node)
		{
			return Node?.GetHashCode() ?? 0;
		}

		/// <summary>
		/// Calculates a hash code for the contents of an array.
		/// </summary>
		/// <param name="Set">Enumerable set.</param>
		/// <returns>Hash code</returns>
		public static int GetHashCode(IEnumerable Set)
		{
			int Result = 0;

			if (!(Set is null))
			{
				foreach (object Item in Set)
				{
					if (!(Item is null))
						Result ^= Result << 5 ^ Item.GetHashCode();
					else
						Result ^= Result << 5;
				}
			}

			return Result;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			if (this.start >= 0 && this.length > 0 &&
				this.start + this.length <= this.expression.Script.Length)
			{
				return this.SubExpression;
			}
			else
				return base.ToString();
		}

		/// <summary>
		/// Tries to convert an element to a boolean value.
		/// </summary>
		/// <param name="Value">Element value.</param>
		/// <returns>Boolean, if successful.</returns>
		protected static bool? ToBoolean(IElement Value)
		{
			object Obj = Value.AssociatedObjectValue;

			if (Obj is bool b)
				return b;
			else if (Obj is double d)
				return d != 0;
			else if (Obj is string s)
				return Functions.Scalar.Boolean.ToBoolean(s);
			else
				return null;
		}

		/// <summary>
		/// Tries to convert an element to an enumeration value.
		/// </summary>
		/// <param name="Value">Element value.</param>
		/// <returns>Enumeration value, if successful.</returns>
		protected T ToEnum<T>(IElement Value)
			where T : struct
		{
			object Obj = Value.AssociatedObjectValue;

			if (Value is T e)
				return e;
			else if (Obj is double d)
				return (T)Enum.ToObject(typeof(T), (int)d);

			string s = Obj?.ToString() ?? string.Empty;

			if (Enum.TryParse(s, out T Result))
				return Result;

			throw new ScriptRuntimeException("Enumeration of type " + typeof(T).FullName + " expected.", this);
		}

		/// <summary>
		/// Waits for any asynchronous process to terminate.
		/// </summary>
		/// <param name="Result">Result, possibly asynchronous result.</param>
		/// <returns>Finished result</returns>
		public static async Task<object> WaitPossibleTask(object Result)
		{
			if (Result is Task Task)
			{
				await Task;

				PropertyInfo PI = Task.GetType().GetRuntimeProperty("Result");
				Result = PI?.GetMethod?.Invoke(Task, null);
			}

			return Result;
		}

		/// <summary>
		/// Checks if <paramref name="Result"/> is an asynchronous results. If so, blocks the current thread until the result
		/// is completed, and returns the completed result instead.
		/// </summary>
		/// <param name="Result">Result</param>
		/// <returns>Finished result.</returns>
		public static object UnnestPossibleTaskSync(object Result)
		{
			if (Result is Task Task)
			{
				Task.Wait();

				PropertyInfo PI = Task.GetType().GetRuntimeProperty("Result");
				Result = PI?.GetMethod?.Invoke(Task, null);
			}

			return Result;
		}

		/// <summary>
		/// Empty Script Node
		/// </summary>
		public static readonly ScriptNode EmptyNode = new ConstantElement(ObjectValue.Null, 0, 0, new Expression("null"));
	}
}
