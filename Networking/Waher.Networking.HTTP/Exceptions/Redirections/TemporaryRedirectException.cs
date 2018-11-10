﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Networking.HTTP
{
	/// <summary>
	/// The requested resource resides temporarily under a different URI. Since the redirection MAY be altered on occasion, the client SHOULD continue 
	/// to use the Request-URI for future requests. This response is only cacheable if indicated by a Cache-Control or Expires header field. 
	/// </summary>
	public class TemporaryRedirectException : HttpException
	{
		private const int Code = 307;
		private const string Msg = "Temporary Redirect";

		/// <summary>
		/// The requested resource resides temporarily under a different URI. Since the redirection MAY be altered on occasion, the client SHOULD continue 
		/// to use the Request-URI for future requests. This response is only cacheable if indicated by a Cache-Control or Expires header field. 
		/// </summary>
		/// <param name="Location">Location.</param>
		public TemporaryRedirectException(string Location)
			: base(Code, Msg, new KeyValuePair<string, string>("Location", Location))
		{
		}

		/// <summary>
		/// The requested resource resides temporarily under a different URI. Since the redirection MAY be altered on occasion, the client SHOULD continue 
		/// to use the Request-URI for future requests. This response is only cacheable if indicated by a Cache-Control or Expires header field. 
		/// </summary>
		/// <param name="Location">Location.</param>
		/// <param name="ContentObject">Any content object to return. The object will be encoded before being sent.</param>
		public TemporaryRedirectException(string Location, object ContentObject)
			: base(Code, Msg, ContentObject, new KeyValuePair<string, string>("Location", Location))
		{
		}

		/// <summary>
		/// The requested resource resides temporarily under a different URI. Since the redirection MAY be altered on occasion, the client SHOULD continue 
		/// to use the Request-URI for future requests. This response is only cacheable if indicated by a Cache-Control or Expires header field. 
		/// </summary>
		/// <param name="Location">Location.</param>
		/// <param name="Content">Any encoded content to return.</param>
		/// <param name="ContentType">The content type of <paramref name="Content"/>, if provided.</param>
		public TemporaryRedirectException(string Location, byte[] Content, string ContentType)
			: base(Code, Msg, Content, ContentType, new KeyValuePair<string, string>("Location", Location))
		{
		}
	}
}
