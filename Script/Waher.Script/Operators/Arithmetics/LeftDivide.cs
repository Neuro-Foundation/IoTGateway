﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects.Sets;

namespace Waher.Script.Operators.Arithmetics
{
	/// <summary>
	/// Left-Division operator.
	/// </summary>
	public class LeftDivide : BinaryOperator 
	{
		/// <summary>
		/// Left-Division operator.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public LeftDivide(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
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

			return EvaluateDivision(Left, Right, this);
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

			return EvaluateDivision(Left, Right, this);
		}

		/// <summary>
		/// Divides the left operand from the right one.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Node">Node performing the operation.</param>
		/// <returns>Result</returns>
		public static IElement EvaluateDivision(IElement Left, IElement Right, ScriptNode Node)
		{
			IElement Result;
			IRingElement Temp;

			if (Left is IRingElement LE && Right is IRingElement RE)
			{
				// TODO: Optimize in case of matrices. It's more efficient to employ a solve algorithm than to compute the inverse and the multiply.

				Temp = LE.Invert();
				if (!(Temp is null))
				{
					Result = RE.MultiplyLeft(Temp);
					if (!(Result is null))
						return Result;

					Result = Temp.MultiplyRight(RE);
					if (!(Result is null))
						return Result;
				}
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
							LE = Left as IRingElement;
							RE = Right as IRingElement;
							if (!(LE is null) && !(RE is null))
							{
								Temp = LE.Invert();
								if (!(Temp is null))
								{
									Result = RE.MultiplyLeft(Temp);
									if (!(Result is null))
										return Result;

									Result = Temp.MultiplyRight(RE);
									if (!(Result is null))
										return Result;
								}
							}
						}
					}

					Result = EvaluateNamedOperator("op_Division", Left, Right, Node);
					if (!(Result is null))
						return Result;

					throw new ScriptRuntimeException("Operands cannot be divided.", Node);
				}
				else
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement RightChild in Right.ChildElements)
						Elements.Add(EvaluateDivision(Left, RightChild, Node));

					return Right.Encapsulate(Elements, Node);
				}
			}
			else
			{
				if (Right.IsScalar)
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement LeftChild in Left.ChildElements)
						Elements.Add(EvaluateDivision(LeftChild, Right, Node));

					return Left.Encapsulate(Elements, Node);
				}
				else
				{
					if (Left is ISet Set1 && Right is ISet Set2)
						return new SetDifference(Set1, Set2);
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
									Elements.Add(EvaluateDivision(eLeft.Current, eRight.Current, Node));
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
									RightResult.Add(EvaluateDivision(LeftChild, RightChild, Node));

								LeftResult.Add(Right.Encapsulate(RightResult, Node));
							}

							return Left.Encapsulate(LeftResult, Node);
						}
					}
				}
			}
		}

	}
}
