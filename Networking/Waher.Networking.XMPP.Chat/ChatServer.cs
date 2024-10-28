﻿using System;
using SkiaSharp;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Html;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP.BitsOfBinary;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Runtime.Cache;
using Waher.Runtime.Inventory;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Graphs;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;
using Waher.Script.Objects.VectorSpaces;
using Waher.Script.Operators.Vectors;
using Waher.Security;
using Waher.Things;
using Waher.Things.SensorData;
using Waher.Things.ControlParameters;

namespace Waher.Networking.XMPP.Chat
{
	/// <summary>
	/// Class managing a chat interface for things.
	/// 
	/// The chat interface is defined in:
	/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
	/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
	/// </summary>
	public class ChatServer : XmppExtension
	{
		private readonly Cache<string, Variables> sessions;
		private readonly SensorServer sensorServer;
		private readonly ControlServer controlServer;
		private readonly ConcentratorServer concentratorServer;
		private readonly ProvisioningClient provisioningClient;
		private readonly BobClient bobClient;
		private HttpFileUploadClient httpUpload = null;

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="SensorServer">Sensor Server. Can be null, if not supporting a sensor interface.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, SensorServer SensorServer)
			: this(Client, BobClient, SensorServer, null, null)
		{
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="ControlServer">Control Server. Can be null, if not supporting a control interface.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, ControlServer ControlServer)
			: this(Client, BobClient, null, ControlServer, null)
		{
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="SensorServer">Sensor Server. Can be null, if not supporting a sensor interface.</param>
		/// <param name="ControlServer">Control Server. Can be null, if not supporting a control interface.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, SensorServer SensorServer, ControlServer ControlServer)
			: this(Client, BobClient, SensorServer, ControlServer, null)
		{
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="ConcentratorServer">Concentrator Server.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, ConcentratorServer ConcentratorServer)
			: this(Client, BobClient, ConcentratorServer, null)
		{
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="SensorServer">Sensor Server. Can be null, if not supporting a sensor interface.</param>
		/// <param name="ProvisioningClient">Provisioning client.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, SensorServer SensorServer, ProvisioningClient ProvisioningClient)
			: this(Client, BobClient, SensorServer, null, ProvisioningClient)
		{
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="ControlServer">Control Server. Can be null, if not supporting a control interface.</param>
		/// <param name="ProvisioningClient">Provisioning client.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, ControlServer ControlServer, ProvisioningClient ProvisioningClient)
			: this(Client, BobClient, null, ControlServer, ProvisioningClient)
		{
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="SensorServer">Sensor Server. Can be null, if not supporting a sensor interface.</param>
		/// <param name="ControlServer">Control Server. Can be null, if not supporting a control interface.</param>
		/// <param name="ProvisioningClient">Provisioning client.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, SensorServer SensorServer, ControlServer ControlServer,
			ProvisioningClient ProvisioningClient)
			: base(Client)
		{
			this.bobClient = BobClient;
			this.sensorServer = SensorServer;
			this.controlServer = ControlServer;
			this.provisioningClient = ProvisioningClient;
			this.concentratorServer = null;

			this.client.OnChatMessage += this.Client_OnChatMessage;

			this.client.RegisterFeature("urn:xmpp:iot:chat");
			this.client.SetPresence(Availability.Chat);

			this.sessions = new Cache<string, Variables>(1000, TimeSpan.MaxValue, new TimeSpan(0, 20, 0));
			this.sessions.Removed += this.Sessions_Removed;
		}

		/// <summary>
		/// Class managing a chat interface for things.
		/// 
		/// The chat interface is defined in:
		/// https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.xml
		/// http://htmlpreview.github.io/?https://github.com/joachimlindborg/XMPP-IoT/blob/master/xep-0000-IoT-Chat.html
		/// </summary>
		/// <param name="Client">XMPP Client.</param>
		/// <param name="BobClient">Bits-of-Binary client.</param>
		/// <param name="ConcentratorServer">Concentrator Server.</param>
		/// <param name="ProvisioningClient">Provisioning client.</param>
		public ChatServer(XmppClient Client, BobClient BobClient, ConcentratorServer ConcentratorServer, ProvisioningClient ProvisioningClient)
			: base(Client)
		{
			this.bobClient = BobClient;
			this.sensorServer = ConcentratorServer.SensorServer;
			this.controlServer = ConcentratorServer.ControlServer;
			this.provisioningClient = ProvisioningClient;
			this.concentratorServer = ConcentratorServer;

			this.client.OnChatMessage += this.Client_OnChatMessage;

			this.client.RegisterFeature("urn:xmpp:iot:chat");
			this.client.SetPresence(Availability.Chat);

			this.sessions = new Cache<string, Variables>(1000, TimeSpan.MaxValue, new TimeSpan(0, 20, 0));
			this.sessions.Removed += this.Sessions_Removed;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			base.Dispose();

			this.client.UnregisterFeature("urn:xmpp:iot:chat");
		}

		/// <summary>
		/// Implemented extensions.
		/// </summary>
		public override string[] Extensions => new string[0];

		private async Task SendChatMessage(string To, string OrgSubject, string OrgCommand, string Markdown, RemoteXmppSupport Support,
			bool Last, params Tuple<string, string, byte[]>[] EmbeddedContent)
		{
			if (Support.Mail && !string.IsNullOrEmpty(OrgCommand))
				Markdown = ">\t" + OrgCommand + "\r\n\r\n" + Markdown;

			MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown, markdownSettings);
			string PlainText = await Doc.GeneratePlainText();
			StringBuilder Xml = new StringBuilder();

			if (Support.Html)
			{
				Xml.Append("<html xmlns='http://jabber.org/protocol/xhtml-im'><body xmlns='http://www.w3.org/1999/xhtml'>");
				HtmlDocument Doc2 = new HtmlDocument(await Doc.GenerateHTML());
				XmlEncode(Doc2.Body, Xml);
				Xml.Append("</body></html>");
			}

			if (Support.Markdown)
			{
				Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
				Xml.Append(XML.Encode(Markdown));
				Xml.Append("</content>");
			}

			if (Support.Mail)
			{
				if (!(EmbeddedContent is null))
				{
					foreach (Tuple<string, string, byte[]> EmbeddedObject in EmbeddedContent)
					{
						Xml.Append("<content xmlns='urn:xmpp:smtp' cid='");
						Xml.Append(XML.Encode(EmbeddedObject.Item1));
						Xml.Append("' type='");
						Xml.Append(XML.Encode(EmbeddedObject.Item2));
						Xml.Append("' disposition='Inline'>");
						Xml.Append(Convert.ToBase64String(EmbeddedObject.Item3));        // TODO: Chunked transfer using Waher.Networking.XMPP.Mail
						Xml.Append("</content>");
					}
				}

				if (Last)
					Xml.Append("<immediate xmlns='urn:xmpp:smtp'/>");
			}

			this.client.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, To, Xml.ToString(), PlainText,
				string.IsNullOrEmpty(OrgSubject) ? string.Empty : "Re: " + OrgSubject, string.Empty, string.Empty,
				string.Empty, null, null);
		}

		private static readonly MarkdownSettings markdownSettings = new MarkdownSettings(null, false);

		private static void XmlEncode(HtmlNode N, StringBuilder Output)
		{
			if (N is HtmlElement E)
			{
				Output.Append('<');
				Output.Append(E.Name);

				if (E.HasAttributes)
				{
					foreach (HtmlAttribute Attr in E.Attributes)
					{
						if (Attr.Name.IndexOf(':') >= 0)
							continue;

						Output.Append(' ');
						Output.Append(Attr.Name);
						Output.Append("=\"");
						Output.Append(XML.HtmlAttributeEncode(Attr.Value));
						Output.Append('"');
					}
				}

				if (E.HasChildren)
				{
					Output.Append('>');

					foreach (HtmlNode N2 in E.Children)
						XmlEncode(N2, Output);

					Output.Append("</");
					Output.Append(E.Name);
					Output.Append('>');
				}
				else if (E.IsEmptyElement)
					Output.Append("/>");
				else
				{
					Output.Append("></");
					Output.Append(E.Name);
					Output.Append('>');
				}
			}
			else if (N is HtmlText Text)
				Output.Append(XML.Encode(Text.InlineText));
			else if (N is HtmlEntity Entity)
			{
				if (N is HtmlEntityUnicode EntityUnicode)
				{
					char ch = (char)EntityUnicode.Code;

					switch (ch)
					{
						case '<':
							Output.Append("&lt;");
							break;

						case '>':
							Output.Append("&gt;");
							break;

						case '"':
							Output.Append("&quot;");
							break;

						case '\'':
							Output.Append("&apos;");
							break;

						case '&':
							Output.Append("&amp;");
							break;

						default:
							Output.Append(ch);
							break;
					}
				}
				else
				{
					switch (Entity.EntityName.ToLower())
					{
						case "lt":
						case "gt":
						case "quot":
						case "apos":
						case "amp":
							Output.Append('&');
							Output.Append(Entity.EntityName);
							Output.Append(';');
							break;

						default:
							Output.Append(HtmlEntity.EntityToCharacter(Entity.EntityName));
							break;
					}
				}
			}
			else if (N is CDATA CDATA)
				Output.Append(XML.Encode(CDATA.Content));
		}

		private async Task Client_OnChatMessage(object Sender, MessageEventArgs e)
		{
			Variables Variables = this.GetVariables(e.From);
			IDataSource SelectedSource = null;
			INode SelectedNode = null;
			int i;

			if (this.httpUpload is null)
			{
				this.httpUpload = new HttpFileUploadClient(this.client);
				this.httpUpload.Discover(null);
			}

			if (!Variables.TryGetVariable(" Menu ", out Variable v) ||
				!(v.ValueObject is SortedDictionary<int, KeyValuePair<string, object>> Menu))
			{
				Menu = null;
			}

			if (!(this.concentratorServer is null))
			{
				if (!Variables.TryGetVariable(" Source ", out v) || (SelectedSource = v.ValueObject as IDataSource) is null)
				{
					if (this.concentratorServer.RootDataSources.Length == 1)
					{
						SelectedSource = this.concentratorServer.RootDataSources[0];
						Variables[" Source "] = SelectedSource;
					}
					else
					{
						foreach (IDataSource Source in this.concentratorServer.DataSources)
						{
							if (Source.SourceID == "MeteringTopology")
							{
								SelectedSource = Source;
								Variables[" Source "] = SelectedSource;
								break;
							}
						}
					}

					SelectedNode = null;
					Variables[" Node "] = SelectedNode;
				}

				if (!Variables.TryGetVariable(" Node ", out v) || (SelectedNode = v.ValueObject as INode) is null)
					SelectedNode = null;
			}

			if (!Variables.TryGetVariable(" Support ", out v) || !(v.ValueObject is RemoteXmppSupport Support))
			{
				Support = new RemoteXmppSupport();

				foreach (XmlNode N in e.Message.ChildNodes)
				{
					if (N is XmlElement E)
					{
						switch (E.LocalName)
						{
							case "content":
								if (E.NamespaceURI == "urn:xmpp:content" && XML.Attribute(E, "type") == MarkdownCodec.ContentType)
									Support.Markdown = true;
								break;

							case "mailInfo":
								if (E.NamespaceURI == "urn:xmpp:smtp")
								{
									Support.Html = true;
									Support.Markdown = true;
									Support.Mail = true;
								}
								break;
						}
					}
				}

				Variables[" Support "] = Support;

				if (!Support.Mail)
				{
					ServiceDiscoveryEventArgs e2 = await this.client.ServiceDiscoveryAsync(e.E2eEncryption, e.From, string.Empty);

					if (e2.Features.ContainsKey("http://jabber.org/protocol/xhtml-im"))
						Support.Html = true;

					Support.ByteStreams = e2.Features.ContainsKey("http://jabber.org/protocol/bytestreams");
					Support.FileTransfer = e2.Features.ContainsKey("http://jabber.org/protocol/si/profile/file-transfer");
					Support.SessionInitiation = e2.Features.ContainsKey("http://jabber.org/protocol/si");
					Support.InBandBytestreams = e2.Features.ContainsKey("http://jabber.org/protocol/ibb");
					Support.BitsOfBinary = e2.Features.ContainsKey("urn:xmpp:bob");
				}

				List<string> Features = new List<string>()
				{
					"Plain text"
				};

				if (Support.Markdown)
					Features.Add("Markdown");

				if (Support.Html)
					Features.Add("HTML");

				if (Support.Mail)
					Features.Add("Mail");

				if (Support.ByteStreams)
					Features.Add("Bytestreams");

				if (Support.FileTransfer)
					Features.Add("File transfer");

				if (Support.SessionInitiation)
					Features.Add("Session Initiation");

				if (Support.InBandBytestreams)
					Features.Add("In-band byte streams");

				if (Support.BitsOfBinary)
					Features.Add("Bits of binary");

				i = 0;
				int c = Features.Count;
				StringBuilder Msg = new StringBuilder("I've detected you support ");

				foreach (string Feature in Features)
				{
					Msg.Append(Feature);

					i++;
					if (i == c)
						Msg.Append('.');
					else if (i == c - 1)
						Msg.Append(" and ");
					else if (i < c - 1)
						Msg.Append(", ");
				}

				await this.SendChatMessage(e.From, e.Subject, string.Empty, Msg.ToString(), Support, false);

				if (!(this.provisioningClient is null))
				{
					if (Variables.TryGetVariable(" User ", out v) &&
						v.ValueObject is User User &&
						Types.TryGetQualifiedNames(typeof(Expression).Namespace + ".Persistence", out string[] P))
					{
						if (string.Compare(e.FromBareJID, this.provisioningClient.OwnerJid, true) == 0)
						{
							User.SetPrivilege(typeof(Expression).Namespace + ".Persistence", true);
							await this.SendChatMessage(e.From, e.Subject, string.Empty, "SQL interface enabled.", Support, false);
						}
						else
						{
							this.provisioningClient.CanRead(e.FromBareJID, FieldType.All, null, null, null, null, null, async (sender3, e3) =>
							{
								if (e3.CanRead && e3.FieldTypes == FieldType.All)
								{
									User.SetPrivilege(typeof(Expression).Namespace + ".Persistence.SQL.Select", true);
									await this.SendChatMessage(e.From, e.Subject, string.Empty, "SQL SELECT interface enabled.", Support, false);
								}
							}, null);
						}
					}
				}
			}

			string Message = e.Body;

			if (string.IsNullOrWhiteSpace(Message))
			{
				foreach (XmlNode N in e.Message.ChildNodes)
				{
					if (N is XmlElement E)
					{
						switch (E.LocalName)
						{
							case "content":
								if (E.NamespaceURI == "urn:xmpp:content" && XML.Attribute(E, "type") == MarkdownCodec.ContentType)
								{
									Support.Markdown = true;

									string s2 = E.InnerText;

									if (!string.IsNullOrEmpty(s2))
										Message = s2;
								}
								break;
						}
					}
				}
			}

			if (string.IsNullOrEmpty(Message = Message?.Trim()))
				return;

			string[] Rows = Message.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);
			int NrRows = Rows.Length;
			int RowNr;

			for (RowNr = 1; RowNr <= NrRows; RowNr++)
			{
				string Row = Rows[RowNr - 1];
				string s = Row;
				bool Last = RowNr == NrRows;

				switch (s.ToLower())
				{
					case "hi":
					case "hello":
					case "hej":
					case "hallo":
					case "hola":
						await this.SendChatMessage(e.From, e.Subject, Row, "Hello. Type # to display the menu.", Support, Last);
						break;

					case "#":
						await this.ShowHelp(e.From, false, Support, e.Subject, Row, Last);
						break;

					case "##":
						await this.ShowHelp(e.From, true, Support, e.Subject, Row, Last);
						break;

					case "?":
					case "??":
						IThingReference ThingRef;
						bool Full = (s == "??");

						if (!(this.concentratorServer is null))
						{
							if (SelectedNode is null)
							{
								await this.Error(e.From, "No node selected.", Support, e.Subject, Row, Last);
								break;
							}

							ThingRef = SelectedNode;
						}
						else
							ThingRef = ThingReference.Empty;

						IThingReference[] NodeReferences = new IThingReference[] { ThingRef ?? ThingReference.Empty };
						FieldType FieldTypes = Full ? FieldType.All : FieldType.AllExceptHistorical;
						InternalReadoutFieldsEventHandler FieldsHandler = Full ? (InternalReadoutFieldsEventHandler)this.AllFieldsRead : this.MomentaryFieldsRead;
						InternalReadoutErrorsEventHandler ErrorsHandler = Full ? (InternalReadoutErrorsEventHandler)this.AllFieldsErrorsRead : this.MomentaryFieldsErrorsRead;

						this.InitReadout(e.From);
						await this.SendChatMessage(e.From, e.Subject, Row, "Readout started...", Support, false);

						if (!(this.provisioningClient is null))
						{
							this.provisioningClient.CanRead(XmppClient.GetBareJID(e.From),
								FieldTypes, NodeReferences, null, new string[0], new string[0], new string[0],
								async (sender2, e2) =>
								{
									if (e2.Ok && e2.CanRead)
									{
										this.sensorServer.DoInternalReadout(e.From, e2.Nodes, e2.FieldTypes, e2.FieldsNames,
											DateTime.MinValue, DateTime.MaxValue, FieldsHandler, ErrorsHandler,
											new object[] { e.From, true, null, Support, e.Subject, RowNr, NrRows });
									}
									else
										await this.Error(e.From, "Access denied.", Support, e.Subject, Row, Last);
								}, null);
						}
						else
						{
							this.sensorServer.DoInternalReadout(e.From, NodeReferences, FieldTypes, null, DateTime.MinValue, DateTime.MaxValue,
								FieldsHandler, ErrorsHandler, new object[] { e.From, true, null, Support, e.Subject, RowNr, NrRows });
						}
						break;

					case "=":
						StringBuilder Markdown = new StringBuilder();

						Markdown.AppendLine("|Variable|Value|");
						Markdown.AppendLine("|:-------|:---:|");

						foreach (Variable v2 in Variables)
						{
							if (v2.Name.StartsWith(" "))
								continue;

							if (Markdown.Length > 3000)
							{
								await this.SendChatMessage(e.From, e.Subject, Row, Markdown.ToString(), Support, false);
								Markdown.Clear();
							}

							string s2 = v2.ValueElement.ToString();
							if (s2.Length > 100)
								s2 = s2.Substring(0, 100) + "...";

							s2 = MarkdownDocument.Encode(s2).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>");

							Markdown.Append('|');
							Markdown.Append(MarkdownDocument.Encode(v2.Name));
							Markdown.Append('|');
							Markdown.Append(s2);
							Markdown.AppendLine("|");
						}

						if (Markdown.Length > 0)
							await this.SendChatMessage(e.From, e.Subject, Row, Markdown.ToString(), Support, Last);
						break;

					case "/":
						if (this.concentratorServer is null)
							await this.SendChatMessage(e.From, e.Subject, Row, "Device is not a concentrator.", Support, Last);
						else if (SelectedSource is null)
							await this.SendChatMessage(e.From, e.Subject, Row, "No source selected.", Support, Last);
						else
						{
							IEnumerable<INode> Nodes = SelectedSource.RootNodes;
							bool First = true;

							SelectedNode = null;
							if (!(Nodes is null))
							{
								foreach (INode Node in Nodes)
								{
									if (First)
									{
										SelectedNode = Node;
										First = false;
									}
									else
									{
										SelectedNode = null;
										break;
									}
								}
							}

							await this.SelectNode(e, SelectedSource, SelectedNode, Variables, Menu, Support, Row, Last);
						}
						break;

					case "//":
						if (this.concentratorServer is null)
							await this.SendChatMessage(e.From, e.Subject, Row, "Device is not a concentrator.", Support, Last);
						else
						{
							IEnumerable<IDataSource> Sources = this.concentratorServer.RootDataSources;
							bool First = true;

							SelectedSource = null;
							foreach (IDataSource Source in Sources)
							{
								if (First)
								{
									SelectedSource = Source;
									First = false;
								}
								else
								{
									SelectedSource = null;
									break;
								}
							}

							await this.SelectSource(e, SelectedSource, Variables, Menu, Support, Row, Last);
						}
						break;

					case ":=":
						ControlParameter[] Parameters;

						if (this.concentratorServer is null)
						{
							if (this.controlServer is null)
							{
								await this.Error(e.From, "Device is not an actuator", Support, e.Subject, Row, Last);
								break;
							}
							else
							{
								Parameters = await this.controlServer.GetControlParameters(ThingReference.Empty);
								SelectedNode = null;
							}
						}
						else
						{
							if (SelectedNode is null)
							{
								await this.Error(e.From, "No node selected.", Support, e.Subject, Row, Last);
								break;
							}
							else if (SelectedNode is IActuator Actuator)
								Parameters = await Actuator.GetControlParameters();
							else
							{
								await this.Error(e.From, "Selected node is not an actuator", Support, e.Subject, Row, Last);
								break;
							}
						}

						await this.ControlParametersMenu(Parameters, SelectedNode, SelectedSource, Menu, Variables, Support, e, Row, Last);
						break;

					default:
						if (int.TryParse(s, out i) && i >= 0 && !(Menu is null) && Menu.TryGetValue(i, out KeyValuePair<string, object> Item))
						{
							if (string.IsNullOrEmpty(Item.Key))
							{
								if (i == 0 && Item.Value is SortedDictionary<int, KeyValuePair<string, object>> PreviousMenu)
								{
									Menu = PreviousMenu;
									if (Menu is null)
										await this.Error(e.From, "There's no previous menu.", Support, e.Subject, Row, Last);
									else
										await this.SendMenu(e.From, Menu, Variables, Support, e.Subject, Row, Last);
								}
							}
							else
							{
								s = Item.Key;

								if (Menu.TryGetValue(-2, out KeyValuePair<string, object> Obj) && Obj.Value is INode Node &&
									Menu.TryGetValue(-1, out Obj) && Obj.Value is IDataSource Source)
								{
									if (Node.HasChildren)
									{
										foreach (INode ChildNode in await Node.ChildNodes)
										{
											if (ChildNode.NodeId == s)
											{
												await this.SelectNode(e, Source, ChildNode, Variables, Menu, Support, Row, Last);
												break;
											}
										}
									}
								}
								else if (Menu.TryGetValue(-1, out Obj))
								{
									if (Obj.Value is null)
									{
										foreach (IDataSource RootSource in this.concentratorServer.DataSources)
										{
											if (RootSource.SourceID == s)
											{
												await this.SelectSource(e, RootSource, Variables, Menu, Support, Row, Last);
												break;
											}
										}
									}
									else if (!((Source = Obj.Value as IDataSource) is null))
									{
										if (Source.HasChildren)
										{
											foreach (IDataSource ChildSource in Source.ChildSources)
											{
												if (ChildSource.SourceID == s)
												{
													await this.SelectSource(e, ChildSource, Variables, Menu, Support, Row, Last);
													s = null;
													break;
												}
											}
										}

										if (!string.IsNullOrEmpty(s))
										{
											if (!(Source.RootNodes is null))
											{
												foreach (INode RootNode in Source.RootNodes)
												{
													if (RootNode.NodeId == s)
													{
														await this.SelectNode(e, Source, RootNode, Variables, Menu, Support, Row, Last);
														break;
													}
												}
											}
										}
									}
								}
							}
						}
						else if (s.EndsWith("?"))
						{
							Full = s.EndsWith("??");
							this.InitReadout(e.From);
							string Field = s.Substring(0, s.Length - (Full ? 2 : 1)).Trim();

							FieldTypes = Full ? FieldType.All : FieldType.AllExceptHistorical;
							FieldsHandler = Full ? (InternalReadoutFieldsEventHandler)this.AllFieldsRead : this.MomentaryFieldsRead;
							ErrorsHandler = Full ? (InternalReadoutErrorsEventHandler)this.AllFieldsErrorsRead : this.MomentaryFieldsErrorsRead;

							if (!(this.concentratorServer is null))
							{
								if (SelectedSource is null)
								{
									await this.Error(e.From, "No source selected.", Support, e.Subject, Row, Last);
									break;
								}

								INode Node;

								i = Field.IndexOf('.');
								if (i >= 0)
								{
									Node = this.CheckMenuValue(Menu, Field.Substring(0, i)) as INode;
									Field = this.CheckMenuKey(Menu, Field.Substring(i + 1));

									if (Node is null)
									{
										await this.Error(e.From, "Node not found.", Support, e.Subject, Row, Last);
										break;
									}
								}
								else if (!((Node = this.CheckMenuValue(Menu, Field) as INode) is null))
									Field = string.Empty;
								else
								{
									Node = SelectedNode;

									if (Node is null || !Node.IsReadable)
									{
										if (!(SelectedSource is null) && await SelectedSource.GetNodeAsync(new ThingReference(Field, SelectedSource.SourceID, string.Empty)) is INode Node2)
										{
											Node = Node2;
											Field = string.Empty;
										}
										else
										{
											await this.Error(e.From, "No node selected.", Support, e.Subject, Row, Last);
											break;
										}
									}
								}

								ThingRef = Node;

								await this.SendChatMessage(e.From, e.Subject, Row, "Readout of " + Field + " on " + Node.NodeId + " started...", Support, false);

								NodeReferences = new IThingReference[] { ThingRef ?? ThingReference.Empty };

								if (string.IsNullOrEmpty(Field))
								{
									if (!(this.provisioningClient is null))
									{
										this.provisioningClient.CanRead(XmppClient.GetBareJID(e.From),
											FieldTypes, NodeReferences, null, new string[0], new string[0], new string[0],
											async (sender2, e2) =>
											{
												if (e2.Ok && e2.CanRead)
												{
													this.sensorServer.DoInternalReadout(e.From, e2.Nodes, e2.FieldTypes, e2.FieldsNames,
														DateTime.MinValue, DateTime.MaxValue, FieldsHandler, ErrorsHandler,
														new object[] { e.From, true, Field, Support, e.Subject, RowNr, NrRows });
												}
												else
													await this.Error(e.From, "Access denied.", Support, e.Subject, Row, Last);
											}, null);
									}
									else
									{
										this.sensorServer.DoInternalReadout(e.From, NodeReferences,
											FieldTypes, null, DateTime.MinValue, DateTime.MaxValue, FieldsHandler, ErrorsHandler,
											new object[] { e.From, true, Field, Support, e.Subject, RowNr, NrRows });
									}
								}
								else
								{
									if (!(this.provisioningClient is null))
									{
										this.provisioningClient.CanRead(XmppClient.GetBareJID(e.From),
											FieldTypes, NodeReferences, new string[] { Field }, new string[0], new string[0], new string[0],
											async (sender2, e2) =>
											{
												if (e2.Ok && e2.CanRead)
												{
													this.sensorServer.DoInternalReadout(e.From, e2.Nodes, e2.FieldTypes, e2.FieldsNames,
														DateTime.MinValue, DateTime.MaxValue, FieldsHandler, ErrorsHandler,
														new object[] { e.From, true, Field, Support, e.Subject, RowNr, NrRows });
												}
												else
													await this.Error(e.From, "Access denied.", Support, e.Subject, Row, Last);
											}, null);
									}
									else
									{
										this.sensorServer.DoInternalReadout(e.From, NodeReferences,
											FieldTypes, new string[] { Field }, DateTime.MinValue, DateTime.MaxValue, FieldsHandler, ErrorsHandler,
											new object[] { e.From, true, Field, Support, e.Subject, RowNr, NrRows });
									}
								}
							}
							else
							{
								await this.SendChatMessage(e.From, e.Subject, Row, "Readout of " + Field + " started...", Support, false);

								if (!(this.provisioningClient is null))
								{
									this.provisioningClient.CanRead(XmppClient.GetBareJID(e.From),
										FieldTypes, null, null, new string[0], new string[0], new string[0],
										async (sender2, e2) =>
										{
											if (e2.Ok && e2.CanRead)
											{
												this.sensorServer.DoInternalReadout(e.From, e2.Nodes, e2.FieldTypes, e2.FieldsNames,
													DateTime.MinValue, DateTime.MaxValue, FieldsHandler, ErrorsHandler,
													new object[] { e.From, true, Field, Support, e.Subject, RowNr, NrRows });
											}
											else
												await this.Error(e.From, "Access denied.", Support, e.Subject, Row, Last);
										}, null);
								}
								else
								{
									this.sensorServer.DoInternalReadout(e.From, null, FieldTypes, null, DateTime.MinValue, DateTime.MaxValue,
										FieldsHandler, ErrorsHandler, new object[] { e.From, true, Field, Support, e.Subject, RowNr, NrRows });
								}
							}
						}
						else if (!(SelectedSource is null) && await SelectedSource.GetNodeAsync(new ThingReference(s, SelectedSource.SourceID, string.Empty)) is INode Node)
							await this.SelectNode(e, SelectedSource, Node, Variables, Menu, Support, Row, Last);
						else
						{
							if (!(this.controlServer is null) && (i = s.IndexOf(":=")) > 0)
							{
								string ParameterName = this.CheckMenuKey(Menu, s.Substring(0, i).Trim());
								string ValueStr = s.Substring(i + 2).Trim();

								if (!(this.concentratorServer is null))
								{
									if (SelectedSource is null)
									{
										await this.Error(e.From, "No source selected.", Support, e.Subject, Row, Last);
										break;
									}

									i = ParameterName.IndexOf('.');
									if (i >= 0)
									{
										s = ParameterName.Substring(0, i);
										ParameterName = this.CheckMenuKey(Menu, ParameterName.Substring(i + 1));

										Node = this.CheckMenuValue(Menu, s) as INode;
										if (Node is null && !(SelectedSource is null) && await SelectedSource.GetNodeAsync(new ThingReference(s, SelectedSource.SourceID, string.Empty)) is INode Node3)
											Node = Node3;
									}
									else
									{
										if (string.IsNullOrEmpty(ValueStr))
										{
											Node = this.CheckMenuValue(Menu, ParameterName) as INode;

											if (!(Node is null))
												ParameterName = string.Empty;
											else
												Node = SelectedNode;
										}
										else
											Node = SelectedNode;
									}

									if (Node is null && !(Menu is null) && Menu.TryGetValue(-2, out KeyValuePair<string, object> P) && P.Value is INode Node2)
										Node = Node2;

									ThingRef = Node;
								}
								else
								{
									i = ParameterName.IndexOf('.');
									if (i < 0)
										ThingRef = ThingReference.Empty;
									else
									{
										ThingRef = this.CheckMenuValue(Menu, ParameterName.Substring(0, i)) as IThingReference;
										ParameterName = this.CheckMenuKey(Menu, ParameterName.Substring(i + 1).TrimStart());
									}
								}

								if (!(ThingRef is null))
								{
									try
									{
										if (string.IsNullOrEmpty(ValueStr))
										{
											if (!(this.concentratorServer is null) && !(SelectedSource is null) && !((Node = await SelectedSource.GetNodeAsync(ThingRef)) is null))
											{
												if (Node is IActuator Actuator)
													Parameters = await Actuator.GetControlParameters();
												else
													throw new Exception("Node is not an actuator.");
											}
											else if (!(SelectedNode is null))
											{
												if (SelectedNode is IActuator Actuator)
												{
													Node = SelectedNode;
													Parameters = await Actuator.GetControlParameters();
												}
												else
													throw new Exception("Selected node is not an actuator.");
											}
											else if (!(this.concentratorServer is null))
												throw new Exception("No node selected.");
											else if (!(this.controlServer is null))
											{
												Node = null;
												Parameters = await this.controlServer.GetControlParameters(ThingRef);
											}
											else
												throw new Exception("Device not an actuator.");

											List<ControlParameter> Parameters2 = new List<ControlParameter>();

											foreach (ControlParameter P in Parameters)
											{
												if (P.Name.StartsWith(ParameterName))
													Parameters2.Add(P);
											}

											if (Parameters2.Count == 0)
												throw new Exception("No control parameter found starting with that name.");

											await this.ControlParametersMenu(Parameters2.ToArray(), SelectedNode, SelectedSource, Menu, Variables, Support, e, Row, Last);
											break;
										}
										else
										{
											Parameters = await this.controlServer.GetControlParameters(ThingRef);

											ControlParameter P0 = null;

											foreach (ControlParameter P in Parameters)
											{
												if (string.Compare(P.Name, ParameterName, true) == 0)
												{
													P0 = P;
													break;
												}
											}

											if (P0 is null)
												await this.Execute(s, e.From, Support, e.Subject, Row, Last, true);
											else if (!(this.provisioningClient is null))
											{
												this.provisioningClient.CanControl(e.FromBareJID, new IThingReference[] { ThingRef ?? ThingReference.Empty },
													new string[] { ParameterName }, new string[0], new string[0], new string[0], async (sender2, e2) =>
													{
														if (e2.Ok && e2.CanControl)
														{
															if (await P0.SetStringValue(ThingRef, ValueStr))
																await this.SendChatMessage(e.From, e.Subject, Row, "Control parameter set.", Support, Last);
															else
																await this.Error(e.From, "Unable to set control parameter value.", Support, e.Subject, Row, Last);
														}
														else
															await this.Error(e.From, "Access denied.", Support, e.Subject, Row, Last);
													}, null);
											}
											else
											{
												if (!await P0.SetStringValue(ThingRef, ValueStr))
													throw new Exception("Unable to set control parameter value.");

												await this.SendChatMessage(e.From, e.Subject, Row, "Control parameter set.", Support, Last);
											}
											return;
										}
									}
									catch (Exception ex)
									{
										await this.Error(e.From, ex.Message, Support, e.Subject, Row, Last);
										break;
									}
								}
							}

							await this.Execute(s, e.From, Support, e.Subject, Row, Last, true);
						}
						break;
				}
			}
		}

