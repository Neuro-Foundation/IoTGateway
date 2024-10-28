﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content.Multipart;
using Waher.Runtime.Inventory;

namespace Waher.Content.Dsn
{
	/// <summary>
	/// Decoder of report data.
	/// 
	/// https://tools.ietf.org/html/rfc3462
	/// </summary>
	public class ReportCodec : IContentDecoder, IContentEncoder
	{
		/// <summary>
		/// multipart/report
		/// </summary>
		public const string ContentType = "multipart/report";

		/// <summary>
		/// Decoder of report data.
		/// 
		/// https://tools.ietf.org/html/rfc3462
		/// </summary>
		public ReportCodec()
		{
		}

		/// <summary>
		/// Supported content types.
		/// </summary>
		public string[] ContentTypes => new string[] { ContentType };

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions => new string[] { "report" };

		/// <summary>
		/// If the decoder decodes an object with a given content type.
		/// </summary>
		/// <param name="ContentType">Content type to decode.</param>
		/// <param name="Grade">How well the decoder decodes the object.</param>
		/// <returns>If the decoder can decode an object with the given type.</returns>
		public bool Decodes(string ContentType, out Grade Grade)
		{
			if (ContentType == ReportCodec.ContentType)
			{
				Grade = Grade.Excellent;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Decodes an object.
		/// </summary>
		/// <param name="ContentType">Internet Content Type.</param>
		/// <param name="Data">Encoded object.</param>
		/// <param name="Encoding">Any encoding specified. Can be null if no encoding specified.</param>
		/// <param name="Fields">Any content-type related fields and their corresponding values.</param>
		///	<param name="BaseUri">Base URI, if any. If not available, value is null.</param>
		/// <returns>Decoded object.</returns>
		/// <exception cref="ArgumentException">If the object cannot be decoded.</exception>
		public async Task<object> DecodeAsync(string ContentType, byte[] Data, Encoding Encoding, KeyValuePair<string, string>[] Fields, Uri BaseUri)
		{
			List<EmbeddedContent> List = new List<EmbeddedContent>();

			await FormDataDecoder.Decode(Data, Fields, null, List, BaseUri);

			return new ReportContent(List.ToArray());
		}

		/// <summary>
		/// Tries to get the content type of an item, given its file extension.
		/// </summary>
		/// <param name="FileExtension">File extension.</param>
		/// <param name="ContentType">Content type.</param>
		/// <returns>If the extension was recognized.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			if (string.Compare(FileExtension, "report", true) == 0)
			{
				ContentType = ReportCodec.ContentType;
				return true;
			}
			else
			{
				ContentType = string.Empty;
				return false;
			}
		}

		/// <summary>
		/// Tries to get the file extension of an item, given its Content-Type.
		/// </summary>
		/// <param name="ContentType">Content type.</param>
		/// <param name="FileExtension">File extension.</param>
		/// <returns>If the Content-Type was recognized.</returns>
		public bool TryGetFileExtension(string ContentType, out string FileExtension)
		{
			switch (ContentType.ToLower())
			{
				case ReportCodec.ContentType:
					FileExtension = "report";
					return true;

				default:
					FileExtension = string.Empty;
					return false;
			}
		}

		/// <summary>
		/// If the encoder encodes a given object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Grade">How well the encoder encodes the object.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>If the encoder can encode the given object.</returns>
		public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
		{
			if (Object is ReportContent &&
				InternetContent.IsAccepted(ContentTypes, AcceptedContentTypes))
			{
				Grade = Grade.Ok;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Encodes an object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Encoding">Desired encoding of text. Can be null if no desired encoding is speified.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>Encoded object, as well as Content Type of encoding. Includes information about any text encodings used.</returns>
		/// <exception cref="ArgumentException">If the object cannot be encoded.</exception>
		public async Task<KeyValuePair<byte[], string>> EncodeAsync(object Object, Encoding Encoding, params string[] AcceptedContentTypes)
		{
			if (Object is ReportContent Report &&
				InternetContent.IsAccepted(ContentTypes, AcceptedContentTypes))
			{
				string Boundary = Guid.NewGuid().ToString();
				string ContentType = ReportCodec.ContentType + "; boundary=\"" + Boundary + "\"";
				byte[] Bin = await FormDataDecoder.Encode(Report.Content, Boundary);

				return new KeyValuePair<byte[], string>(Bin, ContentType);
			}
			else
				throw new ArgumentException("Unable to encode object, or content type not accepted.", nameof(Object));
		}
	}
}
