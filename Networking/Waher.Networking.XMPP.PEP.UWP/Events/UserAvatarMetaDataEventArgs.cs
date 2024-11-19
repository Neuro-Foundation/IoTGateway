﻿using Waher.Events;

namespace Waher.Networking.XMPP.PEP.Events
{
	/// <summary>
	/// Event arguments for user avatar metadata events.
	/// </summary>
	public class UserAvatarMetaDataEventArgs : PersonalEventNotificationEventArgs
	{
		private readonly UserAvatarMetaData avatarMetaData;

		internal UserAvatarMetaDataEventArgs(UserAvatarMetaData AvatarMetaData, PersonalEventNotificationEventArgs e):
			base(e)
		{
			this.avatarMetaData = AvatarMetaData;
		}

		/// <summary>
		/// User avatar metadata.
		/// </summary>
		public UserAvatarMetaData AvatarMetaData => this.avatarMetaData;

		/// <summary>
		/// Gets an avatar published by a user using the Personal Eventing Protocol
		/// </summary>
		/// <param name="Reference">Avatar reference, selected from	an <see cref="UserAvatarMetaData"/> event.</param>
		/// <param name="Callback">Method to call when avatar has been retrieved.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void GetUserAvatarData(UserAvatarReference Reference, EventHandlerAsync<UserAvatarImageEventArgs> Callback, object State)
		{
			this.PepClient.GetUserAvatarData(this.FromBareJID, Reference, Callback, State);
		}
	}
}
