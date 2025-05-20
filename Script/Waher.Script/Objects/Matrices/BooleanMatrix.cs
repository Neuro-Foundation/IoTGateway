﻿using System.Collections.Generic;
using System.Text;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects.VectorSpaces;
using Waher.Script.Operators.Matrices;

namespace Waher.Script.Objects.Matrices
{
	/// <summary>
	/// Boolean-valued matrix.
	/// </summary>
	public sealed class BooleanMatrix : RingElement, IMatrix
	{
		private bool[,] values;
		private IElement[,] matrixElements;
		private ICollection<IElement> elements;
		private readonly int rows;
		private readonly int columns;

		/// <summary>
		/// Boolean-valued matrix.
		/// </summary>
		/// <param name="Values">Boolean value.</param>
		public BooleanMatrix(bool[,] Values)
		{
			this.values = Values;
			this.elements = null;
			this.matrixElements = null;
			this.rows = Values.GetLength(0);
			this.columns = Values.GetLength(1);
		}

		/// <summary>
		/// Boolean-valued vector.
		/// </summary>
		/// <param name="Rows">Number of rows.</param>
		/// <param name="Columns">Number of columns.</param>
		/// <param name="Elements">Elements.</param>
		public BooleanMatrix(int Rows, int Columns, ICollection<IElement> Elements)
		{
			this.values = null;
			this.elements = Elements;
			this.matrixElements = null;
			this.rows = Rows;
			this.columns = Columns;
		}

		/// <summary>
		/// Matrix element values.
		/// </summary>
		public bool[,] Values
		{
			get
			{
				if (this.values is null)
				{
					bool[,] v = new bool[this.rows, this.columns];
					int x = 0;
					int y = 0;

					if (this.elements is ChunkedList<IElement> Values)
					{
						ChunkNode<IElement> Loop = Values.FirstChunk;
						int i, c;

						while (!(Loop is null))
						{
							for (i = Loop.Start, c = Loop.Pos; i < c; i++)
							{
								if (!(Loop[i].AssociatedObjectValue is bool b))
									b = false;

								v[y, x++] = b;
								if (x >= this.columns)
								{
									y++;
									x = 0;
								}
							}

							Loop = Loop.Next;
						}
					}
					else
					{
						foreach (IElement Element in this.elements)
						{
							if (!(Element.AssociatedObjectValue is bool b))
								b = false;

							v[y, x++] = b;
							if (x >= this.columns)
							{
								y++;
								x = 0;
							}
						}
					}

					this.values = v;
				}

				return this.values;
			}
		}

		/// <summary>
		/// Matrix elements.
		/// </summary>
		public ICollection<IElement> Elements
		{
			get
			{
				if (this.elements is null)
				{
					int x, y, i = 0;
					IElement[] v = new IElement[this.rows * this.columns];

					for (y = 0; y < this.rows; y++)
					{
						for (x = 0; x < this.columns; x++)
							v[i++] = new BooleanValue(this.values[y, x]);
					}

					this.elements = v;
				}

				return this.elements;
			}
		}

		/// <summary>
		/// Matrix elements
		/// </summary>
		public IElement[,] MatrixElements
		{
			get
			{
				if (this.matrixElements is null)
				{
					IElement[,] v = new IElement[this.rows, this.columns];
					int x = 0;
					int y = 0;

					foreach (IElement E in this.Elements)
					{
						v[y, x++] = E;
						if (x >= this.columns)
						{
							y++;
							x = 0;
						}
					}

					this.matrixElements = v;
				}

				return this.matrixElements;
			}
		}

		/// <summary>
		/// Number of rows.
		/// </summary>
		public int Rows => this.rows;

		/// <summary>
		/// Number of columns.
		/// </summary>
		public int Columns => this.columns;

		/// <inheritdoc/>
		public override string ToString()
		{
			bool[,] v = this.Values;
			StringBuilder sb = null;
			bool First;
			int x, y;

			for (y = 0; y < this.rows; y++)
			{
				if (sb is null)
					sb = new StringBuilder("[[");
				else
					sb.Append(",\r\n [");

				First = true;
				for (x = 0; x < this.columns; x++)
				{
					if (First)
						First = false;
					else
						sb.Append(", ");

					sb.Append(Expression.ToString(v[y, x]));
				}

				sb.Append(']');
			}

			if (sb is null)
				sb = new StringBuilder("[[]]");
			else
				sb.Append(']');

			return sb.ToString();
		}

