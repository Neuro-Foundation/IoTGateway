﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.WHOIS;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace Waher.Security.LoginMonitor
{
	/// <summary>
	/// Delegate for endpoint annotation event handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate Task AnnotateEndpointEventHandler(object Sender, AnnotateEndpointEventArgs e);

	/// <summary>
	/// Class that monitors login events, and help applications determine malicious intent. 
	/// Register instance of class with <see cref="Log.Register(IEventSink)"/> to activate it.
	/// Call its <see cref="GetEarliestLoginOpportunity(string, string)"/> method to get information about 
	/// when a remote endpoint can login.
	/// 
	/// Stream of events are analyzed, to detect events with Event ID "LoginSuccessful" and "LoginFailure". Individual state objects 
	/// are maintained for each remote endpoint, allowing services to query the LoginAuditor class (using its 
	/// <see cref="GetEarliestLoginOpportunity(string, string)"/> method) about the current state of each remote endpoint trying to login.
	/// </summary>
	public class LoginAuditor : EventSink, ILoginAuditor
	{
		private readonly Dictionary<string, RemoteEndpoint> states = new Dictionary<string, RemoteEndpoint>();
		private readonly LoginInterval[] intervals;
		private readonly int nrIntervals;
		private CaseInsensitiveString domain;

		/// <summary>
		/// Class that monitors login events, and help applications determine malicious intent. 
		/// Register instance of class with <see cref="Log.Register(IEventSink)"/> to activate it.
		/// Call its <see cref="GetEarliestLoginOpportunity(string, string)"/> method to get information about 
		/// when a remote endpoint can login.
		/// 
		/// Stream of events are analyzed, to detect events with Event ID "LoginSuccessful" and "LoginFailure". Individual state objects 
		/// are maintained for each remote endpoint, allowing services to query the LoginAuditor class (using its 
		/// <see cref="GetEarliestLoginOpportunity(string, string)"/> method) about the current state of each remote endpoint trying to login.
		/// </summary>
		/// <param name="ObjectID">Log Object ID</param>
		/// <param name="LoginIntervals">Number of login attempts possible during given time period. Numbers must be positive, and
		/// interval ascending. If continually failing past accepted intervals, remote endpoint will be registered as malicious.</param>
		public LoginAuditor(string ObjectID, params LoginInterval[] LoginIntervals)
			: base(ObjectID)
		{
			this.intervals = LoginIntervals;
			this.nrIntervals = this.intervals.Length;
		}

		/// <summary>
		/// Main Domain the auditor operates on.
		/// </summary>
		public CaseInsensitiveString Domain
		{
			get => this.domain;
			set => this.domain = value;
		}

		/// <summary>
		/// Queues an event to be output.
		/// </summary>
		/// <param name="Event">Event to queue.</param>
		public override Task Queue(Event Event)
		{
			switch (Event.EventId)
			{
				case "LoginFailure":
					return this.ProcessLoginFailure(Event.Actor, this.FindProtocol(Event), Event.Timestamp,
						"Repeatedly failing login attempts.");

				case "LoginSuccessful":
					return this.ProcessLoginSuccessful(Event.Actor, this.FindProtocol(Event));

				default:
					return Task.CompletedTask;
			}
		}

		private string FindProtocol(Event Event)
		{
			if (Event.Tags is null)
				return string.Empty;

			foreach (KeyValuePair<string, object> Tag in Event.Tags)
			{
				if (Tag.Key == "Protocol")
					return Tag.Value?.ToString();
			}

			return string.Empty;
		}

		private string RemovePort(string s)
		{
			int i = s.LastIndexOf(':');
			if (i < 0)
				return s;

			string s2 = s.Substring(0, i);

			if (int.TryParse(s.Substring(i + 1), out int _) && IPAddress.TryParse(s2, out IPAddress _))
				return s2;
			else
				return s;
		}

		/// <summary>
		/// Gets an annotated Remote endpoint state object, if one is available.
		/// </summary>
		/// <param name="RemoteEndpoint">Remote Endpoint.</param>
		/// <returns>Annotated state object, if available. Null otherwise.</returns>
		public Task<RemoteEndpoint> GetAnnotatedStateObject(string RemoteEndpoint)
		{
			return this.GetAnnotatedStateObject(RemoteEndpoint, false);
		}

		/// <summary>
		/// Gets an annotated Remote endpoint state object, if one is available.
		/// </summary>
		/// <param name="RemoteEndpoint">Remote Endpoint.</param>
		/// <param name="CreateNew">If a new object can be created if none exists.</param>
		/// <returns>Annotated state object, if available. Null otherwise.</returns>
		public async Task<RemoteEndpoint> GetAnnotatedStateObject(string RemoteEndpoint, bool CreateNew)
		{
			string s = this.RemovePort(RemoteEndpoint);
			RemoteEndpoint EP = await this.GetStateObject(s, string.Empty, CreateNew);
			if (EP is null)
				return null;

			if (string.IsNullOrEmpty(EP.WhoIs))
			{
				if (IPAddress.TryParse(s, out IPAddress Address) && !IPAddress.IsLoopback(Address))
					EP.WhoIs = await WhoIsClient.Query(Address);

				await EP.Annotate();
				await Database.UpdateLazy(EP);
			}

			return EP;
		}

		private async Task<RemoteEndpoint> GetStateObject(string RemoteEndpoint, string Protocol, bool CreateNew)
		{
			RemoteEndpoint EP;

			RemoteEndpoint = this.RemovePort(RemoteEndpoint);

			lock (this.states)
			{
				if (this.states.TryGetValue(RemoteEndpoint, out EP))
					return EP;
			}

			bool Created = false;
			bool Updated = false;

			EP = await Database.FindFirstIgnoreRest<RemoteEndpoint>(new FilterFieldEqualTo("Endpoint", RemoteEndpoint), "Created");

			if (EP is null)
			{
				if (!CreateNew)
					return null;

				EP = new RemoteEndpoint()
				{
					Endpoint = RemoteEndpoint,
					LastProtocol = Protocol,
					Created = DateTime.Now,
					Blocked = false,
					State = new int[this.nrIntervals],
					Timestamps = new DateTime[this.nrIntervals],
					Domain = this.domain
				};

				EP.Reset(false);
				Created = true;
			}
			else
			{
				if (EP.State is null || EP.State.Length != this.nrIntervals)
				{
					EP.State = new int[this.nrIntervals];
					EP.Timestamps = new DateTime[this.nrIntervals];

					EP.Reset(false);
					Updated = true;
				}
			}

			lock (this.states)
			{
				if (this.states.TryGetValue(RemoteEndpoint, out RemoteEndpoint EP2))
					return EP2;

				this.states[RemoteEndpoint] = EP;
			}

			if (Created)
				await Database.InsertLazy(EP);
			else if (Updated)
				await Database.UpdateLazy(EP);

			return EP;
		}

		/// <summary>
		/// Processes a successful login attempt.
		/// 
		/// NOTE: Typically, logins are audited by listening on logged events.
		/// This method should only be called directly when such events are not logged.
		/// </summary>
		/// <param name="RemoteEndpoint">String-representation of remote endpoint.</param>
		/// <param name="Protocol">Protocol used to log in.</param>
		public async Task ProcessLoginSuccessful(string RemoteEndpoint, string Protocol)
		{
			RemoteEndpoint EP = await this.GetStateObject(RemoteEndpoint, Protocol, true);
			if (EP.LastProtocol == Protocol && !EP.LastFailed)
				return;

			EP.LastProtocol = Protocol;
			EP.Reset(false);

			await Database.UpdateLazy(EP);
		}

		/// <summary>
		/// Processes a failed login attempt.
		/// 
		/// NOTE: Typically, logins are audited by listening on logged events.
		/// This method should only be called directly when such events are not logged.
		/// </summary>
		/// <param name="RemoteEndpoint">String-representation of remote endpoint.</param>
		/// <param name="Protocol">Protocol used to log in.</param>
		/// <param name="Timestamp">Timestamp of event.</param>
		/// <param name="Reason">Reason for the failure. Will be logged with the state object, in case the remote endpoint
		/// gets blocked.</param>
		/// <returns>If the remote endpoint was or has been blocked as a result of the failure.</returns>
		public async Task<bool> ProcessLoginFailure(string RemoteEndpoint, string Protocol, DateTime Timestamp, string Reason)
		{
			RemoteEndpoint EP = await this.GetStateObject(RemoteEndpoint, Protocol, true);
			int i;

			if (EP.Blocked)
				return true;

			if (EP.LastFailed)
			{
				i = 0;

				while (i < this.nrIntervals)
				{
					if (EP.State[i] < this.intervals[i].NrAttempts)
					{
						EP.State[i]++;
						break;
					}
					else
					{
						EP.State[i] = 1;
						EP.Timestamps[i] = Timestamp;

						i++;
						if (i >= this.nrIntervals)
						{
							await this.Block(EP, Reason, Protocol);
							return true;
						}
					}
				}
			}
			else
			{
				for (i = 0; i < this.nrIntervals; i++)
				{
					EP.State[i] = 1;
					EP.Timestamps[i] = Timestamp;
				}
			}

			await Database.UpdateLazy(EP);

			return EP.Blocked;
		}

		/// <summary>
		/// Checks when a remote endpoint can login.
		/// </summary>
		/// <param name="RemoteEndpoint">String-representation of remote endpoint.</param>
		/// <param name="Protocol">Protocol used to log in.</param>
		/// <returns>When the remote endpoint is allowed to login:
		/// 
		/// null = Remote endpoint can login now.
		/// <see cref="DateTime.MaxValue"/> = Remote endpoint has been blocked and cannot login.
		/// Other <see cref="DateTime"/> values indicate when, at the earliest, the remote endpoint
		/// is allowed to login again.
		/// </returns>
		public async Task<DateTime?> GetEarliestLoginOpportunity(string RemoteEndpoint, string Protocol)
		{
			RemoteEndpoint EP = await this.GetStateObject(RemoteEndpoint, Protocol, true);

			if (EP.Blocked)
				return DateTime.MaxValue;

			if (!EP.LastFailed)
				return null;

			return this.GetEarliestLoginOpportunity(EP.State, EP.Timestamps);
		}

		/// <summary>
		/// Gets the earliest next login opportunity for a remote endpoint, given its states and timetamps.
		/// </summary>
		/// <param name="States">Current login attempt states.</param>
		/// <param name="Timestamps">Current login attempt timestamps.</param>
		/// <returns>Earliest login opportunity.</returns>
		public DateTime? GetEarliestLoginOpportunity(int[] States, DateTime[] Timestamps)
		{
			int i = 0;
			int c = Math.Min(Math.Min(States.Length, Timestamps.Length), this.nrIntervals);

			while (i < c && States[i] >= this.intervals[i].NrAttempts)
				i++;

			if (i == 0)
				return null;

			if (i >= c)
				return DateTime.MaxValue;

			DateTime Next = this.intervals[i - 1].AddIntervalTo(Timestamps[i - 1]);
			if (Next <= DateTime.Now)
				return null;
			else
				return Next;
		}

		private async Task Block(RemoteEndpoint EP, string Reason, string Protocol)
		{
			if (!EP.Blocked)
			{
				EP.Blocked = true;
				EP.Reason = Reason;

				KeyValuePair<string, object>[] Tags = await EP.Annotate(
					new KeyValuePair<string, object>("Reason", Reason),
					new KeyValuePair<string, object>("Protocol", Protocol));
				StringBuilder sb = new StringBuilder();

				sb.Append("Remote endpoint blocked.");
				EP.WhoIs = await AppendWhoIsInfo(sb, EP.Endpoint);

				Log.Alert(sb.ToString(), EP.Endpoint, this.ObjectID, "RemoteEndpointBlocked", EventLevel.Major, Tags);

				await Database.UpdateLazy(EP);
			}
		}

		/// <summary>
		/// Appends WHOIS information to a Markdown document.
		/// </summary>
		/// <param name="Markdown">Markdown being built.</param>
		/// <param name="RemoteEndpoint">Remote Endpoint.</param>
		/// <returns>WHOIS information found, or <see cref="string.Empty"/> if no information found.</returns>
		public static async Task<string> AppendWhoIsInfo(StringBuilder Markdown, string RemoteEndpoint)
		{
			if (IPAddress.TryParse(RemoteEndpoint, out IPAddress Address))
			{
				try
				{
					string s = await WhoIsClient.Query(Address);

					Markdown.AppendLine();
					Markdown.AppendLine();
					Markdown.AppendLine("WHOIS Information:");
					Markdown.AppendLine();
					Markdown.AppendLine("```");
					Markdown.AppendLine(s);
					Markdown.AppendLine("```");

					return s;
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Annotates a remote endpoint.
		/// </summary>
		/// <param name="RemoteEndpoint">String-representation of remote endpoint.</param>
		/// <param name="Tags">Predefined tags.</param>
		/// <returns>Tags, including tags provided by external annotation.</returns>
		public static async Task<KeyValuePair<string, object>[]> Annotate(string RemoteEndpoint, params KeyValuePair<string, object>[] Tags)
		{
			AnnotateEndpointEventArgs e = new AnnotateEndpointEventArgs(RemoteEndpoint);

			foreach (KeyValuePair<string, object> Tag in Tags)
				e.AddTag(Tag.Key, Tag.Value);

			AnnotateEndpointEventHandler h = AnnotateEndpoint;
			if (!(h is null))
			{
				try
				{
					await h(null, e);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}

			return e.GetTags();
		}

		/// <summary>
		/// Event raised when an endpoint is to be annotated. Can be used to add additional information about an endpoint.
		/// </summary>
		public static event AnnotateEndpointEventHandler AnnotateEndpoint = null;

		/// <summary>
		/// Unblocks a remote endpoint and resets counters for it.
		/// </summary>
		/// <param name="RemoteEndpoint">String-representation of remote endpoint.</param>
		public Task UnblockAndReset(string RemoteEndpoint)
		{
			return this.UnblockAndReset(RemoteEndpoint, string.Empty);
		}

		/// <summary>
		/// Unblocks a remote endpoint and resets counters for it.
		/// </summary>
		/// <param name="RemoteEndpoint">String-representation of remote endpoint.</param>
		/// <param name="Protocol">Protocol used to log in.</param>
		public async Task UnblockAndReset(string RemoteEndpoint, string Protocol)
		{
			RemoteEndpoint EP = await this.GetStateObject(RemoteEndpoint, Protocol, true);

			if (!string.IsNullOrEmpty(Protocol))
				EP.LastProtocol = Protocol;

			EP.Reset(true);

			await Database.UpdateLazy(EP);
		}

		/// <summary>
		/// Handles a failed login attempt.
		/// </summary>
		/// <param name="Message">Log message</param>
		/// <param name="UserName">Attempted user name.</param>
		/// <param name="RemoteEndpoint">String representation of remote endpoint</param>
		/// <param name="Protocol">Protocol</param>
		/// <param name="Tags">Any informative tags.</param>
		public static async void Fail(string Message, string UserName, string RemoteEndpoint, string Protocol,
			params KeyValuePair<string, object>[] Tags)
		{
			try
			{
				Tags = await Annotate(RemoteEndpoint, Protocol, Tags);
				Log.Notice(Message, UserName, RemoteEndpoint, "LoginFailure", Tags);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private static async Task<KeyValuePair<string, object>[]> Annotate(string RemoteEndpoint, string Protocol,
			params KeyValuePair<string, object>[] Tags)
		{
			int c = Tags?.Length ?? 0;
			if (c == 0)
				Tags = new KeyValuePair<string, object>[1];
			else
				Array.Resize(ref Tags, c + 1);

			Tags[c] = new KeyValuePair<string, object>("Protocol", Protocol);
			return await Annotate(RemoteEndpoint, Tags);
		}

		/// <summary>
		/// Handles a successful login attempt.
		/// </summary>
		/// <param name="Message">Log message</param>
		/// <param name="UserName">Attempted user name.</param>
		/// <param name="RemoteEndpoint">String representation of remote endpoint</param>
		/// <param name="Protocol">Protocol</param>
		/// <param name="Tags">Any informative tags.</param>
		public static async void Success(string Message, string UserName, string RemoteEndpoint, string Protocol,
			params KeyValuePair<string, object>[] Tags)
		{
			try
			{
				Tags = await Annotate(RemoteEndpoint, Protocol, Tags);
				Log.Informational(Message, UserName, RemoteEndpoint, "LoginSuccessful", Tags);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// Reports a TLS hacking attempt from an endpoint. Can be used to deny TLS negotiation to proceed, and conserving resources
		/// through use of <see cref="CanStartTls(string)"/>.
		/// </summary>
		/// <param name="RemoteEndpoint">Remote endpoint performing the attack.</param>
		/// <param name="Message">Message to log.</param>
		/// <param name="Protocol">Protocol used.</param>
		public static async Task ReportTlsHackAttempt(string RemoteEndpoint, string Message, string Protocol)
		{
			lock (tlsHackEndpoints)
			{
				tlsHackEndpoints[RemoteEndpoint] = DateTime.Now;
			}

			LoginAuditor.Fail(Message, string.Empty, RemoteEndpoint, "SMTP", await Annotate(RemoteEndpoint));
		}

		private static readonly Dictionary<string, DateTime> tlsHackEndpoints = new Dictionary<string, DateTime>();

		/// <summary>
		/// Checks if TLS negotiation can start, for a given endpoint. If the endpoint has tries a TLS hack attempt during the
		/// last hour, the answer will be no.
		/// </summary>
		/// <param name="RemoteEndpoint">Remote endpoint wishing to start TLS.</param>
		/// <returns>If TLS-negotiation can proceed.</returns>
		public static bool CanStartTls(string RemoteEndpoint)
		{
			lock (tlsHackEndpoints)
			{
				if (tlsHackEndpoints.TryGetValue(RemoteEndpoint, out DateTime TP))
				{
					if (DateTime.Now.Subtract(TP).TotalHours < 1)
						return false;

					tlsHackEndpoints.Remove(RemoteEndpoint);
					return true;
				}
				else
					return true;
			}
		}

	}
}
