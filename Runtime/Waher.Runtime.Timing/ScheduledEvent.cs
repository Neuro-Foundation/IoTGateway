﻿using System;
using Waher.Events;

namespace Waher.Runtime.Timing
{
	/// <summary>
	/// Contains information about a scheduled event.
	/// </summary>
	internal class ScheduledEvent
	{
		private readonly DateTime when;
		private readonly ScheduledEventCallback eventMethod;
		private readonly object state;

		/// <summary>
		/// Contains information about a scheduled event.
		/// </summary>
		/// <param name="When">When an event is to be executed.</param>
		/// <param name="EventMethod">Method to call when event is executed.</param>
		/// <param name="State">State object passed on to <paramref name="EventMethod"/>.</param>
		public ScheduledEvent(DateTime When, ScheduledEventCallback EventMethod, object State)
		{
			this.when = When;
			this.eventMethod = EventMethod;
			this.state = State;
		}

		/// <summary>
		/// Contains information about a scheduled event.
		/// </summary>
		/// <param name="When">When an event is to be executed.</param>
		/// <param name="EventMethod">Method to call when event is executed.</param>
		/// <param name="State">State object passed on to <paramref name="EventMethod"/>.</param>
		public ScheduledEvent(DateTime When, ScheduledEventCallbackAsync EventMethod, object State)
		{
			this.when = When;
			this.eventMethod = this.AsyncCallback;
			this.state = new object[] { EventMethod, State };
		}

		private async void AsyncCallback(object State)
		{
			try
			{
				object[] P = (object[])State;
				ScheduledEventCallbackAsync Callback = (ScheduledEventCallbackAsync)P[0];
				State = P[1];

				if (!(Callback is null))
					await Callback(State);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// When an event is to be executed.
		/// </summary>
		public DateTime When => this.when;

		/// <summary>
		/// Method to call when event is executed.
		/// </summary>
		public ScheduledEventCallback EventMethod => this.eventMethod;

		/// <summary>
		/// State object passed on to <see cref="EventMethod"/>.
		/// </summary>
		public object State => this.state;

		/// <summary>
		/// Executes the event.
		/// </summary>
		public void Execute()
		{
			if (!(this.eventMethod is null))
			{
				try
				{
					this.eventMethod(this.state);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}
		}

	}
}