		/// <summary>
		/// Associated Ring.
		/// </summary>
		public override IRing AssociatedRing
		{
			get
			{
				if (this.associatedMatrixSpace is null)
					this.associatedMatrixSpace = new BooleanMatrices(this.rows, this.columns);

				return this.associatedMatrixSpace;
			}
		}

		private BooleanMatrices associatedMatrixSpace = null;

		/// <summary>
		/// Associated object value.
		/// </summary>
		public override object AssociatedObjectValue => this;

		/// <summary>
		/// Tries to multiply an element to the current element, from the left.
		/// </summary>
		/// <param name="Element">Element to multiply.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override IRingElement MultiplyLeft(IRingElement Element)
		{
			return this.ToDoubleMatrix().MultiplyLeft(Element);
		}

		/// <summary>
		/// Tries to multiply an element to the current element, from the right.
		/// </summary>
		/// <param name="Element">Element to multiply.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override IRingElement MultiplyRight(IRingElement Element)
		{
			return this.ToDoubleMatrix().MultiplyRight(Element);
		}

		/// <summary>
		/// Inverts the element, if possible.
		/// </summary>
		/// <returns>Inverted element, or null if not possible.</returns>
		public override IRingElement Invert()
		{
			if (this.rows != this.columns)
				return null;

			return this.ToDoubleMatrix().Invert();
		}

		/// <summary>
		/// Reduces a matrix.
		/// </summary>
		/// <param name="Eliminate">By default, reduction produces an
		/// upper triangular matrix. By using elimination, upwards reduction
		/// is also performed.</param>
		/// <param name="BreakIfZero">If elimination process should break if a
		/// zero-row is encountered.</param>
		/// <param name="Rank">Rank of matrix, or -1 if process broken.</param>
		/// <param name="Factor">Multiplication factor for determinant of resulting matrix.</param>
		/// <returns>Reduced matrix</returns>
		public IMatrix Reduce(bool Eliminate, bool BreakIfZero, out int Rank, out ICommutativeRingWithIdentityElement Factor)
		{
			return this.ToDoubleMatrix().Reduce(Eliminate, BreakIfZero, out Rank, out Factor);
		}

		/// <summary>
		/// Converts matrix to a double-valued matrix.
		/// </summary>
		/// <returns>Double-valued matrix.</returns>
		public DoubleMatrix ToDoubleMatrix()
		{
			bool[,] Values = this.Values;
			double[,] v = new double[this.rows, this.columns];
			int x, y;

			for (y = 0; y < this.rows; y++)
			{
				for (x = 0; x < this.columns; x++)
					v[y, x] = Values[y, x] ? 1 : 0;
			}

			return new DoubleMatrix(v);
		}

		/// <summary>
		/// Tries to add an element to the current element.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override IAbelianGroupElement Add(IAbelianGroupElement Element)
		{
			return this.ToDoubleMatrix().Add(Element);
		}

		/// <summary>
		/// Negates the element.
		/// </summary>
		/// <returns>Negation of current element.</returns>
		public override IGroupElement Negate()
		{
			return this;
		}