		private async Task ControlParametersMenu(ControlParameter[] Parameters, INode SelectedNode, IDataSource SelectedSource,
			SortedDictionary<int, KeyValuePair<string, object>> PrevMenu, Variables Variables, RemoteXmppSupport Support,
			MessageEventArgs e, string OrgCommand, bool Last)
		{
			SortedDictionary<int, KeyValuePair<string, object>> Menu = new SortedDictionary<int, KeyValuePair<string, object>>()
			{
				{ -3, new KeyValuePair<string, object>(null, Parameters) },
				{ -2, new KeyValuePair<string, object>(null, SelectedNode) },
				{ -1, new KeyValuePair<string, object>(null, SelectedSource) },
				{ 0, new KeyValuePair<string, object>(null, PrevMenu) }
			};

			int i = 0;

			if (!(this.provisioningClient is null))
			{
				int j, c = Parameters.Length;
				string[] ParameterNames = new string[c];

				for (j = 0; j < c; j++)
					ParameterNames[j] = Parameters[j].Name;

				this.provisioningClient.CanControl(e.FromBareJID, null, ParameterNames,
					new string[0], new string[0], new string[0], async (sender2, e2) =>
					{
						if (e2.Ok && e2.CanControl)
						{
							List<ControlParameter> Parameters2 = new List<ControlParameter>();

							foreach (ControlParameter P in Parameters)
							{
								if (e2.ParameterNames is null || Array.IndexOf<string>(e2.ParameterNames, P.Name) >= 0)
								{
									Parameters2.Add(P);
									Menu[++i] = new KeyValuePair<string, object>(P.Name, P.Name + " (" + 
										(await P.GetStringValue((IThingReference)SelectedNode ?? ThingReference.Empty)) + ")");
								}
							}

							Menu[-3] = new KeyValuePair<string, object>(null, Parameters2.ToArray());

							await this.SendMenu(e.From, Menu, Variables, Support, e.Subject, OrgCommand, Last);
						}
						else
							await this.Error(e.From, "Access denied.", Support, e.Subject, OrgCommand, Last);
					}, null);
			}
			else
			{
				foreach (ControlParameter P in Parameters)
				{
					Menu[++i] = new KeyValuePair<string, object>(P.Name, P.Name + " (" +
						await P.GetStringValue((IThingReference)SelectedNode ?? ThingReference.Empty) + ")");
				}

				await this.SendMenu(e.From, Menu, Variables, Support, e.Subject, OrgCommand, Last);
			}
		}

