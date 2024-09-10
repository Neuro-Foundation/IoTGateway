﻿using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Waher.Content;
using Waher.Content.Xml;
using System.Threading.Tasks;
#if !WINDOWS_UWP
using Waher.Content.Xsl;
#endif
using Waher.Networking.XMPP;
using Waher.Runtime.Console;

namespace Waher.Mock
{
	/// <summary>
	/// Class containing information about a simple XMPP component connection.
	/// </summary>
	public class SimpleComponentConfiguration : IHostReference
	{
#if !WINDOWS_UWP
		private static readonly XmlSchema schema = XSL.LoadSchema("Waher.Mock.Schema.SimpleComponentConfiguration.xsd");
		private const string expectedRootElement = "SimpleComponentConfiguration";
		private const string expectedNamespace = "http://waher.se/Schema/SimpleComponentConfiguration.xsd";
#endif
		/// <summary>
		/// Default XMPP component port number.
		/// </summary>
		public const int DefaultPort = 5275;

		private string host = string.Empty;
		private string component = string.Empty;
		private string secret = string.Empty;
		private bool sniffer = false;
		private int port = DefaultPort;

		/// <summary>
		/// Class containing information about a simple XMPP component connection.
		/// </summary>
		public SimpleComponentConfiguration()
		{
		}

		/// <summary>
		/// Class containing information about a simple XMPP component connection.
		/// </summary>
		/// <param name="FileName">Name of file containing configuration.</param>
		public SimpleComponentConfiguration(string FileName)
		{
			XmlDocument Xml = new XmlDocument();
			using (StreamReader r = File.OpenText(FileName))
			{
				Xml.Load(r);
			}

#if !WINDOWS_UWP
			XSL.Validate(FileName, Xml, expectedRootElement, expectedNamespace, schema);
#endif

			this.Init(Xml.DocumentElement);
		}

		/// <summary>
		/// Class containing information about a simple XMPP component connection.
		/// </summary>
		/// <param name="Xml">XML Document containing information.</param>
		public SimpleComponentConfiguration(XmlDocument Xml)
		{
#if !WINDOWS_UWP
			XSL.Validate(string.Empty, Xml, expectedRootElement, expectedNamespace, schema);
#endif

			this.Init(Xml.DocumentElement);
		}

		/// <summary>
		/// Class containing information about a simple XMPP connection.
		/// </summary>
		/// <param name="Xml">XML element containing information.</param>
		public SimpleComponentConfiguration(XmlElement Xml)
		{
			this.Init(Xml);
		}

		private string AssertNotEnter(string s)
		{
			if (s.StartsWith("ENTER ", StringComparison.CurrentCultureIgnoreCase))
				throw new Exception("XMPP component configuration not correctly provided.");

			return s;
		}

		private void Init(XmlElement E)
		{
			foreach (XmlNode N in E.ChildNodes)
			{
				switch (N.LocalName)
				{
					case "Host":
						this.host = this.AssertNotEnter(N.InnerText);
						break;

					case "Port":
						this.port = int.Parse(N.InnerText);
						break;

					case "Component":
						this.component = this.AssertNotEnter(N.InnerText);
						break;

					case "Secret":
						this.secret = this.AssertNotEnter(N.InnerText);
						break;

					case "Sniffer":
						if (!CommonTypes.TryParse(N.InnerText, out this.sniffer))
							this.sniffer = false;
						break;
				}
			}
		}

		/// <summary>
		/// Host name of XMPP server.
		/// </summary>
		public string Host
		{
			get => this.host;
			set => this.host = value;
		}

		/// <summary>
		/// Name of component subdomain.
		/// </summary>
		public string Component
		{
			get => this.component;
			set => this.component = value;
		}

		/// <summary>
		/// Secret of component.
		/// </summary>
		public string Secret
		{
			get => this.secret;
			set => this.secret = value;
		}

		/// <summary>
		/// If a sniffer is to be used ('true' or 'false'). If 'true', network communication will be output to the console.
		/// </summary>
		public bool Sniffer
		{
			get => this.sniffer;
			set => this.sniffer = value;
		}

		/// <summary>
		/// Port number to use when connecting to XMPP server.
		/// </summary>
		public int Port
		{
			get => this.port;
			set => this.port = value;
		}

