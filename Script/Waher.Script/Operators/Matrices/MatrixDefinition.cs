﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;

namespace Waher.Script.Operators.Matrices
{
	/// <summary>
	/// Creates a matrix.
	/// </summary>
	public class MatrixDefinition : ElementList
	{
		/// <summary>
		/// Creates a matrix.
		/// </summary>
		/// <param name="Rows">Row vectors.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public MatrixDefinition(ScriptNode[] Rows, int Start, int Length, Expression Expression)
			: base(Rows, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			ChunkedList<IElement> Rows = new ChunkedList<IElement>();

			foreach (ScriptNode Node in this.Elements)
				Rows.Add(Node.Evaluate(Variables));

			return Encapsulate(Rows, this);
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

			ChunkedList<IElement> Rows = new ChunkedList<IElement>();

			foreach (ScriptNode Node in this.Elements)
				Rows.Add(await Node.EvaluateAsync(Variables));

			return Encapsulate(Rows, this);
		}
		/// <summary>
		/// Encapsulates the elements of a matrix.
		/// </summary>
		/// <param name="Rows">Matrix rows.</param>
		/// <param name="Node">Script node from where the encapsulation is done.</param>
		/// <returns>Encapsulated matrix.</returns>
		public static IMatrix Encapsulate(ICollection<IElement> Rows, ScriptNode Node)
		{
			ChunkedList<IElement> Elements = new ChunkedList<IElement>();
			IVectorSpaceElement Vector;
			int? Columns = null;
			int i;

			foreach (IElement Row in Rows)
			{
				Vector = Row as IVectorSpaceElement;

				if (Vector is null)
				{
					Columns = -1;
					break;
				}
				else
				{
					i = Vector.Dimension;
					if (Columns.HasValue)
					{
						if (Columns.Value != i)
						{
							Columns = -1;
							break;
						}
					}
					else
						Columns = i;

					Elements.AddRange(Vector.VectorElements);
				}
			}

			if (!Columns.HasValue || Columns.Value < 0)
			{
				IVector V = Vectors.VectorDefinition.Encapsulate(Rows, false, Node);
				if (V is IMatrix M)
					return M;
				else
					throw new ScriptRuntimeException("Unable to convert vector of vectors to matrix.", Node);
			}
			else
				return Encapsulate(Elements, Rows.Count, Columns.Value, Node);
		}

		/// <summary>
		/// Encapsulates the elements of a matrix.
		/// </summary>
		/// <param name="Elements">Matrix elements.</param>
		/// <param name="Rows">Rows</param>
		/// <param name="Columns">Columns</param>
		/// <param name="Node">Script node from where the encapsulation is done.</param>
		/// <returns>Encapsulated matrix.</returns>
		public static IMatrix Encapsulate(ICollection<IElement> Elements, int Rows, int Columns, ScriptNode Node)
		{
			IElement SuperSetExample = null;
			IElement Element2;
			ISet CommonSuperSet = null;
			ISet Set;
			ChunkedList<IElement> Upgraded = null;
			int ItemIndex = 0;

			if (Elements.Count == Rows && Columns > 1)
			{
				ChunkedList<IElement> Temp = new ChunkedList<IElement>();

				foreach (IElement E in Elements)
				{
					if (E is IVector V)
						Temp.AddRange(V.VectorElements);
					else
						throw new ScriptRuntimeException("Invalid number of elements.", Node);
				}

				Elements = Temp;
			}

			foreach (IElement Element in Elements)
			{
				if (CommonSuperSet is null)
				{
					SuperSetExample = Element;

					if (Element is null)
						CommonSuperSet = new ObjectValues();
					else
						CommonSuperSet = Element.AssociatedSet;
				}
				else
				{
					if (Element is null)
						Set = new ObjectValues();
					else
						Set = Element.AssociatedSet;

					if (Set.Equals(CommonSuperSet))
						Upgraded?.Add(Element);
					else
					{
						Element2 = Element;
						if (!Expression.UpgradeField(ref Element2, ref Set, ref SuperSetExample, ref CommonSuperSet))
						{
							CommonSuperSet = null;
							break;
						}
						else
						{
							if (Upgraded is null)
							{
								Upgraded = new ChunkedList<IElement>();

								IElement Element3;
								int i = 0;

								foreach (IElement E in Elements)
								{
									Element3 = E;
									if (!Expression.UpgradeField(ref Element3, ref Set, ref SuperSetExample, ref CommonSuperSet))
									{
										CommonSuperSet = null;
										break;
									}

									Upgraded.Add(Element3);
									if (++i >= ItemIndex)
										break;
								}
							}

							Upgraded.Add(Element2);
						}
					}
				}

				ItemIndex++;
			}

			if (!(CommonSuperSet is null))
			{
				if (!(Upgraded is null))
					Elements = Upgraded;

				if (CommonSuperSet is DoubleNumbers)
					return new DoubleMatrix(Rows, Columns, Elements);
				else if (CommonSuperSet is ComplexNumbers)
					return new ComplexMatrix(Rows, Columns, Elements);
				else if (CommonSuperSet is BooleanValues)
					return new BooleanMatrix(Rows, Columns, Elements);
			}

			return new ObjectMatrix(Rows, Columns, Elements);
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
			int c = Elements.Length;

			if (!(CheckAgainst is IMatrix Matrix) || Matrix.Rows != c)
				return PatternMatchResult.NoMatch;

			PatternMatchResult Result;
			int i;

			if (Matrix is IVector RowVectors)
			{
				i = 0;

				foreach (IElement E in RowVectors.VectorElements)
				{
					Result = Elements[i++].PatternMatch(E, AlreadyFound);
					if (Result != PatternMatchResult.Match)
						return Result;
				}
			}
			else
			{
				for (i = 0; i < c; i++)
				{
					Result = Elements[i].PatternMatch(Matrix.GetRow(i), AlreadyFound);
					if (Result != PatternMatchResult.Match)
						return Result;
				}
			}

			return PatternMatchResult.Match;
		}

	}
}
