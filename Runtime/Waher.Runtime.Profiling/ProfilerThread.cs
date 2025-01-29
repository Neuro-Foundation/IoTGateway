﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using Waher.Runtime.Profiling.Events;
using Waher.Runtime.Profiling.Export;

namespace Waher.Runtime.Profiling
{
	/// <summary>
	/// Type of profiler thread.
	/// </summary>
	public enum ProfilerThreadType
	{
		/// <summary>
		/// Documents the flow of states along time.
		/// </summary>
		Sequential,

		/// <summary>
		/// Documents the changes between states.
		/// </summary>
		StateMachine,

		/// <summary>
		/// Accumulates times in various sections during execution.
		/// </summary>
		Accumulating,

		/// <summary>
		/// Documents the changes between two binary states.
		/// </summary>
		Binary,

		/// <summary>
		/// Documents the change of an analog input through sampled values.
		/// </summary>
		Analog,

		/// <summary>
		/// Documents the change of an analog input through sampled delta-values.
		/// </summary>
		AnalogDelta
	}

	/// <summary>
	/// Class that keeps track of events and timing for one thread.
	/// </summary>
	public class ProfilerThread
	{
		private readonly List<ProfilerThread> subThreads = new List<ProfilerThread>();
		private readonly List<ProfilerEvent> events = new List<ProfilerEvent>();
		private readonly string name;
		private readonly int order;
		private readonly bool sampleDelta;
		private readonly ProfilerThreadType type;
		private readonly Profiler profiler;
		private readonly ProfilerThread parent;
		private string label;
		private double lastSample = 0;
		private long? startedAt = null;
		private long? stoppedAt = null;

		/// <summary>
		/// Class that keeps track of events and timing for one thread.
		/// </summary>
		/// <param name="Name">Name of thread.</param>
		/// <param name="Order">Order created.</param>
		/// <param name="Type">Type of profiler thread.</param>
		/// <param name="Profiler">Profiler reference.</param>
		public ProfilerThread(string Name, int Order, ProfilerThreadType Type, Profiler Profiler)
		{
			this.name = this.label = Name;
			this.order = Order;
			this.type = Type;
			this.profiler = Profiler;
			this.parent = null;
			this.sampleDelta = Type == ProfilerThreadType.AnalogDelta;
		}

		/// <summary>
		/// Class that keeps track of events and timing for one thread.
		/// </summary>
		/// <param name="Name">Name of thread.</param>
		/// <param name="Order">Order created.</param>
		/// <param name="Type">Type of profiler thread.</param>
		/// <param name="Parent">Parent thread.</param>
		public ProfilerThread(string Name, int Order, ProfilerThreadType Type, ProfilerThread Parent)
		{
			this.name = this.label = Name;
			this.order = Order;
			this.type = Type;
			this.parent = Parent;
			this.profiler = this.parent.Profiler;
			this.sampleDelta = Type == ProfilerThreadType.AnalogDelta;
		}

		/// <summary>
		/// Name of thread.
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// Displayable label, equal to the name by default, but can be set later
		/// during the profiling stage.
		/// </summary>
		public string Label
		{
			get => this.label;
			set => this.label = value;
		}

		/// <summary>
		/// Order created.
		/// </summary>
		public int Order => this.order;

		/// <summary>
		/// Type of profiler thread.
		/// </summary>
		public ProfilerThreadType Type => this.type;

		/// <summary>
		/// Profiler reference.
		/// </summary>
		public Profiler Profiler => this.profiler;

		/// <summary>
		/// Parent profiler thread, if any.
		/// </summary>
		public ProfilerThread Parent => this.parent;

		/// <summary>
		/// Ticks when thread was started.
		/// </summary>
		public long? StartedAt => this.startedAt;

		/// <summary>
		/// Ticks when thread was stopped.
		/// </summary>
		public long? StoppedAt => this.stoppedAt;

		/// <summary>
		/// Creates a new profiler thread.
		/// </summary>
		/// <param name="Name">Name of profiler thread.</param>
		/// <param name="Type">Type of profiler thread.</param>
		/// <returns>Profiler thread reference.</returns>
		public ProfilerThread CreateSubThread(string Name, ProfilerThreadType Type)
		{
			ProfilerThread Result = this.profiler.CreateThread(Name, Type, this);
			this.subThreads.Add(Result);
			return Result;
		}

