﻿using System;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Security.EllipticCurves;

namespace Waher.Script.Cryptography.Functions.Ecc
{
	/// <summary>
	/// Creates an NIST-P192 Elliptic curve.
	/// </summary>
	public class P192 : FunctionMultiVariate
	{
		/// <summary>
		/// Creates an NIST-P192 Elliptic curve.
		/// </summary>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public P192(int Start, int Length, Expression Expression)
			: base(Array.Empty<ScriptNode>(), argumentTypes0, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Creates an NIST-P192 Elliptic curve.
		/// </summary>
		/// <param name="PrivateKey">Private key.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public P192(ScriptNode PrivateKey, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { PrivateKey }, argumentTypes1Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(P192);

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "PrivateKey" };

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			int c = Arguments.Length;

			if (c == 0)
				return new ObjectValue(new NistP192());

			object Obj = Arguments[0].AssociatedObjectValue;
			if (!(Obj is byte[] PrivateKey))
				PrivateKey = Convert.FromBase64String(Obj?.ToString());

			return new ObjectValue(new NistP192(PrivateKey));
		}
	}
}
