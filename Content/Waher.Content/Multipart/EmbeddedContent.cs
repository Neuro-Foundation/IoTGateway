﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Waher.Content.Multipart
{
	/// <summary>
	/// Content disposition
	/// </summary>
	public enum ContentDisposition
	{
		/// <summary>
		/// Content appears inline
		/// </summary>
		Inline,

		/// <summary>
		/// Content is available as an attachment
		/// </summary>
		Attachment,

		/// <summary>
		/// Content is available as a field in a form
		/// </summary>
		FormData,

		/// <summary>
		/// Unknown or unspecified disposition
		/// </summary>
		Unknown
	}

	/// <summary>
	/// Represents content embedded in other content.
	/// </summary>
	public class EmbeddedContent
	{
		private ContentDisposition disposition = ContentDisposition.Unknown;
		private string contentType = null;
		private string name = null;
		private string fileName = null;
		private string transferEncoding = null;
		private string id = null;
		private string description = null;
		private byte[] raw = null;
		private byte[] transferDecoded = null;
		private object decoded = null;
		private int? size = null;
		private DateTimeOffset? creationDate = null;
		private DateTimeOffset? modificationDate = null;

		/// <summary>
		/// Content-Type of embedded object.
		/// </summary>
		public string ContentType
		{
			get => this.contentType;
			set => this.contentType = value;
		}

		/// <summary>
		/// Disposition of embedded object.
		/// </summary>
		public ContentDisposition Disposition
		{
			get => this.disposition;
			set => this.disposition = value;
		}

		/// <summary>
		/// Name of embedded object.
		/// </summary>
		public string Name
		{
			get => this.name;
			set => this.name = value;
		}

		/// <summary>
		/// Filename of embedded object.
		/// </summary>
		public string FileName
		{
			get => this.fileName;
			set => this.fileName = value;
		}

		/// <summary>
		/// Content Transfer Encoding of embedded object, if defined.
		/// Affects how <see cref="Raw"/> is transformed
		/// into <see cref="TransferDecoded"/>.
		/// </summary>
		public string TransferEncoding
		{
			get => this.transferEncoding;
			set => this.transferEncoding = value;
		}

		/// <summary>
		/// Raw, untrasnformed body of embedded object.
		/// </summary>
		public byte[] Raw
		{
			get => this.raw;
			set => this.raw = value;
		}

		/// <summary>
		/// Transformed body of embedded object. <see cref="TransferEncoding"/> 
		/// defines how <see cref="Raw"/> is transformed into TransferDecoded.
		/// </summary>
		public byte[] TransferDecoded
		{
			get => this.transferDecoded;
			set => this.transferDecoded = value;
		}

		/// <summary>
		/// Decoded body of embedded object. <see cref="ContentType"/> defines
		/// how <see cref="TransferDecoded"/> is transformed into Decoded.
		/// </summary>
		public object Decoded
		{
			get => this.decoded;
			set => this.decoded = value;
		}

		/// <summary>
		/// Content-ID of embedded object, if defined.
		/// </summary>
		public string ID
		{
			get => this.id;
			set => this.id = value;
		}

		/// <summary>
		/// Content-Description of embedded object, if defined.
		/// </summary>
		public string Description
		{
			get => this.description;
			set => this.description = value;
		}

		/// <summary>
		/// Size of content, if available.
		/// </summary>
		public int? Size
		{
			get => this.size;
			set => this.size = value;
		}

		/// <summary>
		/// Creation-Date of content, if available.
		/// </summary>
		public DateTimeOffset? CreationDate
		{
			get => this.creationDate;
			set => this.creationDate = value;
		}

		/// <summary>
		/// Modification-Date of content, if available.
		/// </summary>
		public DateTimeOffset? ModificationDate
		{
			get => this.modificationDate;
			set => this.modificationDate = value;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			if (!string.IsNullOrEmpty(this.name))
				return this.name;
			else if (!string.IsNullOrEmpty(this.fileName))
				return this.fileName;
			else
				return base.ToString();
		}

		internal async Task AssertEncoded()
		{
			if (!(this.raw is null))
				return;

			if (this.transferDecoded is null)
			{
				ContentResponse P = await InternetContent.EncodeAsync(this.decoded, Encoding.UTF8);
				P.AssertOk();

				this.transferDecoded = P.Encoded;
				this.contentType = P.ContentType;
				this.raw = null;
			}

			if (this.raw is null)
			{
				this.raw = Encoding.ASCII.GetBytes(Convert.ToBase64String(this.transferDecoded));
				this.transferEncoding = "base64";
			}
		}
	}
}