		/// <summary>
		/// Thread changes state.
		/// </summary>
		/// <param name="State">String representation of the new state.</param>
		public void NewState(string State)
		{
			this.events.Add(new NewState(this.profiler.ElapsedTicks, State, this));
		}

		/// <summary>
		/// A new sample value has been recored
		/// </summary>
		/// <param name="Sample">New sample value.</param>
		public void NewSample(double Sample)
		{
			if (this.sampleDelta)
			{
				Sample += this.lastSample;
				this.events.Add(new NewSample(this.profiler.ElapsedTicks, Sample, this));
				this.lastSample = Sample;
			}
			else
				this.events.Add(new NewSample(this.profiler.ElapsedTicks, Sample, this));

		}

		/// <summary>
		/// Thread goes idle.
		/// </summary>
		public void Idle()
		{
			this.events.Add(new Idle(this.profiler.ElapsedTicks, this));
		}

		/// <summary>
		/// Sets the (binary) state to "high".
		/// </summary>
		public void High()
		{
			this.NewState("high");
		}

		/// <summary>
		/// Sets the (binary) state to "low".
		/// </summary>
		public void Low()
		{
			this.NewState("low");
		}

		/// <summary>
		/// Records an interval in the profiler thread.
		/// </summary>
		/// <param name="From">Starting timepoint.</param>
		/// <param name="To">Ending timepoint.</param>
		/// <param name="Label">Interval label.</param>
		public void Interval(DateTime From, DateTime To, string Label)
		{
			this.events.Add(new Interval(this.profiler.GetTicks(From), this.profiler.GetTicks(To), Label, this));
		}

		/// <summary>
		/// Records an interval in the profiler thread.
		/// </summary>
		/// <param name="From">Starting timepoint, in ticks.</param>
		/// <param name="To">Ending timepoint, in ticks.</param>
		/// <param name="Label">Interval label.</param>
		public void Interval(long From, long To, string Label)
		{
			this.events.Add(new Interval(From, To, Label, this));
		}

		/// <summary>
		/// Event occurred
		/// </summary>
		/// <param name="Name">Name of event.</param>
		public void Event(string Name)
		{
			this.events.Add(new Event(this.profiler.ElapsedTicks, Name, this));
		}

		/// <summary>
		/// Event occurred
		/// </summary>
		/// <param name="Name">Name of event.</param>
		/// <param name="Label">Optional label.</param>
		public void Event(string Name, string Label)
		{
			this.events.Add(new Event(this.profiler.ElapsedTicks, Name, Label, this));
		}

		/// <summary>
		/// Exception occurred
		/// </summary>
		/// <param name="Exception">Exception object.</param>
		public void Exception(System.Exception Exception)
		{
			this.events.Add(new Events.Exception(this.profiler.ElapsedTicks, Exception, this));
		}

		/// <summary>
		/// Exception occurred
		/// </summary>
		/// <param name="Exception">Exception object.</param>
		/// <param name="Label">Optional label.</param>
		public void Exception(System.Exception Exception, string Label)
		{
			this.events.Add(new Events.Exception(this.profiler.ElapsedTicks, Exception, Label, this));
		}

		/// <summary>
		/// Processing starts.
		/// </summary>
		public void Start()
		{
			long Ticks = this.profiler.ElapsedTicks;
			this.startedAt = Ticks;
			this.events.Add(new Start(Ticks, this));
		}

		/// <summary>
		/// Processing starts.
		/// </summary>
		public void Stop()
		{
			long Ticks = this.profiler.ElapsedTicks;
			this.stoppedAt = Ticks;
			this.events.Add(new Stop(Ticks, this));
		}

		/// <summary>
		/// Exports events to XML.
		/// </summary>
		/// <param name="Output">XML output.</param>
		/// <param name="TimeUnit">Time unit to use.</param>
		public void ExportXml(XmlWriter Output, TimeUnit TimeUnit)
		{
			Output.WriteStartElement("Thread");
			Output.WriteAttributeString("name", this.name);
			Output.WriteAttributeString("label", this.label);
			Output.WriteAttributeString("type", this.type.ToString());

			Output.WriteStartElement("Events");

			ProfilerEvent Prev = null;

			foreach (ProfilerEvent Event in this.events)
			{
				Event.ExportXml(Output, Prev, TimeUnit);
				Prev = Event;
			}

			Output.WriteEndElement();

			foreach (ProfilerThread Thread in this.subThreads)
				Thread.ExportXml(Output, TimeUnit);

			Output.WriteEndElement();
		}

