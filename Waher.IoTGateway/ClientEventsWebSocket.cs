﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking.HTTP;
using Waher.Networking.HTTP.WebSockets;
using Waher.Security;

namespace Waher.IoTGateway
{
	/// <summary>
	/// Web-socket binding method for the <see cref="ClientEvents"/> class. It allows clients connect to the gateway using web-sockets to
	/// get events.
	/// </summary>
	public class ClientEventsWebSocket : WebSocketListener
	{
		private static readonly string serverId = Hashes.BinaryToString(Gateway.NextBytes(32));

		/// <summary>
		/// Resource managing asynchronous events to web clients.
		/// </summary>
		public ClientEventsWebSocket()
			: base("/ClientEventsWS", true, 10 * 1024 * 1024, 10 * 1024 * 1024, "ls")
		{
			this.Accept += this.ClientEventsWebSocket_Accept;
			this.Connected += this.ClientEventsWebSocket_Connected;
		}

		/// <summary>
		/// Executes the GET method on the resource.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Response">HTTP Response</param>
		/// <exception cref="HttpException">If an error occurred when processing the method.</exception>
		public override Task GET(HttpRequest Request, HttpResponse Response)
		{
			SetTransparentCorsHeaders(this, Request, Response);
			return base.GET(Request, Response);
		}

		private Task ClientEventsWebSocket_Connected(object Sender, WebSocketEventArgs e)
		{
			e.Socket.Closed += this.Socket_Closed;
			e.Socket.Disposed += this.Socket_Disposed;
			e.Socket.TextReceived += this.Socket_TextReceived;

			return Task.CompletedTask;
		}

		private async Task Socket_TextReceived(object Sender, WebSocketTextEventArgs e)
		{
			if (JSON.Parse(e.Payload) is Dictionary<string, object> Obj &&
				Obj.TryGetValue("cmd", out object Value) && Value is string Command)
			{
				switch (Command)
				{
					case "Register":
						if (Obj.TryGetValue("tabId", out object O1) && O1 is string TabID &&
							Obj.TryGetValue("location", out object O2) && O2 is string Location)
						{
							e.Socket.Tag = new Info()
							{
								Location = Location,
								TabID = TabID
							};

							try
							{
								await ClientEvents.RegisterWebSocket(e.Socket, Location, TabID);
							}
							catch (Exception ex)
							{
								Log.Exception(ex);
							}

							await ClientEvents.PushEvent(new string[] { TabID }, "CheckServerInstance", serverId, false);
						}
						break;

					case "Unregister":
						await this.Close(e.Socket);
						break;

					case "Ping":
						if (e.Socket.Tag is Info Info)
							ClientEvents.Ping(Info.TabID);
						break;
				}
			}
		}

		private class Info
		{
			public string Location;
			public string TabID;
		}

		private async Task Socket_Disposed(object Sender, EventArgs e)
		{
			if (Sender is WebSocket WebSocket)
				await this.Close(WebSocket);
		}

		private Task Socket_Closed(object Sender, WebSocketClosedEventArgs e)
		{
			return this.Close(e.Socket);
		}

		private async Task Close(WebSocket Socket)
		{
			try
			{
				if (Socket.Tag is Info Info)
				{
					await ClientEvents.UnregisterWebSocket(Socket, Info.Location, Info.TabID);
					Socket.Tag = null;
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private Task ClientEventsWebSocket_Accept(object Sender, WebSocketEventArgs e)
		{
			// Cross-domain use allowed.
			//
			//HttpFieldCookie Cookie;
			//
			//if ((Cookie = e.Socket.HttpRequest.Header.Cookie) is null ||
			//	string.IsNullOrEmpty(Cookie[HttpResource.HttpSessionID]))
			//{
			//	throw new ForbiddenException("HTTP Session required.");
			//}

			return Task.CompletedTask;
		}

		/// <summary>
		/// If the resource handles sub-paths.
		/// </summary>
		public override bool HandlesSubPaths
		{
			get
			{
				return false;
			}
		}
	}
}
