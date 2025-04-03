﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Waher.Content.Markdown.Model;
using Waher.Runtime.Collections;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Graphs;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;
using Waher.Script.Operators.Matrices;

namespace Waher.Content.Markdown.Functions
{
	/// <summary>
	/// Converts an element to a markdown string.
	/// </summary>
	public class ToMarkdown : FunctionOneVariable
	{
		/// <summary>
		/// Converts an element to a markdown string.
		/// </summary>
		/// <param name="Argument">Argument.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public ToMarkdown(ScriptNode Argument, int Start, int Length, Expression Expression)
			: base(Argument, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(ToMarkdown);

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement Argument, Variables Variables)
		{
			return new StringValue(Evaluate(Argument));
		}

		/// <summary>
		/// Converts an element to a markdown string.
		/// </summary>
		/// <param name="Argument">Element to convert to markdown.</param>
		/// <returns>Markdown representation of element.</returns>
		public static string Evaluate(IElement Argument)
		{
			if (Argument.AssociatedObjectValue is string s)
				return s;

			if (Argument.AssociatedObjectValue is IToMatrix ToMatrix)
				Argument = ToMatrix.ToMatrix();

			if (Argument is IMatrix Matrix)
				return MatrixToMarkdown(Matrix);
			else if (Argument is IVector Vector)
				return VectorToMarkdown(Vector);
			else if (Argument is ISet Set)
				return SetToMarkdown(Set);
			else if (Argument is Graph Graph)
				return GraphToMarkdown(Graph);
			else if (Argument is PixelInformation Pixels)
				return PixelsToMarkdown(Pixels);
			else if (Argument is SKImage Image)
				return ImageToMarkdown(Image);
			else
				return Argument.AssociatedObjectValue?.ToString();
		}

		/// <summary>
		/// Converts a matrix to Markdown.
		/// </summary>
		/// <param name="Matrix">Matrix</param>
		/// <returns>Markdown</returns>
		public static string MatrixToMarkdown(IMatrix Matrix)
		{
			StringBuilder Markdown = new StringBuilder();
			MatrixToMarkdown(Matrix, Markdown);
			return Markdown.ToString();
		}

