﻿using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Graphs.Functions.Plots
{
	/// <summary>
	/// Plots a two-dimensional polygon.
	/// </summary>
	/// <example>
	/// t:=0..9;
	/// x:=sin(t*pi/5);
	/// y:=cos(t*pi/5);
	/// polygon2d(x,y)
	/// </example>
	public class Polygon2D : FunctionMultiVariate
	{
		private static readonly ArgumentType[] argumentTypes3Parameters = new ArgumentType[] { ArgumentType.Vector, ArgumentType.Vector, ArgumentType.Scalar };
		private static readonly ArgumentType[] argumentTypes2Parameters = new ArgumentType[] { ArgumentType.Vector, ArgumentType.Vector };

		/// <summary>
		/// Plots a two-dimensional polygon.
		/// </summary>
		/// <param name="X">X-axis.</param>
		/// <param name="Y">Y-axis.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Polygon2D(ScriptNode X, ScriptNode Y, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { X, Y }, argumentTypes2Parameters, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Plots a two-dimensional polygon.
		/// </summary>
		/// <param name="X">X-axis.</param>
		/// <param name="Y">Y-axis.</param>
		/// <param name="Color">Color</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Polygon2D(ScriptNode X, ScriptNode Y, ScriptNode Color, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { X, Y, Color }, argumentTypes3Parameters, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(Polygon2D);

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames
		{
			get { return new string[] { "x", "y", "color" }; }
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			if (!(Arguments[0] is IVector X))
				throw new ScriptRuntimeException("Expected vector for X argument.", this);

			if (!(Arguments[1] is IVector Y))
				throw new ScriptRuntimeException("Expected vector for Y argument.", this);

			int Dimension = X.Dimension;
			if (Y.Dimension != Dimension)
				throw new ScriptRuntimeException("Vector size mismatch.", this);

			IElement Color = Arguments.Length <= 2 ? null : Arguments[2];

			return new Graph2D(Variables, X, Y, new Polygon2DPainter(), false, false, this, 
				Color?.AssociatedObjectValue ?? Graph.DefaultColor);
		}
	}
}
