﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Networking.HTTP
{
	/// <summary>
	/// The request could not be completed due to a conflict with the current state of the resource. This code is only allowed in situations where 
	/// it is expected that the user might be able to resolve the conflict and resubmit the request. The response body SHOULD include enough 
	/// information for the user to recognize the source of the conflict. Ideally, the response entity would include enough information for the user 
	/// or user agent to fix the problem; however, that might not be possible and is not required. 
	/// </summary>
	public class ConflictException : HttpException
	{
		private const int Code = 409;
		private const string Msg = "Conflict";

		/// <summary>
		/// The request could not be completed due to a conflict with the current state of the resource. This code is only allowed in situations where 
		/// it is expected that the user might be able to resolve the conflict and resubmit the request. The response body SHOULD include enough 
		/// information for the user to recognize the source of the conflict. Ideally, the response entity would include enough information for the user 
		/// or user agent to fix the problem; however, that might not be possible and is not required. 
		/// </summary>
		public ConflictException()
			: base(Code, Msg)
		{
		}

		/// <summary>
		/// The request could not be completed due to a conflict with the current state of the resource. This code is only allowed in situations where 
		/// it is expected that the user might be able to resolve the conflict and resubmit the request. The response body SHOULD include enough 
		/// information for the user to recognize the source of the conflict. Ideally, the response entity would include enough information for the user 
		/// or user agent to fix the problem; however, that might not be possible and is not required. 
		/// </summary>
		/// <param name="ContentObject">Any content object to return. The object will be encoded before being sent.</param>
		public ConflictException(object ContentObject)
			: base(Code, Msg, ContentObject)
		{
		}

		/// <summary>
		/// The request could not be completed due to a conflict with the current state of the resource. This code is only allowed in situations where 
		/// it is expected that the user might be able to resolve the conflict and resubmit the request. The response body SHOULD include enough 
		/// information for the user to recognize the source of the conflict. Ideally, the response entity would include enough information for the user 
		/// or user agent to fix the problem; however, that might not be possible and is not required. 
		/// </summary>
		/// <param name="Content">Any encoded content to return.</param>
		/// <param name="ContentType">The content type of <paramref name="Content"/>, if provided.</param>
		public ConflictException(byte[] Content, string ContentType)
			: base(Code, Msg, Content, ContentType)
		{
		}
	}
}
