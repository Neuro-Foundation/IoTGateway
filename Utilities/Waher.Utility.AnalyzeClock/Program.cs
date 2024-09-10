﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Synchronization;
using Waher.Runtime.Console;

namespace Waher.Utility.AnalyzeClock
{
	class Program
	{
		/// <summary>
		/// Analyzes the difference between the clock on the local machine with the clock on
		/// another machine, connected to the XMPP network, and compatible with the Neuro-Foundation
		/// interfaces.
		/// 
		/// Command line switches:
		/// 
		/// -h HOST               XMPP Server host name.
		/// -p PORT               XMPP Port number, if different from 5222
		/// -a ACCOUNT            XMPP Account name to use when connecting to the server.
		/// -pwd PASSWORD         PASSWORD to use when authenticating with the server.
		/// -i INTERVAL           Interval (in milliseconds) used to check clocks.
		/// -j JID                JID of clock source to monitor.
		///                       Default=5000.
		/// -r RECORDS            Number of measurements to collect.
		/// -n HISTORY            Number of records in history. Averages are calculated
		///                       on records in this history. Default=100
		/// -w WINDOW             Filter window size. The window is used to detect
		///                       and eliminate bad measurements. Default=16
		/// -s SPIKE_POS          Spike position. Where spikes are detected, in
		///                       window. Default=6
		/// -sw SPIKE_WIDTH       Spike width. Number of measurements in a row that can
		///                       constitute a spike. Default=3
		/// -o OUTPUT_FILE        File name of report file.
		/// -enc ENCODING         Text encoding. Default=UTF-8
		/// -t TRANSFORM_FILE     XSLT transform to use.
		/// -?                    Help.
		/// </summary>
		static int Main(string[] args)
		{
			try
			{
				Encoding Encoding = Encoding.UTF8;
				string OutputFileName = null;
				string XsltPath = null;
				string Host = null;
				string Account = null;
				string Password = null;
				string Jid = null;
				string s;
				int Port = 5222;
				int Records = 0;
				int Interval = 5000;
				int History = 100;
				int Window = 16;
				int SpikePos = 6;
				int SpikeWidth = 3;
				int i = 0;
				int c = args.Length;
				bool Help = false;

				while (i < c)
				{
					s = args[i++].ToLower();

					switch (s)
					{
						case "-o":
							if (i >= c)
								throw new Exception("Missing output file name.");

							if (string.IsNullOrEmpty(OutputFileName))
								OutputFileName = args[i++];
							else
								throw new Exception("Only one output file name allowed.");
							break;

						case "-h":
							if (i >= c)
								throw new Exception("Missing host name.");

							if (string.IsNullOrEmpty(Host))
								Host = args[i++];
							else
								throw new Exception("Only one host name allowed.");
							break;

						case "-p":
							if (i >= c)
								throw new Exception("Missing port number.");

							if (!int.TryParse(args[i++], out Port) || Port <= 0 || Port > 65535)
								throw new Exception("Invalid port number.");
							break;

						case "-j":
							if (i >= c)
								throw new Exception("Missing JID.");

							if (string.IsNullOrEmpty(Jid))
								Jid = args[i++];
							else
								throw new Exception("Only one JID allowed.");
							break;

						case "-i":
							if (i >= c)
								throw new Exception("Missing interval.");

							if (!int.TryParse(args[i++], out Interval) || Interval < 1000)
								throw new Exception("Invalid interval.");
							break;

						case "-r":
							if (i >= c)
								throw new Exception("Missing number of records to collect.");

							if (!int.TryParse(args[i++], out Records) || Records <= 0)
								throw new Exception("Invalid number of records to collect.");
							break;

						case "-n":
							if (i >= c)
								throw new Exception("Missing number of history records.");

							if (!int.TryParse(args[i++], out History) || History <= 0)
								throw new Exception("Invalid number of history records.");
							break;

						case "-w":
							if (i >= c)
								throw new Exception("Missing window size.");

							if (!int.TryParse(args[i++], out Window) || Window <= 0)
								throw new Exception("Invalid window size.");
							break;

						case "-s":
							if (i >= c)
								throw new Exception("Missing spike position.");

							if (!int.TryParse(args[i++], out SpikePos) || SpikePos <= 0)
								throw new Exception("Invalid spike position.");
							break;

						case "-sw":
							if (i >= c)
								throw new Exception("Missing spike width.");

							if (!int.TryParse(args[i++], out SpikeWidth) || SpikeWidth <= 0)
								throw new Exception("Invalid spike width.");
							break;

						case "-a":
							if (i >= c)
								throw new Exception("Missing account name.");

							if (string.IsNullOrEmpty(Account))
								Account = args[i++];
							else
								throw new Exception("Only one account name allowed.");
							break;

						case "-pwd":
							if (i >= c)
								throw new Exception("Missing password.");

							if (string.IsNullOrEmpty(Password))
								Password = args[i++];
							else
								throw new Exception("Only one password allowed.");
							break;

						case "-enc":
							if (i >= c)
								throw new Exception("Text encoding missing.");

							Encoding = Encoding.GetEncoding(args[i++]);
							break;

						case "-t":
							if (i >= c)
								throw new Exception("XSLT transform missing.");

							XsltPath = args[i++];
							break;

						case "-?":
							Help = true;
							break;

						default:
							throw new Exception("Unrecognized switch: " + s);
					}
				}

				if (Help || c == 0)
				{
					ConsoleOut.WriteLine("Analyzes the difference between the clock on the local machine with the clock on");
					ConsoleOut.WriteLine("another machine, connected to the XMPP network, and compatible with the Neuro-Foundation");
					ConsoleOut.WriteLine("interfaces.");
					ConsoleOut.WriteLine();
					ConsoleOut.WriteLine("Command line switches:");
					ConsoleOut.WriteLine();
					ConsoleOut.WriteLine("-h HOST               XMPP Server host name.");
					ConsoleOut.WriteLine("-p PORT               XMPP Port number, if different from 5222");
					ConsoleOut.WriteLine("-a ACCOUNT            XMPP Account name to use when connecting to the server.");
					ConsoleOut.WriteLine("-pwd PASSWORD         PASSWORD to use when authenticating with the server.");
					ConsoleOut.WriteLine("-j JID                JID of clock source to monitor.");
					ConsoleOut.WriteLine("-i INTERVAL           Interval (in milliseconds) used to check clocks.");
					ConsoleOut.WriteLine("                      Default=5000.");
					ConsoleOut.WriteLine("-r RECORDS            Number of measurements to collect.");
					ConsoleOut.WriteLine("-n HISTORY            Number of records in history. Averages are calculated");
					ConsoleOut.WriteLine("                      on records in this history. Default=100");
					ConsoleOut.WriteLine("-w WINDOW             Filter window size. The window is used to detect");
					ConsoleOut.WriteLine("                      and eliminate bad measurements. Default=16");
					ConsoleOut.WriteLine("-s SPIKE_POS          Spike position. Where spikes are detected, in");
					ConsoleOut.WriteLine("                      window. Default=6");
					ConsoleOut.WriteLine("-sw SPIKE_WIDTH       Spike width. Number of measurements in a row that can");
					ConsoleOut.WriteLine("                      constitute a spike. Default=3");
					ConsoleOut.WriteLine("-o OUTPUT_FILE        File name of report file.");
					ConsoleOut.WriteLine("-enc ENCODING         Text encoding. Default=UTF-8");
					ConsoleOut.WriteLine("-t TRANSFORM_FILE     XSLT transform to use.");
					ConsoleOut.WriteLine("-?                    Help.");
					return 0;
				}

				if (string.IsNullOrEmpty(Host))
					throw new Exception("No host name specified.");

				if (string.IsNullOrEmpty(Account))
					throw new Exception("No account name specified.");

				if (string.IsNullOrEmpty(Password))
					throw new Exception("No password specified.");

				if (string.IsNullOrEmpty(Jid))
					throw new Exception("No clock source JID specified.");

				if (Records <= 0)
					throw new Exception("Number of records to collect not specified.");

				if (string.IsNullOrEmpty(OutputFileName))
					throw new Exception("No output filename specified.");

				XmppCredentials Credentials = new()
				{
					Host = Host,
					Port = Port,
					Account = Account,
					Password = Password,
					AllowCramMD5 = true,
					AllowEncryption = true,
					AllowDigestMD5 = true,
					AllowPlain = true,
					AllowScramSHA1 = true,
					AllowScramSHA256 = true,
					AllowRegistration = false
				};

				using XmppClient Client = new(Credentials, "en", typeof(Program).Assembly);
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				Client.OnStateChanged += (sender, NewState) =>
				{
					switch (NewState)
					{
						case XmppState.Connected:
							Done.Set();
							break;

						case XmppState.Error:
						case XmppState.Offline:
							Error.Set();
							break;
					}

					return Task.CompletedTask;
				};

				Client.Connect();

				i = WaitHandle.WaitAny(new WaitHandle[] { Done, Error });
				if (i == 1)
					throw new Exception("Unable to connect to broker.");

				if (Jid.Contains('@') && !Jid.Contains('/'))
				{
					RosterItem Contact = Client.GetRosterItem(Jid);
					if (Contact is null || (Contact.State != SubscriptionState.Both && Contact.State != SubscriptionState.To))
					{
						Done.Reset();

						Client.OnPresenceSubscribed += (sender, e) =>
						{
							if (string.Compare(e.FromBareJID, Jid, true) == 0)
								Done.Set();

							return Task.CompletedTask;
						};

						Client.OnPresenceUnsubscribed += (sender, e) =>
						{
							if (string.Compare(e.FromBareJID, Jid, true) == 0)
								Error.Set();

							return Task.CompletedTask;
						};

						ConsoleOut.WriteLine("Requesting presence subscription to " + Jid);

						Client.RequestPresenceSubscription(Jid);

						i = WaitHandle.WaitAny(new WaitHandle[] { Done, Error });
						if (i == 1)
							throw new Exception("Unable to obtain presence subscription.");

						ConsoleOut.WriteLine("Presence subscription obtained.");
					}
				}

				ManualResetEvent Done2 = new(false);

				using StreamWriter f = File.CreateText(OutputFileName);
				XmlWriterSettings Settings = new()
				{
					Encoding = Encoding,
					Indent = true,
					IndentChars = "\t",
					NewLineChars = ConsoleOut.NewLine,
					OmitXmlDeclaration = false,
					WriteEndDocumentOnClose = true
				};

				XmlWriter w = XmlWriter.Create(f, Settings);

				w.WriteStartDocument();

				if (!string.IsNullOrEmpty(XsltPath))
				{
					if (File.Exists(XsltPath))
					{
						try
						{
							byte[] XsltBin = File.ReadAllBytes(XsltPath);

							w.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"data:text/xsl;base64," +
								Convert.ToBase64String(XsltBin) + "\"");
						}
						catch (Exception)
						{
							w.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"" + XML.Encode(XsltPath) + "\"");
						}
					}
					else
						w.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"" + XML.Encode(XsltPath) + "\"");
				}