		private string CheckMenuKey(SortedDictionary<int, KeyValuePair<string, object>> Menu, string s)
		{
			if (!(Menu is null) &&
				int.TryParse(s, out int i) &&
				i > 0 &&
				Menu.TryGetValue(i, out KeyValuePair<string, object> Obj) &&
				!string.IsNullOrEmpty(Obj.Key))
			{
				return Obj.Key;
			}
			else
				return s;
		}

		private object CheckMenuValue(SortedDictionary<int, KeyValuePair<string, object>> Menu, string s)
		{
			if (!(Menu is null) &&
				int.TryParse(s, out int i) &&
				i > 0 &&
				Menu.TryGetValue(i, out KeyValuePair<string, object> Obj) &&
				!string.IsNullOrEmpty(Obj.Key))
			{
				return Obj.Value;
			}
			else
				return null;
		}

		private async Task SelectNode(MessageEventArgs e, IDataSource SelectedSource, INode SelectedNode, Variables Variables,
			SortedDictionary<int, KeyValuePair<string, object>> Menu, RemoteXmppSupport Support, string OrgCommand, bool Last)
		{
			Variables[" Node "] = SelectedNode;

			Menu = new SortedDictionary<int, KeyValuePair<string, object>>()
			{
				{ -2, new KeyValuePair<string, object>(null, SelectedNode) },
				{ -1, new KeyValuePair<string, object>(null, SelectedSource) },
				{ 0, new KeyValuePair<string, object>(null, Menu) }
			};
			int i = 0;

			if (SelectedNode is null)
			{
				Menu[-1] = new KeyValuePair<string, object>(null, SelectedSource);

				await this.SendChatMessage(e.From, e.Subject, OrgCommand, "Root nodes of data source " + SelectedSource.SourceID + ":", Support, false);

				if (!(SelectedSource.RootNodes is null))
				{
					foreach (INode Node in SelectedSource.RootNodes)
						Menu[++i] = new KeyValuePair<string, object>(Node.NodeId, Node);
				}
			}
			else
			{
				await this.SendChatMessage(e.From, e.Subject, OrgCommand, "Selecting node " + SelectedNode.NodeId + " of " + SelectedSource.SourceID + ".", Support, false);

				if (SelectedNode.HasChildren)
				{
					await this.SendChatMessage(e.From, e.Subject, OrgCommand, "Child nodes:", Support, false);

					foreach (INode Node in await SelectedNode.ChildNodes)
						Menu[++i] = new KeyValuePair<string, object>(Node.NodeId, Node);
				}
			}

			if (i > 0)
				await this.SendMenu(e.From, Menu, Variables, Support, e.Subject, OrgCommand, Last);
		}

