﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.CoAP.CoRE;
using Waher.Runtime.Inventory;

namespace Waher.Networking.CoAP.ContentFormats
{
	/// <summary>
	/// CoRE Link Format
	/// </summary>
	public class CoreLinkFormat : ICoapContentFormat, IContentDecoder, IContentEncoder
	{
		/// <summary>
		/// 40
		/// </summary>
		public const int ContentFormatCode = 40;

		/// <summary>
		/// application/link-format
		/// </summary>
		public const string LinkFormatContentType = "application/link-format";

		/// <summary>
		/// CoRE Link Format
		/// </summary>
		public CoreLinkFormat()
		{
		}

		/// <summary>
		/// Content format number
		/// </summary>
		public int ContentFormat => ContentFormatCode;

		/// <summary>
		/// Internet content type.
		/// </summary>
		public string ContentType => LinkFormatContentType;

		/// <summary>
		/// Supported content types.
		/// </summary>
		public string[] ContentTypes => contentTypes;

		private static readonly string[] contentTypes = new string[] { LinkFormatContentType };

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions => new string[] { "wlnk" };

		/// <summary>
		/// Decodes an object.
		/// </summary>
		/// <param name="ContentType">Internet Content Type.</param>
		/// <param name="Data">Encoded object.</param>
		/// <param name="Encoding">Any encoding specified. Can be null if no encoding specified.</param>
		///	<param name="Fields">Any content-type related fields and their corresponding values.</param>
		///	<param name="BaseUri">Base URI, if any. If not available, value is null.</param>
		/// <returns>Decoded object.</returns>
		/// <exception cref="ArgumentException">If the object cannot be decoded.</exception>
		public Task<ContentResponse> DecodeAsync(string ContentType, byte[] Data, Encoding Encoding, KeyValuePair<string, string>[] Fields, Uri BaseUri)
		{
			string s = Encoding.UTF8.GetString(Data);
			return Task.FromResult(new ContentResponse(ContentType, new LinkDocument(s, BaseUri), Data));
		}

		/// <summary>
		/// If the decoder decodes an object with a given content type.
		/// </summary>
		/// <param name="ContentType">Content type to decode.</param>
		/// <param name="Grade">How well the decoder decodes the object.</param>
		/// <returns>If the decoder can decode an object with the given type.</returns>
		public bool Decodes(string ContentType, out Grade Grade)
		{
			if (ContentType == LinkFormatContentType)
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
		/// Encodes an object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Encoding">Desired encoding of text. Can be null if no desired encoding is speified.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>Encoded object, as well as Content Type of encoding. Includes information about any text encodings used.</returns>
		/// <exception cref="ArgumentException">If the object cannot be encoded.</exception>
		public Task<ContentResponse> EncodeAsync(object Object, Encoding Encoding, params string[] AcceptedContentTypes)
		{
			if (!(Object is LinkDocument Doc))
				return Task.FromResult(new ContentResponse(new ArgumentException("Object not a CoRE link document.", nameof(Object))));

			Encoding ??= Encoding.UTF8;

			string ContentType = LinkFormatContentType + "; charset=" + Encoding.WebName;
			byte[] Bin = Encoding.GetBytes(Doc.Text);

			return Task.FromResult(new ContentResponse(ContentType, Object, Bin));
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
			if (Object is LinkDocument)
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
		/// Tries to get the content type of an item, given its file extension.
		/// </summary>
		/// <param name="FileExtension">File extension.</param>
		/// <param name="ContentType">Content type.</param>
		/// <returns>If the extension was recognized.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			if (FileExtension == "wlnk")
			{
				ContentType = LinkFormatContentType;
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
				case LinkFormatContentType:
					FileExtension = "wlnk";
					return true;

				default:
					FileExtension = string.Empty;
					return false;
			}
		}
	}
}
