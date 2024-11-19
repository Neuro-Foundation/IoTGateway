﻿using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP.MUC
{
	/// <summary>
	/// Message from a MUC room containing an invitation.
	/// </summary>
	public class RoomInvitationMessageEventArgs : RoomMessageEventArgs
	{
		private readonly MultiUserChatClient mucClient;
		private readonly string inviteFrom;
		private readonly string reason;
		private readonly string password;

		/// <summary>
		/// Message from a MUC room containing an invitation.
		/// </summary>
		/// <param name="MucClient">Multi-User Chat Client.</param>
		/// <param name="e">Message event arguments.</param>
		/// <param name="RoomId">Room ID</param>
		/// <param name="Domain">Domain hosting the room.</param>
		/// <param name="InviteFrom">JID of entity sending the invitation.</param>
		/// <param name="Reason">Reason for invitation.</param>
		/// <param name="Password">Password, if used.</param>
		public RoomInvitationMessageEventArgs(MultiUserChatClient MucClient, MessageEventArgs e, string RoomId, string Domain, 
			string InviteFrom, string Reason, string Password)
			: base(e, RoomId, Domain)
		{
			this.mucClient = MucClient;
			this.inviteFrom = InviteFrom;
			this.reason = Reason;
			this.password = Password;
		}

		/// <summary>
		/// Multi-User Chat Client.
		/// </summary>
		public MultiUserChatClient MucClient => this.mucClient;

		/// <summary>
		/// JID of entity sending the invitation.
		/// </summary>
		public string InviteFrom => this.inviteFrom;

		/// <summary>
		/// Reason for invitation.
		/// </summary>
		public string Reason => this.reason;

		/// <summary>
		/// Password, if used.
		/// </summary>
		public string Password => this.password;

		/// <summary>
		/// Accepts the invitation, and enters the room.
		/// </summary>
		/// <param name="NickName">Nick-name to use in the room.</param>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public Task Accept(string NickName, EventHandlerAsync<UserPresenceEventArgs> Callback, object State)
		{
			return this.mucClient.EnterRoom(this.RoomId, this.Domain, NickName, this.password, Callback, State);
		}

		/// <summary>
		/// Accepts the invitation, and enters the room.
		/// </summary>
		/// <param name="NickName">Nick-name to use in the room.</param>
		public Task<UserPresenceEventArgs> Accept(string NickName)
		{
			return this.mucClient.EnterRoomAsync(this.RoomId, this.Domain, NickName, this.password);
		}

		/// <summary>
		/// Declines the invitation, and enters the room.
		/// </summary>
		public Task Decline()
		{
			return this.Decline(string.Empty, string.Empty);
		}

		/// <summary>
		/// Declines the invitation, and enters the room.
		/// </summary>
		/// <param name="Reason">Reason for declining invitation.</param>
		public Task Decline(string Reason)
		{
			return this.Decline(Reason, string.Empty);
		}

		/// <summary>
		/// Declines the invitation, and enters the room.
		/// </summary>
		/// <param name="Reason">Reason for declining invitation.</param>
		/// <param name="Language">Language of text.</param>
		public Task Decline(string Reason, string Language)
		{
			return this.mucClient.DeclineInvitation(this.RoomId, this.Domain, this.inviteFrom, Reason, Language);
		}
	}
}
