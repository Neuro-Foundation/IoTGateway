﻿using System.Threading.Tasks;
using Waher.Persistence;

namespace Waher.Networking.SASL
{
	/// <summary>
	/// Interface for server-side client connections.
	/// </summary>
	public interface ISaslServerSide
	{
		/// <summary>
		/// ID client claims to have
		/// </summary>
		string AuthId
		{
			get;
		}

		/// <summary>
		/// User name
		/// </summary>
		CaseInsensitiveString UserName
		{
			get;
		}

		/// <summary>
		/// Remote endpoint.
		/// </summary>
		string RemoteEndpoint
		{
			get;
		}

		/// <summary>
		/// String representing protocol being used.
		/// </summary>
		string Protocol
		{
			get;
		}

		/// <summary>
		/// Object tagged to the connection.
		/// </summary>
		object Tag
		{
			get;
			set;
		}

		/// <summary>
		/// Sets the account for the connection.
		/// </summary>
		/// <param name="Account">Account.</param>
		Task SetAccount(IAccount Account);

		/// <summary>
		/// Resets the state machine.
		/// </summary>
		/// <param name="Authenticated">If the client is authenticated.</param>
		void ResetState(bool Authenticated);

		/// <summary>
		/// Sets the identity of the user.
		/// </summary>
		/// <param name="UserName">Name of user.</param>
		void SetUserIdentity(CaseInsensitiveString UserName);

		/// <summary>
		/// Reports the SASL error: Not Authorized
		/// </summary>
		/// <returns>If transmitted.</returns>
		Task<bool> SaslErrorNotAuthorized();

		/// <summary>
		/// Reports the SASL error: Account disabled
		/// </summary>
		/// <returns>If transmitted.</returns>
		Task<bool> SaslErrorAccountDisabled();

		/// <summary>
		/// Reports the SASL error: Malformed request
		/// </summary>
		/// <returns>If transmitted.</returns>
		Task<bool> SaslErrorMalformedRequest();

		/// <summary>
		/// Returns a challenge to the client.
		/// </summary>
		/// <param name="ChallengeBase64">Base64-encoded challenge.</param>
		Task<bool> SaslChallenge(string ChallengeBase64);

		/// <summary>
		/// Returns a sucess response to the client.
		/// </summary>
		/// <param name="ProofBase64">Optional base64-encoded proof.</param>
		Task<bool> SaslSuccess(string ProofBase64);
	}
}