				w.WriteStartElement("ClockStatistics", "http://waher.se/Schema/Networking/ClockStatistics.xsd");

				w.WriteStartElement("Parameters");
				w.WriteAttributeString("clientJid", Client.BareJID);
				w.WriteAttributeString("sourceJid", Jid);
				w.WriteAttributeString("records", Records.ToString());
				w.WriteAttributeString("interval", Interval.ToString());
				w.WriteAttributeString("history", History.ToString());
				w.WriteAttributeString("window", Window.ToString());
				w.WriteAttributeString("spikePos", SpikePos.ToString());
				w.WriteAttributeString("spikeWidth", SpikeWidth.ToString());
				w.WriteAttributeString("hfFreq", System.Diagnostics.Stopwatch.Frequency.ToString());
				w.WriteEndElement();

				w.WriteStartElement("Samples");

				using (SynchronizationClient SynchClient = new(Client))
				{
					SynchClient.OnUpdated += (sender, e) =>
					{
						DateTime TP = DateTime.Now;
						double? StdDev;

						w.WriteStartElement("Sample");
						w.WriteAttributeString("timestamp", XML.Encode(TP));

						if (SynchClient.RawLatency100Ns.HasValue)
							w.WriteAttributeString("rawLatencyMs", CommonTypes.Encode(SynchClient.RawLatency100Ns.Value * 1e-4));

						if (SynchClient.LatencySpikeRemoved.HasValue)
							w.WriteAttributeString("spikeLatencyRemoved", CommonTypes.Encode(SynchClient.LatencySpikeRemoved.Value));

						if (SynchClient.RawClockDifference100Ns.HasValue)
							w.WriteAttributeString("rawDifferenceMs", CommonTypes.Encode(SynchClient.RawClockDifference100Ns.Value * 1e-4));

						if (SynchClient.ClockDifferenceSpikeRemoved.HasValue)
							w.WriteAttributeString("spikeDifferenceRemoved", CommonTypes.Encode(SynchClient.ClockDifferenceSpikeRemoved.Value));

						if (SynchClient.FilteredLatency100Ns.HasValue)
							w.WriteAttributeString("filteredLatencyMs", CommonTypes.Encode(SynchClient.FilteredLatency100Ns.Value * 1e-4));

						if (SynchClient.FilteredClockDifference100Ns.HasValue)
							w.WriteAttributeString("filteredDifferenceMs", CommonTypes.Encode(SynchClient.FilteredClockDifference100Ns.Value * 1e-4));

						if (SynchClient.AvgLatency100Ns.HasValue)
							w.WriteAttributeString("avgLatencyMs", CommonTypes.Encode(SynchClient.AvgLatency100Ns.Value * 1e-4));

						if (SynchClient.AvgClockDifference100Ns.HasValue)
							w.WriteAttributeString("avgDifferenceMs", CommonTypes.Encode(SynchClient.AvgClockDifference100Ns.Value * 1e-4));

						StdDev = SynchClient.CalcStdDevLatency100Ns();
						if (StdDev.HasValue)
							w.WriteAttributeString("stdDevLatencyMs", CommonTypes.Encode(StdDev.Value * 1e-4));

						StdDev = SynchClient.CalcStdDevClockDifference100Ns();
						if (StdDev.HasValue)
							w.WriteAttributeString("stdDevDifferenceMs", CommonTypes.Encode(StdDev.Value * 1e-4));

						if (SynchClient.RawLatencyHf.HasValue)
							w.WriteAttributeString("rawLatencyHf", SynchClient.RawLatencyHf.Value.ToString());

						if (SynchClient.LatencyHfSpikeRemoved.HasValue)
							w.WriteAttributeString("spikeLatencyHfRemoved", CommonTypes.Encode(SynchClient.LatencyHfSpikeRemoved.Value));

						if (SynchClient.RawClockDifferenceHf.HasValue)
							w.WriteAttributeString("rawDifferenceHf", SynchClient.RawClockDifferenceHf.Value.ToString());

						if (SynchClient.ClockDifferenceHfSpikeRemoved.HasValue)
							w.WriteAttributeString("spikeDifferenceHfRemoved", CommonTypes.Encode(SynchClient.ClockDifferenceHfSpikeRemoved.Value));

						if (SynchClient.FilteredLatencyHf.HasValue)
							w.WriteAttributeString("filteredLatencyHf", SynchClient.FilteredLatencyHf.ToString());

						if (SynchClient.FilteredClockDifferenceHf.HasValue)
							w.WriteAttributeString("filteredDifferenceHf", SynchClient.FilteredClockDifferenceHf.ToString());

						if (SynchClient.AvgLatencyHf.HasValue)
							w.WriteAttributeString("avgLatencyHf", SynchClient.AvgLatencyHf.ToString());

						if (SynchClient.AvgClockDifferenceHf.HasValue)
							w.WriteAttributeString("avgDifferenceHf", SynchClient.AvgClockDifferenceHf.ToString());

						StdDev = SynchClient.CalcStdDevLatencyHf();
						if (StdDev.HasValue)
							w.WriteAttributeString("stdDevLatencyHf", CommonTypes.Encode(StdDev.Value));

						StdDev = SynchClient.CalcStdDevClockDifferenceHf();
						if (StdDev.HasValue)
							w.WriteAttributeString("stdDevDifferenceHf", CommonTypes.Encode(StdDev.Value));

						w.WriteEndElement();

						ConsoleOut.Write(".");

						if (--Records <= 0)
							Done2.Set();
					};

					SynchClient.MonitorClockDifference(Jid, Interval, History, Window, SpikePos, SpikeWidth, true);

					Done2.WaitOne();
				}

				w.WriteEndElement();
				w.WriteEndElement();
				w.WriteEndDocument();

				w.Flush();

				ConsoleOut.WriteLine();

				return 0;
			}
			catch (Exception ex)
			{
				ConsoleOut.WriteLine(ex.Message);
				return -1;
			}
		}
	}
}
