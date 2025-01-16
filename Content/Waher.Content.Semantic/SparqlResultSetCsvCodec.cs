﻿using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Content.Semantic.Model;
using Waher.Content.Text;
using Waher.Runtime.Inventory;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Objects.Matrices;

namespace Waher.Content.Semantic
{
	/// <summary>
	/// Encoder and Decoder of semantic information from SPARQL queries using CSV.
	/// https://www.w3.org/TR/sparql12-results-csv-tsv/
	/// </summary>
	public class SparqlResultSetCsvCodec : IContentEncoder
	{
		/// <summary>
		/// Encoder and Decoder of semantic information from SPARQL queries using CSV.
		/// </summary>
		public SparqlResultSetCsvCodec()
		{
		}

		/// <summary>
		/// Supported Internet Content Types.
		/// </summary>
		public string[] ContentTypes => new string[0];

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions => new string[0];

		/// <summary>
		/// If the encoder encodes a specific object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Grade">How well the encoder supports the given object.</param>
		/// <param name="AcceptedContentTypes">Accepted content types.</param>
		/// <returns>If the encoder encodes the given object.</returns>
		public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
		{
			if (Object is SparqlResultSet &&
				InternetContent.IsAccepted(CsvCodec.CsvContentTypes, AcceptedContentTypes))
			{
				Grade = Grade.Excellent;
				return true;
			}
			else if (Object is ObjectMatrix M && M.HasColumnNames &&
				InternetContent.IsAccepted(CsvCodec.CsvContentTypes, AcceptedContentTypes))
			{
				Grade = Grade.Ok;
				return true;
			}
			else if (Object is bool &&
				InternetContent.IsAccepted(CsvCodec.CsvContentTypes, AcceptedContentTypes))
			{
				Grade = Grade.Barely;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Encodes an object
		/// </summary>
		/// <param name="Object">Object to encode</param>
		/// <param name="Encoding">Encoding</param>
		/// <param name="Progress">Optional progress reporting of encoding/decoding. Can be null.</param>
		/// <param name="AcceptedContentTypes">Accepted content types.</param>
		/// <returns>Encoded object.</returns>
		public Task<ContentResponse> EncodeAsync(object Object, Encoding Encoding, ICodecProgress Progress, params string[] AcceptedContentTypes)
		{
			string Text;

			if (Encoding is null)
				Encoding = Encoding.UTF8;

			if (Object is SparqlResultSet Result)
			{
				if (Result.BooleanResult.HasValue)
				{
					string[][] Records = new string[1][];
					Records[0] = new string[] { CommonTypes.Encode(Result.BooleanResult.Value) };

					Text = CSV.Encode(Records);
				}
				else
					Text = CSV.Encode(Result.ToMatrix());
			}
			else if (Object is ObjectMatrix M)
				Text = CSV.Encode(M, ElementToString);
			else if (Object is bool b)
			{
				string[][] Records = new string[1][];
				Records[0] = new string[] { CommonTypes.Encode(b) };

				Text = CSV.Encode(Records);
			}
			else
				return Task.FromResult(new ContentResponse(new ArgumentException("Unable to encode object.", nameof(Object))));

			byte[] Bin = Encoding.GetBytes(Text);
			string ContentType = CsvCodec.CsvContentTypes[0] + "; charset=" + Encoding.WebName;

			return Task.FromResult(new ContentResponse(ContentType, Object, Bin));
		}

		private static string ElementToString(IElement E)
		{
			object Obj = E.AssociatedObjectValue;

			if (Obj is string s)
				return s;
			else if (Obj is SemanticLiteral Literal)
				return Literal.StringValue;
			else if (Obj is SemanticTriple Triple)
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("<<");
				sb.Append(ElementToString(Triple.Subject));
				sb.Append(' ');
				sb.Append(ElementToString(Triple.Predicate));
				sb.Append(' ');
				sb.Append(ElementToString(Triple.Object));
				sb.Append(">>");

				return sb.ToString();
			}
			else
				return Obj?.ToString() ?? string.Empty;
		}

		/// <summary>
		/// Tries to get the content type of content of a given file extension.
		/// </summary>
		/// <param name="FileExtension">File Extension</param>
		/// <param name="ContentType">Content Type, if recognized.</param>
		/// <returns>If File Extension was recognized and Content Type found.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			ContentType = null;
			return false;
		}

		/// <summary>
		/// Tries to get the file extension of content of a given content type.
		/// </summary>
		/// <param name="ContentType">Content Type</param>
		/// <param name="FileExtension">File Extension, if recognized.</param>
		/// <returns>If Content Type was recognized and File Extension found.</returns>
		public bool TryGetFileExtension(string ContentType, out string FileExtension)
		{
			FileExtension = null;
			return false;
		}
	}
}
