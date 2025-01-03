﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.HTTP.HeaderFields;

namespace Waher.Networking.HTTP
{
	/// <summary>
	/// Base class of all HTTP Exceptions.
	/// </summary>
	public class HttpException : Exception
	{
		/// <summary>
		/// Empty array of custom headers.
		/// </summary>
		protected static readonly KeyValuePair<string, string>[] NoCustomHeaders = new KeyValuePair<string, string>[0];

		private readonly KeyValuePair<string, string>[] headerFields;
		private readonly int statusCode;
		private readonly byte[] content = null;
		private readonly string contentType = null;
		private object contentObject = null;

		/// <summary>
		/// Base class of all HTTP Exceptions.
		/// </summary>
		/// <param name="StatusCode">HTTP Status Code.</param>
		/// <param name="Message">HTTP Status Message.</param>
		public HttpException(int StatusCode, string Message)
			: this(StatusCode, Message, NoCustomHeaders)
		{
		}

		/// <summary>
		/// Base class of all HTTP Exceptions.
		/// </summary>
		/// <param name="StatusCode">HTTP Status Code.</param>
		/// <param name="Message">HTTP Status Message.</param>
		/// <param name="HeaderFields">HTTP Header fields to include in the response.</param>
		public HttpException(int StatusCode, string Message, params KeyValuePair<string, string>[] HeaderFields)
			: base(Message)
		{
			this.statusCode = StatusCode;
			this.headerFields = HeaderFields;
		}

		/// <summary>
		/// Base class of all HTTP Exceptions.
		/// </summary>
		/// <param name="StatusCode">HTTP Status Code.</param>
		/// <param name="Message">HTTP Status Message.</param>
		/// <param name="ContentObject">Any content object to return. The object will be encoded before being sent.</param>
		public HttpException(int StatusCode, string Message, object ContentObject)
			: this(StatusCode, Message, ContentObject, NoCustomHeaders)
		{
		}

		/// <summary>
		/// Base class of all HTTP Exceptions.
		/// </summary>
		/// <param name="StatusCode">HTTP Status Code.</param>
		/// <param name="Message">HTTP Status Message.</param>
		/// <param name="ContentObject">Any content object to return. The object will be encoded before being sent.</param>
		/// <param name="HeaderFields">HTTP Header fields to include in the response.</param>
		public HttpException(int StatusCode, string Message, object ContentObject, params KeyValuePair<string, string>[] HeaderFields)
			: base(Message)
		{
			this.statusCode = StatusCode;
			this.headerFields = HeaderFields;
			this.contentObject = ContentObject;
		}

		/// <summary>
		/// Base class of all HTTP Exceptions.
		/// </summary>
		/// <param name="StatusCode">HTTP Status Code.</param>
		/// <param name="Message">HTTP Status Message.</param>
		/// <param name="Content">Any encoded content to return.</param>
		/// <param name="ContentType">The content type of <paramref name="Content"/>, if provided.</param>
		public HttpException(int StatusCode, string Message, byte[] Content, string ContentType)
			: this(StatusCode, Message, Content, ContentType, NoCustomHeaders)
		{
		}

		/// <summary>
		/// Base class of all HTTP Exceptions.
		/// </summary>
		/// <param name="StatusCode">HTTP Status Code.</param>
		/// <param name="Message">HTTP Status Message.</param>
		/// <param name="Content">Any encoded content to return.</param>
		/// <param name="ContentType">The content type of <paramref name="Content"/>, if provided.</param>
		/// <param name="HeaderFields">HTTP Header fields to include in the response.</param>
		public HttpException(int StatusCode, string Message, byte[] Content, string ContentType, params KeyValuePair<string, string>[] HeaderFields)
			: base(Message)
		{
			this.statusCode = StatusCode;
			this.headerFields = HeaderFields;
			this.content = Content;
			this.contentType = ContentType;
		}

