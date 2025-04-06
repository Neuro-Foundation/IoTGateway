﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Model;
using Waher.Script.Objects.Sets;

namespace Waher.Script.Operators.Sets
{
    /// <summary>
    /// Creates a set.
    /// </summary>
    public class SetDefinition : ElementList
    {
		/// <summary>
		/// Creates a set.
		/// </summary>
		/// <param name="Elements">Set elements.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public SetDefinition(ScriptNode[] Elements, int Start, int Length, Expression Expression)
            : base(Elements, Start, Length, Expression)
        {
        }

        /// <summary>
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
        public override IElement Evaluate(Variables Variables)
        {
			ChunkedList<IElement> Elements = new ChunkedList<IElement>();

            foreach (ScriptNode N in this.Elements)
                Elements.Add(N.Evaluate(Variables));

            return Encapsulate(Elements);
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

			ChunkedList<IElement> Elements = new ChunkedList<IElement>();

            foreach (ScriptNode N in this.Elements)
                Elements.Add(await N.EvaluateAsync(Variables));

            return Encapsulate(Elements);
        }

        /// <summary>
        /// Encapsulates the elements of a set.
        /// </summary>
        /// <param name="Elements">Set elements.</param>
        /// <returns>Encapsulated set.</returns>
        public static IElement Encapsulate(ICollection<IElement> Elements)
        {
            return new FiniteSet(Elements);
        }

		/// <summary>
		/// Encapsulates the elements of a set.
		/// </summary>
		/// <param name="Elements">Set elements.</param>
		/// <returns>Encapsulated set.</returns>
		public static IElement Encapsulate(IEnumerable Elements)
		{
			return new FiniteSet(Elements);
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			ScriptNode[] Elements = this.Elements;

            int? Size;

            if (!(CheckAgainst is ISet Set))
				return PatternMatchResult.NoMatch;

			if (!(Size = Set.Size).HasValue)
				return PatternMatchResult.Unknown;

			if (Size.Value != Elements.Length)
				return PatternMatchResult.NoMatch;

			PatternMatchResult Result;
			int i = 0;

			foreach (IElement E in Set.ChildElements)
			{
				Result = Elements[i++].PatternMatch(E, AlreadyFound);
				if (Result != PatternMatchResult.Match)
					return Result;
			}

			return PatternMatchResult.Match;
        }

    }
}