﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.VectorSpaces;

namespace Waher.Script.Content.Functions.Encoding
{
	/// <summary>
	/// Encode(Object)
	/// </summary>
	public class Encode : FunctionMultiVariate
	{
		/// <summary>
		/// Encode(Object)
		/// </summary>
		/// <param name="Object">Object to encode</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Encode(ScriptNode Object, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Object }, argumentTypes1Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Encode(Object,AcceptedTypes)
		/// </summary>
		/// <param name="Object">Object to encode</param>
		/// <param name="AcceptedTypes">Accepted content types.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Encode(ScriptNode Object, ScriptNode AcceptedTypes, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Object, AcceptedTypes }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Vector },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(Encode);

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Object" };

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="ScriptNode.EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			return this.EvaluateAsync(Arguments, Variables).Result;
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override async Task<IElement> EvaluateAsync(IElement[] Arguments, Variables Variables)
		{
			ContentResponse Content;

			if (Arguments.Length > 1)
			{
				if (!(Arguments[1].AssociatedObjectValue is Array A))
					throw new ScriptRuntimeException("Second parameter to Encode should be an array of acceptable content types.", this);

				int i, c = A.Length;
				string[] AcceptedTypes = new string[c];

				for (i = 0; i < c; i++)
					AcceptedTypes[i] = (await WaitPossibleTask(A.GetValue(i)))?.ToString();

				Content = await InternetContent.EncodeAsync(Arguments[0].AssociatedObjectValue, System.Text.Encoding.UTF8, AcceptedTypes);
			}
			else
				Content = await InternetContent.EncodeAsync(Arguments[0].AssociatedObjectValue, System.Text.Encoding.UTF8);

			return new ObjectVector(new ObjectValue(Content.Encoded), new StringValue(Content.ContentType));
		}

	}
}
