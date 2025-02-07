﻿using System;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Functions.Runtime
{
    /// <summary>
    /// Destroys a value. If the function references a variable, the variable is also removed.
    /// </summary>
    public class Destroy : FunctionOneVariable
    {
        private readonly string variableName = string.Empty;

        /// <summary>
        /// Destroys a value. If the function references a variable, the variable is also removed.
        /// </summary>
        /// <param name="Argument">Argument.</param>
        /// <param name="Start">Start position in script expression.</param>
        /// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
        public Destroy(ScriptNode Argument, int Start, int Length, Expression Expression)
            : base(Argument, Start, Length, Expression)
        {
			if (Argument is VariableReference Ref)
				this.variableName = Ref.VariableName;
		}

        /// <summary>
        /// Name of variable.
        /// </summary>
        public string VariableName => this.variableName;

        /// <summary>
        /// Name of the function
        /// </summary>
        public override string FunctionName => nameof(Destroy);

        /// <summary>
        /// Optional aliases. If there are no aliases for the function, null is returned.
        /// </summary>
        public override string[] Aliases => new string[] { "delete" };

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
        /// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
        /// </summary>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Result.</returns>
		public override async Task<IElement> EvaluateAsync(Variables Variables)
		{
            IElement Element;

            if (!string.IsNullOrEmpty(this.variableName))
            {
                if (Variables.TryGetVariable(this.variableName, out Variable v))
                {
                    Variables.Remove(this.variableName);
                    Element = v.ValueElement;
                }
                else
                    Element = null;
            }
            else
                Element = await this.Argument.EvaluateAsync(Variables);

            if (!(Element is null))
            {
                object Obj = Element.AssociatedObjectValue;

				if (Obj is IDisposableAsync DAsync)
					await DAsync.DisposeAsync();
				else if (Obj is IDisposable D)
                    D.Dispose();
            }

            return ObjectValue.Null;
        }

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement Argument, Variables Variables)
        {
            return ObjectValue.Null;
        }
    }
}
