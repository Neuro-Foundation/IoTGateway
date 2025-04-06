﻿using System;
using System.Numerics;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Operators.Arithmetics;

namespace Waher.Script.Functions.Analytic
{
	/// <summary>
	/// Sqrt(x)
	/// </summary>
	public class Sqrt : FunctionOneScalarVariable, IDifferentiable
	{
		/// <summary>
		/// Sqrt(x)
		/// </summary>
		/// <param name="Argument">Argument.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Sqrt(ScriptNode Argument, int Start, int Length, Expression Expression)
			: base(Argument, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(Sqrt);

		/// <summary>
		/// Optional aliases. If there are no aliases for the function, null is returned.
		/// </summary>
		public override string[] Aliases => new string[] { "√" };

		/// <summary>
		/// Differentiates a script node, if possible.
		/// </summary>
		/// <param name="VariableName">Name of variable to differentiate on.</param>
		/// <param name="Variables">Collection of variables.</param>
		/// <returns>Differentiated node.</returns>
		public ScriptNode Differentiate(string VariableName, Variables Variables)
		{
			if (VariableName == this.DefaultVariableName)
			{
				int Start = this.Start;
				int Len = this.Length;
				Expression Exp = this.Expression;

				return this.DifferentiationChainRule(VariableName, Variables, this.Argument,
					new Invert(
						new Multiply(
							new ConstantElement(DoubleNumber.TwoElement, Start, Len, Exp),
							this,
							Start, Len, Exp),
						Start, Len, Exp));
			}
			else
				return new ConstantElement(DoubleNumber.ZeroElement, this.Start, this.Length, this.Expression);
		}

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(double Argument, Variables Variables)
		{
			if (Argument < 0)
				return new ComplexNumber(0, Math.Sqrt(-Argument));
			else
				return new DoubleNumber(Math.Sqrt(Argument));
		}

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(Complex Argument, Variables Variables)
		{
			return new ComplexNumber(Complex.Sqrt(Argument));
		}

		/// <summary>
		/// Calculates the square root of an operand.
		/// </summary>
		/// <param name="Operand">Operand whose square root is to be returned.</param>
		/// <param name="Node">Node performing the operation.</param>
		/// <returns>Result</returns>
		public static IElement EvaluateSquareRoot(IElement Operand, ScriptNode Node)
		{
			if (Operand is DoubleNumber D)
			{
				double d = D.Value;

				if (d < 0)
					return new ComplexNumber(0, Math.Sqrt(-d));
				else
					return new DoubleNumber(Math.Sqrt(d));
			}
			else if (Operand is ComplexNumber C)
				return new ComplexNumber(Complex.Sqrt(C.Value));
			else
				throw new ScriptRuntimeException("Unable to calculate the square root.", Node);
		}

	}
}
