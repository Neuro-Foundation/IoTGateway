﻿using Waher.Events;
using Waher.Events.Console;
using Waher.Events.XMPP;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.BitsOfBinary;
using Waher.Networking.XMPP.Chat;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.Interoperability;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Runtime.Console;
using Waher.Things;
using Waher.Things.ControlParameters;
using Waher.Things.SensorData;

namespace Waher.Mock.Lamp
{
	/// <summary>
	/// Mock lamp switch using XMPP.
	/// </summary>
	public class Program
	{
		private static XmppCredentials credentials;
		private static ThingRegistryClient thingRegistryClient = null;
		private static string ownerJid = null;
		private static bool registered = false;

		static void Main(string[] _)
		{
			try
			{
				ConsoleOut.ForegroundColor = ConsoleColor.White;

				ConsoleOut.WriteLine("Welcome to the Mock Temperature sensor application.");
				ConsoleOut.WriteLine(new string('-', 79));
				ConsoleOut.WriteLine("This application will simulate an outside temperature sensor.");
				ConsoleOut.WriteLine("Values will be published over XMPP using the interface defined in the Neuro-Foundation IoT extensions.");
				ConsoleOut.WriteLine("You can also chat with the sensor.");

				Log.RegisterAlertExceptionType(true,
					typeof(OutOfMemoryException),
					typeof(StackOverflowException),
					typeof(AccessViolationException),
					typeof(InsufficientMemoryException));

				Log.Register(new ConsoleEventSink());
				Log.RegisterExceptionToUnnest(typeof(System.Runtime.InteropServices.ExternalException));
				Log.RegisterExceptionToUnnest(typeof(System.Security.Authentication.AuthenticationException));

				credentials = SimpleXmppConfiguration.GetConfigUsingSimpleConsoleDialog("xmpp.config",
					Guid.NewGuid().ToString().Replace("-", string.Empty),	// Default user name.
					Guid.NewGuid().ToString().Replace("-", string.Empty),	// Default password.
					typeof(Program).Assembly);

				using (XmppClient Client = new XmppClient(credentials, "en", typeof(Program).Assembly))
				{
					if (credentials.Sniffer)
						Client.Add(new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount, LineEnding.PadWithSpaces));

					if (!string.IsNullOrEmpty(credentials.Events))
						Log.Register(new XmppEventSink("XMPP Event Sink", Client, credentials.Events, false));

					if (!string.IsNullOrEmpty(credentials.ThingRegistry))
					{
						thingRegistryClient = new ThingRegistryClient(Client, credentials.ThingRegistry);

						thingRegistryClient.Claimed += (Sender, e) =>
						{
							ownerJid = e.JID;
							Log.Informational("Thing has been claimed.", ownerJid, new KeyValuePair<string, object>("Public", e.IsPublic));
							return Task.CompletedTask;
						};

						thingRegistryClient.Disowned += (Sender, e) =>
						{
							Log.Informational("Thing has been disowned.", ownerJid);
							ownerJid = string.Empty;
							Register();
							return Task.CompletedTask;
						};

						thingRegistryClient.Removed += (Sender, e) =>
						{
							Log.Informational("Thing has been removed from the public registry.", ownerJid);
							return Task.CompletedTask;
						};
					}

					ProvisioningClient ProvisioningClient = null;
					if (!string.IsNullOrEmpty(credentials.Provisioning))
						ProvisioningClient = new ProvisioningClient(Client, credentials.Provisioning);

					Timer ConnectionTimer = new Timer((P) =>
					{
						if (Client.State == XmppState.Offline || Client.State == XmppState.Error || Client.State == XmppState.Authenticating)
						{
							try
							{
								Client.Reconnect();
							}
							catch (Exception ex)
							{
								Log.Exception(ex);
							}
						}
					}, null, 60000, 60000);

					bool Connected = false;
					bool ImmediateReconnect;

					Client.OnStateChanged += (sender, NewState) =>
					{
						switch (NewState)
						{
							case XmppState.Connected:
								Connected = true;

								if (!registered && !(thingRegistryClient is null))
									Register();
								break;

							case XmppState.Offline:
								ImmediateReconnect = Connected;
								Connected = false;

								if (ImmediateReconnect)
									Client.Reconnect();
								break;
						}

						return Task.CompletedTask;
					};

					Client.OnPresenceSubscribe += (Sender, e) =>
					{
						e.Accept();     // TODO: Provisioning

						RosterItem Item = Client.GetRosterItem(e.FromBareJID);
						if (Item is null || Item.State == SubscriptionState.None || Item.State == SubscriptionState.From)
							Client.RequestPresenceSubscription(e.FromBareJID);

						Client.SetPresence(Availability.Chat);

						return Task.CompletedTask;
					};

					Client.OnPresenceUnsubscribe += (Sender, e) =>
					{
						e.Accept();
						return Task.CompletedTask;
					};

					Client.OnRosterItemUpdated += (Sender, e) =>
					{
						if (e.State == SubscriptionState.None && e.PendingSubscription != PendingSubscription.Subscribe)
							Client.RemoveRosterItem(e.BareJid);

						return Task.CompletedTask;
					};

					bool SwitchOn = false;

					SensorServer SensorServer = new SensorServer(Client, ProvisioningClient, false);
					SensorServer.OnExecuteReadoutRequest += (Sender, Request) =>
					{
						DateTime Now = DateTime.Now;

						Log.Informational("Readout requested", string.Empty, Request.Actor);

						Request.ReportFields(true, new BooleanField(ThingReference.Empty, Now, "Lamp", SwitchOn, FieldType.Momentary, FieldQoS.AutomaticReadout));

						return Task.CompletedTask;
					};

					ControlServer ControlServer = new ControlServer(Client,
						new BooleanControlParameter("Lamp", "Control", "Lamp switch on.", "If checked, lamp is turned on.",
							(Node) => Task.FromResult<bool?>(SwitchOn),
							(Node, Value) =>
							{
								SwitchOn = Value;
								Log.Informational(Environment.NewLine + Environment.NewLine + "Lamp turned " + (SwitchOn ? "ON" : "OFF") + Environment.NewLine + Environment.NewLine);
								return Task.CompletedTask;
							}));

					BobClient BobClient = new BobClient(Client, Path.Combine(Path.GetTempPath(), "BitsOfBinary"));
					ChatServer ChatServer = new ChatServer(Client, BobClient, SensorServer, ControlServer, ProvisioningClient);

					InteroperabilityServer InteroperabilityServer = new InteroperabilityServer(Client);
					InteroperabilityServer.OnGetInterfaces += (Sender, e) =>
					{
						e.Add("XMPP.IoT.Actuator.Lamp");
						return Task.CompletedTask;
					};

					Client.Connect();

					while (true)
						Thread.Sleep(1000);
				}
			}
			catch (Exception ex)
			{
				ConsoleOut.ForegroundColor = ConsoleColor.Red;
				ConsoleOut.WriteLine(ex.Message);
			}
			finally
			{
				Log.Terminate();
			}
		}

		private static void Register()
		{
			string Key = Guid.NewGuid().ToString().Replace("-", string.Empty);

			// For info on tag names, see: http://xmpp.org/extensions/xep-0347.html#tags
			MetaDataTag[] MetaData = new MetaDataTag[]
			{
				new MetaDataStringTag("KEY", Key),
				new MetaDataStringTag("CLASS", "Lamp Actuator"),
				new MetaDataStringTag("MAN", "waher.se"),
				new MetaDataStringTag("MODEL", "Waher.Mock.Lamp"),
				new MetaDataStringTag("PURL", "https://github.com/PeterWaher/IoTGateway/tree/master/Mocks/Waher.Mock.Lamp"),
				new MetaDataNumericTag("V",1.0)
			};

			thingRegistryClient.RegisterThing(MetaData, (sender2, e2) =>
			{
				if (e2.Ok)
				{
					registered = true;

					if (e2.IsClaimed)
						ownerJid = e2.OwnerJid;
					else
					{
						ownerJid = string.Empty;
						SimpleXmppConfiguration.PrintQRCode(thingRegistryClient.EncodeAsIoTDiscoURI(MetaData));
					}
				}

				return Task.CompletedTask;

			}, null);
		}

	}
}
