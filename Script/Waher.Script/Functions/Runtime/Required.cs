﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Functions.Runtime
{
    /// <summary>
    /// Makes sure an expression is defined. Otherwise, an exception is thrown.
    /// </summary>
    public class Required : FunctionOneVariable
    {
        /// <summary>
        /// Makes sure an expression is defined. Otherwise, an exception is thrown.
        /// </summary>
        /// <param name="Argument">Argument.</param>
        /// <param name="Start">Start position in script expression.</param>
        /// <param name="Length">Length of expression covered by node.</param>
        /// <param name="Expression">Expression containing script.</param>
        public Required(ScriptNode Argument, int Start, int Length, Expression Expression)
            : base(Argument, Start, Length, Expression)
        {
        }

        /// <summary>
        /// Name of the function
        /// </summary>
        public override string FunctionName => nameof(Required);

        /// <summary>
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
        public override IElement Evaluate(Variables Variables)
        {
            IElement E = this.Argument.Evaluate(Variables);
            if (E.AssociatedObjectValue is null)
                throw new ScriptRuntimeException("Not defined.", this);

            return E;
        }

        /// <summary>
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
        public override async Task<IElement> EvaluateAsync(Variables Variables)
        {
            IElement E = await this.Argument.EvaluateAsync(Variables);
            if (E.AssociatedObjectValue is null)
                throw new ScriptRuntimeException("Not defined.", this);

            return E;
        }

        /// <summary>
        /// Evaluates the function.
        /// </summary>
        /// <param name="Argument">Function argument.</param>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Function result.</returns>
		public override IElement Evaluate(IElement Argument, Variables Variables)
        {
            return Argument;
        }

        /// <summary>
        /// Performs a pattern match operation.
        /// </summary>
        /// <param name="CheckAgainst">Value to check against.</param>
        /// <param name="AlreadyFound">Variables already identified.</param>
        /// <returns>Pattern match result</returns>
        public override PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
            if (CheckAgainst.AssociatedObjectValue is null)
                return PatternMatchResult.NoMatch;

            return this.Argument.PatternMatch(CheckAgainst, AlreadyFound);
		}
	}
}
