﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Content.Functions.Encoding
{
	/// <summary>
	/// Decode(Content,ContentType)
	/// </summary>
	public class Decode : FunctionTwoScalarVariables
	{
		/// <summary>
		/// Decode(Content,ContentType)
		/// </summary>
		/// <param name="Content">Content</param>
		/// <param name="ContentType">Content Type</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Decode(ScriptNode Content, ScriptNode ContentType, int Start, int Length, Expression Expression)
			: base(Content, ContentType, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(Decode);

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="ScriptNode.EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(IElement Argument1, IElement Argument2, Variables Variables)
		{
			return this.EvaluateScalarAsync(Argument1, Argument2, Variables).Result;
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override Task<IElement> EvaluateScalarAsync(IElement Argument1, IElement Argument2, Variables Variables)
		{
			if (!(Argument1.AssociatedObjectValue is byte[] Bin))
				throw new ScriptRuntimeException("Binary data expected.", this);

			string ContentType = Argument2.AssociatedObjectValue is string s2 ? s2 : Expression.ToString(Argument2.AssociatedObjectValue);

			return this.DoDecodeAsync(Bin, ContentType, null);
		}

		private async Task<IElement> DoDecodeAsync(byte[] Data, string ContentType, System.Text.Encoding Encoding)
		{
			ContentResponse Content;

			if (Encoding is null)
				Content = await InternetContent.DecodeAsync(ContentType, Data, null);
			else
				Content = await InternetContent.DecodeAsync(ContentType, Data, Encoding, Array.Empty<KeyValuePair<string, string>>(), null);

			if (Content.HasError)
				throw new ScriptRuntimeException(Content.Error.Message, this, Content.Error);

			if (Content.Decoded is string[][] Records)
			{
				int Rows = Records.Length;
				int MaxCols = 0;
				int i, c;

				foreach (string[] Rec in Records)
				{
					if (Rec is null)
						continue;

					c = Rec.Length;
					if (c > MaxCols)
						MaxCols = c;
				}

				ChunkedList<IElement> Elements = new ChunkedList<IElement>();

				foreach (string[] Rec in Records)
				{
					i = 0;

					if (!(Rec is null))
					{
						foreach (string s in Rec)
						{
							if (s is null || string.IsNullOrEmpty(s))
								Elements.Add(new ObjectValue(null));
							else if (CommonTypes.TryParse(s, out double dbl))
								Elements.Add(new DoubleNumber(dbl));
							else if (CommonTypes.TryParse(s, out bool b))
								Elements.Add(new BooleanValue(b));
							else if (XML.TryParse(s, out DateTime TP))
								Elements.Add(new DateTimeValue(TP));
							else if (TimeSpan.TryParse(s, out TimeSpan TS))
								Elements.Add(new ObjectValue(TS));
							else
								Elements.Add(new StringValue(s));

							i++;
						}
					}

					while (i++ < MaxCols)
						Elements.Add(new StringValue(string.Empty));
				}

				return Operators.Matrices.MatrixDefinition.Encapsulate(Elements, Rows, MaxCols, this);
			}
			else
				return Expression.Encapsulate(Content.Decoded);
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(string Argument1, string Argument2, Variables Variables)
		{
			return this.EvaluateScalarAsync(Argument1, Argument2, Variables).Result;
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override Task<IElement> EvaluateScalarAsync(string Argument1, string Argument2, Variables Variables)
		{
			System.Text.Encoding Encoding = System.Text.Encoding.UTF8;
			byte[] Bin = Encoding.GetBytes(Argument1);

			return this.DoDecodeAsync(Bin, Argument2, Encoding);
		}
	}
}
