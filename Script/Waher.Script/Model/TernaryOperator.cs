﻿namespace Waher.Script.Model
{
	/// <summary>
	/// Base class for all ternary operators.
	/// </summary>
	public abstract class TernaryOperator : BinaryOperator
	{
		/// <summary>
		/// Middle operand.
		/// </summary>
		protected ScriptNode middle;

		/// <summary>
		/// Base class for all ternary operators.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Middle">Middle operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public TernaryOperator(ScriptNode Left, ScriptNode Middle, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
			this.middle = Middle;
			this.middle?.SetParent(this);

			this.CalcIsAsync();
		}

		/// <summary>
		/// Recalculates if operator is asynchronous or not.
		/// </summary>
		protected override void CalcIsAsync()
		{
			this.isAsync =
				(this.left?.IsAsynchronous ?? false) ||
				(this.middle?.IsAsynchronous ?? false) ||
				(this.right?.IsAsynchronous ?? false);
		}

		/// <summary>
		/// Middle operand.
		/// </summary>
		public ScriptNode MiddleOperand => this.middle;

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
				if (!(this.left?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				if (!(this.middle?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				if (!(this.right?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;
			}

			ScriptNode NewNode;
			bool RecalcIsAsync = false;
			bool b;

			if (!(this.left is null))
			{
				b = !Callback(this.left, out NewNode, State);
				if (!(NewNode is null))
				{
					this.left = NewNode;
					this.left.SetParent(this);
				
					RecalcIsAsync = true;
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.left.ForAllChildNodes(Callback, State, Order)))
				{
					if (RecalcIsAsync)
						this.CalcIsAsync();

					return false;
				}
			}

			if (!(this.middle is null))
			{
				b = !Callback(this.middle, out NewNode, State);
				if (!(NewNode is null))
				{
					this.middle = NewNode;
					this.middle.SetParent(this);
				
					RecalcIsAsync = true;
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.middle.ForAllChildNodes(Callback, State, Order)))
				{
					if (RecalcIsAsync)
						this.CalcIsAsync();

					return false;
				}
			}

			if (!(this.right is null))
			{
				b = !Callback(this.right, out NewNode, State);
				if (!(NewNode is null))
				{
					this.right = NewNode;
					this.right.SetParent(this);
				
					RecalcIsAsync = true;
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.right.ForAllChildNodes(Callback, State, Order)))
				{
					if (RecalcIsAsync)
						this.CalcIsAsync();

					return false;
				}
			}

			if (RecalcIsAsync)
				this.CalcIsAsync();

			if (Order == SearchMethod.BreadthFirst)
			{
				if (!(this.left?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				if (!(this.middle?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				if (!(this.right?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is TernaryOperator O &&
				AreEqual(this.middle, O.middle) &&
				base.Equals(obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = base.GetHashCode();
			Result ^= Result << 5 ^ GetHashCode(this.middle);
			return Result;
		}

	}
}