		/// <summary>
		/// Loads simple XMPP component configuration from an XML file. If file does not exist, or is not valid, a console dialog with the 
		/// user is performed, to get the relevant information, test it, and create the corresponding XML file for later use.
		/// </summary>
		/// <param name="FileName">Name of configuration file.</param>
		/// <returns>Simple XMPP component configuration.</returns>
		public static SimpleComponentConfiguration GetConfigUsingSimpleConsoleDialog(string FileName)
		{
			try
			{
				return new SimpleComponentConfiguration(FileName);
			}
			catch (Exception)
			{
				SimpleComponentConfiguration Config = new SimpleComponentConfiguration();
				bool Ok;

				Config.host = string.Empty;
				Config.port = DefaultPort;
				Config.component = string.Empty;
				Config.secret = string.Empty;

#if WINDOWS_UWP
				using (XmppComponent Component = new XmppComponent(Config.host, Config.port, Config.component, Config.secret,
					"collaboration", "test", "Test Component"))
				{
					ManualResetEvent Connected = new ManualResetEvent(false);
					ManualResetEvent Failure = new ManualResetEvent(false);

					Component.OnStateChanged += (sender, NewState) =>
					{
						ConsoleOut.WriteLine(NewState.ToString());

						switch (NewState)
						{
							case XmppState.Connected:
								ConsoleOut.ForegroundColor = ConsoleColor.Green;
								ConsoleOut.WriteLine("Connection successful.");
								Connected.Set();
								break;

							case XmppState.Error:
								ConsoleOut.ForegroundColor = ConsoleColor.Red;
								ConsoleOut.WriteLine("Connection failed. Please update connection information.");
								Failure.Set();
								break;
						}
					};

					switch (WaitHandle.WaitAny(new WaitHandle[] { Connected, Failure }, 20000))
					{
						case 0:
							Ok = true;
							break;

						case 1:
						default:
							Ok = false;
							break;
					}

					if (!Ok)
						System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
				}
#else
				ConsoleColor FgBak = ConsoleOut.ForegroundColor;
				string s;
				string Default;

				do
				{
					ConsoleOut.ForegroundColor = ConsoleColor.Yellow;

					ConsoleOut.WriteLine();
					ConsoleOut.WriteLine("To setup a connection with the XMPP component, please answer the following");
					ConsoleOut.WriteLine("questions:");

					Default = Config.host;
					ConsoleOut.WriteLine();
					ConsoleOut.WriteLine("What XMPP server do you want to use? Press ENTER to use " + Default);

					do
					{
						ConsoleOut.ForegroundColor = ConsoleColor.White;
						ConsoleOut.Write("XMPP Server: ");
						Config.host = ConsoleIn.ReadLine();
						if (string.IsNullOrEmpty(Config.host))
							Config.host = Default;

						ConsoleOut.ForegroundColor = ConsoleColor.Green;
						ConsoleOut.WriteLine();
						ConsoleOut.WriteLine("You've selected to use '" + Config.host + "'. Is this correct? [y/n]");
						s = ConsoleIn.ReadLine();
						ConsoleOut.WriteLine();
					}
					while (!s.StartsWith("y", StringComparison.InvariantCultureIgnoreCase));

					Default = Config.port.ToString();
					ConsoleOut.ForegroundColor = ConsoleColor.Yellow;
					ConsoleOut.WriteLine("What port do you want to connect to? Press ENTER to use " + Default);

					do
					{
						ConsoleOut.ForegroundColor = ConsoleColor.White;

						do
						{
							ConsoleOut.Write("Port Number: ");
							s = ConsoleIn.ReadLine();
							if (string.IsNullOrEmpty(s))
								s = Default;
						}
						while (!int.TryParse(s, out Config.port) || Config.port < 1 || Config.port > 65535);

						ConsoleOut.ForegroundColor = ConsoleColor.Green;
						ConsoleOut.WriteLine();
						ConsoleOut.WriteLine("You've selected to use '" + Config.port.ToString() + "'. Is this correct? [y/n]");
						s = ConsoleIn.ReadLine();
						ConsoleOut.WriteLine();
					}
					while (!s.StartsWith("y", StringComparison.InvariantCultureIgnoreCase));

					Default = Config.component;
					ConsoleOut.ForegroundColor = ConsoleColor.Yellow;
					ConsoleOut.WriteLine("What component to you want to connect to?");

					if (!string.IsNullOrEmpty(Default))
						ConsoleOut.WriteLine("Press ENTER to use " + Default);

					do
					{
						ConsoleOut.ForegroundColor = ConsoleColor.White;
						do
						{
							ConsoleOut.Write("Component: ");
							Config.component = ConsoleIn.ReadLine();
							if (string.IsNullOrEmpty(Config.component))
								Config.component = Default;
						}
						while (string.IsNullOrEmpty(Config.component));

						ConsoleOut.ForegroundColor = ConsoleColor.Green;
						ConsoleOut.WriteLine();
						ConsoleOut.WriteLine("You've selected to use '" + Config.component + "'. Is this correct? [y/n]");
						s = ConsoleIn.ReadLine();
						ConsoleOut.WriteLine();
					}
					while (!s.StartsWith("y", StringComparison.InvariantCultureIgnoreCase));

					Default = Config.secret;
					ConsoleOut.ForegroundColor = ConsoleColor.Yellow;
					ConsoleOut.WriteLine("What secret goes with the component? Remember that the configuration will,");
					ConsoleOut.WriteLine("be stored in a simple text file along with the application.");

					if (!string.IsNullOrEmpty(Default))
						ConsoleOut.WriteLine("Press ENTER to use " + Default);

					do
					{
						ConsoleOut.ForegroundColor = ConsoleColor.White;
						do
						{
							ConsoleOut.Write("Secret: ");
							Config.secret = ConsoleIn.ReadLine();
							if (string.IsNullOrEmpty(Config.secret))
								Config.secret = Default;
						}
						while (string.IsNullOrEmpty(Config.secret));

						ConsoleOut.ForegroundColor = ConsoleColor.Green;
						ConsoleOut.WriteLine();
						ConsoleOut.WriteLine("You've selected to use '" + Config.secret + "'. Is this correct? [y/n]");
						s = ConsoleIn.ReadLine();
						ConsoleOut.WriteLine();
					}
					while (!s.StartsWith("y", StringComparison.InvariantCultureIgnoreCase));

					ConsoleOut.ForegroundColor = ConsoleColor.Yellow;
					ConsoleOut.WriteLine("I will now try to connect to the server to see if the information");
					ConsoleOut.WriteLine("provided is correct.");

					using (XmppComponent Component = new XmppComponent(Config.host, Config.port, Config.component, Config.secret,
						"collaboration", "test", "Test Component"))
					{
						ManualResetEvent Connected = new ManualResetEvent(false);
						ManualResetEvent Failure = new ManualResetEvent(false);

						Component.OnStateChanged += (sender, NewState) =>
						{
							ConsoleOut.WriteLine(NewState.ToString());

							switch (NewState)
							{
								case XmppState.Connected:
									ConsoleOut.ForegroundColor = ConsoleColor.Green;
									ConsoleOut.WriteLine("Connection successful.");
									Connected.Set();
									break;

								case XmppState.Error:
									ConsoleOut.ForegroundColor = ConsoleColor.Red;
									ConsoleOut.WriteLine("Connection failed. Please update connection information.");
									Failure.Set();
									break;
							}

							return Task.CompletedTask;
						};

						switch (WaitHandle.WaitAny(new WaitHandle[] { Connected, Failure }, 20000))
						{
							case 0:
								Ok = true;
								break;

							case 1:
							default:
								Ok = false;
								break;
						}
					}
				}
				while (!Ok);

				ConsoleOut.ForegroundColor = ConsoleColor.Yellow;
				ConsoleOut.WriteLine("Do you want to use a sniffer? If you use a sniffer, XMPP network");
				ConsoleOut.WriteLine("communication will be Output to the console in real-time. This can");
				ConsoleOut.WriteLine("come in handy when debugging network communication.");

				do
				{
					ConsoleOut.ForegroundColor = ConsoleColor.White;
					ConsoleOut.Write("Sniffer [y/n]? ");

					s = ConsoleIn.ReadLine();
					Config.sniffer = s.StartsWith("y", StringComparison.InvariantCultureIgnoreCase);
				}
				while (!Config.sniffer && !s.StartsWith("n", StringComparison.InvariantCultureIgnoreCase));

				ConsoleOut.WriteLine();
				ConsoleOut.ForegroundColor = FgBak;
#endif
				Config.Save(FileName);

				return Config;
			}
		}

