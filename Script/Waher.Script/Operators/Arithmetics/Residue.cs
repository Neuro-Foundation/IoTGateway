﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators.Arithmetics
{
	/// <summary>
	/// Residue operator.
	/// </summary>
	public class Residue : BinaryOperator
	{
		/// <summary>
		/// Residue operator.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Residue(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
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
			IElement Left = this.left.Evaluate(Variables);
			IElement Right = this.right.Evaluate(Variables);

			return EvaluateResidue(Left, Right, this);
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

			IElement Left = await this.left.EvaluateAsync(Variables);
			IElement Right = await this.right.EvaluateAsync(Variables);

			return EvaluateResidue(Left, Right, this);
		}

		/// <summary>
		/// Divides the right operand from the left one.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Node">Node performing the operation.</param>
		/// <returns>Result</returns>
		public static IElement EvaluateResidue(IElement Left, IElement Right, ScriptNode Node)
		{
			if (Left.AssociatedObjectValue is double dl && Right.AssociatedObjectValue is double dr)
			{
				if (dl < long.MinValue || dl > long.MaxValue || dl != Math.Truncate(dl))
					throw new ScriptRuntimeException("Modulus operator does not work on decimal numbers.", Node);

				if (dr < long.MinValue || dr > long.MaxValue || dr != Math.Truncate(dr))
					throw new ScriptRuntimeException("Modulus operator does not work on decimal numbers.", Node);

				long l = (long)dl;
				long r = (long)dr;

				return new DoubleNumber(l % r);
			}

			if (Left.IsScalar)
			{
				if (Right.IsScalar)
				{
					ISet LeftSet = Left.AssociatedSet;
					ISet RightSet = Right.AssociatedSet;

					if (!LeftSet.Equals(RightSet))
					{
						if (Expression.UpgradeField(ref Left, ref LeftSet, ref Right, ref RightSet))
						{
							if (Left is IEuclidianDomainElement LE && Right is IEuclidianDomainElement RE)
							{
								((IEuclidianDomain)LeftSet).Divide(LE, RE, out IEuclidianDomainElement Result);
								return Result;
							}
						}
					}

					IElement Result2 = EvaluateNamedOperator("op_Modulus", Left, Right, Node);
					if (!(Result2 is null))
						return Result2;

					throw new ScriptRuntimeException("Residue could not be computed.", Node);
				}
				else
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement RightChild in Right.ChildElements)
						Elements.Add(EvaluateResidue(Left, RightChild, Node));

					return Right.Encapsulate(Elements, Node);
				}
			}
			else
			{
				if (Right.IsScalar)
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement LeftChild in Left.ChildElements)
						Elements.Add(EvaluateResidue(LeftChild, Right, Node));

					return Left.Encapsulate(Elements, Node);
				}
				else
				{
					ICollection<IElement> LeftChildren = Left.ChildElements;
					ICollection<IElement> RightChildren = Right.ChildElements;

					if (LeftChildren.Count == RightChildren.Count)
					{
						ChunkedList<IElement> Elements = new ChunkedList<IElement>();
						IEnumerator<IElement> eLeft = LeftChildren.GetEnumerator();
						IEnumerator<IElement> eRight = RightChildren.GetEnumerator();

						try
						{
							while (eLeft.MoveNext() && eRight.MoveNext())
								Elements.Add(EvaluateResidue(eLeft.Current, eRight.Current, Node));
						}
						finally
						{
							eLeft.Dispose();
							eRight.Dispose();
						}

						return Left.Encapsulate(Elements, Node);
					}
					else
					{
						ChunkedList<IElement> LeftResult = new ChunkedList<IElement>();

						foreach (IElement LeftChild in LeftChildren)
						{
							ChunkedList<IElement> RightResult = new ChunkedList<IElement>();

							foreach (IElement RightChild in RightChildren)
								RightResult.Add(EvaluateResidue(LeftChild, RightChild, Node));

							LeftResult.Add(Right.Encapsulate(RightResult, Node));
						}

						return Left.Encapsulate(LeftResult, Node);
					}
				}
			}
		}

	}
}