		/// <summary>
		/// Compares the element to another.
		/// </summary>
		/// <param name="obj">Other element to compare against.</param>
		/// <returns>If elements are equal.</returns>
		public override bool Equals(object obj)
		{
			if (!(obj is BooleanMatrix Matrix))
				return false;

			if (this.columns != Matrix.columns || this.rows != Matrix.rows)
				return false;

			bool[,] V1 = this.Values;
			bool[,] V2 = Matrix.Values;
			int x, y;

			for (y = 0; y < this.rows; y++)
			{
				for (x = 0; x < this.columns; x++)
				{
					if (V1[y, x] != V2[y, x])
						return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Calculates a hash code of the element.
		/// </summary>
		/// <returns>Hash code.</returns>
		public override int GetHashCode()
		{
			int Result = 0;
			int x, y;
			int i = 0;

			for (y = 0; y < this.rows; y++)
			{
				for (x = 0; x < this.columns; x++)
				{
					if (this.values[y, x])
						Result ^= (1 << i);

					i++;
					i &= 31;
				}
			}

			return Result;
		}

		/// <summary>
		/// If the element represents a scalar value.
		/// </summary>
		public override bool IsScalar => false;

		/// <summary>
		/// An enumeration of child elements. If the element is a scalar, this property will return null.
		/// </summary>
		public override ICollection<IElement> ChildElements => this.Elements;

		/// <summary>
		/// Encapsulates a set of elements into a similar structure as that provided by the current element.
		/// </summary>
		/// <param name="Elements">New set of child elements, not necessarily of the same type as the child elements of the current object.</param>
		/// <param name="Node">Script node from where the encapsulation is done.</param>
		/// <returns>Encapsulated object of similar type as the current object.</returns>
		public override IElement Encapsulate(ChunkedList<IElement> Elements, ScriptNode Node)
		{
			return MatrixDefinition.Encapsulate(Elements, this.rows, this.columns, Node);
		}

		/// <summary>
		/// Encapsulates a set of elements into a similar structure as that provided by the current element.
		/// </summary>
		/// <param name="Elements">New set of child elements, not necessarily of the same type as the child elements of the current object.</param>
		/// <param name="Node">Script node from where the encapsulation is done.</param>
		/// <returns>Encapsulated object of similar type as the current object.</returns>
		public override IElement Encapsulate(ICollection<IElement> Elements, ScriptNode Node)
		{
			return MatrixDefinition.Encapsulate(Elements, this.rows, this.columns, Node);
		}

		/// <summary>
		/// Returns the zero element of the group.
		/// </summary>
		public override IAbelianGroupElement Zero
		{
			get
			{
				if (this.zero is null)
					this.zero = new BooleanMatrix(new bool[this.rows, this.columns]);

				return this.zero;
			}
		}

		private BooleanMatrix zero = null;

		/// <summary>
		/// Dimension of matrix, if seen as a vector of row vectors.
		/// </summary>
		public int Dimension => this.rows;

		/// <summary>
		/// Vector of row vectors.
		/// </summary>
		public ICollection<IElement> VectorElements
		{
			get
			{
				if (!(this.rowVectors is null))
					return this.rowVectors;

				bool[,] v = this.Values;
				ChunkedList<IElement> Rows = new ChunkedList<IElement>();
				int x, y;
				bool[] r;

				for (y = 0; y < this.rows; y++)
				{
					r = new bool[this.columns];

					for (x = 0; x < this.columns; x++)
						r[x] = v[y, x];

					Rows.Add(new BooleanVector(r));
				}

				this.rowVectors = Rows;
				return Rows;
			}
		}

		private ChunkedList<IElement> rowVectors = null;

		/// <summary>
		/// Returns a transposed matrix.
		/// </summary>
		/// <returns>Transposed matrix.</returns>
		public IMatrix Transpose()
		{
			bool[,] v = new bool[this.columns, this.rows];
			bool[,] Values = this.Values;
			int x, y;

			for (y = 0; y < this.rows; y++)
			{
				for (x = 0; x < this.columns; x++)
					v[x, y] = Values[y, x];
			}

			return new BooleanMatrix(v);
		}

		/// <summary>
		/// Returns a conjugate transposed matrix.
		/// </summary>
		/// <returns>Conjugate transposed matrix.</returns>
		public IMatrix ConjugateTranspose()
		{
			return this.Transpose();
		}

		/// <summary>
		/// Gets an element of the vector.
		/// </summary>
		/// <param name="Index">Zero-based index into the vector.</param>
		/// <returns>Vector element.</returns>
		public IElement GetElement(int Index)
		{
			if (Index < 0 || Index >= this.rows)
				throw new ScriptException("Index out of bounds.");

			bool[,] M = this.Values;
			bool[] V = new bool[this.columns];
			int i;

			for (i = 0; i < this.columns; i++)
				V[i] = M[Index, i];

			return new BooleanVector(V);
		}

		/// <summary>
		/// Sets an element in the vector.
		/// </summary>
		/// <param name="Index">Index.</param>
		/// <param name="Value">Element to set.</param>
		public void SetElement(int Index, IElement Value)
		{
			if (Index < 0 || Index >= this.rows)
				throw new ScriptException("Index out of bounds.");

			if (!(Value is BooleanVector V))
				throw new ScriptException("Row vectors in a boolean matrix are required to be boolean vectors.");

			if (V.Dimension != this.columns)
				throw new ScriptException("Dimension mismatch.");

			bool[] V2 = V.Values;
			bool[,] M = this.Values;
			this.elements = null;

			int i;

			for (i = 0; i < this.columns; i++)
				M[Index, i] = V2[i];
		}

		/// <summary>
		/// Gets an element of the matrix.
		/// </summary>
		/// <param name="Column">Zero-based column index into the matrix.</param>
		/// <param name="Row">Zero-based row index into the matrix.</param>
		/// <returns>Vector element.</returns>
		public IElement GetElement(int Column, int Row)
		{
			if (Column < 0 || Column >= this.columns || Row < 0 || Row >= this.rows)
				throw new ScriptException("Index out of bounds.");

			return new BooleanValue(this.Values[Row, Column]);
		}

		/// <summary>
		/// Sets an element in the matrix.
		/// </summary>
		/// <param name="Column">Zero-based column index into the matrix.</param>
		/// <param name="Row">Zero-based row index into the matrix.</param>
		/// <param name="Value">Element value.</param>
		public void SetElement(int Column, int Row, IElement Value)
		{
			if (Column < 0 || Column >= this.columns || Row < 0 || Row >= this.rows)
				throw new ScriptException("Index out of bounds.");

			if (!(Value.AssociatedObjectValue is bool V))
				throw new ScriptException("Elements in a boolean matrix must be boolean values.");

			bool[,] M = this.Values;
			this.elements = null;

			M[Row, Column] = V;
		}

		/// <summary>
		/// Gets a row vector from the matrix.
		/// </summary>
		/// <param name="Row">Zero-based row index into the matrix.</param>
		/// <returns>Vector element.</returns>
		public IVector GetRow(int Row)
		{
			if (Row < 0 || Row >= this.rows)
				throw new ScriptException("Index out of bounds.");

			bool[,] M = this.Values;
			bool[] V = new bool[this.columns];
			int i;

			for (i = 0; i < this.columns; i++)
				V[i] = M[Row, i];

			return new BooleanVector(V);
		}

		/// <summary>
		/// Gets a column vector from the matrix.
		/// </summary>
		/// <param name="Column">Zero-based column index into the matrix.</param>
		/// <returns>Vector element.</returns>
		public IVector GetColumn(int Column)
		{
			if (Column < 0 || Column >= this.columns)
				throw new ScriptException("Index out of bounds.");

			bool[,] M = this.Values;
			bool[] V = new bool[this.rows];
			int i;

			for (i = 0; i < this.rows; i++)
				V[i] = M[i, Column];

			return new BooleanVector(V);
		}

		/// <summary>
		/// Gets a row vector from the matrix.
		/// </summary>
		/// <param name="Row">Zero-based row index into the matrix.</param>
		/// <param name="Vector">New row vector.</param>
		public void SetRow(int Row, IVector Vector)
		{
			if (Row < 0 || Row >= this.rows)
				throw new ScriptException("Index out of bounds.");

			if (Vector.Dimension != this.columns)
				throw new ScriptException("Vector dimension does not match number of columns");

			if (!(Vector is BooleanVector V))
				throw new ScriptException("Row vectors in a boolean matrix must be boolean vectors.");

			bool[] V2 = V.Values;
			bool[,] M = this.Values;
			this.elements = null;
			int i;

			for (i = 0; i < this.columns; i++)
				M[Row, i] = V2[i];
		}

		/// <summary>
		/// Gets a column vector from the matrix.
		/// </summary>
		/// <param name="Column">Zero-based column index into the matrix.</param>
		/// <param name="Vector">New column vector.</param>
		public void SetColumn(int Column, IVector Vector)
		{
			if (Column < 0 || Column >= this.columns)
				throw new ScriptException("Index out of bounds.");

			if (Vector.Dimension != this.rows)
				throw new ScriptException("Vector dimension does not match number of rows");

			if (!(Vector is BooleanVector V))
				throw new ScriptException("Column vectors in a boolean matrix must be boolean vectors.");

			bool[] V2 = V.Values;
			bool[,] M = this.Values;
			this.elements = null;
			int i;

			for (i = 0; i < this.rows; i++)
				M[i, Column] = V2[i];
		}

		/// <summary>
		/// Tries to find an element in the matrix. Search is done, left to right,
		/// top to bottom.
		/// </summary>
		/// <param name="Element">Element to search form.</param>
		/// <param name="Column">Column, if found.</param>
		/// <param name="Row">Row, if found.</param>
		/// <returns>If element was found.</returns>
		public bool TryFind(IElement Element, out int Column, out int Row)
		{
			return this.TryFind(Element, 0, 0, out Column, out Row);
		}

		/// <summary>
		/// Tries to find an element in the matrix, continuing search from a given
		/// position in the matrix. Search is done, left to right, top to bottom.
		/// </summary>
		/// <param name="Element">Element to search form.</param>
		/// <param name="FromColumn">Start searching on this column.</param>
		/// <param name="FromRow">Start searching on this row.</param>
		/// <param name="Column">Column, if found.</param>
		/// <param name="Row">Row, if found.</param>
		/// <returns>If element was found.</returns>
		public bool TryFind(IElement Element, int FromColumn, int FromRow, out int Column, out int Row)
		{
			if (Element is BooleanValue B)
				return this.TryFind(B.Value, FromColumn, FromRow, out Column, out Row);
			else
			{
				Column = -1;
				Row = -1;
				return false;
			}
		}

		/// <summary>
		/// Tries to find an element in the matrix, continuing search from a given
		/// position in the matrix. Search is done, left to right, top to bottom.
		/// </summary>
		/// <param name="Element">Element to search form.</param>
		/// <param name="FromColumn">Start searching on this column.</param>
		/// <param name="FromRow">Start searching on this row.</param>
		/// <param name="Column">Column, if found.</param>
		/// <param name="Row">Row, if found.</param>
		/// <returns>If element was found.</returns>
		public bool TryFind(bool Element, int FromColumn, int FromRow, out int Column, out int Row)
		{
			bool[,] Values = this.Values;

			while (FromRow < this.rows)
			{
				while (FromColumn < this.columns)
				{
					if (Values[FromRow, FromColumn] == Element)
					{
						Column = FromColumn;
						Row = FromRow;

						return true;
					}

					FromColumn++;
				}

				FromColumn = 0;
				FromRow++;
			}

			Column = -1;
			Row = -1;

			return false;
		}

		/// <summary>
		/// Tries to find the last element in the matrix. Search is done, right to left,
		/// bottom to top.
		/// </summary>
		/// <param name="Element">Element to search form.</param>
		/// <param name="Column">Column, if found.</param>
		/// <param name="Row">Row, if found.</param>
		/// <returns>If element was found.</returns>
		public bool TryFindLast(IElement Element, out int Column, out int Row)
		{
			return this.TryFindLast(Element, this.columns - 1, this.rows - 1, out Column, out Row);
		}

		/// <summary>
		/// Tries to find the last element in the matrix, continuing search from a given
		/// position in the matrix. Search is done, right to left, bottom to top.
		/// </summary>
		/// <param name="Element">Element to search form.</param>
		/// <param name="FromColumn">Start searching on this column.</param>
		/// <param name="FromRow">Start searching on this row.</param>
		/// <param name="Column">Column, if found.</param>
		/// <param name="Row">Row, if found.</param>
		/// <returns>If element was found.</returns>
		public bool TryFindLast(IElement Element, int FromColumn, int FromRow, out int Column, out int Row)
		{
			if (Element is BooleanValue B)
				return this.TryFindLast(B.Value, FromColumn, FromRow, out Column, out Row);
			else
			{
				Column = -1;
				Row = -1;
				return false;
			}
		}

		/// <summary>
		/// Tries to find the last element in the matrix, continuing search from a given
		/// position in the matrix. Search is done, right to left, bottom to top.
		/// </summary>
		/// <param name="Element">Element to search form.</param>
		/// <param name="FromColumn">Start searching on this column.</param>
		/// <param name="FromRow">Start searching on this row.</param>
		/// <param name="Column">Column, if found.</param>
		/// <param name="Row">Row, if found.</param>
		/// <returns>If element was found.</returns>
		public bool TryFindLast(bool Element, int FromColumn, int FromRow, out int Column, out int Row)
		{
			bool[,] Values = this.Values;

			while (FromRow >= 0)
			{
				while (FromColumn >= 0)
				{
					if (Values[FromRow, FromColumn] == Element)
					{
						Column = FromColumn;
						Row = FromRow;

						return true;
					}

					FromColumn--;
				}

				FromColumn = this.columns - 1;
				FromRow--;
			}

			Column = -1;
			Row = -1;

			return false;
		}
	}
}