		/// <summary>
		/// Saves the configuration to a file.
		/// </summary>
		/// <param name="FileName">File Name.</param>
		public void Save(string FileName)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.AppendLine("<?xml version='1.0' encoding='utf-8'?>");
			Xml.AppendLine("<SimpleComponentConfiguration xmlns='http://waher.se/Schema/SimpleComponentConfiguration.xsd'>");

			Xml.Append("\t<Host>");
			Xml.Append(XML.Encode(this.host));
			Xml.AppendLine("</Host>");

			Xml.Append("\t<Port>");
			Xml.Append(this.port.ToString());
			Xml.AppendLine("</Port>");

			Xml.Append("\t<Component>");
			Xml.Append(XML.Encode(this.component));
			Xml.AppendLine("</Component>");

			Xml.Append("\t<Secret>");
			Xml.Append(XML.Encode(this.secret));
			Xml.AppendLine("</Secret>");

			Xml.Append("\t<Sniffer>");
			Xml.Append(CommonTypes.Encode(this.sniffer));
			Xml.AppendLine("</Sniffer>");

			Xml.AppendLine("</SimpleComponentConfiguration>");

			File.WriteAllText(FileName, Xml.ToString(), Encoding.UTF8);
		}

		/// <summary>
		/// Gets a new XMPP component connection using the settings provided in the current object.
		/// </summary>
		/// <returns>XMPP Component connection object.</returns>
		/// <param name="IdentityCategory">Identity category, as defined in XEP-0030.</param>
		/// <param name="IdentityType">Identity type, as defined in XEP-0030.</param>
		/// <param name="IdentityName">Identity name, as defined in XEP-0030.</param>
		public XmppComponent GetComponent(string IdentityCategory, string IdentityType, string IdentityName)
		{
			return new XmppComponent(this.host, this.port, this.component, this.secret, IdentityCategory, IdentityType, IdentityName);
		}

	}
}
