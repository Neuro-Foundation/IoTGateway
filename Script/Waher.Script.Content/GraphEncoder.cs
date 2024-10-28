﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Images;
using Waher.Runtime.Inventory;
using Waher.Script.Graphs;

namespace Waher.Script.Content
{
	/// <summary>
	/// Encodes graphs as images
	/// </summary>
	public class GraphEncoder : IContentEncoder
	{
		/// <summary>
		/// Encodes graphs as images
		/// </summary>
		public GraphEncoder()
		{
		}

		/// <summary>
		/// Supported content types.
		/// </summary>
		public string[] ContentTypes => new string[] { ImageCodec.ContentTypePng };

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions => new string[] { ImageCodec.FileExtensionPng };

		/// <summary>
		/// If the encoder encodes a given object.
		/// </summary>
		/// <param name="Object">Object to encode.</param>
		/// <param name="Grade">How well the encoder encodes the object.</param>
		/// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
		/// <returns>If the encoder can encode the given object.</returns>
		public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
		{
			if ((Object is Graph || Object is PixelInformation) && InternetContent.IsAccepted(ImageCodec.ContentTypePng, AcceptedContentTypes))
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
		public Task<KeyValuePair<byte[], string>> EncodeAsync(object Object, Encoding Encoding, params string[] AcceptedContentTypes)
		{
			if (!(Object is PixelInformation Pixels))
			{
				if (Object is Graph G)
					Pixels = G.CreatePixels();
				else
					throw new ArgumentException("Object not PixelInformation or Graph.", nameof(Object));
			}

			return Task.FromResult(new KeyValuePair<byte[], string>(Pixels.EncodeAsPng(), ImageCodec.ContentTypePng));
		}

		/// <summary>
		/// Tries to get the content type of an item, given its file extension.
		/// </summary>
		/// <param name="FileExtension">File extension.</param>
		/// <param name="ContentType">Content type.</param>
		/// <returns>If the extension was recognized.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			if (string.Compare(FileExtension, ImageCodec.FileExtensionPng, true) == 0)
			{
				ContentType = ImageCodec.ContentTypePng;
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
				case ImageCodec.ContentTypePng:
					FileExtension = ImageCodec.FileExtensionPng;
					return true;

				default:
					FileExtension = string.Empty;
					return false;
			}
		}

	}
}