		/// <summary>
		/// Exports events to PlantUML.
		/// </summary>
		/// <param name="Output">PlantUML output.</param>
		/// <param name="TimeUnit">Time unit to use.</param>
		public void ExportPlantUmlDescription(StringBuilder Output, TimeUnit TimeUnit)
		{
			switch (this.type)
			{
				case ProfilerThreadType.StateMachine:
					Output.Append("robust \"");
					break;

				case ProfilerThreadType.Binary:
					Output.Append("binary \"");
					break;

				case ProfilerThreadType.Analog:
				case ProfilerThreadType.AnalogDelta:
					Output.Append("analog \"");
					break;

				case ProfilerThreadType.Sequential:
				case ProfilerThreadType.Accumulating:
				default:
					Output.Append("concise \"");
					break;
			}

			Output.Append(this.label);
			Output.Append("\" as T");
			Output.AppendLine(this.order.ToString());

			if (this.type == ProfilerThreadType.Accumulating)
			{
				Accumulator Accumulator = new Accumulator(this);
				ProfilerEvent[] Events = this.events.ToArray();
				this.events.Clear();

				foreach (ProfilerEvent Event in Events)
					Event.Accumulate(Accumulator);

				Accumulator.Report(this.events);
			}

			foreach (ProfilerEvent Event in this.events)
				Event.ExportPlantUmlPreparation();

			foreach (ProfilerThread Thread in this.subThreads)
				Thread.ExportPlantUmlDescription(Output, TimeUnit);
		}

		/// <summary>
		/// Thread key in diagrams.
		/// </summary>
		public string Key => "T" + this.order.ToString();

		/// <summary>
		/// Exports events to PlantUML.
		/// </summary>
		/// <param name="States">PlantUML States</param>
		public void ExportPlantUmlEvents(PlantUmlStates States)
		{
			foreach (ProfilerEvent Event in this.events)
				Event.ExportPlantUml(States);

			foreach (ProfilerThread Thread in this.subThreads)
				Thread.ExportPlantUmlEvents(States);

			if (this.startedAt.HasValue && this.stoppedAt.HasValue)
			{
				StringBuilder Output = States.Summary;
				long Ticks = this.stoppedAt.Value - this.startedAt.Value;
				string ElapsedStr = this.profiler.ToTimeStr(Ticks, this, States.TimeUnit, 3);
				KeyValuePair<double, string> Time;

				Output.Append(this.Key);
				Output.Append('@');

				Time = this.profiler.ToTime(this.startedAt.Value, this, States.TimeUnit);
				Output.Append(Time.Key.ToString("F0").Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."));

				Output.Append(" <-> @");

				Time = this.profiler.ToTime(this.stoppedAt.Value, this, States.TimeUnit);
				Output.Append(Time.Key.ToString("F0").Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."));

				Output.Append(" : ");
				Output.AppendLine(ElapsedStr);
			}
		}

		/// <summary>
		/// Time (amount, unit), corresponding to a measured number of high-frequency clock ticks.
		/// </summary>
		/// <param name="Ticks">Ticks</param>
		/// <param name="TimeUnit">Time unit to use.</param>
		/// <returns>Corresponding time.</returns>
		public KeyValuePair<double, string> ToTime(long Ticks, TimeUnit TimeUnit)
		{
			return this.profiler.ToTime(Ticks, this, TimeUnit);
		}

		/// <summary>
		/// String representation of time, corresponding to a measured number of high-frequency clock ticks.
		/// </summary>
		/// <param name="Ticks">Ticks</param>
		/// <param name="TimeUnit">Time unit to use.</param>
		/// <returns>Corresponding time as a string.</returns>
		public string ToTimeStr(long Ticks, TimeUnit TimeUnit)
		{
			return this.profiler.ToTimeStr(Ticks, this, TimeUnit, 7);
		}

	}
}