		private async Task SelectSource(MessageEventArgs e, IDataSource SelectedSource, Variables Variables, SortedDictionary<int, KeyValuePair<string, object>> Menu,
			RemoteXmppSupport Support, string OrgCommand, bool Last)
		{
			Variables[" Source "] = SelectedSource;
			Menu = new SortedDictionary<int, KeyValuePair<string, object>>()
			{
				{ -1, new KeyValuePair<string, object>(null, SelectedSource) },
				{ 0, new KeyValuePair<string, object>(null, Menu) }
			};
			int i = 0;

			if (SelectedSource is null)
			{
				await this.SendChatMessage(e.From, e.Subject, OrgCommand, "Root data sources:", Support, Last);

				foreach (IDataSource Source in this.concentratorServer.RootDataSources)
					Menu[++i] = new KeyValuePair<string, object>(Source.SourceID, Source);
			}
			else
			{
				await this.SendChatMessage(e.From, e.Subject, OrgCommand, "Selecting data source " + SelectedSource.SourceID + ":", Support, Last);

				if (SelectedSource.HasChildren)
				{
					foreach (IDataSource Source in SelectedSource.ChildSources)
						Menu[++i] = new KeyValuePair<string, object>(Source.SourceID, Source);
				}

				if (!(SelectedSource.RootNodes is null))
				{
					foreach (INode Node in SelectedSource.RootNodes)
						Menu[++i] = new KeyValuePair<string, object>(Node.NodeId, Node);
				}
			}

			if (i > 0)
				await this.SendMenu(e.From, Menu, Variables, Support, e.Subject, OrgCommand, Last);
		}

