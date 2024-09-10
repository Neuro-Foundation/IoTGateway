﻿using System.Collections.Generic;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators.Comparisons
{
	/// <summary>
	/// Greater Than Or Equal To.
	/// </summary>
	public class GreaterThanOrEqualTo : BinaryScalarOperator
    {
		/// <summary>
		/// Greater Than Or Equal To.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public GreaterThanOrEqualTo(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
		}

        /// <summary>
        /// Evaluates the operator on scalar operands.
        /// </summary>
        /// <param name="Left">Left value.</param>
        /// <param name="Right">Right value.</param>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result</returns>
        public override IElement EvaluateScalar(IElement Left, IElement Right, Variables Variables)
        {
			if (!(Left.AssociatedSet is IOrderedSet S))
				throw new ScriptRuntimeException("Cannot compare operands.", this);

			if (S.Compare(Left, Right) >= 0)
                return BooleanValue.True;
            else
                return BooleanValue.False;
        }

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			int i;

			if (!(CheckAgainst.AssociatedSet is IOrderedSet S))
				return PatternMatchResult.NoMatch;

			if (this.left is ConstantElement LeftConstant)
			{
				i = S.Compare(LeftConstant.Constant, CheckAgainst);
				if (i >= 0)
					return this.right.PatternMatch(CheckAgainst, AlreadyFound);
				else
					return PatternMatchResult.NoMatch;
			}

			if (this.right is ConstantElement RightConstant)
			{
				i = S.Compare(CheckAgainst, RightConstant.Constant);
				if (i >= 0)
					return this.left.PatternMatch(CheckAgainst, AlreadyFound);
				else
					return PatternMatchResult.NoMatch;
			}

			if (this.left is VariableReference LeftReference &&
				(AlreadyFound.TryGetValue(LeftReference.VariableName, out IElement Value) ||
				Expression.TryGetConstant(LeftReference.VariableName, null, out Value)))
			{
				i = S.Compare(Value, CheckAgainst);
				if (i >= 0)
					return this.right.PatternMatch(CheckAgainst, AlreadyFound);
				else
					return PatternMatchResult.NoMatch;
			}

			if (this.right is VariableReference RightReference &&
				(AlreadyFound.TryGetValue(RightReference.VariableName, out Value) ||
				Expression.TryGetConstant(RightReference.VariableName, null, out Value)))
			{
				i = S.Compare(CheckAgainst, Value);
				if (i >= 0)
					return this.left.PatternMatch(CheckAgainst, AlreadyFound);
				else
					return PatternMatchResult.NoMatch;
			}

			return PatternMatchResult.NoMatch;
		}
	}
}
