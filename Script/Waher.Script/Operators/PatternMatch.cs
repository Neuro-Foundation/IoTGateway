﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Operators
{
    /// <summary>
    /// Assignment operator.
    /// </summary>
    public class PatternMatch : BinaryOperator
    {
        /// <summary>
        /// Assignment operator.
        /// </summary>
        /// <param name="Left">Left Operand.</param>
        /// <param name="Right">Right Operand.</param>
        /// <param name="Start">Start position in script expression.</param>
        /// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
        public PatternMatch(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
            : base(Left, Right, Start, Length, Expression)
        {
        }

        /// <summary>
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
        public override IElement Evaluate(Variables Variables)
        {
            IElement Result = this.right.Evaluate(Variables);
			Match(this.left, Result, Variables, this);
			return Result;
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

            IElement Result = await this.right.EvaluateAsync(Variables);
            Match(this.left, Result, Variables, this);
            return Result;
        }
        /// <summary>
        /// Performs a pattern-matching operation
        /// </summary>
        /// <param name="Branch">Branch to examine</param>
        /// <param name="Value">Value to examine against</param>
        /// <param name="Variables">Current set of variables where corresponding variable references will be set.</param>
        /// <param name="Node">Node performing pattern matching operation.</param>
        public static void Match(ScriptNode Branch, IElement Value, Variables Variables, ScriptNode Node)
		{
			Dictionary<string, IElement> AlreadyFound = new Dictionary<string, IElement>();

			switch (Branch.PatternMatch(Value, AlreadyFound))
			{
				case PatternMatchResult.Match:
					foreach (KeyValuePair<string, IElement> P in AlreadyFound)
						Variables[P.Key] = P.Value;
					break;

				case PatternMatchResult.NoMatch:
					throw new ScriptRuntimeException("Pattern mismatch.", Node);

				case PatternMatchResult.Unknown:
				default:
					throw new ScriptRuntimeException("Unable to compute pattern match.", Node);
			}
		}
	}
}
