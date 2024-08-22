﻿namespace Waher.Networking.XMPP.Authentication
{
	/// <summary>
	/// Authentication method: PLAIN
	/// 
	/// See RFC 4616 for a description of the PLAIN method:
	/// https://tools.ietf.org/html/rfc4616
	/// </summary>
	public class Plain : MD5AuthenticationMethod
	{
		/// <summary>
		/// Authentication method: CRAM-MD5
		/// 
		/// See RFC 4616 for a description of the PLAIN method:
		/// https://tools.ietf.org/html/rfc4616
		/// </summary>
		public Plain()
		{
		}

		/// <summary>
		/// Name of hash method.
		/// </summary>
		public override string HashMethodName => "PLAIN";

		/// <inheritdoc/>
		public override string Challenge(string Challenge, XmppClient Client)
		{
			return string.Empty;
		}

		/// <inheritdoc/>
		public override bool CheckSuccess(string Success, XmppClient Client)
		{
			return true;
		}
	
	}
}