		/// <summary>
		/// Converts a matrix to Markdown.
		/// </summary>
		/// <param name="Matrix">Matrix</param>
		/// <param name="Markdown">Markdown output.</param>
		public static void MatrixToMarkdown(IMatrix Matrix, StringBuilder Markdown)
		{
			int Cols = Matrix.Columns;
			int Rows = Matrix.Rows;
			int Col, Row;
			IElement E;
			string s;

			if (Matrix is ObjectMatrix OM && OM.HasColumnNames)
			{
				foreach (string Name in OM.ColumnNames)
				{
					Markdown.Append("| ");
					Markdown.Append(MarkdownDocument.Encode(Name));
					Markdown.Append(' ');
				}
			}
			else
			{
				for (Col = 1; Col <= Cols; Col++)
				{
					Markdown.Append("| ");
					Markdown.Append(Col.ToString());
					Markdown.Append(' ');
				}
			}

			Markdown.AppendLine("|");

			for (Col = 0; Col < Cols; Col++)
			{
				switch (ColumnAlignment(Matrix, Col))
				{
					case TextAlignment.Left:
						Markdown.Append("|:--");
						break;

					case TextAlignment.Center:
						Markdown.Append("|:-:");
						break;

					case TextAlignment.Right:
						Markdown.Append("|--:");
						break;

					default:
						Markdown.Append("|---");
						break;
				}
			}

			Markdown.AppendLine("|");

			ChunkedList<KeyValuePair<string, string>> Notes = null;

			for (Row = 0; Row < Rows; Row++)
			{
				for (Col = 0; Col < Cols; Col++)
				{
					Markdown.Append("| ");

					E = Matrix.GetElement(Col, Row);

					if (!(E?.AssociatedObjectValue is null))
					{
						s = Evaluate(E);

						if (s.IndexOfAny(CommonTypes.CRLF) >= 0)
						{
							string NoteId = "n" + Guid.NewGuid().ToString().Replace("-", string.Empty);

							if (Notes is null)
								Notes = new ChunkedList<KeyValuePair<string, string>>();

							Notes.Add(new KeyValuePair<string, string>(NoteId, s));

							Markdown.Append("[^");
							Markdown.Append(NoteId);
							Markdown.Append(']');
						}
						else
							Markdown.Append(s);

						Markdown.Append(' ');
					}
				}

				Markdown.AppendLine("|");
			}

			if (!(Notes is null))
			{
				foreach (KeyValuePair<string, string> Note in Notes)
				{
					Markdown.AppendLine();
					Markdown.Append("[^");
					Markdown.Append(Note.Key);
					Markdown.Append("]:");

					foreach (string Row2 in Note.Value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
					{
						Markdown.Append('\t');
						Markdown.AppendLine(Row2);
					}
				}
			}
		}

		private static TextAlignment ColumnAlignment(IMatrix Matrix, int Column)
		{
			int Row;
			int Rows = Matrix.Rows;
			TextAlignment? Result = null;
			TextAlignment Cell;
			IElement E;
			object Obj;

			for (Row = 0; Row < Rows; Row++)
			{
				E = Matrix.GetElement(Column, Row);
				Obj = E.AssociatedObjectValue;

				if (Obj is null)
					continue;

				if (Obj is string)
					Cell = TextAlignment.Left;
				else if (Obj is double)
					Cell = TextAlignment.Right;
				else if (Obj is bool)
					Cell = TextAlignment.Center;
				else
				{
					switch (Convert.GetTypeCode(Obj))
					{
						case TypeCode.Boolean:
							Cell = TextAlignment.Center;
							break;

						case TypeCode.Char:
						case TypeCode.DateTime:
						case TypeCode.String:
							Cell = TextAlignment.Left;
							break;

						case TypeCode.Byte:
						case TypeCode.Decimal:
						case TypeCode.Double:
						case TypeCode.Int16:
						case TypeCode.Int32:
						case TypeCode.Int64:
						case TypeCode.SByte:
						case TypeCode.Single:
						case TypeCode.UInt16:
						case TypeCode.UInt32:
						case TypeCode.UInt64:
							Cell = TextAlignment.Right;
							break;

						case TypeCode.Empty:
						case TypeCode.Object:
						default:
							continue;
					}
				}

				if (!Result.HasValue)
					Result = Cell;
				else if (Result.Value != Cell)
					return TextAlignment.Left;
			}

			return Result ?? TextAlignment.Left;
		}

		/// <summary>
		/// Converts a vector to Markdown.
		/// </summary>
		/// <param name="Vector">Vector</param>
		/// <returns>Markdown</returns>
		public static string VectorToMarkdown(IVector Vector)
		{
			return EnumerableToMarkdown(Vector.ChildElements);
		}

		private static string EnumerableToMarkdown(IEnumerable<IElement> Elements)
		{
			StringBuilder Markdown = new StringBuilder();

			foreach (IElement E in Elements)
			{
				Markdown.AppendLine(E.AssociatedObjectValue?.ToString() ?? string.Empty);
				Markdown.AppendLine();
			}

			return Markdown.ToString();
		}

		/// <summary>
		/// Converts a set to Markdown.
		/// </summary>
		/// <param name="Set">Set</param>
		/// <returns>Markdown</returns>
		public static string SetToMarkdown(ISet Set)
		{
			return EnumerableToMarkdown(Set.ChildElements);
		}

		/// <summary>
		/// Converts a graph to Markdown.
		/// </summary>
		/// <param name="Graph">Graph</param>
		/// <returns>Markdown</returns>
		public static string GraphToMarkdown(Graph Graph)
		{
			StringBuilder Markdown = new StringBuilder();
			GraphToMarkdown(Graph, Markdown);
			return Markdown.ToString();
		}

		/// <summary>
		/// Converts a graph to Markdown.
		/// </summary>
		/// <param name="Graph">Graph</param>
		/// <param name="Markdown">Markdown output.</param>
		public static void GraphToMarkdown(Graph Graph, StringBuilder Markdown)
		{
			Markdown.AppendLine("```graph");
			Graph.ToXml(Markdown);
			Markdown.AppendLine();
			Markdown.AppendLine("```");
			Markdown.AppendLine();
		}

		/// <summary>
		/// Converts pixels to Markdown.
		/// </summary>
		/// <param name="Pixels">Pixels</param>
		public static string PixelsToMarkdown(PixelInformation Pixels)
		{
			StringBuilder Markdown = new StringBuilder();
			PixelsToMarkdown(Pixels, Markdown);
			return Markdown.ToString();
		}

		/// <summary>
		/// Converts pixels to Markdown.
		/// </summary>
		/// <param name="Pixels">Pixels</param>
		/// <param name="Markdown">Markdown output.</param>
		public static void PixelsToMarkdown(PixelInformation Pixels, StringBuilder Markdown)
		{
			byte[] Bin = Pixels.EncodeAsPng();

			Markdown.AppendLine("```image/png");
			Markdown.AppendLine(Convert.ToBase64String(Bin));
			Markdown.AppendLine("```");
			Markdown.AppendLine();
		}

		/// <summary>
		/// Converts an image to Markdown.
		/// </summary>
		/// <param name="Image">Image</param>
		public static string ImageToMarkdown(SKImage Image)
		{
			StringBuilder Markdown = new StringBuilder();
			ImageToMarkdown(Image, Markdown);
			return Markdown.ToString();
		}

		/// <summary>
		/// Converts an image to Markdown.
		/// </summary>
		/// <param name="Image">Image</param>
		/// <param name="Markdown">Markdown output.</param>
		public static void ImageToMarkdown(SKImage Image, StringBuilder Markdown)
		{
			using (SKData Data = Image.Encode(SKEncodedImageFormat.Png, 100))
			{
				byte[] Bin = Data.ToArray();

				Markdown.AppendLine("```image/png");
				Markdown.AppendLine(Convert.ToBase64String(Bin));
				Markdown.AppendLine("```");
				Markdown.AppendLine();
			}

		}
	}
}