		private Task SendMenu(string To, SortedDictionary<int, KeyValuePair<string, object>> Menu, Variables Variables,
			RemoteXmppSupport Support, string OrgSubject, string OrgCommand, bool Last)
		{
			StringBuilder sb = new StringBuilder();
			bool HasBack = false;

			Variables[" Menu "] = Menu;

			foreach (KeyValuePair<int, KeyValuePair<string, object>> P in Menu)
			{
				if (P.Key < 0)
					continue;
				else if (P.Key == 0)
					HasBack = !(P.Value.Value is null);
				else
				{
					sb.Append(P.Key);
					sb.Append(". ");

					if (!(P.Value.Value is null))
						sb.AppendLine(MarkdownDocument.Encode(P.Value.Value.ToString()));
				}
			}

			if (HasBack)
				sb.AppendLine("0. Back");

			return this.SendChatMessage(To, OrgSubject, OrgCommand, sb.ToString(), Support, Last);
		}

		private class RemoteXmppSupport
		{
			public bool Markdown = false;
			public bool Html = false;
			public bool ByteStreams = false;
			public bool FileTransfer = false;
			public bool SessionInitiation = false;
			public bool InBandBytestreams = false;
			public bool BitsOfBinary = false;
			public bool Mail = false;
		}

		private async Task Execute(string s, string From, RemoteXmppSupport Support, string OrgSubject, string OrgCommand, bool Last, bool CheckAuthorization)
		{
			Expression Exp;

			try
			{
				Exp = new Expression(s);
			}
			catch (Exception)
			{
				await this.Que(From, Support, OrgSubject, OrgCommand, Last);
				return;
			}

			Variables Variables = this.GetVariables(From);
			TextWriter Bak = Variables.ConsoleOut;
			StringBuilder sb = new StringBuilder();

			await Variables.LockAsync();
			Variables.ConsoleOut = new StringWriter(sb);
			try
			{
				if (CheckAuthorization && 
					(!Variables.TryGetVariable(" User ", out Variable v) ||
					!(v.ValueObject is IUser User) ||
					!Exp.ForAll(this.IsAuthorized, User, SearchMethod.TreeOrder)))
				{
					throw new Exception("Unauthorized to execute expression.");
				}

				IElement Result;

				try
				{
					Result = await Exp.Root.EvaluateAsync(Variables);
				}
				catch (ScriptReturnValueException ex)
				{
					Result = ex.ReturnValue;
				}
				catch (Exception ex)
				{
					Result = new ObjectValue(ex);
				}

				Variables["Ans"] = Result;

				if (Result is Graph G)
				{
					PixelInformation Pixels = G.CreatePixels(G.Settings);
					await this.ImageResult(From, Pixels, Support, Variables, true, OrgSubject, OrgCommand, Last);
					return;
				}
				else if (Result.AssociatedObjectValue is SKImage Img)
				{
					await this.ImageResult(From, PixelInformation.FromImage(Img), Support, Variables, true, OrgSubject, OrgCommand, Last);
					return;
				}
				else if (Result.AssociatedObjectValue is Exception ex)
				{
					ex = Log.UnnestException(ex);

					if (ex is AggregateException ex2)
					{
						foreach (Exception ex3 in ex2.InnerExceptions)
							await this.Error(From, ex3.Message, Support, OrgSubject, OrgCommand, false);
					}
					else
						await this.Error(From, ex.Message, Support, OrgSubject, OrgCommand, false);
				}
				else if (Result.AssociatedObjectValue is ObjectMatrix M && !(M.ColumnNames is null))
				{
					StringBuilder Markdown = new StringBuilder();

					foreach (string s2 in M.ColumnNames)
					{
						Markdown.Append("| ");
						Markdown.Append(MarkdownDocument.Encode(s2));
					}

					Markdown.AppendLine(" |");

					foreach (string s2 in M.ColumnNames)
						Markdown.Append("|---");

					Markdown.AppendLine("|");

					int x, y;

					for (y = 0; y < M.Rows; y++)
					{
						for (x = 0; x < M.Columns; x++)
						{
							Markdown.Append("| ");

							object Item = M.GetElement(x, y).AssociatedObjectValue;
							if (!(Item is null))
							{
								if (!(Item is string s2))
									s2 = Expression.ToString(Item);

								s2 = s2.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>");
								Markdown.Append(MarkdownDocument.Encode(s2));
							}
						}

						Markdown.AppendLine(" |");
					}

					await this.SendChatMessage(From, OrgSubject, OrgCommand, Markdown.ToString(), Support, Last);
				}
				else
				{
					s = Result.ToString();
					await this.SendChatMessage(From, OrgSubject, OrgCommand, MarkdownDocument.Encode(s), Support, Last);
				}
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);

				await this.Error(From, ex.Message, Support, OrgSubject, OrgCommand, false);
				await this.Que(From, Support, OrgSubject, string.Empty, Last);
			}
			finally
			{
				Variables.ConsoleOut.Flush();
				Variables.ConsoleOut = Bak;
				Variables.Release();
			}
		}

		private bool IsAuthorized(ScriptNode Node, out ScriptNode NewNode, object State)
		{
			NewNode = null;

			if (State is IUser User)
				return User.HasPrivilege("Script." + Node.GetType().FullName);
			else
				return false;
		}

		private Task ImageResult(string To, PixelInformation Pixels, RemoteXmppSupport Support, Variables Variables, bool AllowHttpUpload,
			string OrgSubject, string OrgCommand, bool Last)
		{
			byte[] Bin = Pixels.EncodeAsPng();
			return this.ImageResult(To, Bin, Support, Variables, AllowHttpUpload, OrgSubject, OrgCommand, Last);
		}

		private async Task ImageResult(string To, byte[] Bin, RemoteXmppSupport Support, Variables Variables, bool AllowHttpUpload,
			string OrgSubject, string OrgCommand, bool Last)
		{
			string s;

			if (Support.Mail)
			{
				string Cid = Guid.NewGuid().ToString();
				s = "![Image result](cid:" + Cid + ")";
				await this.SendChatMessage(To, OrgSubject, OrgCommand, s, Support, Last, new Tuple<string, string, byte[]>(Cid, ImageCodec.ContentTypePng, Bin));
			}
			else if (Support.Markdown)
			{
				s = Convert.ToBase64String(Bin, 0, Bin.Length);
				s = "![Image result](data:image/png;base64," + s + ")";
				await this.SendChatMessage(To, OrgSubject, OrgCommand, s, Support, Last);
			}
			else if (AllowHttpUpload && !(this.httpUpload is null) && this.httpUpload.HasSupport)
			{
				string FileName = Guid.NewGuid().ToString().Replace("-", string.Empty) + "." + ImageCodec.FileExtensionPng;
				string ContentType = ImageCodec.ContentTypePng;

				this.httpUpload.RequestUploadSlot(FileName, ContentType, Bin.Length, async (sender, e) =>
				{
					try
					{
						if (e.Ok)
						{
							using (HttpClient HttpClient = new HttpClient())
							{
								HttpClient.Timeout = TimeSpan.FromMilliseconds(30000);
								HttpClient.DefaultRequestHeaders.ExpectContinue = false;

								HttpContent Body = new ByteArrayContent(Bin);

								if (!(e.PutHeaders is null))
								{
									foreach (KeyValuePair<string, string> P in e.PutHeaders)
										Body.Headers.Add(P.Key, P.Value);

									Body.Headers.Add("Content-Type", ContentType);
								}

								HttpResponseMessage Response = await HttpClient.PutAsync(e.PutUrl, Body);
								if (!Response.IsSuccessStatusCode)
									await this.ImageResult(To, Bin, Support, Variables, false, OrgSubject, OrgCommand, Last);
								else
									await this.SendChatMessage(To, OrgSubject, OrgCommand, "![Image result](" + e.GetUrl + ")", Support, Last);
							}
						}
						else
							await this.ImageResult(To, Bin, Support, Variables, false, OrgSubject, OrgCommand, Last);
					}
					catch (Exception)
					{
						await this.ImageResult(To, Bin, Support, Variables, false, OrgSubject, OrgCommand, Last);
					}

				}, null);

				return;
			}
			else if (Support.Html && !(this.bobClient is null) && Support.BitsOfBinary)
			{
				s = await this.bobClient.StoreData(Bin, ImageCodec.ContentTypePng);

				if (!Variables.TryGetVariable(" ContentIDs ", out Variable v) ||
					!(v.ValueObject is Dictionary<string, bool> ContentIDs))
				{
					ContentIDs = new Dictionary<string, bool>();
					Variables[" ContentIDs "] = ContentIDs;
				}

				lock (ContentIDs)
				{
					ContentIDs[s] = true;
				}

				await this.SendChatMessage(To, OrgSubject, OrgCommand, "[Image result](cid:" + s + ")", Support, Last);
			}
			else
			{
				s = Convert.ToBase64String(Bin, 0, Bin.Length);
				s = "![Image result](data:image/png;base64," + s + ")";
				await this.SendChatMessage(To, OrgSubject, OrgCommand, s, Support, Last);
			}
		}

		private void InitReadout(string Address)
		{
			Variables Variables = this.GetVariables(Address);
			Dictionary<string, SortedDictionary<DateTime, Field>> Fields = new Dictionary<string, SortedDictionary<DateTime, Field>>();
			Variables[" Readout "] = Fields;
		}

		private Variables GetVariables(string Address)
		{
			if (!this.sessions.TryGetValue(Address, out Variables Variables))
			{
				User User = new User(Address);

				User.SetPrivilege(typeof(Expression).Namespace, true);
				User.SetPrivilege(typeof(Expression).Namespace + ".Persistence", false);

				Variables = new Variables(new Variable(" User ", User));
				this.sessions.Add(Address, Variables);
			}

			return Variables;
		}

		private void Sessions_Removed(object Sender, CacheItemEventArgs<string, Variables> e)
		{
			if (!(this.bobClient is null) &&
				e.Value.TryGetVariable(" ContentIDs ", out Variable v) &&
				v.ValueObject is Dictionary<string, bool> ContentIDs)
			{
				foreach (string ContentID in ContentIDs.Keys)
					this.bobClient.DeleteData(ContentID);
			}
		}

		private KeyValuePair<string, string>[] UpdateReadoutVariables(string Address, InternalReadoutFieldsEventArgs e, string _, string Field)
		{
			Variables Variables = this.GetVariables(Address);
			List<KeyValuePair<string, string>> Exp = null;

			if (Variables.TryGetVariable(" Readout ", out Variable v) &&
				v.ValueObject is Dictionary<string, SortedDictionary<DateTime, Field>> Fields)
			{
				foreach (Field F in e.Fields)
				{
					if (!string.IsNullOrEmpty(Field) && !F.Name.StartsWith(Field))
						continue;

					if (!Fields.TryGetValue(F.Name, out SortedDictionary<DateTime, Field> Times))
					{
						Times = new SortedDictionary<DateTime, Field>();
						Fields[F.Name] = Times;
					}

					Times[F.Timestamp] = F;
				}

				if (e.Done)
				{
					Variables.Remove(" Readout ");

					SortedDictionary<string, SeriesTypes> Series = null;
					string s;

					foreach (KeyValuePair<string, SortedDictionary<DateTime, Field>> P in Fields)
					{
						if (P.Value.Count == 1)
						{
							foreach (Field F in P.Value.Values)
								Variables[this.PascalCasing(P.Key)] = this.FieldElement(F);
						}
						else
						{
							List<ObjectVector> Values = new List<ObjectVector>();
							IElement E;
							bool Numeric = true;
							SeriesTypes Types;
							SeriesTypes Type;

							foreach (KeyValuePair<DateTime, Field> P2 in P.Value)
							{
								E = this.FieldElement(P2.Value);
								Values.Add(new ObjectVector(new DateTimeValue(P2.Key),
									Expression.Encapsulate(E),
									new ObjectValue(P2.Value.Type), new ObjectValue(P2.Value.QoS)));

								if (!(E is DoubleNumber) && !(E is PhysicalQuantity))
									Numeric = false;
							}

							if (Numeric)
							{
								string Suffix;

								s = P.Key;

								if (EndsWith(ref s, "Average") || EndsWith(ref s, "Avg"))
								{
									Type = SeriesTypes.Average;
									Suffix = "Average";
								}
								else if (EndsWith(ref s, "Minimum") || EndsWith(ref s, "Min"))
								{
									Type = SeriesTypes.Minimum;
									Suffix = "Minimum";
								}
								else if (EndsWith(ref s, "Maximum") || EndsWith(ref s, "Max"))
								{
									Type = SeriesTypes.Maximum;
									Suffix = "Maximum";
								}
								else
								{
									Type = SeriesTypes.Normal;
									Suffix = string.Empty;
								}

								Variables[this.PascalCasing(s) + Suffix] = VectorDefinition.Encapsulate(Values.ToArray(), true, null);

								if (Series is null)
								{
									Series = new SortedDictionary<string, SeriesTypes>();
									Types = Type;
								}
								else if (Series.TryGetValue(s, out Types))
									Types |= Type;
								else
									Types = Type;

								Series[s] = Types;
							}
						}
					}

					if (!(Series is null))
					{
						StringBuilder Expression = new StringBuilder();
						string VariableName;

						foreach (KeyValuePair<string, SeriesTypes> P in Series)
						{
							VariableName = this.PascalCasing(P.Key);

							if ((P.Value & SeriesTypes.Minimum) != 0)
							{
								Expression.Append("MinTP:=");
								Expression.Append(VariableName);
								Expression.Append("Minimum[0,];");
								Expression.Append("Min:=");
								Expression.Append(VariableName);
								Expression.Append("Minimum[1,];");
							}

							if ((P.Value & SeriesTypes.Maximum) != 0)
							{
								Expression.Append("MaxTP:=");
								Expression.Append(VariableName);
								Expression.Append("Maximum[0,];");
								Expression.Append("Max:=");
								Expression.Append(VariableName);
								Expression.Append("Maximum[1,];");
							}

							if ((P.Value & SeriesTypes.Average) != 0)
							{
								Expression.Append("AvgTP:=");
								Expression.Append(VariableName);
								Expression.Append("Average[0,];");
								Expression.Append("Avg:=");
								Expression.Append(VariableName);
								Expression.Append("Average[1,];");
							}

							if ((P.Value & SeriesTypes.Normal) != 0)
							{
								Expression.Append("TP:=");
								Expression.Append(VariableName);
								Expression.Append("[0,];");
								Expression.Append("V:=");
								Expression.Append(VariableName);
								Expression.Append("[1,];");
							}

							bool First = true;

							if ((P.Value & SeriesTypes.MinMax) == SeriesTypes.MinMax)
							{
								First = false;
								Expression.Append("MinMaxTP:=join(MinTP,reverse(MaxTP));");
								Expression.Append("MinMax:=join(Min,reverse(Max));");
								Expression.Append("polygon2d(MinMaxTP, MinMax, rgba(0, 0, 255, 32))");
							}
							else if ((P.Value & SeriesTypes.Minimum) != 0)
							{
								First = false;
								Expression.Append("plot2dline(MinTP, Min, \"Blue\")");
							}
							else if ((P.Value & SeriesTypes.Maximum) != 0)
							{
								First = false;
								Expression.Append("plot2dline(MaxTP, Max, \"Orange\")");
							}

							if ((P.Value & SeriesTypes.Average) != 0)
							{
								if (First)
									First = false;
								else
									Expression.Append('+');

								Expression.Append("plot2dline(AvgTP, Avg, \"Green\")");
							}

							if ((P.Value & SeriesTypes.Normal) != 0)
							{
								if (First)
									First = false;
								else
									Expression.Append('+');

								Expression.Append("plot2dline(TP, V, \"Red\")");
							}

							if (Exp is null)
								Exp = new List<KeyValuePair<string, string>>();

							s = Expression.ToString();
							Exp.Add(new KeyValuePair<string, string>(P.Key, s));
							Expression.Clear();
						}
					}
				}
			}

			return Exp?.ToArray();
		}

		[Flags]
		private enum SeriesTypes
		{
			Normal = 1,
			Minimum = 2,
			Maximum = 4,
			MinMax = 6,
			Average = 8
		}

		private static bool EndsWith(ref string s, string Suffix)
		{
			if (!s.EndsWith(Suffix, StringComparison.CurrentCultureIgnoreCase))
				return false;

			s = s.Substring(0, s.Length - Suffix.Length).TrimEnd();

			if (s.EndsWith(","))
				s = s.Substring(0, s.Length - 1).TrimEnd();

			return true;
		}

		private IElement FieldElement(Field Field)
		{
			if (Field is QuantityField Q)
			{
				if (string.IsNullOrEmpty(Q.Unit))
					return new DoubleNumber(Q.Value);

				try
				{
					return Q.Quantity;
				}
				catch (Exception)
				{
					return new StringValue(Q.ValueString);
				}
			}

			if (Field is Int32Field I32)
				return new DoubleNumber(I32.Value);
			else if (Field is Int64Field I64)
				return new DoubleNumber(I64.Value);
			else if (Field is StringField S)
				return new StringValue(S.Value);
			else if (Field is BooleanField B)
				return new BooleanValue(B.Value);
			else if (Field is DateTimeField DT)
				return new DateTimeValue(DT.Value);
			else if (Field is DateField D)
				return new DateTimeValue(D.Value);
			else
				return new ObjectValue(Field.ObjectValue);
		}

		private string PascalCasing(string Name)
		{
			StringBuilder Result = new StringBuilder();
			bool First = true;

			foreach (char ch in Name)
			{
				if (char.IsLetter(ch))
				{
					if (First)
					{
						First = false;
						Result.Append(char.ToUpper(ch));
					}
					else
						Result.Append(char.ToLower(ch));
				}
				else
					First = true;
			}

			return Result.ToString();
		}

		private async Task MomentaryFieldsRead(object _, InternalReadoutFieldsEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P[0];
			string Field = (string)P[2];
			RemoteXmppSupport Support = (RemoteXmppSupport)P[3];
			string OrgSubject = (string)P[4];
			//int RowNr = (int)P[5];
			int NrRows = (int)P[6];
			//bool Last = RowNr == NrRows;
			StringBuilder sb = new StringBuilder();
			QuantityField QF;

			KeyValuePair<string, string>[] Exp = this.UpdateReadoutVariables(From, e, From, Field);

			foreach (Field F in e.Fields)
			{
				if (!string.IsNullOrEmpty(Field) && !F.Name.StartsWith(Field))
					continue;

				if ((Support.Markdown || !Support.Html) && sb.Length > 3000)
				{
					await this.SendChatMessage(From, OrgSubject, string.Empty, sb.ToString(), Support, false);
					sb.Clear();
				}

				this.CheckMomentaryValuesHeader(P, sb, Support);

				QF = F as QuantityField;

				if (!(QF is null))
				{
					sb.Append('|');
					sb.Append(MarkdownDocument.Encode(F.Name));
					sb.Append('|');
					sb.Append(CommonTypes.Encode(QF.Value, QF.NrDecimals));
					sb.Append('|');
					sb.Append(MarkdownDocument.Encode(QF.Unit));
					sb.AppendLine("|");
				}
				else
				{
					sb.Append('|');
					sb.Append(MarkdownDocument.Encode(F.Name));
					sb.Append('|');
					sb.Append(MarkdownDocument.Encode(F.ValueString));
					sb.AppendLine("||");
				}
			}

			await this.Send(From, sb, Support, OrgSubject, false);
			await this.SendExpressionResults(Exp, From, Support, OrgSubject, false, false);

			if (e.Done)
				await this.SendChatMessage(From, OrgSubject, string.Empty, "Readout complete.", Support, NrRows == 1);

			// TODO: Localization
		}

		private async Task Send(string To, StringBuilder sb, RemoteXmppSupport Support, string OrgSubject, bool Last)
		{
			if (sb.Length > 0)
				await this.SendChatMessage(To, OrgSubject, string.Empty, sb.ToString(), Support, Last);
		}

		private async Task SendExpressionResults(KeyValuePair<string, string>[] Exp, string From, RemoteXmppSupport Support,
			string OrgSubject, bool Last, bool CheckAuthorization)
		{
			if (!(Exp is null))
			{
				foreach (KeyValuePair<string, string> Expression in Exp)
				{
					await this.SendChatMessage(From, OrgSubject, string.Empty, "## " + MarkdownDocument.Encode(Expression.Key), Support, false);
					await this.Execute(Expression.Value, From, Support, OrgSubject, string.Empty, Last, CheckAuthorization);
				}
			}
		}

		private void CheckMomentaryValuesHeader(object[] P, StringBuilder sb, RemoteXmppSupport _)
		{
			if ((bool)P[1])
			{
				P[1] = false;

				sb.AppendLine("|Field|Value|Unit|");
				sb.AppendLine("|---|--:|---|");
			}
		}

		private async Task MomentaryFieldsErrorsRead(object _, InternalReadoutErrorsEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P[0];
			//string Field = (string)P[2];
			RemoteXmppSupport Support = (RemoteXmppSupport)P[3];
			string OrgSubject = (string)P[4];
			//int RowNr = (int)P[5];
			int NrRows = (int)P[6];
			//bool Last = RowNr == NrRows;
			StringBuilder sb = new StringBuilder();

			foreach (ThingError Error in e.Errors)
			{
				if ((Support.Markdown || !Support.Html) && sb.Length > 3000)
				{
					await this.SendChatMessage(From, OrgSubject, string.Empty, sb.ToString(), Support, false);
					sb.Clear();
				}

				this.CheckMomentaryValuesHeader(P, sb, Support);

				sb.Append("|");
				sb.Append(MarkdownDocument.Encode(Error.ToString()));
				sb.AppendLine("|||");
			}

			await this.Send(From, sb, Support, OrgSubject, false);

			if (e.Done)
				await this.SendChatMessage(From, OrgSubject, string.Empty, "Readout complete.", Support, NrRows == 1);
		}

		private async Task AllFieldsRead(object _, InternalReadoutFieldsEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P[0];
			string Field = (string)P[2];
			RemoteXmppSupport Support = (RemoteXmppSupport)P[3];
			string OrgSubject = (string)P[4];
			//int RowNr = (int)P[5];
			int NrRows = (int)P[6];
			//bool Last = RowNr == NrRows;
			StringBuilder sb = new StringBuilder();
			QuantityField QF;
			DateTime TP;
			string s;

			KeyValuePair<string, string>[] Exp = this .UpdateReadoutVariables(From, e, From, Field);

			foreach (Field F in e.Fields)
			{
				if ((F.Type & FieldType.Historical) > 0)
					continue;

				if (!string.IsNullOrEmpty(Field) && !F.Name.StartsWith(Field))
					continue;

				if ((Support.Markdown || !Support.Html) && sb.Length > 3000)
				{
					await this.SendChatMessage(From, OrgSubject, string.Empty, sb.ToString(), Support, false);
					sb.Clear();
				}

				this.CheckAllFieldsHeader(P, sb, Support);

				TP = F.Timestamp;

				sb.Append('|');
				sb.Append(s = MarkdownDocument.Encode(F.Name));
				sb.Append('|');
				sb.Append(s);
				sb.Append('|');

				QF = F as QuantityField;

				if (!(QF is null))
				{
					sb.Append(CommonTypes.Encode(QF.Value, QF.NrDecimals));
					sb.Append('|');
					sb.Append(MarkdownDocument.Encode(QF.Unit));
				}
				else
				{
					sb.Append(MarkdownDocument.Encode(F.ValueString));
					sb.Append('|');
				}

				sb.Append('|');
				sb.Append(TP.Year.ToString("D4"));
				sb.Append('-');
				sb.Append(TP.Month.ToString("D2"));
				sb.Append('-');
				sb.Append(TP.Day.ToString("D2"));
				sb.Append(' ');
				sb.Append(TP.Hour.ToString("D2"));
				sb.Append(':');
				sb.Append(TP.Minute.ToString("D2"));
				sb.Append(':');
				sb.Append(TP.Second.ToString("D2"));
				sb.Append('|');
				sb.Append(F.Type.ToString());
				sb.Append('|');
				sb.Append(F.QoS.ToString());
				sb.AppendLine("|");
			}

			await this.Send(From, sb, Support, OrgSubject, false);
			await this.SendExpressionResults(Exp, From, Support, OrgSubject, false, false);

			if (e.Done)
				await this.SendChatMessage(From, OrgSubject, string.Empty, "Readout complete.", Support, NrRows == 1);

			// TODO: Localization
		}

		private void CheckAllFieldsHeader(object[] P, StringBuilder sb, RemoteXmppSupport _)
		{
			if ((bool)P[1])
			{
				P[1] = false;

				sb.AppendLine("|Field|Localized|Value|Unit|Timestamp|Type|QoS|");
				sb.AppendLine("|---|---|--:|---|:-:|:-:|:-:|");
			}
		}

		private async Task AllFieldsErrorsRead(object _, InternalReadoutErrorsEventArgs e)
		{
			object[] P = (object[])e.State;
			string From = (string)P[0];
			//string Field = (string)P[2];
			RemoteXmppSupport Support = (RemoteXmppSupport)P[3];
			string OrgSubject = (string)P[4];
			//int RowNr = (int)P[5];
			int NrRows = (int)P[6];
			//bool Last = RowNr == NrRows;
			StringBuilder sb = new StringBuilder();

			foreach (ThingError Error in e.Errors)
			{
				if ((Support.Markdown || !Support.Html) && sb.Length > 3000)
				{
					await this.SendChatMessage(From, OrgSubject, string.Empty, sb.ToString(), Support, false);
					sb.Clear();
				}

				this.CheckAllFieldsHeader(P, sb, Support);

				sb.Append('|');
				sb.Append(MarkdownDocument.Encode(Error.ToString()));
				sb.AppendLine("|||||||");
			}

			await this.Send(From, sb, Support, OrgSubject, false);

			if (e.Done)
				await this.SendChatMessage(From, OrgSubject, string.Empty, "Readout complete.", Support, NrRows == 1);
		}

		private Task Que(string To, RemoteXmppSupport Support, string OrgSubject, string OrgCommand, bool Last)
		{
			return this.Error(To, "Sorry. Can't understand what you're trying to say. Type # to display the menu.", Support,
				OrgSubject, OrgCommand, Last);
		}

		private Task Error(string To, string ErrorMessage, RemoteXmppSupport Support, string OrgSubject, string OrgCommand, bool Last)
		{
			return this.SendChatMessage(To, OrgSubject, OrgCommand, "**" + MarkdownDocument.Encode(ErrorMessage) + "**", Support, Last);
		}

		private async Task ShowHelp(string To, bool Extended, RemoteXmppSupport Support, string OrgSubject, string OrgCommand, bool Last)
		{
			StringBuilder Output = new StringBuilder();

			Output.AppendLine("|Command | Description|");
			Output.AppendLine("|---|---|");
			Output.AppendLine("|#|Displays the short version of the menu.|");
			Output.AppendLine("|##|Displays the extended version of the menu.|");

			if (!(this.concentratorServer is null))
			{
				Output.AppendLine("|/|Selects the root node in the current data source.|");
				Output.AppendLine("|//|Selects the root data source.|");
				Output.AppendLine("|NODE|Selects the node \"NODE\".|");
				Output.AppendLine("|NR|The number NR corresponds to the item in the menu last shown. The menu number can be used as a replacement for the corresponding string in all constructs.|");
			}

			if (!(this.sensorServer is null))
			{
				if (!(this.concentratorServer is null))
				{
					Output.AppendLine("|?|Reads non-historical values of the currently selected object.|");
					Output.AppendLine("|??|Performs a full readout of the currently selected object.|");
					Output.AppendLine("|FIELD?|Reads the non-historical fields that begin with \"FIELD\", of the currently selected object.|");
					Output.AppendLine("|FIELD??|Reads all values from fields that begin with \"FIELD\", of the currently selected object.|");
					Output.AppendLine("|NODE?|Reads the non-historical fields for the node \"NODE\".|");
					Output.AppendLine("|NODE??|Reads all values from fields for the node \"NODE\".|");
					Output.AppendLine("|NODE.FIELD?|Reads the non-historical fields that begin with \"FIELD\", of the node \"NODE\".|");
					Output.AppendLine("|NODE.FIELD??|Reads all values from fields that begin with \"FIELD\", of the node \"NODE\".|");
				}
				else
				{
					Output.AppendLine("|?|Reads non-historical values.|");
					Output.AppendLine("|??|Performs a full readout.|");
					Output.AppendLine("|FIELD?|Reads the non-historical fields that begin with \"FIELD\".|");
					Output.AppendLine("|FIELD??|Reads all values from fields that begin with \"FIELD\".|");
				}
			}

			if (!(this.controlServer is null))
			{
				Output.AppendLine("|:=|Lists available control parameters of the currently selected object.|");

				if (!(this.concentratorServer is null))
				{
					Output.AppendLine("|NODE:=|Lists available control parameters of the node \"NODE\".|");
					Output.AppendLine("|PARAMETER:=|Displays the current value of the control parameter \"PARAMETER\" of the currently selected object.|");
					Output.AppendLine("|PARAMETER:=VALUE|Sets the control parameter named \"PARAMETER\" of the currently selected object to the value VALUE.|");
					Output.AppendLine("|NODE.PARAMETER:=VALUE|Sets the control parameter named \"PARAMETER\" of the node \"NODE\" to the value VALUE.|");
				}
				else
					Output.AppendLine("|PARAMETER:=VALUE|Sets the control parameter named \"PARAMETER\" to the value VALUE.|");
			}

			Output.AppendLine("|=|Displays available variables in the session.|");
			Output.AppendLine("| |Anything else is assumed to be evaluated as a [mathematical expression](http://waher.se/Script.md)|");

			await this.SendChatMessage(To, OrgSubject, OrgCommand, Output.ToString(), Support, Last && !Extended);

			if (Extended)
			{
				Output.Clear();

				Output.Append("When reading the device, results will be available as pascal cased variables in the current session. You can use ");
				Output.Append("these to perform calculations. If a single field value is available for a specific field name, the corresponding ");
				Output.Append("variable will contain only the field value. If several values are available for a given field name, the corresponding");
				Output.Append("variable will contain a matrix with their corresponding contents. Use column indexing ");

				Output.Append("`Field[Col,]`");

				Output.AppendLine(" to access individual columns.");

				Output.AppendLine();
				Output.Append("Historical values with multiple numerical values will be shown in graph formats. ");
				Output.Append("You can control the graph size using the variables ");

				Output.AppendLine("`GraphWidth` and `GraphHeight`.");

				await this.SendChatMessage(To, OrgSubject, string.Empty, Output.ToString(), Support, Last);
			}
		}

		// TODO: Configuration
		// TODO: Node Commands.
		// TODO: Browsing data sources.
		// TODO: User authentication
		// TODO: Localization
	}
}
