﻿using System.Collections.Generic;
using Waher.Runtime.Collections;
using Waher.Runtime.Profiling.Events;

namespace Waher.Runtime.Profiling.Export
{
	/// <summary>
	/// Accumulates events
	/// </summary>
	public class Accumulator
	{
		private readonly Dictionary<string, long> sumTicks = new Dictionary<string, long>();
		private readonly SortedDictionary<long, ChunkedList<ProfilerEvent>> timeline = new SortedDictionary<long, ChunkedList<ProfilerEvent>>();
		private readonly ChunkedList<string> order = new ChunkedList<string>();
		private readonly ProfilerThread thread;
		private readonly object synchObj = new object();
		private long startTick = 0;
		private long lastTick = 0;
		private string lastName = null;

		/// <summary>
		/// Accumulates events
		/// </summary>
		/// <param name="Thread">Current thread.</param>
		public Accumulator(ProfilerThread Thread)
		{
			this.thread = Thread;
		}

		/// <summary>
		/// Adds an event, as-is.
		/// </summary>
		/// <param name="Event">Event</param>
		public void AddAsIs(ProfilerEvent Event)
		{
			long Tick = Event.Ticks;

			lock (this.synchObj)
			{
				this.CheckNameLocked(Tick);
				this.lastTick = Tick;
				this.AddLocked(Tick, Event);
			}
		}

		private void AddLocked(long Tick, ProfilerEvent Event)
		{
			if (!this.timeline.TryGetValue(Tick, out ChunkedList<ProfilerEvent> Events))
			{
				Events = new ChunkedList<ProfilerEvent>();
				this.timeline[Tick] = Events;
			}

			Events.Add(Event);
		}

		private void CheckNameLocked(long Tick)
		{
			if (!string.IsNullOrEmpty(this.lastName))
			{
				long Diff = Tick - this.lastTick;

				if (this.sumTicks.TryGetValue(this.lastName, out long l))
					this.sumTicks[this.lastName] = l + Diff;
				else
				{
					this.sumTicks[this.lastName] = Diff;
					this.order.Add(this.lastName);
				}

				this.lastName = null;
			}
		}

		/// <summary>
		/// Adds an event, as-is.
		/// </summary>
		/// <param name="Event">Event</param>
		public void Start(Start Event)
		{
			this.startTick = Event.Ticks;
			this.AddAsIs(Event);
		}

		/// <summary>
		/// Reports thread to be idle.
		/// </summary>
		/// <param name="Event">Event</param>
		public void Idle(Idle Event)
		{
			long Tick = Event.Ticks;

			lock (this.synchObj)
			{
				this.CheckNameLocked(Tick);
				this.lastTick = Tick;
			}
		}

		/// <summary>
		/// Sums time for a given state.
		/// </summary>
		/// <param name="Event">New state.</param>
		public void Sum(NewState Event)
		{
			long Tick = Event.Ticks;

			lock (this.synchObj)
			{
				this.CheckNameLocked(Tick);
				this.lastTick = Tick;
				this.lastName = Event.State;
			}
		}

		/// <summary>
		/// Reports accumulated events to a list of events.
		/// </summary>
		/// <param name="Result">Resulting list of events.</param>
		public void Report(ChunkedList<ProfilerEvent> Result)
		{
			lock (this.synchObj)
			{
				long Tick = this.startTick;

				foreach (string Name in this.order)
				{
					if (this.sumTicks.TryGetValue(Name, out long Ticks))
					{
						this.AddLocked(Tick, new NewState(Tick, Name, this.thread));
						Tick += Ticks;
					}
				}

				this.AddLocked(Tick, new Idle(Tick, this.thread));

				foreach (ChunkedList<ProfilerEvent> Events in this.timeline.Values)
					Result.AddRange(Events);
			}
		}

	}
}
