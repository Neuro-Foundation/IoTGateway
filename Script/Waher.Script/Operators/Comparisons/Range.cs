﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators.Comparisons
{
	/// <summary>
	/// Range operator
	/// </summary>
	public class Range : TernaryOperator
	{
		private readonly bool leftInclusive;
		private readonly bool rightInclusive;

		/// <summary>
		/// Range operator
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Middle">Middle operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="LeftInclusive">If the value specified by <paramref name="Left"/> is included in the range.</param>
		/// <param name="RightInclusive">If the value specified by <paramref name="Right"/> is included in the range.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Range(ScriptNode Left, ScriptNode Middle, ScriptNode Right, bool LeftInclusive, bool RightInclusive,
			int Start, int Length, Expression Expression)
			: base(Left, Middle, Right, Start, Length, Expression)
		{
			this.leftInclusive = LeftInclusive;
			this.rightInclusive = RightInclusive;
		}

		/// <summary>
		/// If the value specified by <see cref="BinaryOperator.LeftOperand"/> is included in the range.
		/// </summary>
		public bool LeftInclusive => this.leftInclusive;

		/// <summary>
		/// If the value specified by <see cref="BinaryOperator.RightOperand"/> is included in the range.
		/// </summary>
		public bool RightInclusive => this.rightInclusive;

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			IElement Left = this.left.Evaluate(Variables);
			IElement Middle = this.middle.Evaluate(Variables);
			IElement Right = this.right.Evaluate(Variables);

			return this.Evaluate(Left, Middle, Right);
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

			IElement Left = await this.left.EvaluateAsync(Variables);
			IElement Middle = await this.middle.EvaluateAsync(Variables);
			IElement Right = await this.right.EvaluateAsync(Variables);

			return this.Evaluate(Left, Middle, Right);
		}

		private IElement Evaluate(IElement Left, IElement Middle, IElement Right)
		{
			if (!(Middle.AssociatedSet is IOrderedSet S))
				throw new ScriptRuntimeException("Cannot compare operands.", this);

			int i = S.Compare(Middle, Left);

			if (i < 0 || (i == 0 && !this.leftInclusive))
				return BooleanValue.False;

			i = S.Compare(Middle, Right);

			if (i > 0 || (i == 0 && !this.rightInclusive))
				return BooleanValue.False;

			return BooleanValue.True;
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			if (!(CheckAgainst.AssociatedSet is IOrderedSet S))
				return PatternMatchResult.NoMatch;

			IElement LeftLimit;
			IElement RightLimit;

			if (this.left is ConstantElement LeftConstant)
				LeftLimit = LeftConstant.Constant;
			else if (!(this.left is VariableReference LeftVariable) ||
				!Expression.TryGetConstant(LeftVariable.VariableName, null, out LeftLimit))
			{
				return PatternMatchResult.NoMatch;
			}

			if (this.right is ConstantElement RightConstant)
				RightLimit = RightConstant.Constant;
			else if (!(this.right is VariableReference RightVariable) ||
				!Expression.TryGetConstant(RightVariable.VariableName, null, out RightLimit))
			{
				return PatternMatchResult.NoMatch;
			}

			int i = S.Compare(CheckAgainst, LeftLimit);

			if (i < 0 || (i == 0 && !this.leftInclusive))
				return PatternMatchResult.NoMatch;

			i = S.Compare(CheckAgainst, RightLimit);

			if (i > 0 || (i == 0 && !this.rightInclusive))
				return PatternMatchResult.NoMatch;

			return this.middle.PatternMatch(CheckAgainst, AlreadyFound);
		}

	}
}