		/// <summary>
		/// Gets the default status message, given a status code.
		/// </summary>
		/// <param name="StatusCode">Status code.</param>
		/// <returns>Status message.</returns>
		public static string GetStatusMessage(int StatusCode)
		{
			switch (StatusCode)
			{
				// Information
				case 100: return "Continue";
				case 101: return "Switching Protocols";
				case 102: return "Processing";
				case 103: return "Early Hints";

				// Successful responses
				case 200: return "OK";
				case 201: return "Created";
				case 202: return "Accepted";
				case 203: return "Non-Authoritative Information";
				case 204: return "No Content";
				case 205: return "Reset Content";
				case 206: return "Partial Content";
				case 207: return "Multi-Status";
				case 208: return "Already Reported";
				case 226: return "IM Used";

				// Redirections
				case 300: return "Multiple Choices";
				case MovedPermanentlyException.Code: return MovedPermanentlyException.StatusMessage;
				case FoundException.Code: return FoundException.StatusMessage;
				case SeeOtherException.Code: return SeeOtherException.StatusMessage;
				case NotModifiedException.Code: return NotModifiedException.StatusMessage;
				case UseProxyException.Code: return UseProxyException.StatusMessage;
				case 306: return "unused";
				case TemporaryRedirectException.Code: return TemporaryRedirectException.StatusMessage;
				case PermanentRedirectException.Code: return PermanentRedirectException.StatusMessage;

				// Client errors
				case BadRequestException.Code: return BadRequestException.StatusMessage;
				case ConflictException.Code: return ConflictException.StatusMessage;
				case FailedDependencyException.Code: return FailedDependencyException.StatusMessage;
				case ForbiddenException.Code: return ForbiddenException.StatusMessage;
				case GoneException.Code: return GoneException.StatusMessage;
				case LockedException.Code: return LockedException.StatusMessage;
				case MethodNotAllowedException.Code: return MethodNotAllowedException.StatusMessage;
				case MisdirectedRequestException.Code: return MisdirectedRequestException.StatusMessage;
				case NotAcceptableException.Code: return NotAcceptableException.StatusMessage;
				case NotFoundException.Code: return NotFoundException.StatusMessage;
				case PreconditionFailedException.Code: return PreconditionFailedException.StatusMessage;
				case PreconditionRequiredException.Code: return PreconditionRequiredException.StatusMessage;
				case RangeNotSatisfiableException.Code: return RangeNotSatisfiableException.StatusMessage;
				case RequestTimeoutException.Code: return RequestTimeoutException.StatusMessage;
				case TooManyRequestsException.Code: return TooManyRequestsException.StatusMessage;
				case UnauthorizedException.Code: return UnauthorizedException.StatusMessage;
				case UnavailableForLegalReasonsException.Code: return UnavailableForLegalReasonsException.StatusMessage;
				case UnprocessableEntityException.Code: return UnprocessableEntityException.StatusMessage;
				case UnsupportedMediaTypeException.Code: return UnsupportedMediaTypeException.StatusMessage;
				case UpgradeRequiredException.Code: return UpgradeRequiredException.StatusMessage;

				// Server errors
				case BadGatewayException.Code: return BadGatewayException.StatusMessage;
				case GatewayTimeoutException.Code: return GatewayTimeoutException.StatusMessage;
				case InsufficientStorageException.Code: return InsufficientStorageException.StatusMessage;
				case InternalServerErrorException.Code: return InternalServerErrorException.StatusMessage;
				case LoopDetectedException.Code: return LoopDetectedException.StatusMessage;
				case NetworkAuthenticationRequiredException.Code: return NetworkAuthenticationRequiredException.StatusMessage;
				case NotExtendedException.Code: return NotExtendedException.StatusMessage;
				case NotImplementedException.Code: return NotImplementedException.StatusMessage;
				case ServiceUnavailableException.Code: return ServiceUnavailableException.StatusMessage;
				case VariantAlsoNegotiatesException.Code: return VariantAlsoNegotiatesException.StatusMessage;

				default: return "Error";
			};
		}

		/// <summary>
		/// Joins two sets (possibly empty) of header arrays.
		/// </summary>
		/// <param name="Headers1">First array of headers.</param>
		/// <param name="Headers2">Second array of headers.</param>
		/// <returns>Joined array of headers.</returns>
		protected static KeyValuePair<string, string>[] Join(KeyValuePair<string, string>[] Headers1, params KeyValuePair<string, string>[] Headers2)
		{
			int c1 = Headers1?.Length ?? 0;
			if (c1 == 0)
				return Headers2;

			int c2 = Headers2?.Length ?? 0;
			if (c2 == 0)
				return Headers1;

			KeyValuePair<string, string>[] Result = new KeyValuePair<string, string>[c1 + c2];
			Array.Copy(Headers1, 0, Result, 0, c1);
			Array.Copy(Headers2, 0, Result, c1, c2);

			return Result;
		}

		/// <summary>
		/// HTTP Status Code.
		/// </summary>
		public int StatusCode => this.statusCode;

		/// <summary>
		/// HTTP Header fields to include in the response.
		/// </summary>
		public KeyValuePair<string, string>[] HeaderFields => this.headerFields;

		/// <summary>
		/// Any content object to return. The object will be encoded before being sent.
		/// </summary>
		[Obsolete("Use GetContentObjectAsync() instead, for better performance processing asynchronous elements in parallel environments.")]
		public object ContentObject => this.GetContentObjectAsync().Result;

		/// <summary>
		/// Any content object to return. The object will be encoded before being sent.
		/// </summary>
		public async Task<object> GetContentObjectAsync()
		{
			if (!(this.contentObject is null))
				return this.contentObject;

			if (this.content is null)
				return null;

			try
			{
				HttpFieldContentType ContentType = new HttpFieldContentType("Content-Type", this.contentType);
				ContentResponse Content = await InternetContent.DecodeAsync(ContentType.Type, this.content, ContentType.Encoding, null, null);

				if (Content.HasError)
					this.contentObject = this.content;
				else
					this.contentObject = Content.Decoded;
			}
			catch (Exception)
			{
				this.contentObject = this.content;
			}

			return this.contentObject;
		}

		/// <summary>
		/// Any encoded content to return.
		/// </summary>
		public byte[] Content => this.content;

		/// <summary>
		/// The content type of <see cref="Content"/>, if provided.
		/// </summary>
		public string ContentType => this.contentType;

	}
}
