﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Operators.Matrices
{
	/// <summary>
	/// Matrix Index operator.
	/// </summary>
	public class MatrixIndex : NullCheckTernaryOperator
	{
		/// <summary>
		/// Matrix Index operator.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="X">X-coordinate operand.</param>
		/// <param name="Y">Y-coordinate operand.</param>
		/// <param name="NullCheck">If null should be returned if left operand is null.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public MatrixIndex(ScriptNode Left, ScriptNode X, ScriptNode Y, bool NullCheck, int Start, int Length, Expression Expression)
			: base(Left, X, Y, NullCheck, Start, Length, Expression)
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
			if (this.nullCheck && Left.AssociatedObjectValue is null)
				return Left;

            IElement Middle = this.middle.Evaluate(Variables);
            IElement Right = this.right.Evaluate(Variables);

            return EvaluateIndex(Left, Middle, Right, this.nullCheck, this);
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
            if (this.nullCheck && Left.AssociatedObjectValue is null)
                return Left;

            IElement Middle = await this.middle.EvaluateAsync(Variables);
            IElement Right = await this.right.EvaluateAsync(Variables);

            return EvaluateIndex(Left, Middle, Right, this.nullCheck, this);
        }
        /// <summary>
        /// Evaluates the matrix index operator.
        /// </summary>
        /// <param name="Matrix">Matrix</param>
        /// <param name="IndexX">X-Index</param>
        /// <param name="IndexY">Y-Index</param>
        /// <param name="NullCheck">If null should be returned if left operand is null.</param>
        /// <param name="Node">Node performing the operation.</param>
        /// <returns>Result</returns>
        public static IElement EvaluateIndex(IElement Matrix, IElement IndexX, IElement IndexY, bool NullCheck, ScriptNode Node)
        {
            if (Matrix is IMatrix M)
                return EvaluateIndex(M, IndexX, IndexY, Node);
            else if (Matrix.IsScalar)
			{
				if (NullCheck && Matrix.AssociatedObjectValue is null)
					return Matrix;

                throw new ScriptRuntimeException("The index operator operates on matrices.", Node);
			}
			else
            {
                ChunkedList<IElement> Elements = new ChunkedList<IElement>();

                foreach (IElement E in Matrix.ChildElements)
                    Elements.Add(EvaluateIndex(E, IndexX, IndexY, NullCheck, Node));

                return Matrix.Encapsulate(Elements, Node);
            }
        }

		/// <summary>
		/// Evaluates the vector index operator.
		/// </summary>
		/// <param name="Matrix">Vector</param>
		/// <param name="IndexX">X-Index</param>
		/// <param name="IndexY">Y-Index</param>
		/// <param name="Node">Node performing the operation.</param>
		/// <returns>Result</returns>
		public static IElement EvaluateIndex(IMatrix Matrix, IElement IndexX, IElement IndexY, ScriptNode Node)
        {
            if (IndexX.AssociatedObjectValue is double x && 
                IndexY.AssociatedObjectValue is double y)
            {
                if (x < 0 || x > int.MaxValue || x != Math.Truncate(x) || y < 0 || y > int.MaxValue || y != Math.Truncate(y))
                    throw new ScriptRuntimeException("Indices must be non-negative integers.", Node);

                return Matrix.GetElement((int)x, (int)y);
            }

            if (IndexX.IsScalar)
            {
                if (IndexY.IsScalar)
                    throw new ScriptRuntimeException("Index must be a non-negative integer.", Node);
                else
                {
                    ChunkedList<IElement> Elements = new ChunkedList<IElement>();

                    foreach (IElement E in IndexY.ChildElements)
                        Elements.Add(EvaluateIndex(Matrix, IndexX, E, Node));

                    return IndexY.Encapsulate(Elements, Node);
                }
            }
            else
            {
                if (IndexY.IsScalar)
                {
                    ChunkedList<IElement> Elements = new ChunkedList<IElement>();

                    foreach (IElement E in IndexX.ChildElements)
                        Elements.Add(EvaluateIndex(Matrix, E, IndexY, Node));

                    return IndexX.Encapsulate(Elements, Node);
                }
                else
                {
                    ICollection<IElement> IndexXChildren = IndexX.ChildElements;
                    ICollection<IElement> IndexYChildren = IndexY.ChildElements;

                    if (IndexXChildren.Count == IndexYChildren.Count)
                    {
                        ChunkedList<IElement> Elements = new ChunkedList<IElement>();
                        IEnumerator<IElement> eX = IndexXChildren.GetEnumerator();
                        IEnumerator<IElement> eY = IndexYChildren.GetEnumerator();

                        try
                        {
                            while (eX.MoveNext() && eY.MoveNext())
                                Elements.Add(EvaluateIndex(Matrix, eX.Current, eY.Current, Node));
                        }
                        finally
                        {
                            eX.Dispose();
                            eY.Dispose();
                        }

                        return IndexX.Encapsulate(Elements, Node);
                    }
                    else
                    {
                        ChunkedList<IElement> XResult = new ChunkedList<IElement>();

                        foreach (IElement XChild in IndexXChildren)
                        {
                            ChunkedList<IElement> YResult = new ChunkedList<IElement>();

                            foreach (IElement YChild in IndexYChildren)
                                YResult.Add(EvaluateIndex(Matrix, XChild, YChild, Node));

                            XResult.Add(IndexY.Encapsulate(YResult, Node));
                        }

                        return IndexX.Encapsulate(XResult, Node);
                    }
                }
            }
        }

    }
}
