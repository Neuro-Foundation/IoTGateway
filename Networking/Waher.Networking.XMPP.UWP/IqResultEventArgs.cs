﻿using System;
using System.Xml;
using Waher.Content.Xml;

namespace Waher.Networking.XMPP
{
	/// <summary>
	/// Error Type
	/// </summary>
	public enum ErrorType
	{
		/// <summary>
		/// No error
		/// </summary>
		None,

		/// <summary>
		/// Retry after providing credentials
		/// </summary>
		Auth,

		/// <summary>
		/// Do not retry (the error cannot be remedied)
		/// </summary>
		Cancel,

		/// <summary>
		/// Proceed (the condition was only a warning)
		/// </summary>
		Continue,

		/// <summary>
		/// Retry after changing the data sent
		/// </summary>
		Modify,

		/// <summary>
		/// Retry after waiting (the error is temporary)
		/// </summary>
		Wait,

		/// <summary>
		/// Undefined error type
		/// </summary>
		Undefined
	}

	/// <summary>
	/// Event arguments for responses to IQ queries.
	/// </summary>
	public class IqResultEventArgs : EventArgs
	{
		private readonly XmlElement response;
		private XmlElement element = null;
		private readonly XmlElement errorElement = null;
		private readonly ErrorType errorType = ErrorType.None;
		private readonly XmppException stanzaError = null;
        private readonly IEndToEndEncryption e2eEncryption = null;
        private readonly IE2eSymmetricCipher e2eSymmetricCipher = null;
        private readonly string e2eReference = null;
        private readonly string errorText = string.Empty;
		private object state;
		private readonly string id;
		private readonly string to;
		private readonly string from;
		private readonly int errorCode;
		private bool ok;

		/// <summary>
		/// Event arguments for responses to IQ queries.
		/// </summary>
		/// <param name="e">Values are taken from this object.</param>
		protected IqResultEventArgs(IqResultEventArgs e)
		{
			this.response = e.response;
			this.errorElement = e.errorElement;
			this.errorType = e.errorType;
			this.stanzaError = e.stanzaError;
			this.errorText = e.errorText;
			this.state = e.state;
			this.id = e.id;
			this.to = e.to;
			this.from = e.from;
			this.errorCode = e.errorCode;
			this.ok = e.ok;
            this.e2eEncryption = e.e2eEncryption;
            this.e2eReference = e.e2eReference;
            this.e2eSymmetricCipher = e.e2eSymmetricCipher;
        }

        /// <summary>
        /// Event arguments for responses to IQ queries.
        /// </summary>
        /// <param name="E2eEncryption">End-to-end encryption algorithm used.</param>
        /// <param name="E2eReference">Reference to End-to-end encryption endpoint used.</param>
        /// <param name="E2eSymmetricCipher">Type of symmetric cipher used in E2E encryption.</param>
        /// <param name="Response">Response element.</param>
        /// <param name="Id">ID attribute.</param>
        /// <param name="To">To attribute.</param>
        /// <param name="From">From attribute.</param>
        /// <param name="Ok">If response is a proper response (true), or an error response (false).</param>
        /// <param name="State">State object passed in the original request.</param>
        public IqResultEventArgs(IEndToEndEncryption E2eEncryption, string E2eReference, IE2eSymmetricCipher E2eSymmetricCipher,
            XmlElement Response, string Id, string To, string From, bool Ok, object State)
            : this(Response, Id, To, From, Ok, State)
        {
            this.e2eEncryption = E2eEncryption;
            this.e2eReference = E2eReference;
            this.e2eSymmetricCipher = E2eSymmetricCipher;
        }

		/// <summary>
		/// Event arguments for responses to IQ queries.
		/// </summary>
		/// <param name="Response">Response element.</param>
		/// <param name="Id">ID attribute.</param>
		/// <param name="To">To attribute.</param>
		/// <param name="From">From attribute.</param>
		/// <param name="Ok">If response is a proper response (true), or an error response (false).</param>
		/// <param name="State">State object passed in the original request.</param>
		public IqResultEventArgs(XmlElement Response, string Id, string To, string From, bool Ok, object State)
		{
			this.response = Response;
			this.id = Id;
			this.to = To;
			this.from = From;
			this.ok = Ok;
			this.state = State;
			this.errorCode = 0;

			if (!Ok && !(Response is null))
			{
				foreach (XmlNode N in Response.ChildNodes)
				{
					if (!(N is XmlElement E))
						continue;

					if (E.LocalName == "error" && E.NamespaceURI == Response.NamespaceURI)
					{
						this.errorElement = E;
						this.errorCode = XML.Attribute(E, "code", 0);

						switch (XML.Attribute(E, "type"))
						{
							case "auth":
								this.errorType = ErrorType.Auth;
								break;

							case "cancel":
								this.errorType = ErrorType.Cancel;
								break;

							case "continue":
								this.errorType = ErrorType.Continue;
								break;

							case "modify":
								this.errorType = ErrorType.Modify;
								break;

							case "wait":
								this.errorType = ErrorType.Wait;
								break;

							default:
								this.errorType = ErrorType.Undefined;
								break;
						}

						this.stanzaError = XmppClient.GetExceptionObject(E);
						this.errorText = this.stanzaError.Message;
					}
				}
			}
		}

		/// <summary>
		/// IQ Response element.
		/// </summary>
		public XmlElement Response => this.response;

		/// <summary>
		/// First child element of the <see cref="Response"/> element.
		/// </summary>
		public XmlElement FirstElement
		{
			get
			{
				if (!(this.element is null))
					return this.element;

				foreach (XmlNode N in this.response.ChildNodes)
				{
					this.element = N as XmlElement;
					if (!(this.element is null))
						return this.element;
				}

				return null;
			}
		}
		/// <summary>
		/// State object passed to the original request.
		/// </summary>
		public object State 
		{
			get => this.state;
			set => this.state = value; 
		}

		/// <summary>
		/// ID of the request.
		/// </summary>
		public string Id => this.id;

		/// <summary>
		/// To address attribute
		/// </summary>
		public string To => this.to;

		/// <summary>
		/// From address attribute
		/// </summary>
		public string From => this.from;

		/// <summary>
		/// If the response is an OK result response (true), or an error response (false).
		/// </summary>
		public bool Ok
		{
			get => this.ok;
			set => this.ok = value;
		}

		/// <summary>
		/// Error Code
		/// </summary>
		public int ErrorCode => this.errorCode;

		/// <summary>
		/// Error Type
		/// </summary>
		public ErrorType ErrorType => this.errorType;

		/// <summary>
		/// Error element.
		/// </summary>
		public XmlElement ErrorElement => this.errorElement;

		/// <summary>
		/// Any error specific text.
		/// </summary>
		public string ErrorText => this.errorText;

		/// <summary>
		/// Any stanza error returned.
		/// </summary>
		public XmppException StanzaError => this.stanzaError;

        /// <summary>
        /// If end-to-end encryption was used in the response.
        /// </summary>
        public bool UsesE2eEncryption
        {
            get { return !(this.e2eEncryption is null); }
        }

        /// <summary>
        /// End-to-end encryption interface, if used in the request.
        /// </summary>
        public IEndToEndEncryption E2eEncryption => this.e2eEncryption;

        /// <summary>
        /// Reference to End-to-end encryption endpoint used.
        /// </summary>
        public string E2eReference => this.e2eReference;

        /// <summary>
        /// Type of symmetric cipher used in E2E encryption.
        /// </summary>
        public IE2eSymmetricCipher E2eSymmetricCipher => this.e2eSymmetricCipher;
    }
}
