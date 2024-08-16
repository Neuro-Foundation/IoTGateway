﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Waher.Content;
using Waher.Content.Emoji.Emoji1;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Markdown.GraphViz;
using Waher.Content.Markdown.Layout2D;
using Waher.Content.Markdown.PlantUml;
using Waher.Content.Markdown.Rendering;
using Waher.Content.Markdown.Web;
using Waher.Content.Markdown.Web.WebScript;
using Waher.Content.SystemFiles;
using Waher.Content.Text;
using Waher.Content.Xml;
using Waher.Content.Xsl;
using Waher.Events;
using Waher.Events.Files;
using Waher.Events.Persistence;
using Waher.Events.XMPP;
using Waher.IoTGateway.Events;
using Waher.IoTGateway.Exceptions;
using Waher.IoTGateway.Setup;
using Waher.IoTGateway.Setup.Legal;
using Waher.IoTGateway.WebResources;
using Waher.IoTGateway.WebResources.ExportFormats;
using Waher.Networking;
using Waher.Networking.CoAP;
using Waher.Networking.HTTP;
using Waher.Networking.HTTP.ContentEncodings;
using Waher.Networking.HTTP.HeaderFields;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.HTTPX;
using Waher.Networking.XMPP.Mail;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.P2P.SOCKS5;
using Waher.Networking.XMPP.PEP;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.Software;
using Waher.Networking.XMPP.Synchronization;
using Waher.Runtime.Language;
using Waher.Runtime.Inventory;
using Waher.Runtime.Inventory.Loader;
using Waher.Runtime.ServiceRegistration;
using Waher.Runtime.Settings;
using Waher.Runtime.Threading;
using Waher.Runtime.Threading.Sync;
using Waher.Runtime.Timing;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Script.Model;
using Waher.Security;
using Waher.Security.CallStack;
using Waher.Security.LoginMonitor;
using Waher.Security.SHA3;
using Waher.Security.Users;
using Waher.Things;
using Waher.Things.Metering;
using Waher.Things.SensorData;
using Waher.Content.Images;
using Waher.Events.Filter;

namespace Waher.IoTGateway
{
	/// <summary>
	/// Delegate for callback methods used for the creation of database providers.
	/// </summary>
	/// <param name="Definition">XML definition of provider.</param>
	/// <returns>Database provider.</returns>
	public delegate Task<IDatabaseProvider> GetDatabaseProviderEventHandler(XmlElement Definition);

	/// <summary>
	/// Delegate for registration callback methods.
	/// </summary>
	/// <param name="MetaData">Meta data used in registration.</param>
	/// <param name="e">Event arguments.</param>
	public delegate Task RegistrationEventHandler(MetaDataTag[] MetaData, RegistrationEventArgs e);

	/// <summary>
	/// Delegate for events requesting meta data for registration.
	/// </summary>
	/// <param name="MetaData">Defult meta data.</param>
	/// <returns>Meta data to register.</returns>
	public delegate Task<MetaDataTag[]> GetRegistryMetaDataEventHandler(MetaDataTag[] MetaData);

	/// <summary>
	/// Delegate for events requesting an array of data sources.
	/// </summary>
	/// <param name="DataSources">Default set of data sources.</param>
	/// <returns>Data sources to publish.</returns>
	public delegate Task<IDataSource[]> GetDataSources(params IDataSource[] DataSources);

	/// <summary>
	/// Static class managing the runtime environment of the IoT Gateway.
	/// </summary>
	public static class Gateway
	{
		/// <summary>
		/// Gateway.config
		/// </summary>
		public const string GatewayConfigLocalFileName = "Gateway.config";

		/// <summary>
		/// GatewayConfiguration
		/// </summary>
		public const string GatewayConfigLocalName = "GatewayConfiguration";

		/// <summary>
		/// http://waher.se/Schema/GatewayConfiguration.xsd
		/// </summary>
		public const string GatewayConfigNamespace = "http://waher.se/Schema/GatewayConfiguration.xsd";

		private const int MaxChunkSize = 4096;

		private static readonly LinkedList<KeyValuePair<string, int>> ports = new LinkedList<KeyValuePair<string, int>>();
		private static readonly Dictionary<int, EventHandler> serviceCommandByNr = new Dictionary<int, EventHandler>();
		private static readonly Dictionary<EventHandler, int> serviceCommandNrByCallback = new Dictionary<EventHandler, int>();
		private static readonly Dictionary<string, DateTime> lastUnauthorizedAccess = new Dictionary<string, DateTime>();
		private static readonly DateTime startTime = DateTime.Now;
		private static IDatabaseProvider internalProvider = null;
		private static ThingRegistryClient thingRegistryClient = null;
		private static ProvisioningClient provisioningClient = null;
		private static XmppCredentials xmppCredentials = null;
		private static XmppClient xmppClient = null;
		private static Networking.XMPP.Avatar.AvatarClient avatarClient = null;
		private static Networking.XMPP.InBandBytestreams.IbbClient ibbClient = null;
		private static Socks5Proxy socksProxy = null;
		private static ConcentratorServer concentratorServer = null;
		private static SensorClient sensorClient = null;
		private static ControlClient controlClient = null;
		private static ConcentratorClient concentratorClient = null;
		private static SynchronizationClient synchronizationClient = null;
		private static PepClient pepClient = null;
		private static MultiUserChatClient mucClient = null;
		private static ContractsClient contractsClient = null;
		private static SoftwareUpdateClient softwareUpdateClient = null;
		private static MailClient mailClient = null;
		private static X509Certificate2 certificate = null;
		private static DateTime checkCertificate = DateTime.MinValue;
		private static DateTime checkIp = DateTime.MinValue;
		private static HttpServer webServer = null;
		private static HttpFolderResource root = null;
		private static HttpxProxy httpxProxy = null;
		private static HttpxServer httpxServer = null;
		private static CoapEndpoint coapEndpoint = null;
		private static SystemConfiguration[] configurations;
		private static LoginAuditor loginAuditor = null;
		private static Scheduler scheduler = null;
		private readonly static RandomNumberGenerator rnd = RandomNumberGenerator.Create();
		private static AsyncMutex gatewayRunning = null;
		private static AsyncMutex startingServer = null;
		private static Emoji1LocalFiles emoji1_24x24 = null;
		private static StreamWriter exceptionFile = null;
		private static CaseInsensitiveString domain = null;
		private static CaseInsensitiveString[] alternativeDomains = null;
		private static CaseInsensitiveString ownerJid = null;
		private static Dictionary<string, string> defaultPageByHostName = null;
		private static string instance;
		private static string appDataFolder;
		private static string runtimeFolder;
		private static string rootFolder;
		private static string applicationName;
		private static string exceptionFolder = null;
		private static string exceptionFileName = null;
		private static int nextServiceCommandNr = 128;
		private static int beforeUninstallCommandNr = 0;
		private static bool firstStart = true;
		private static bool registered = false;
		private static bool connected = false;
		private static bool immediateReconnect;
		private static bool consoleOutput;
		private static bool loopbackIntefaceAvailable;
		private static bool configuring = false;
		private static bool exportExceptions = false;
		private static bool stopped = false;

		#region Life Cycle

		/// <summary>
		/// Timepoint of starting the gateway.
		/// </summary>
		public static DateTime StartTime => startTime;

		/// <summary>
		/// Starts the gateway.
		/// </summary>
		/// <param name="ConsoleOutput">If console output is permitted.</param>
		/// <returns>If the gateway was successfully started.</returns>
		public static Task<bool> Start(bool ConsoleOutput)
		{
			return Start(ConsoleOutput, true, string.Empty);
		}

		/// <summary>
		/// Starts the gateway.
		/// </summary>
		/// <param name="ConsoleOutput">If console output is permitted.</param>
		/// <param name="LoopbackIntefaceAvailable">If the loopback interface is available.</param>
		/// <returns>If the gateway was successfully started.</returns>
		public static Task<bool> Start(bool ConsoleOutput, bool LoopbackIntefaceAvailable)
		{
			return Start(ConsoleOutput, LoopbackIntefaceAvailable, string.Empty);
		}

		/// <summary>
		/// Starts the gateway.
		/// </summary>
		/// <param name="ConsoleOutput">If console output is permitted.</param>
		/// <param name="LoopbackIntefaceAvailable">If the loopback interface is available.</param>
		/// <param name="InstanceName">Name of instance. Default=<see cref="string.Empty"/>.</param>
		/// <returns>If the gateway was successfully started.</returns>
		public static async Task<bool> Start(bool ConsoleOutput, bool LoopbackIntefaceAvailable, string InstanceName)
		{
			bool FirstStart = firstStart;

			firstStart = false;
			instance = InstanceName;

			string Suffix = string.IsNullOrEmpty(InstanceName) ? string.Empty : "." + InstanceName;
			gatewayRunning = new AsyncMutex(false, "Waher.IoTGateway.Running" + Suffix);
			if (!await gatewayRunning.WaitOne(1000))
				return false; // Is running in another process.

			startingServer = new AsyncMutex(false, "Waher.IoTGateway.Starting" + Suffix);
			if (!await startingServer.WaitOne(1000))
			{
				await gatewayRunning.ReleaseMutex();
				gatewayRunning.Dispose();
				gatewayRunning = null;

				startingServer.Dispose();
				startingServer = null;
				return false; // Being started in another process.
			}

			try
			{
				stopped = false;
				consoleOutput = ConsoleOutput;
				loopbackIntefaceAvailable = LoopbackIntefaceAvailable;

				appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

				if (!appDataFolder.EndsWith(new string(Path.DirectorySeparatorChar, 1)))
					appDataFolder += Path.DirectorySeparatorChar;

				appDataFolder += "IoT Gateway";

				if (!string.IsNullOrEmpty(InstanceName))
					appDataFolder += " " + InstanceName;

				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !Directory.Exists(appDataFolder))
					appDataFolder = appDataFolder.Replace("/usr/share", "/usr/local/share");

				appDataFolder += Path.DirectorySeparatorChar;
				rootFolder = appDataFolder + "Root" + Path.DirectorySeparatorChar;

				Log.Register(new EventFilter("Alert Filter", new AlertNotifier("Alert Notifier"), EventType.Alert));
				Log.Register(new XmlFileEventSink("XML File Event Sink",
					appDataFolder + "Events" + Path.DirectorySeparatorChar + "Event Log %YEAR%-%MONTH%-%DAY%T%HOUR%.xml",
					appDataFolder + "Transforms" + Path.DirectorySeparatorChar + "EventXmlToHtml.xslt", 7));

				if (FirstStart)
					Assert.UnauthorizedAccess += Assert_UnauthorizedAccess;

				Log.Informational("Server starting up.");

				if (FirstStart)
				{
					Initialize();

					beforeUninstallCommandNr = RegisterServiceCommand(BeforeUninstall);

					if (!Directory.Exists(rootFolder))
					{
						string s = Path.Combine(runtimeFolder, "Root");
						if (Directory.Exists(s))
						{
							CopyFolder(runtimeFolder, appDataFolder, "*.config", true);
							CopyFolders(s, rootFolder, true);
							CopyFolders(Path.Combine(runtimeFolder, "Graphics"), Path.Combine(appDataFolder, "Graphics"), true);
							CopyFolders(Path.Combine(runtimeFolder, "Transforms"), Path.Combine(appDataFolder, "Transforms"), true);
						}
					}

					string[] ManifestFiles = Directory.GetFiles(runtimeFolder, "*.manifest", SearchOption.TopDirectoryOnly);
					Dictionary<string, CopyOptions> ContentOptions = new Dictionary<string, CopyOptions>();
					int i;

					for (i = 0; i < 2; i++)
					{
						foreach (string ManifestFile in ManifestFiles)
						{
							string FileName = Path.GetFileName(ManifestFile);
							bool GatewayFile = FileName.StartsWith("Waher.IoTGateway", StringComparison.CurrentCultureIgnoreCase);

							if ((i == 0 && GatewayFile) || (i == 1 && !GatewayFile))
							{
								CheckContentFiles(ManifestFile, ContentOptions);

								if (ManifestFile.EndsWith("Waher.Utility.Install.manifest"))
									CheckInstallUtilityFiles(ManifestFile);
							}
						}
					}
				}

				Types.SetModuleParameter("AppData", appDataFolder);
				Types.SetModuleParameter("Runtime", runtimeFolder);
				Types.SetModuleParameter("Root", rootFolder);

				scheduler = new Scheduler();

				if (FirstStart)
				{
					Task T = Task.Run(() =>
					{
						GraphViz.Init(rootFolder);
						XmlLayout.Init(rootFolder);
						PlantUml.Init(rootFolder);
					});
				}

				XmlDocument Config = new XmlDocument()
				{
					PreserveWhitespace = true
				};

				string GatewayConfigFileName = ConfigFilePath;
				if (!File.Exists(GatewayConfigFileName))
					GatewayConfigFileName = GatewayConfigLocalFileName;

				Config.Load(GatewayConfigFileName);
				XSL.Validate(GatewayConfigLocalFileName, Config, GatewayConfigLocalName, GatewayConfigNamespace,
					XSL.LoadSchema(typeof(Gateway).Namespace + ".Schema.GatewayConfiguration.xsd", typeof(Gateway).Assembly));

				IDatabaseProvider DatabaseProvider = null;
				ClientCertificates ClientCertificates = ClientCertificates.NotUsed;
				bool TrustClientCertificates = false;
				Dictionary<int, KeyValuePair<ClientCertificates, bool>> PortSpecificMTlsSettings = null;

				foreach (XmlNode N in Config.DocumentElement.ChildNodes)
				{
					if (N is XmlElement E)
					{
						switch (E.LocalName)
						{
							case "ApplicationName":
								applicationName = E.InnerText;
								break;

							case "DefaultPage":
								if (defaultPageByHostName is null)
									defaultPageByHostName = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

								defaultPageByHostName[XML.Attribute(E, "host")] = E.InnerText;
								break;

							case "MutualTls":
								ClientCertificates = XML.Attribute(E, "clientCertificates", ClientCertificates.NotUsed);
								TrustClientCertificates = XML.Attribute(E, "trustCertificates", false);

								foreach (XmlNode N2 in E.ChildNodes)
								{
									if (N2.LocalName == "Port" && int.TryParse(N2.InnerText, out int PortNumber))
									{
										XmlElement E2 = (XmlElement)N2;
										ClientCertificates ClientCertificatesPort = XML.Attribute(E2, "clientCertificates", ClientCertificates);
										bool TrustClientCertificatesPort = XML.Attribute(E2, "trustCertificates", TrustClientCertificates);

										if (PortSpecificMTlsSettings is null)
											PortSpecificMTlsSettings = new Dictionary<int, KeyValuePair<ClientCertificates, bool>>();

										PortSpecificMTlsSettings[PortNumber] = new KeyValuePair<ClientCertificates, bool>(ClientCertificatesPort, TrustClientCertificatesPort);
									}
								}
								break;

							case "ContentEncodings":
								foreach (XmlNode N2 in E.ChildNodes)
								{
									if (N2.LocalName == "ContentEncoding")
									{
										XmlElement E2 = (XmlElement)N2;
										string Method = XML.Attribute(E2, "method");
										bool Dynamic = XML.Attribute(E2, "dynamic", true);
										bool Static = XML.Attribute(E2, "static", true);

										IContentEncoding Encoding = Types.FindBest<IContentEncoding, string>(Method);

										if (Encoding is null)
											Log.Error("Content-Encoding not found: " + Method, GatewayConfigLocalFileName);
										else
											Encoding.ConfigureSupport(Dynamic, Static);
									}
								}

								HttpFieldAcceptEncoding.ContentEncodingsReconfigured();
								break;

							case "ExportExceptions":
								exceptionFolder = Path.Combine(appDataFolder, XML.Attribute(E, "folder", "Exceptions"));

								if (!Directory.Exists(exceptionFolder))
									Directory.CreateDirectory(exceptionFolder);

								DateTime Now = DateTime.Now;
								string[] ExceptionFiles = Directory.GetFiles(exceptionFolder, "*.txt", SearchOption.TopDirectoryOnly);
								foreach (string ExceptionFile in ExceptionFiles)
								{
									try
									{
										DateTime TP = File.GetLastWriteTime(ExceptionFile);
										if ((Now - TP).TotalDays > 90)
											File.Delete(ExceptionFile);
										else
										{
											string XmlFile = Path.ChangeExtension(ExceptionFile, "xml");
											if (!File.Exists(XmlFile))
											{
												Log.Informational("Processing " + ExceptionFile);
												Analyze.Process(ExceptionFile, XmlFile);
											}
										}
									}
									catch (Exception ex)
									{
										Log.Critical(ex, ExceptionFile);
									}
								}

								exceptionFileName = Path.Combine(exceptionFolder, Now.Year.ToString("D4") + "-" + Now.Month.ToString("D2") + "-" + Now.Day.ToString("D2") +
									" " + Now.Hour.ToString("D2") + "." + Now.Minute.ToString("D2") + "." + Now.Second.ToString("D2") + ".txt");
								exceptionFile = File.CreateText(exceptionFileName);
								exportExceptions = true;

								exceptionFile.Write("Start of export: ");
								exceptionFile.WriteLine(DateTime.Now.ToString());

								if (FirstStart)
								{
									AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
									{
										if (exceptionFile is null)
											return;

										lock (exceptionFile)
										{
											if (!exportExceptions || e.Exception.StackTrace.Contains("FirstChanceExceptionEventArgs"))
												return;

											exceptionFile.WriteLine(new string('-', 80));
											exceptionFile.Write("Type: ");

											if (!(e.Exception is null))
												exceptionFile.WriteLine(e.Exception.GetType().FullName);
											else
												exceptionFile.WriteLine("null");

											exceptionFile.Write("Time: ");
											exceptionFile.WriteLine(DateTime.Now.ToString());

											if (!(e.Exception is null))
											{
												LinkedList<Exception> Exceptions = new LinkedList<Exception>();
												Exceptions.AddLast(e.Exception);

												while (!(Exceptions.First is null))
												{
													Exception ex = Exceptions.First.Value;
													Exceptions.RemoveFirst();

													exceptionFile.WriteLine();

													exceptionFile.WriteLine(ex.Message);
													exceptionFile.WriteLine();
													exceptionFile.WriteLine(Log.CleanStackTrace(ex.StackTrace));
													exceptionFile.WriteLine();

													if (ex is AggregateException ex2)
													{
														foreach (Exception ex3 in ex2.InnerExceptions)
															Exceptions.AddLast(ex3);
													}
													else if (!(ex.InnerException is null))
														Exceptions.AddLast(ex.InnerException);
												}
											}

											exceptionFile.Flush();
										}
									};
								}
								break;

							case "Database":
								if (FirstStart || !Database.HasProvider)
								{
									if (!(DatabaseProvider is null))
										throw new Exception("Database provider already initiated.");

									if (!(GetDatabaseProvider is null))
										DatabaseProvider = await GetDatabaseProvider(E);
									else
										DatabaseProvider = null;

									if (DatabaseProvider is null)
										throw new Exception("Database provider not defined. Make sure the GetDatabaseProvider event has an appropriate event handler.");

									internalProvider = DatabaseProvider;
									Database.Register(DatabaseProvider, false);
								}
								else
								{
									DatabaseProvider = Database.Provider;
									await DatabaseProvider.Start();

									if (Ledger.HasProvider)
										await Ledger.Provider.Start();
								}
								break;

							case "Ports":
								foreach (XmlNode N2 in E.ChildNodes)
								{
									if (N2.LocalName == "Port")
									{
										XmlElement E2 = (XmlElement)N2;
										string Protocol = XML.Attribute(E2, "protocol");
										if (!string.IsNullOrEmpty(Protocol) && int.TryParse(E2.InnerText, out int Port2))
											ports.AddLast(new KeyValuePair<string, int>(Protocol, Port2));
									}
								}
								break;

							case "LoginAuditor":
								List<LoginInterval> LoginIntervals = new List<LoginInterval>();
								Duration LastInterval = Duration.Zero;
								bool LastMaxInterval = false;

								foreach (XmlNode N2 in E.ChildNodes)
								{
									if (N2.LocalName == "Interval")
									{
										if (LastMaxInterval)
										{
											Log.Error("Only the last login auditor interval can be the empty 'eternal' interval.",
												GatewayConfigLocalFileName);
											break;
										}

										XmlElement E2 = (XmlElement)N2;
										int NrAttempts = XML.Attribute(E2, "nrAttempts", 0);
										if (NrAttempts <= 0)
										{
											Log.Error("Number of attempts must be positive when defining an interval for the LoginAuditor",
												GatewayConfigLocalFileName);
											continue;
										}

										if (!E2.HasAttribute("interval"))
										{
											LoginIntervals.Add(new LoginInterval(NrAttempts, TimeSpan.MaxValue));
											LastMaxInterval = true;
										}
										else
										{
											Duration Interval = XML.Attribute(E2, "interval", Duration.Zero);
											if (Interval <= Duration.Zero)
											{
												Log.Error("Login Auditor intervals must be positive", GatewayConfigLocalFileName);
												continue;
											}

											if (Interval <= LastInterval)
											{
												Log.Error("Login Auditor intervals must be specified in an increasing order.",
													GatewayConfigLocalFileName);
												continue;
											}

											LoginIntervals.Add(new LoginInterval(NrAttempts, Interval));
											LastInterval = Interval;
										}
									}
								}

								if (LoginIntervals.Count == 0)
									Log.Error("Login Auditor intervals not specified.", GatewayConfigLocalFileName);
								else
									loginAuditor = new LoginAuditor("Login Auditor", LoginIntervals.ToArray());
								break;
						}
					}
				}

				if (DatabaseProvider is null)
					throw new Exception("Database provider not defined in " + GatewayConfigLocalFileName + ".");

				Database.CollectionRepaired += Database_CollectionRepaired;

				await RepairIfInproperShutdown();

				PersistedEventLog PersistedEventLog = new PersistedEventLog(90, new TimeSpan(4, 15, 0));
				Log.Register(PersistedEventLog);
				try
				{
					await PersistedEventLog.Queue(new Event(EventType.Informational, "Server starting up.", string.Empty, string.Empty, string.Empty, EventLevel.Minor, string.Empty, string.Empty, string.Empty));
				}
				catch (Exception ex)
				{
					Event Event = new Event(DateTime.Now, EventType.Critical, ex.Message, PersistedEventLog.ObjectID, string.Empty, string.Empty,
						EventLevel.Major, string.Empty, ex.Source, Log.CleanStackTrace(ex.StackTrace));

					Event.Avoid(PersistedEventLog);

					Log.Event(Event);
				}

				if (loginAuditor is null)
				{
					loginAuditor = new LoginAuditor("Login Auditor",
						new LoginInterval(5, TimeSpan.FromHours(1)),    // Maximum 5 failed login attempts in an hour
						new LoginInterval(2, TimeSpan.FromDays(1)),     // Maximum 2x5 failed login attempts in a day
						new LoginInterval(2, TimeSpan.FromDays(7)),     // Maximum 2x2x5 failed login attempts in a week
						new LoginInterval(2, TimeSpan.MaxValue));       // Maximum 2x2x2x5 failed login attempts in total, then blocked.
				}

				Log.Register(loginAuditor);

				// Protecting Markdown resources:
				if (!MarkdownCodec.IsRawEncodingAllowedLocked)
					MarkdownCodec.AllowRawEncoding(false, true);
				HttpFolderResource.ProtectContentType(MarkdownCodec.ContentType);

				// Protecting web-script resources:
				if (!WsCodec.IsRawEncodingAllowedLocked)
					WsCodec.AllowRawEncoding(false, true);
				HttpFolderResource.ProtectContentType(WsCodec.ContentType);

				LinkedList<SystemConfiguration> NewConfigurations = null;
				Dictionary<string, Type> SystemConfigurationTypes = new Dictionary<string, Type>();
				Dictionary<string, SystemConfiguration> SystemConfigurations = new Dictionary<string, SystemConfiguration>();
				bool Configured = true;
				bool Simplify = (await ServiceRegistrationClient.GetRegistrationTime()).HasValue;

				foreach (Type SystemConfigurationType in Types.GetTypesImplementingInterface(typeof(ISystemConfiguration)))
				{
					if (SystemConfigurationType.IsAbstract || SystemConfigurationType.IsInterface || SystemConfigurationType.IsGenericTypeDefinition)
						continue;

					SystemConfigurationTypes[SystemConfigurationType.FullName] = SystemConfigurationType;
				}

				foreach (SystemConfiguration SystemConfiguration in await Database.Find<SystemConfiguration>())
				{
					string s = SystemConfiguration.GetType().FullName;

					if (SystemConfigurations.ContainsKey(s))
						await Database.Delete(SystemConfiguration);     // No duplicates allowed by mistake
					else
					{
						SystemConfigurations[s] = SystemConfiguration;
						SystemConfigurationTypes.Remove(s);

						if (!SystemConfiguration.Complete)
						{
							if (await SystemConfiguration.EnvironmentConfiguration())
							{
								await SystemConfiguration.MakeCompleted();
								await Database.Update(SystemConfiguration);

								if (NewConfigurations is null)
									NewConfigurations = new LinkedList<SystemConfiguration>();

								NewConfigurations.AddLast(SystemConfiguration);
								continue;
							}

							if (Simplify && await SystemConfiguration.SimplifiedConfiguration())
							{
								await SystemConfiguration.MakeCompleted();
								await Database.Update(SystemConfiguration);

								if (NewConfigurations is null)
									NewConfigurations = new LinkedList<SystemConfiguration>();

								NewConfigurations.AddLast(SystemConfiguration);
								continue;
							}

							Configured = false;
						}
					}
				}

				foreach (KeyValuePair<string, Type> P in SystemConfigurationTypes)
				{
					try
					{
						SystemConfiguration SystemConfiguration = (SystemConfiguration)Types.Instantiate(P.Value);
						SystemConfiguration.Complete = false;
						SystemConfiguration.Created = DateTime.Now;

						await Database.Insert(SystemConfiguration);

						SystemConfigurations[P.Key] = SystemConfiguration;

						if (await SystemConfiguration.EnvironmentConfiguration())
						{
							await SystemConfiguration.MakeCompleted();
							await Database.Update(SystemConfiguration);

							if (NewConfigurations is null)
								NewConfigurations = new LinkedList<SystemConfiguration>();

							NewConfigurations.AddLast(SystemConfiguration);
							continue;
						}

						if (Simplify && await SystemConfiguration.SimplifiedConfiguration())
						{
							await SystemConfiguration.MakeCompleted();
							await Database.Update(SystemConfiguration);

							if (NewConfigurations is null)
								NewConfigurations = new LinkedList<SystemConfiguration>();

							NewConfigurations.AddLast(SystemConfiguration);
							continue;
						}

						Configured = false;
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
						continue;
					}
				}

				configurations = new SystemConfiguration[SystemConfigurations.Count];
				SystemConfigurations.Values.CopyTo(configurations, 0);
				Array.Sort(configurations, (c1, c2) => c1.Priority - c2.Priority);

				ISystemConfiguration CurrentConfiguration = null;
				LinkedList<HttpResource> SetupResources = null;

				if (!Configured)
				{
					configuring = true;

					if (loopbackIntefaceAvailable)
						Log.Notice("System needs to be configured. This is done by navigating to the loopback interface using a browser on this machine.");
					else
						Log.Notice("System needs to be configured. This is done by navigating to the machine using a browser on another machine in the same network.");

					webServer = new HttpServer(GetConfigPorts("HTTP"), null, null)
					{
						ResourceOverride = "/Starting.md",
						ResourceOverrideFilter = "(?<!Login)[.]md(\\?[.]*)?$",
						LoginAuditor = loginAuditor
					};

					webServer.Register("/Starting.md", StartingMd);
					webServer.CustomError += WebServer_CustomError;

					SetupResources = new LinkedList<HttpResource>();

					SetupResources.AddLast(webServer.Register(new HttpFolderResource("/Graphics", Path.Combine(appDataFolder, "Graphics"), false, false, true, false, HostDomainOptions.SameForAllDomains))); // TODO: Add authentication mechanisms for PUT & DELETE.
					SetupResources.AddLast(webServer.Register(new HttpFolderResource("/Transforms", Path.Combine(appDataFolder, "Transforms"), false, false, true, false, HostDomainOptions.SameForAllDomains))); // TODO: Add authentication mechanisms for PUT & DELETE.
					SetupResources.AddLast(webServer.Register(new HttpFolderResource("/highlight", "Highlight", false, false, true, false, HostDomainOptions.SameForAllDomains)));   // Syntax highlighting library, provided by http://highlightjs.org
					SetupResources.AddLast(webServer.Register(root = new HttpFolderResource(string.Empty, rootFolder, false, false, true, true, HostDomainOptions.UseDomainSubfolders)));    // TODO: Add authentication mechanisms for PUT & DELETE.
					SetupResources.AddLast(webServer.Register("/", GoToDefaultPage));
					SetupResources.AddLast(webServer.Register(new ClientEvents()));
					SetupResources.AddLast(webServer.Register(new ClientEventsWebSocket()));
					SetupResources.AddLast(webServer.Register(new Login()));
					SetupResources.AddLast(webServer.Register(new Logout()));

					emoji1_24x24 = new Emoji1LocalFiles(Emoji1SourceFileType.Svg, 24, 24, "/Graphics/Emoji1/svg/%FILENAME%",
						Path.Combine(runtimeFolder, "Graphics", "Emoji1.zip"), Path.Combine(appDataFolder, "Graphics"));

					root.AllowTypeConversion();

					MarkdownToHtmlConverter.EmojiSource = emoji1_24x24;
					MarkdownToHtmlConverter.RootFolder = rootFolder;
				}

				foreach (SystemConfiguration Configuration in configurations)
				{
					Configuration.SetStaticInstance(Configuration);

					if (!(webServer is null))
						await Configuration.InitSetup(webServer);
				}

				bool ReloadConfigurations;

				do
				{
					ReloadConfigurations = false;

					foreach (SystemConfiguration Configuration in configurations)
					{
						bool NeedsCleanup = false;

						if (!Configuration.Complete)
						{
							CurrentConfiguration = Configuration;

							if (!(webServer is null))
								webServer.ResourceOverride = Configuration.Resource;

							Configuration.SetStaticInstance(Configuration);

							if (!(startingServer is null))
							{
								await startingServer.ReleaseMutex();
								startingServer.Dispose();
								startingServer = null;
							}

							await ClientEvents.PushEvent(ClientEvents.GetTabIDs(), "Reload", string.Empty);

							if (!(webServer is null) && await Configuration.SetupConfiguration(webServer))
								ReloadConfigurations = true;

							NeedsCleanup = true;
						}

						DateTime StartConfig = DateTime.Now;

						try
						{
							await Configuration.ConfigureSystem();
						}
						catch (Exception)
						{
							await RepairIfInproperShutdown();

							try
							{
								await Configuration.ConfigureSystem();
							}
							catch (Exception ex)
							{
								Log.Critical(ex);
							}
						}

						if (NeedsCleanup && !(webServer is null))
							await Configuration.CleanupAfterConfiguration(webServer);

						if (ReloadConfigurations)
						{
							Configured = true;

							foreach (SystemConfiguration SystemConfiguration in await Database.Find<SystemConfiguration>())
							{
								string s = SystemConfiguration.GetType().FullName;

								if (!(webServer is null) && SystemConfigurations.TryGetValue(s, out SystemConfiguration OldConfiguration))
									await OldConfiguration.UnregisterSetup(webServer);

								SystemConfigurations[s] = SystemConfiguration;
								SystemConfiguration.SetStaticInstance(SystemConfiguration);

								if (!(webServer is null))
									await SystemConfiguration.InitSetup(webServer);
							}

							foreach (SystemConfiguration SystemConfiguration in SystemConfigurations.Values)
							{
								if (!SystemConfiguration.Complete)
								{
									Configured = false;
									break;
								}
							}

							configurations = new SystemConfiguration[SystemConfigurations.Count];
							SystemConfigurations.Values.CopyTo(configurations, 0);
							Array.Sort(configurations, (c1, c2) => c1.Priority - c2.Priority);

							break;
						}

						if (DateTime.Now.Subtract(StartConfig).TotalSeconds > 2)
							await ClientEvents.PushEvent(ClientEvents.GetTabIDs(), "Reload", string.Empty);
					}
				}
				while (ReloadConfigurations);

				configuring = false;

				if (!(webServer is null))
				{
					webServer.ResourceOverride = "/Starting.md";
					await ClientEvents.PushEvent(ClientEvents.GetTabIDs(), "Reload", string.Empty);

					if (!(SetupResources is null))
					{
						foreach (HttpResource Resource in SetupResources)
							webServer.Unregister(Resource);
					}

					webServer.ConfigureMutualTls(ClientCertificates, TrustClientCertificates, PortSpecificMTlsSettings, true);
					webServer.NetworkChanged();

					webServer.AddHttpPorts(GetConfigPorts("HTTP"));

					if (!(certificate is null))
					{
						webServer.AddHttpsPorts(GetConfigPorts("HTTPS"));
						webServer.UpdateCertificate(certificate);
					}
				}
				else
				{
					if (!(certificate is null))
					{
						webServer = new HttpServer(GetConfigPorts("HTTP"), GetConfigPorts("HTTPS"), certificate, true,
							ClientCertificates, TrustClientCertificates, PortSpecificMTlsSettings, true);
					}
					else
						webServer = new HttpServer(GetConfigPorts("HTTP"), null, null);

					webServer.Register("/Starting.md", StartingMd);
					webServer.ResourceOverride = "/Starting.md";
					webServer.LoginAuditor = loginAuditor;

					webServer.CustomError += WebServer_CustomError;

					foreach (SystemConfiguration Configuration in configurations)
					{
						try
						{
							await Configuration.InitSetup(webServer);
						}
						catch (Exception)
						{
							await RepairIfInproperShutdown();

							try
							{
								await Configuration.InitSetup(webServer);
							}
							catch (Exception ex)
							{
								Log.Critical(ex);
							}
						}
					}
				}

				Types.SetModuleParameter("HTTP", webServer);
				Types.SetModuleParameter("X509", certificate);

				await WriteWebServerOpenPorts();
				webServer.OnNetworkChanged += async (sender, e) =>
				{
					try
					{
						await WriteWebServerOpenPorts();
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
					}
				};

				webServer.Register(new HttpFolderResource("/Graphics", Path.Combine(appDataFolder, "Graphics"), false, false, true, false, HostDomainOptions.SameForAllDomains)); // TODO: Add authentication mechanisms for PUT & DELETE.
				webServer.Register(new HttpFolderResource("/Transforms", Path.Combine(appDataFolder, "Transforms"), false, false, true, false, HostDomainOptions.SameForAllDomains)); // TODO: Add authentication mechanisms for PUT & DELETE.
				webServer.Register(new HttpFolderResource("/highlight", "Highlight", false, false, true, false, HostDomainOptions.SameForAllDomains));   // Syntax highlighting library, provided by http://highlightjs.org
				webServer.Register(root = new HttpFolderResource(string.Empty, rootFolder, false, false, true, true, HostDomainOptions.UseDomainSubfolders));    // TODO: Add authentication mechanisms for PUT & DELETE.
				webServer.Register(httpxProxy = new HttpxProxy("/HttpxProxy", xmppClient, MaxChunkSize));
				webServer.Register("/", GoToDefaultPage);
				webServer.Register(new HttpConfigurableFileResource("/robots.txt", Path.Combine(rootFolder, "robots.txt"), PlainTextCodec.DefaultContentType));
				webServer.Register(new HttpConfigurableFileResource("/favicon.ico", Path.Combine(rootFolder, "favicon.ico"), ImageCodec.ContentTypeIcon));
				webServer.Register(new ClientEvents());
				webServer.Register(new ClientEventsWebSocket());
				webServer.Register(new Login());
				webServer.Register(new Logout());
				webServer.Register(new ProposeContract());

				if (emoji1_24x24 is null)
				{
					emoji1_24x24 = new Emoji1LocalFiles(Emoji1SourceFileType.Svg, 24, 24, "/Graphics/Emoji1/svg/%FILENAME%",
						Path.Combine(runtimeFolder, "Graphics", "Emoji1.zip"), Path.Combine(appDataFolder, "Graphics"));

					MarkdownToHtmlConverter.EmojiSource = emoji1_24x24;
					MarkdownToHtmlConverter.RootFolder = rootFolder;
				}

				root.AllowTypeConversion();

				XmlElement DefaultHttpResponseHeaders = Config.DocumentElement["DefaultHttpResponseHeaders"];
				if (!(DefaultHttpResponseHeaders is null))
				{
					foreach (XmlNode N in DefaultHttpResponseHeaders.ChildNodes)
					{
						if (N is XmlElement E && E.LocalName == "DefaultHttpResponseHeader")
						{
							string HeaderKey = XML.Attribute(E, "key");
							string HeaderValue = XML.Attribute(E, "value");

							root.AddDefaultResponseHeader(HeaderKey, HeaderValue);
						}
					}
				}

				XmlElement FileFolders = Config.DocumentElement["FileFolders"];
				if (!(FileFolders is null))
				{
					foreach (XmlNode N in FileFolders.ChildNodes)
					{
						if (N is XmlElement E && E.LocalName == "FileFolder")
						{
							string WebFolder = XML.Attribute(E, "webFolder");
							string FolderPath = XML.Attribute(E, "folderPath");

							HttpFolderResource FileFolder = new HttpFolderResource(WebFolder, FolderPath, false, false, true, true, HostDomainOptions.SameForAllDomains);
							webServer.Register(FileFolder);

							foreach (XmlNode N2 in E.ChildNodes)
							{
								if (N2 is XmlElement E2 && E2.LocalName == "DefaultHttpResponseHeader")
								{
									string HeaderKey = XML.Attribute(E2, "key");
									string HeaderValue = XML.Attribute(E2, "value");

									FileFolder.AddDefaultResponseHeader(HeaderKey, HeaderValue);
								}
							}
						}
					}
				}

				XmlElement VanityResources = Config.DocumentElement["VanityResources"];
				if (!(VanityResources is null))
				{
					foreach (XmlNode N in VanityResources.ChildNodes)
					{
						if (N is XmlElement E && E.LocalName == "VanityResource")
						{
							string RegEx = XML.Attribute(E, "regex");
							string Url = XML.Attribute(E, "url");

							try
							{
								webServer.RegisterVanityResource(RegEx, Url);
							}
							catch (Exception ex)
							{
								Log.Error("Unable to register vanity resource: " + ex.Message,
									new KeyValuePair<string, object>("RegEx", RegEx),
									new KeyValuePair<string, object>("Url", Url));
							}
						}
					}
				}

				XmlElement Redirections = Config.DocumentElement["Redirections"];
				if (!(Redirections is null))
				{
					foreach (XmlNode N in Redirections.ChildNodes)
					{
						if (N is XmlElement E && E.LocalName == "Redirection")
						{
							string Resource = XML.Attribute(E, "resource");
							string Location = XML.Attribute(E, "location");
							bool IncludeSubPaths = XML.Attribute(E, "includeSubPaths", false);
							bool Permanent = XML.Attribute(E, "permanent", false);

							try
							{
								webServer.Register(new HttpRedirectionResource(Resource, Location, IncludeSubPaths, Permanent));
							}
							catch (Exception ex)
							{
								Log.Error("Unable to register redirection: " + ex.Message,
									new KeyValuePair<string, object>("Resource", Resource),
									new KeyValuePair<string, object>("Location", Location),
									new KeyValuePair<string, object>("IncludeSubPaths", IncludeSubPaths),
									new KeyValuePair<string, object>("Permanent", Permanent));
							}
						}
					}
				}

				XmlElement ReverseProxy = Config.DocumentElement["ReverseProxy"];
				if (!(ReverseProxy is null))
				{
					foreach (XmlNode N in ReverseProxy.ChildNodes)
					{
						if (N is XmlElement E && E.LocalName == "ProxyResource")
						{
							string LocalResource = XML.Attribute(E, "localResource");
							string RemoteDomain = XML.Attribute(E, "remoteDomain");
							string RemoteFolder = XML.Attribute(E, "remoteFolder");
							bool Encrypted = XML.Attribute(E, "encrypted", false);
							int RemotePort = XML.Attribute(E, "remotePort", Encrypted ? HttpServer.DefaultHttpsPort : HttpServer.DefaultHttpPort);
							bool UseSession = XML.Attribute(E, "useSession", false);
							int TimeoutMs = XML.Attribute(E, "timeoutMs", 10000);

							try
							{
								webServer.Register(new HttpReverseProxyResource(LocalResource, RemoteDomain, RemotePort, RemoteFolder, Encrypted,
									TimeSpan.FromMilliseconds(TimeoutMs), UseSession));
							}
							catch (Exception ex)
							{
								Log.Error("Unable to register reverse proxy: " + ex.Message,
									new KeyValuePair<string, object>("LocalResource", LocalResource),
									new KeyValuePair<string, object>("RemoteDomain", RemoteDomain),
									new KeyValuePair<string, object>("Encrypted", Encrypted),
									new KeyValuePair<string, object>("RemotePort", RemotePort),
									new KeyValuePair<string, object>("RemoteFolder", RemoteFolder),
									new KeyValuePair<string, object>("UseSession", UseSession),
									new KeyValuePair<string, object>("TimeoutMs", TimeoutMs));
							}
						}
					}
				}

				await LoadScriptResources();

				httpxServer = new HttpxServer(xmppClient, webServer, MaxChunkSize);
				Types.SetModuleParameter("HTTPX", httpxProxy);
				Types.SetModuleParameter("HTTPXS", httpxServer);

				httpxProxy.IbbClient = ibbClient;
				httpxServer.IbbClient = ibbClient;

				httpxProxy.Socks5Proxy = socksProxy;
				httpxServer.Socks5Proxy = socksProxy;

				if (xmppCredentials.Sniffer)
				{
					ISniffer Sniffer;

					Sniffer = new XmlFileSniffer(appDataFolder + "HTTP" + Path.DirectorySeparatorChar +
						"HTTP Log %YEAR%-%MONTH%-%DAY%T%HOUR%.xml",
						appDataFolder + "Transforms" + Path.DirectorySeparatorChar + "SnifferXmlToHtml.xslt",
						7, BinaryPresentationMethod.ByteCount);
					webServer.Add(Sniffer);
				}

				await ClientEvents.PushEvent(ClientEvents.GetTabIDs(), "Reload", string.Empty);

				try
				{
					coapEndpoint = new CoapEndpoint();
					Types.SetModuleParameter("CoAP", coapEndpoint);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}

				IDataSource[] Sources;

				try
				{
					Sources = new IDataSource[] { new MeteringTopology() };
				}
				catch (Exception)
				{
					await RepairIfInproperShutdown();

					try
					{
						Sources = new IDataSource[] { new MeteringTopology() };
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
						Sources = new IDataSource[0];
					}
				}

				Types.GetLoadedModules();   // Makes sure all modules are instantiated, allowing static constructors to add
											// appropriate data sources, if necessary.

				if (!(GetDataSources is null))
					Sources = await GetDataSources(Sources);

				concentratorServer = new ConcentratorServer(xmppClient, thingRegistryClient, provisioningClient, Sources);
				avatarClient = new Networking.XMPP.Avatar.AvatarClient(xmppClient, pepClient);

				Types.SetModuleParameter("Concentrator", concentratorServer);
				Types.SetModuleParameter("Sources", concentratorServer.DataSources);
				Types.SetModuleParameter("DefaultSource", MeteringTopology.SourceID);
				Types.SetModuleParameter("Sensor", concentratorServer.SensorServer);
				Types.SetModuleParameter("Control", concentratorServer.ControlServer);
				Types.SetModuleParameter("Registry", thingRegistryClient);
				Types.SetModuleParameter("Provisioning", provisioningClient);
				Types.SetModuleParameter("Avatar", avatarClient);
				Types.SetModuleParameter("Scheduler", scheduler);

				if (FirstStart)
				{
					MeteringTopology.OnNewMomentaryValues += NewMomentaryValues;
					ProvisionedMeteringNode.QrCodeUrlRequested += ProvisionedMeteringNode_QrCodeUrlRequested;
				}

				DeleteOldDataSourceEvents(null);

				try
				{
					string BinaryFolder = AppDomain.CurrentDomain.BaseDirectory;
					string[] LanguageFiles = Directory.GetFiles(BinaryFolder, "*.lng", SearchOption.AllDirectories);
					string FileName;

					if (LanguageFiles.Length > 0)
					{
						XmlSchema Schema = XSL.LoadSchema(Translator.SchemaResource, typeof(Translator).Assembly);

						foreach (string LanguageFile in LanguageFiles)
						{
							string Xml = null;

							try
							{
								FileName = LanguageFile;
								if (FileName.StartsWith(BinaryFolder))
									FileName = FileName.Substring(BinaryFolder.Length);

								DateTime LastWriteTime = File.GetLastWriteTime(LanguageFile);
								DateTime LastImportedTime = await RuntimeSettings.GetAsync(FileName, DateTime.MinValue);

								if (LastWriteTime > LastImportedTime)
								{
									Log.Informational("Importing language file.", FileName);

									Xml = await Resources.ReadAllTextAsync(LanguageFile);
									XmlDocument Doc = new XmlDocument()
									{
										PreserveWhitespace = true
									};
									Doc.LoadXml(Xml);

									XSL.Validate(FileName, Doc, Translator.SchemaRoot, Translator.SchemaNamespace, Schema);

									using (XmlReader r = new XmlNodeReader(Doc))
									{
										await Translator.ImportAsync(r);
									}

									RuntimeSettings.Set(FileName, LastWriteTime);
								}
							}
							catch (XmlException ex)
							{
								ex = XML.AnnotateException(ex, Xml);
								Log.Critical(ex, LanguageFile);
							}
							catch (Exception ex)
							{
								Log.Critical(ex, LanguageFile);
							}
						}
					}

					foreach (string UnhandledException in Directory.GetFiles(appDataFolder, "UnhandledException*.txt", SearchOption.TopDirectoryOnly))
					{
						try
						{
							string Msg = await Resources.ReadAllTextAsync(UnhandledException);
							File.Delete(UnhandledException);

							StringBuilder sb = new StringBuilder();

							sb.AppendLine("Unhandled Exception");
							sb.AppendLine("=======================");
							sb.AppendLine();
							sb.AppendLine("```");
							sb.AppendLine(Msg);
							sb.AppendLine("```");

							Log.Alert(sb.ToString());
						}
						catch (Exception ex)
						{
							Log.Critical(ex, UnhandledException);
						}
					}

					if (await Types.StartAllModules(int.MaxValue, new ModuleStartOrder()))
					{
						Log.Informational("Server started.");
						await ProcessServiceConfigurations(false);
					}
					else
						Log.Critical("Unable to start all modules.");

					if (!(NewConfigurations is null))
					{
						foreach (SystemConfiguration Configuration in NewConfigurations)
						{
							StringBuilder sb = new StringBuilder();

							sb.AppendLine("New System Configuration");
							sb.AppendLine("=============================");
							sb.AppendLine();
							sb.AppendLine("A new system configuration is available.");
							sb.AppendLine("It has been set to simplified configuration, to not stop processing.");
							sb.AppendLine("You should review the configuration however, as soon as possible.");
							sb.AppendLine();
							sb.Append("[Click here to review the new system configuration](http");

							if (DomainConfiguration.Instance.UseEncryption && !string.IsNullOrEmpty(DomainConfiguration.Instance.Domain))
								sb.Append('s');

							sb.Append("://");
							sb.Append(DomainConfiguration.Instance.Domain);
							sb.Append(Configuration.Resource);
							sb.AppendLine(").");

							Log.Alert(sb.ToString());
						}
					}
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
				finally
				{
					if (!(webServer is null))
					{
						webServer.ResourceOverride = null;
						webServer.ResourceOverrideFilter = null;
					}

					if (!(startingServer is null))
					{
						await startingServer.ReleaseMutex();
						startingServer.Dispose();
						startingServer = null;
					}

					if (xmppClient.State != XmppState.Connected)
						xmppClient.Connect();
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);

				if (!(startingServer is null))
				{
					await startingServer.ReleaseMutex();
					startingServer.Dispose();
					startingServer = null;
				}

				if (!(gatewayRunning is null))
				{
					await gatewayRunning.ReleaseMutex();
					gatewayRunning.Dispose();
					gatewayRunning = null;
				}

				ExceptionDispatchInfo.Capture(ex).Throw();
			}

			return true;
		}

		private static void ProvisionedMeteringNode_QrCodeUrlRequested(object sender, GetQrCodeUrlEventArgs e)
		{
			StringBuilder Link = new StringBuilder();
			Link.Append("https://");

			if (string.IsNullOrEmpty(Domain))
				Link.Append(XmppClient.Domain);
			else
				Link.Append(Domain);

			Link.Append("/QR/");
			Link.Append(WebUtility.UrlEncode(e.Text));

			e.Url = Link.ToString();
		}

		private static async Task StartingMd(HttpRequest Request, HttpResponse Response)
		{
			if (string.IsNullOrEmpty(webServer?.ResourceOverride))
				throw new TemporaryRedirectException("/");

			string Markdown = await Resources.ReadAllTextAsync(Path.Combine(rootFolder, "Starting.md"));
			Variables v = Request.Session ?? new Variables();
			MarkdownSettings Settings = new MarkdownSettings(emoji1_24x24, true, v);
			MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown, Settings);
			string Html = await Doc.GenerateHTML();

			Response.ContentType = "text/html; charset=utf-8";
			await Response.Write(System.Text.Encoding.UTF8.GetBytes(Html));
			await Response.SendResponse();
		}

		private static Task GoToDefaultPage(HttpRequest Request, HttpResponse Response)
		{
			if (TryGetDefaultPage(Request, out string DefaultPage))
				throw new TemporaryRedirectException(DefaultPage);
			else
				throw new NotFoundException("No default page defined.");
		}

		private class ModuleStartOrder : IComparer<IModule>
		{
			private readonly DependencyOrder dependencyOrder = new DependencyOrder();

			public int Compare(IModule x, IModule y)
			{
				int c1 = this.ModuleCategory(x);
				int c2 = this.ModuleCategory(y);

				int i = c1 - c2;
				if (i != 0)
					return i;

				return this.dependencyOrder.Compare(x, y);
			}

			private int ModuleCategory(IModule x)
			{
				if (x is Persistence.LifeCycle.DatabaseModule)
					return 1;
				else if (x is Runtime.Transactions.TransactionModule)
					return 2;
				else
					return 3;
			}
		}

		private static async Task RepairIfInproperShutdown()
		{
			IDatabaseProvider DatabaseProvider = Database.Provider;
			Type ProviderType = DatabaseProvider.GetType();
			PropertyInfo AutoRepairReportFolder = ProviderType.GetProperty("AutoRepairReportFolder");
			AutoRepairReportFolder?.SetValue(DatabaseProvider, Path.Combine(AppDataFolder, "Backup"));

			MethodInfo MI = ProviderType.GetMethod("RepairIfInproperShutdown", new Type[] { typeof(string) });

			if (!(MI is null))
			{
				Task T = MI.Invoke(DatabaseProvider, new object[] { AppDataFolder + "Transforms" + Path.DirectorySeparatorChar + "DbStatXmlToHtml.xslt" }) as Task;

				if (T is Task<string[]> StringArrayTask)
					DatabaseConfiguration.RepairedCollections = await StringArrayTask;
				else if (!(T is null))
					await T;
			}
		}

		private static async Task WriteWebServerOpenPorts()
		{
			StringBuilder sb = new StringBuilder();

			foreach (int Port in webServer.OpenPorts)
				sb.AppendLine(Port.ToString());

			try
			{
				await Resources.WriteAllTextAsync(appDataFolder + "Ports.txt", sb.ToString());
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private static void Assert_UnauthorizedAccess(object Sender, UnauthorizedAccessEventArgs e)
		{
			DateTime Now = DateTime.Now;
			string Key = e.Trace.ToString();

			lock (lastUnauthorizedAccess)
			{
				if (lastUnauthorizedAccess.TryGetValue(Key, out DateTime TP) && (Now - TP).TotalHours < 1)
					return;

				lastUnauthorizedAccess[Key] = Now;
			}

			StringBuilder Markdown = new StringBuilder();

			Markdown.AppendLine("Unauthorized access detected and prevented.");
			Markdown.AppendLine("===============================================");
			Markdown.AppendLine();
			Markdown.AppendLine("| Details ||");
			Markdown.AppendLine("|:---|:---|");
			Markdown.Append("| Method | `");
			Markdown.Append(e.Method.Name);
			Markdown.AppendLine("` |");
			Markdown.Append("| Type | `");
			Markdown.Append(e.Type.FullName);
			Markdown.AppendLine("` |");
			Markdown.Append("| Assembly | `");
			Markdown.Append(e.Assembly.GetName().Name);
			Markdown.AppendLine("` |");
			Markdown.Append("| Date | ");
			Markdown.Append(MarkdownDocument.Encode(Now.ToShortDateString()));
			Markdown.AppendLine(" |");
			Markdown.Append("| Time | ");
			Markdown.Append(MarkdownDocument.Encode(Now.ToLongTimeString()));
			Markdown.AppendLine(" |");
			Markdown.AppendLine();
			Markdown.AppendLine("Stack Trace:");
			Markdown.AppendLine();
			Markdown.AppendLine("```");
			Markdown.AppendLine(Log.CleanStackTrace(e.Trace.ToString()));
			Markdown.AppendLine("```");

			SendNotification(Markdown.ToString());
		}

		private static void CheckContentFiles(string ManifestFileName, Dictionary<string, CopyOptions> ContentOptions)
		{
			try
			{
				XmlDocument Doc = new XmlDocument()
				{
					PreserveWhitespace = true
				};
				Doc.Load(ManifestFileName);

				if (!(Doc.DocumentElement is null) &&
					Doc.DocumentElement.LocalName == "Module" &&
					Doc.DocumentElement.NamespaceURI == "http://waher.se/Schema/ModuleManifest.xsd")
				{
					CheckContentFiles(Doc.DocumentElement, runtimeFolder, runtimeFolder, appDataFolder, ContentOptions);
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex, ManifestFileName);
			}
		}

		private enum CopyOptions
		{
			IfNewer,
			Always
		}

		private static void CheckContentFiles(XmlElement Element, string RuntimeFolder, string RuntimeSubfolder, string AppDataSubFolder,
			Dictionary<string, CopyOptions> ContentOptions)
		{
			bool AppDataFolderChecked = false;

			foreach (XmlNode N in Element.ChildNodes)
			{
				if (N is XmlElement E)
				{
					switch (E.LocalName)
					{
						case "Folder":
							string Name = XML.Attribute(E, "name");
							CheckContentFiles(E, RuntimeFolder, Path.Combine(RuntimeSubfolder, Name), Path.Combine(AppDataSubFolder, Name),
								ContentOptions);
							break;

						case "Content":
							Name = XML.Attribute(E, "fileName");
							CopyOptions CopyOptions = XML.Attribute(E, "copy", CopyOptions.IfNewer);

							string s = Path.Combine(RuntimeSubfolder, Name);
							if (!File.Exists(s))
							{
								s = Path.Combine(RuntimeFolder, Name);
								if (!File.Exists(s))
									break;
							}

							if (!AppDataFolderChecked)
							{
								AppDataFolderChecked = true;

								if (!Directory.Exists(AppDataSubFolder))
									Directory.CreateDirectory(AppDataSubFolder);
							}

							string s2 = Path.Combine(AppDataSubFolder, Name);

							if (CopyOptions == CopyOptions.Always || !File.Exists(s2))
							{
								File.Copy(s, s2, true);
								ContentOptions[s2] = CopyOptions;
							}
							else
							{
								DateTime TP = File.GetLastWriteTime(s);
								DateTime TP2 = File.GetLastWriteTime(s2);

								if (TP > TP2 &&
									(!ContentOptions.TryGetValue(s2, out CopyOptions CopyOptions2) ||
									CopyOptions2 != CopyOptions.Always))
								{
									File.Copy(s, s2, true);
									ContentOptions[s2] = CopyOptions;
								}
							}
							break;
					}
				}
			}
		}

		private static void CheckInstallUtilityFiles(string ManifestFileName)
		{
			try
			{
				XmlDocument Doc = new XmlDocument()
				{
					PreserveWhitespace = true
				};
				Doc.Load(ManifestFileName);

				if (!(Doc.DocumentElement is null) &&
					Doc.DocumentElement.LocalName == "Module" &&
					Doc.DocumentElement.NamespaceURI == "http://waher.se/Schema/ModuleManifest.xsd")
				{
					string InstallUtilityFolder = Path.Combine(runtimeFolder, "InstallUtility");
					bool NoticeLogged = false;

					if (!Directory.Exists(InstallUtilityFolder))
						Directory.CreateDirectory(InstallUtilityFolder);

					foreach (XmlNode N in Doc.DocumentElement.ChildNodes)
					{
						if (N is XmlElement E)
						{
							switch (E.LocalName)
							{
								case "Assembly":
									string Name = XML.Attribute(E, "fileName");
									CopyOptions CopyOptions = XML.Attribute(E, "copy", CopyOptions.IfNewer);

									string s = Path.Combine(runtimeFolder, Name);
									if (!File.Exists(s))
										break;

									string s2 = Path.Combine(InstallUtilityFolder, Name);

									if (CopyOptions == CopyOptions.IfNewer && File.Exists(s2))
									{
										DateTime TP = File.GetLastWriteTime(s);
										DateTime TP2 = File.GetLastWriteTime(s2);

										if (TP <= TP2)
											break;
									}

									if (!NoticeLogged)
									{
										NoticeLogged = true;
										Log.Notice("Copying Installation Utility executable files to InstallUtility subfolder.");
									}

									File.Copy(s, s2, true);
									break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex, ManifestFileName);
			}
		}

		internal static bool ConsoleOutput => consoleOutput;

		internal static async Task ConfigureXmpp(XmppConfiguration Configuration)
		{
			xmppCredentials = Configuration.GetCredentials();
			xmppClient = new XmppClient(xmppCredentials, "en", typeof(Gateway).Assembly);
			xmppClient.OnValidateSender += XmppClient_OnValidateSender;
			Types.SetModuleParameter("XMPP", xmppClient);

			if (xmppCredentials.Sniffer)
			{
				ISniffer Sniffer;

				if (consoleOutput)
				{
					Sniffer = new ConsoleOutSniffer(BinaryPresentationMethod.ByteCount, LineEnding.PadWithSpaces);
					xmppClient.Add(Sniffer);
				}

				Sniffer = new XmlFileSniffer(appDataFolder + "XMPP" + Path.DirectorySeparatorChar +
					"XMPP Log %YEAR%-%MONTH%-%DAY%T%HOUR%.xml",
					appDataFolder + "Transforms" + Path.DirectorySeparatorChar + "SnifferXmlToHtml.xslt",
					7, BinaryPresentationMethod.ByteCount);
				xmppClient.Add(Sniffer);
			}

			if (!string.IsNullOrEmpty(xmppCredentials.Events))
			{
				string s = xmppCredentials.Events;
				int i = s.IndexOf('.');
				if (i > 0)
					s = s.Substring(i + 1);

				if (!IsDomain(s, true))
				{
					Log.Register(new EventFilter("XMPP Event Filter",
						new XmppEventSink("XMPP Event Sink", xmppClient, xmppCredentials.Events, false),
						EventType.Error));
				}
			}

			if (!string.IsNullOrEmpty(xmppCredentials.ThingRegistry))
			{
				thingRegistryClient = new ThingRegistryClient(xmppClient, xmppCredentials.ThingRegistry);
				thingRegistryClient.Claimed += ThingRegistryClient_Claimed;
				thingRegistryClient.Disowned += ThingRegistryClient_Disowned;
				thingRegistryClient.Removed += ThingRegistryClient_Removed;
			}

			if (!string.IsNullOrEmpty(xmppCredentials.Provisioning))
				provisioningClient = new ProvisioningClient(xmppClient, xmppCredentials.Provisioning);
			else
				provisioningClient = null;

			scheduler.Add(DateTime.Now.AddMinutes(1), CheckConnection, null);

			xmppClient.OnStateChanged += XmppClient_OnStateChanged;

			ibbClient = new Networking.XMPP.InBandBytestreams.IbbClient(xmppClient, MaxChunkSize);
			Types.SetModuleParameter("IBB", ibbClient);

			socksProxy = new Socks5Proxy(xmppClient);
			Types.SetModuleParameter("SOCKS5", socksProxy);

			sensorClient = new SensorClient(xmppClient);
			controlClient = new ControlClient(xmppClient);
			concentratorClient = new ConcentratorClient(xmppClient);
			synchronizationClient = new SynchronizationClient(xmppClient);
			pepClient = new PepClient(xmppClient, XmppConfiguration.Instance.PubSub);

			if (!string.IsNullOrEmpty(XmppConfiguration.Instance.LegalIdentities))
			{
				contractsClient = new ContractsClient(xmppClient, XmppConfiguration.Instance.LegalIdentities);
				contractsClient.SetKeySettingsInstance(string.Empty, true);

				await contractsClient.LoadKeys(true);
			}
			else
				contractsClient = null;

			if (!string.IsNullOrEmpty(XmppConfiguration.Instance.MultiUserChat))
				mucClient = new MultiUserChatClient(xmppClient, XmppConfiguration.Instance.MultiUserChat);
			else
				mucClient = null;

			if (!string.IsNullOrEmpty(XmppConfiguration.Instance.SoftwareUpdates))
			{
				string PackagesFolder = Path.Combine(appDataFolder, "Packages");
				if (!Directory.Exists(PackagesFolder))
					Directory.CreateDirectory(PackagesFolder);

				softwareUpdateClient = new SoftwareUpdateClient(xmppClient, XmppConfiguration.Instance.SoftwareUpdates, PackagesFolder);
			}
			else
				softwareUpdateClient = null;

			mailClient = new MailClient(xmppClient);
			mailClient.MailReceived += MailClient_MailReceived;
		}

		internal static async Task ConfigureDomain(DomainConfiguration Configuration)
		{
			int i, c = Configuration.AlternativeDomains?.Length ?? 0;

			domain = Configuration.Domain;
			alternativeDomains = new CaseInsensitiveString[c];

			for (i = 0; i < c; i++)
				alternativeDomains[i] = Configuration.AlternativeDomains[i];

			if (Configuration.UseDomainName)
			{
				if (Configuration.DynamicDns)
				{
					await Configuration.CheckDynamicIp();

					if (Configuration.DynDnsInterval > 0)
					{
						if (checkIp > DateTime.MinValue)
						{
							scheduler.Remove(checkIp);
							checkIp = DateTime.MinValue;
						}

						checkIp = scheduler.Add(DateTime.Now.AddSeconds(Configuration.DynDnsInterval), CheckIp, Configuration);
					}
				}

				if (Configuration.UseEncryption &&
					!(Configuration.Certificate is null) &&
					!(Configuration.PrivateKey is null))
				{
					UpdateCertificate(Configuration);

					if (checkCertificate > DateTime.MinValue)
					{
						scheduler.Remove(checkCertificate);
						checkCertificate = DateTime.MinValue;
					}

					checkCertificate = scheduler.Add(DateTime.Now.AddHours(0.5 + NextDouble()), CheckCertificate, Configuration);
				}
				else
					certificate = null;
			}
			else
				certificate = null;

			if (!Users.HashMethodLocked)
				Users.Register(ComputeUserPasswordHash, "DIGEST-SHA3-256", LoginAuditor, domain, true);

			await Privileges.LoadAll();
			await Roles.LoadAll();
		}

		/// <summary>
		/// Computes a hash digest based on a user name and a password, and the current domain.
		/// </summary>
		/// <param name="UserName">User Name</param>
		/// <param name="Password">Password</param>
		/// <returns>Hash Digest</returns>
		public static byte[] ComputeUserPasswordHash(string UserName, string Password)
		{
			SHA3_256 H = new SHA3_256();
			return H.ComputeVariable(System.Text.Encoding.UTF8.GetBytes(UserName + ":" + domain + ":" + Password));
		}

		internal static bool UpdateCertificate(DomainConfiguration Configuration)
		{
			try
			{
				if (!(Configuration.PFX is null))
					certificate = new X509Certificate2(Configuration.PFX, Configuration.Password);
				else
				{
					RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
					RSA.ImportCspBlob(Configuration.PrivateKey);

					certificate = new X509Certificate2(Configuration.Certificate)
					{
						PrivateKey = RSA
					};
				}

				try
				{
					webServer?.UpdateCertificate(Certificate);
					OnNewCertificate?.Invoke(typeof(Gateway), new Events.CertificateEventArgs(certificate));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}

			return false;
		}

		private static async void CheckCertificate(object P)
		{
			DomainConfiguration Configuration = (DomainConfiguration)P;
			DateTime Now = DateTime.Now;

			try
			{
				if (Now.AddDays(50) >= certificate.NotAfter)
				{
					Log.Notice("Updating certificate");

					if (await Configuration.CreateCertificate())
					{
						Log.Notice("Certificate created.");
						if (!UpdateCertificate(Configuration))
							Log.Error("Unable to update gatetway with new certificate.");
					}
					else
					{
						int DaysLeft = (int)Math.Round((certificate.NotAfter - Now.Date).TotalDays);

						if (DaysLeft < 2)
							Log.Emergency("Unable to generate new certificate.", domain);
						else if (DaysLeft < 5)
							Log.Alert("Unable to generate new certificate.", domain);
						else if (DaysLeft < 10)
							Log.Critical("Unable to generate new certificate.", domain);
						else if (DaysLeft < 20)
							Log.Error("Unable to generate new certificate.", domain);
						else
							Log.Warning("Unable to generate new certificate.", domain);
					}
				}
			}
			catch (Exception ex)
			{
				int DaysLeft = (int)Math.Round((certificate.NotAfter - Now.Date).TotalDays);

				if (DaysLeft < 2)
					Log.Emergency(ex, domain);
				else if (DaysLeft < 5)
					Log.Alert(ex, domain);
				else
					Log.Critical(ex);
			}
			finally
			{
				checkCertificate = scheduler.Add(DateTime.Now.AddDays(0.5 + NextDouble()), CheckCertificate, Configuration);
			}
		}

		/// <summary>
		/// Event raised when a new server certificate has been generated.
		/// </summary>
		public static event CertificateEventHandler OnNewCertificate = null;

		private static async void CheckIp(object P)
		{
			DomainConfiguration Configuration = (DomainConfiguration)P;

			try
			{
				await Configuration.CheckDynamicIp();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
			finally
			{
				checkIp = scheduler.Add(DateTime.Now.AddSeconds(Configuration.DynDnsInterval), CheckIp, Configuration);
			}
		}

		private static async void DeleteOldDataSourceEvents(object P)
		{
			try
			{
				await MeteringTopology.DeleteOldEvents(TimeSpan.FromDays(7));
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
			finally
			{
				ScheduleEvent(DeleteOldDataSourceEvents, DateTime.Today.AddDays(1).AddHours(4), null);
			}
		}

		/// <summary>
		/// Event raised when the Gateway requires its database provider from the host.
		/// </summary>
		public static event GetDatabaseProviderEventHandler GetDatabaseProvider = null;

		/// <summary>
		/// Event raised when the Gateway requires a set of data sources to publish.
		/// </summary>
		public static event GetDataSources GetDataSources = null;

		/// <summary>
		/// Initializes the inventory engine by loading available assemblies in the installation folder (top directory only).
		/// </summary>
		private static void Initialize()
		{
			string Folder = Assembly.GetExecutingAssembly().Location;
			if (string.IsNullOrEmpty(Folder))
				Folder = AppDomain.CurrentDomain.BaseDirectory;

			runtimeFolder = Path.GetDirectoryName(Folder);

			Directory.SetCurrentDirectory(runtimeFolder);

			TypesLoader.Initialize(runtimeFolder, (FileName) =>
			{
				FileName = Path.GetFileName(FileName).ToLower();
				if (FileName.StartsWith("api-ms-win-") ||
					FileName.StartsWith("system.") ||
					FileName.StartsWith("microsoft.") ||
					FileName.StartsWith("windows.") ||
					FileName.StartsWith("waher.client.") ||
					FileName.StartsWith("waher.utility.") ||
					FileName.StartsWith("mscordaccore_x86_x86_") ||
					FileName.StartsWith("sos_x86_x86_"))
				{
					return false;
				}

				switch (FileName)
				{
					case "clrcompression.dll":
					case "clretwrc.dll":
					case "clrjit.dll":
					case "coreclr.dll":
					case "dbgshim.dll":
					case "hostpolicy.dll":
					case "hostfxr.dll":
					case "libegl.dll":
					case "libglesv2.dll":
					case "libskiasharp.dll":
					case "libzstd.dll":
					case "mongocrypt.dll":
					case "mscordaccore.dll":
					case "mscordbi.dll":
					case "mscorlib.dll":
					case "mscorrc.debug.dll":
					case "mscorrc.dll":
					case "msquic.dll":
					case "netstandard.dll":
					case "snappy32.dll":
					case "snappy64.dll":
					case "snappier.dll":
					case "sni.dll":
					case "sos.dll":
					case "sos.netcore.dll":
					case "ucrtbase.dll":
					case "windowsbase.dll":
					case "waher.persistence.fileslw.dll":
					case "waher.persistence.serialization.dll":
					case "zstdsharp.dll":
						return false;
				}

				return true;
			});
		}

		private static bool CopyFile(string From, string To, bool OnlyIfNewer)
		{
			if (From == To)
				return false;

			if (!File.Exists(From))
				return false;

			if (OnlyIfNewer && File.Exists(To))
			{
				DateTime ToTP = File.GetLastWriteTimeUtc(To);
				DateTime FromTP = File.GetLastWriteTimeUtc(From);

				if (ToTP >= FromTP)
					return false;
			}

			File.Copy(From, To, true);

			return true;
		}

		private static void CopyFolder(string From, string To, string Mask, bool OnlyIfNewer)
		{
			if (Directory.Exists(From))
			{
				if (!Directory.Exists(To))
					Directory.CreateDirectory(To);

				string[] Files = Directory.GetFiles(From, Mask, SearchOption.TopDirectoryOnly);

				foreach (string File in Files)
				{
					string FileName = Path.GetFileName(File);
					CopyFile(File, Path.Combine(To, FileName), OnlyIfNewer);
				}
			}
		}

		private static void CopyFolders(string From, string To, bool OnlyIfNewer)
		{
			if (Directory.Exists(From))
			{
				CopyFolder(From, To, "*.*", OnlyIfNewer);

				string[] Folders = Directory.GetDirectories(From, "*.*", SearchOption.TopDirectoryOnly);

				foreach (string Folder in Folders)
				{
					string FolderName = Path.GetFileName(Folder);
					CopyFolders(Folder, Path.Combine(To, FolderName), OnlyIfNewer);
				}
			}
		}

		/// <summary>
		/// Stops the gateway.
		/// </summary>
		public static async Task Stop()
		{
			if (stopped)
			{
				Log.Notice("Request to stop Gateway, but Gateway already stopped.");
				return;
			}

			Log.Informational("Server shutting down.");

			bool StopInternalProvider = !(internalProvider is null) && Database.Provider != internalProvider;

			stopped = true;
			try
			{
				scheduler?.Dispose();
				scheduler = null;

				await Script.Threading.Functions.Background.TerminateTasks(10000);
				await Types.StopAllModules();

				Database.CollectionRepaired -= Database_CollectionRepaired;

				if (StopInternalProvider)
					await internalProvider.Stop();

				if (!(startingServer is null))
				{
					await startingServer.ReleaseMutex();
					startingServer.Dispose();
					startingServer = null;
				}

				if (!(gatewayRunning is null))
				{
					await gatewayRunning.ReleaseMutex();
					gatewayRunning.Dispose();
					gatewayRunning = null;
				}

				if (!(configurations is null))
				{
					foreach (SystemConfiguration Configuration in configurations)
					{
						if (Configuration is IDisposable D)
						{
							try
							{
								D.Dispose();
							}
							catch (Exception ex)
							{
								Log.Critical(ex);
							}
						}
					}

					configurations = null;
				}

				ibbClient?.Dispose();
				ibbClient = null;

				httpxProxy?.Dispose();
				httpxProxy = null;

				httpxServer?.Dispose();
				httpxServer = null;

				provisioningClient?.Dispose();
				provisioningClient = null;

				thingRegistryClient?.Dispose();
				thingRegistryClient = null;

				concentratorServer?.Dispose();
				concentratorServer = null;

				avatarClient?.Dispose();
				avatarClient = null;

				sensorClient?.Dispose();
				sensorClient = null;

				controlClient?.Dispose();
				controlClient = null;

				concentratorClient?.Dispose();
				concentratorClient = null;

				synchronizationClient?.Dispose();
				synchronizationClient = null;

				pepClient?.Dispose();
				pepClient = null;

				mucClient?.Dispose();
				mucClient = null;

				mailClient?.Dispose();
				mailClient = null;

				contractsClient?.Dispose();
				contractsClient = null;

				softwareUpdateClient?.Dispose();
				softwareUpdateClient = null;

				if (!(xmppClient is null))
				{
					using (ManualResetEvent OfflineSent = new ManualResetEvent(false))
					{
						xmppClient.SetPresence(Availability.Offline, (sender, e) => OfflineSent.Set());
						OfflineSent.WaitOne(1000);
					}

					foreach (ISniffer Sniffer in xmppClient.Sniffers)
					{
						XmppClient.Remove(Sniffer);

						if (Sniffer is IDisposable Disposable)
							Disposable.Dispose();
					}

					xmppClient.Dispose();
					xmppClient = null;
				}

				coapEndpoint?.Dispose();
				coapEndpoint = null;

				if (!(webServer is null))
				{
					foreach (ISniffer Sniffer in webServer.Sniffers)
					{
						webServer.Remove(Sniffer);

						if (Sniffer is IDisposable Disposable)
							Disposable.Dispose();
					}

					webServer.Dispose();
					webServer = null;
				}

				root = null;

				if (exportExceptions)
				{
					lock (exceptionFile)
					{
						exportExceptions = false;

						exceptionFile.WriteLine(new string('-', 80));
						exceptionFile.Write("End of export: ");
						exceptionFile.WriteLine(DateTime.Now.ToString());

						exceptionFile.Flush();
						exceptionFile.Close();
					}

					exceptionFile = null;
				}
			}
			finally
			{
				Persistence.LifeCycle.DatabaseModule.Flush().Wait(60000);

				if (StopInternalProvider)
					internalProvider.Flush().Wait(60000);
			}
		}

		/// <summary>
		/// Domain certificate.
		/// </summary>
		public static X509Certificate2 Certificate => certificate;

		/// <summary>
		/// Domain name.
		/// </summary>
		public static CaseInsensitiveString Domain => domain;

		/// <summary>
		/// Alternative domain names
		/// </summary>
		public static CaseInsensitiveString[] AlternativeDomains => alternativeDomains;

		/// <summary>
		/// Name of the current instance. Default instance=<see cref="string.Empty"/>
		/// </summary>
		public static string InstanceName => instance;

		/// <summary>
		/// Application data folder.
		/// </summary>
		public static string AppDataFolder => appDataFolder;

		/// <summary>
		/// Runtime folder.
		/// </summary>
		public static string RuntimeFolder => runtimeFolder;

		/// <summary>
		/// Web root folder.
		/// </summary>
		public static string RootFolder => rootFolder;

		/// <summary>
		/// Root folder resource.
		/// </summary>
		public static HttpFolderResource Root => root;

		/// <summary>
		/// Application Name.
		/// </summary>
		public static string ApplicationName
		{
			get => applicationName;
			internal set => applicationName = value;
		}

		/// <summary>
		/// Emojis.
		/// </summary>
		public static Emoji1LocalFiles Emoji1_24x24 => emoji1_24x24;

		/// <summary>
		/// If the gateway is being configured.
		/// </summary>
		public static bool Configuring => configuring;

		/// <summary>
		/// Local Internal Encrypted Object Database provider.
		/// </summary>
		public static IDatabaseProvider InternalDatabase => internalProvider;

		/// <summary>
		/// Full path to Gateway.config file.
		/// </summary>
		public static string ConfigFilePath => Path.Combine(appDataFolder, GatewayConfigLocalFileName);

		/// <summary>
		/// Gets the port numbers defined for a given protocol in the configuration file.
		/// </summary>
		/// <param name="Protocol">Protocol.</param>
		/// <returns>Defined port numbers.</returns>
		public static int[] GetConfigPorts(string Protocol)
		{
			List<int> Result = new List<int>();

			foreach (KeyValuePair<string, int> P in ports)
			{
				if (P.Key == Protocol)
					Result.Add(P.Value);
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Gets the protocol names defined in the configuration file.
		/// </summary>
		/// <returns>Defined protocol names.</returns>
		public static string[] GetProtocols()
		{
			SortedDictionary<string, bool> Protocols = new SortedDictionary<string, bool>();

			foreach (KeyValuePair<string, int> P in ports)
				Protocols[P.Key] = true;

			string[] Result = new string[Protocols.Count];
			Protocols.Keys.CopyTo(Result, 0);

			return Result;
		}

		/// <summary>
		/// Raises the <see cref="OnTerminate"/> event handler, letting the container executable know the application
		/// needs to close.
		/// </summary>
		/// <exception cref="InvalidOperationException">If no <see cref="OnTerminate"/> event handler has been set.</exception>
		public static void Terminate()
		{
			EventHandler h = OnTerminate ?? throw new InvalidOperationException("No OnTerminate event handler set.");
			try
			{
				h(instance, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Event raised when the <see cref="Terminate"/> method has been called, letting the container exceutable know
		/// the application needs to close.
		/// </summary>
		public static event EventHandler OnTerminate = null;

		/// <summary>
		/// Tries to get the default page of a host.
		/// </summary>
		/// <param name="Request">HTTP Request object.</param>
		/// <param name="DefaultPage">Default page, if found.</param>
		/// <returns>If a default page was found.</returns>
		public static bool TryGetDefaultPage(HttpRequest Request, out string DefaultPage)
		{
			return TryGetDefaultPage(Request.Header.Host?.Value ?? string.Empty, out DefaultPage);
		}

		/// <summary>
		/// Tries to get the default page of a host.
		/// </summary>
		/// <param name="Host">Host name.</param>
		/// <param name="DefaultPage">Default page, if found.</param>
		/// <returns>If a default page was found.</returns>
		public static bool TryGetDefaultPage(string Host, out string DefaultPage)
		{
			if (defaultPageByHostName.TryGetValue(Host, out DefaultPage))
				return true;

			if (Host.StartsWith("www.", StringComparison.CurrentCultureIgnoreCase) && defaultPageByHostName.TryGetValue(Host.Substring(4), out DefaultPage))
				return true;

			if (defaultPageByHostName.TryGetValue(string.Empty, out DefaultPage))
				return true;

			DefaultPage = string.Empty;
			return false;
		}

		internal static void SetDefaultPages(params KeyValuePair<string, string>[] DefaultPages)
		{
			Dictionary<string, string> List = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

			foreach (KeyValuePair<string, string> P in DefaultPages)
				List[P.Key] = P.Value;

			defaultPageByHostName = List;
		}

		#endregion

		#region XMPP

		private static Task XmppClient_OnValidateSender(object Sender, ValidateSenderEventArgs e)
		{
			RosterItem Item;
			string BareJid = e.FromBareJID.ToLower();

			if (string.IsNullOrEmpty(BareJid) ||
				(!(xmppClient is null) &&
				(BareJid == xmppClient.Domain.ToLower() ||
				BareJid == xmppClient.BareJID.ToLower())))
			{
				e.Accept();
			}
			else if (BareJid.IndexOf('@') > 0 &&
				(xmppClient is null ||
				(Item = xmppClient.GetRosterItem(BareJid)) is null ||
				Item.State == SubscriptionState.None ||
				Item.State == SubscriptionState.Remove ||
				Item.State == SubscriptionState.Unknown))
			{
				foreach (XmlNode N in e.Stanza.ChildNodes)
				{
					if (N.LocalName == "query" && N.NamespaceURI == XmppClient.NamespaceServiceDiscoveryInfo)
						return Task.CompletedTask;
				}

				e.Reject();
			}

			return Task.CompletedTask;
		}

		private static async void CheckConnection(object State)
		{
			try
			{
				if (!stopped)
				{
					scheduler.Add(DateTime.Now.AddMinutes(1), CheckConnection, null);

					XmppState? State2 = xmppClient?.State;
					if (State2.HasValue && (State2 == XmppState.Offline || State2 == XmppState.Error || State2 == XmppState.Authenticating))
					{
						try
						{
							xmppClient?.Reconnect();
						}
						catch (Exception ex)
						{
							Log.Critical(ex);
						}
					}

					await CheckBackup();

					EventHandler h = MinuteTick;
					if (!(h is null))
					{
						try
						{
							h(null, EventArgs.Empty);
						}
						catch (Exception ex)
						{
							Log.Critical(ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Event raised every minute.
		/// </summary>
		public static event EventHandler MinuteTick = null;

		private static Task XmppClient_OnStateChanged(object _, XmppState NewState)
		{
			switch (NewState)
			{
				case XmppState.Connected:
					connected = true;

					MarkdownToHtmlConverter.BareJID = xmppClient.BareJID;

					if (!registered && !(thingRegistryClient is null))
						Task.Run(Register);

					if (!socksProxy.HasProxy)
						socksProxy.StartSearch(null);
					break;

				case XmppState.Offline:
					immediateReconnect = connected;
					connected = false;

					if (immediateReconnect && !(xmppClient is null))
						xmppClient.Reconnect();
					break;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Checks if a web request comes from the local host in the current session. If so, the user is automatically logged in.
		/// </summary>
		/// <param name="Request">Web request</param>
		public static void CheckLocalLogin(HttpRequest Request)
		{
			string RemoteEndpoint = Request.RemoteEndPoint;
			string From;
			int i;
			bool DoLog = false;

			if (Request.Header.TryGetQueryParameter("debug", out string s) && CommonTypes.TryParse(s, out bool b))
				DoLog = b;

			if (Request.Session is null)
			{
				if (DoLog)
					Log.Debug("No local login: No session.");

				return;
			}

			if (!Request.Session.TryGetVariable("from", out Variable v) || string.IsNullOrEmpty(From = v.ValueObject as string))
				From = "/";

			if (!loopbackIntefaceAvailable && (XmppConfiguration.Instance is null || !XmppConfiguration.Instance.Complete || configuring))
			{
				LoginAuditor.Success("User logged in by default, since XMPP not configued and loopback interface not available.",
					string.Empty, Request.RemoteEndPoint, "Web");

				Login.DoLogin(Request, From);
				return;
			}

			if (DoLog)
				Log.Debug("Checking for local login from: " + RemoteEndpoint);

			i = RemoteEndpoint.LastIndexOf(':');
			if (i < 0 || !int.TryParse(RemoteEndpoint.Substring(i + 1), out int Port))
			{
				if (DoLog)
					Log.Debug("Invalid port number: " + RemoteEndpoint);

				return;
			}

			if (!IPAddress.TryParse(RemoteEndpoint.Substring(0, i), out IPAddress Address))
			{
				if (DoLog)
					Log.Debug("Invalid IP Address: " + RemoteEndpoint);

				return;
			}

			if (!IsLocalCall(Address, Request, DoLog))
			{
				if (DoLog)
					Log.Debug("Request not local: " + RemoteEndpoint);

				return;
			}

#if !MONO
			if (XmppConfiguration.Instance is null || !XmppConfiguration.Instance.Complete || configuring)
#endif
			{
				LoginAuditor.Success("Local user logged in.", string.Empty, Request.RemoteEndPoint, "Web");
				Login.DoLogin(Request, From);
				return;
			}

#if !MONO
			try
			{
				string FileName;
				string Arguments;
				bool WaitForExit;

				switch (Environment.OSVersion.Platform)
				{
					case PlatformID.Win32S:
        			case PlatformID.Win32Windows:
        			case PlatformID.Win32NT:
        			case PlatformID.WinCE:
						FileName = "netstat.exe";
						Arguments = "-a -n -o";
						WaitForExit = false;
						break;

					case PlatformID.Unix:
					case PlatformID.MacOSX:
						FileName = "netstat";
						Arguments = "-anv -p tcp";
						WaitForExit = true;
						break;

					default:
						if (DoLog)
							Log.Debug("No local login: Unsupported operating system: " + Environment.OSVersion.Platform.ToString());

						return;
				}

				using (Process Proc = new Process())
				{
					ProcessStartInfo StartInfo = new ProcessStartInfo()
					{
						FileName = FileName,
						Arguments = Arguments,
						WindowStyle = ProcessWindowStyle.Hidden,
						UseShellExecute = false,
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					};

					Proc.StartInfo = StartInfo;
					Proc.Start();

					if (WaitForExit)
					{
						Proc.WaitForExit(5000);
						if (!Proc.HasExited)
							return;
					}

					string Output = Proc.StandardOutput.ReadToEnd();
					if (DoLog)
						Log.Debug("Netstat output:\r\n\r\n" + Output);

					if (Proc.ExitCode != 0)
					{
						if (DoLog)
							Log.Debug("Netstat exit code: " + Proc.ExitCode.ToString());

						return;
					}

					string[] Rows = Output.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);

					foreach (string Row in Rows)
					{
						string[] Tokens = Regex.Split(Row, @"\s+");

						switch (Environment.OSVersion.Platform)
						{
							case PlatformID.Win32S:
							case PlatformID.Win32Windows:
							case PlatformID.Win32NT:
							case PlatformID.WinCE:
								if (Tokens.Length < 6)
									break;

								if (Tokens[1] != "TCP")
									break;

								if (!SameEndpoint(Tokens[2], RemoteEndpoint))
									break;

								if (Tokens[4] != "ESTABLISHED")
									break;

								if (!int.TryParse(Tokens[5], out int PID))
									break;

								Process P = Process.GetProcessById(PID);
								int CurrentSession = WTSGetActiveConsoleSessionId();

								if (P.SessionId == CurrentSession)
								{
									LoginAuditor.Success("Local user logged in.", string.Empty, Request.RemoteEndPoint, "Web");
									Login.DoLogin(Request, From);
									return;
								}
								break;

							case PlatformID.Unix:
							case PlatformID.MacOSX:
								if (Tokens.Length < 9)
									break;

								if (Tokens[0] != "tcp4" && Tokens[0] != "tcp6")
									break;

								if (!SameEndpoint(Tokens[4], RemoteEndpoint))
									break;

								if (Tokens[5] != "ESTABLISHED")
									break;

								if (!int.TryParse(Tokens[8], out PID))
									break;

								P = Process.GetProcessById(PID);
								CurrentSession = Process.GetCurrentProcess().SessionId;

								if (P.SessionId == CurrentSession)
								{
									LoginAuditor.Success("Local user logged in.", string.Empty, Request.RemoteEndPoint, "Web");
									Login.DoLogin(Request, From);
									return;
								}
								break;

							default:
								if (DoLog)
									Log.Debug("No local login: Unsupported operating system: " + Environment.OSVersion.Platform.ToString());

								return;
						}
					}
				}
			}
			catch (HttpException ex)
			{
				if (DoLog)
					Log.Critical(ex);

				ExceptionDispatchInfo.Capture(ex).Throw();
			}
			catch (Exception ex)
			{
				if (DoLog)
					Log.Critical(ex);

				return;
			}
#endif
		}

		private static bool SameEndpoint(string EP1, string EP2)
		{
			if (string.Compare(EP1, EP2, true) == 0)
				return true;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Unix:
				case PlatformID.MacOSX:
					int i = EP1.LastIndexOf('.');
					if (i < 0)
						break;

					if (!int.TryParse(EP1.Substring(i + 1), out int Port1))
						break;

					if (!IPAddress.TryParse(EP1.Substring(0, i), out IPAddress Addr1))
						break;

					i = EP2.LastIndexOf(':');
					if (i < 0)
						break;

					if (!int.TryParse(EP2.Substring(i + 1), out int Port2) || Port1 != Port2)
						break;

					if (!IPAddress.TryParse(EP2.Substring(0, i), out IPAddress Addr2))
						break;

					string s1 = Addr1.ToString();
					string s2 = Addr2.ToString();

					if (string.Compare(s1, s2, true) == 0)
						return true;

					break;
			}

			return false;
		}

		private static readonly IPAddress ipv6Local = IPAddress.Parse("[::1]");
		private static readonly IPAddress ipv4Local = IPAddress.Parse("127.0.0.1");

		private static bool IsLocalCall(IPAddress Address, HttpRequest Request, bool DoLog)
		{
			if (Address.Equals(ipv4Local) || Address.Equals(ipv6Local))
				return true;

			string s = Request.Header.Host?.Value ?? string.Empty;
			int i = s.IndexOf(':');
			if (i > 0)
				s = s.Substring(0, i);

			if (string.Compare(s, "localhost", true) != 0)
			{
				if (!IPAddress.TryParse(s, out IPAddress IP) || !Address.Equals(IP))
				{
					if (DoLog)
						Log.Debug("Host is not localhost or an IP Address: " + (Request.Header.Host?.Value ?? string.Empty));

					return false;
				}
			}

			try
			{
				foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (Interface.OperationalStatus != OperationalStatus.Up)
						continue;

					IPInterfaceProperties Properties = Interface.GetIPProperties();

					foreach (UnicastIPAddressInformation UnicastAddress in Properties.UnicastAddresses)
					{
						if (Address.Equals(UnicastAddress.Address))
							return true;
					}
				}

				if (DoLog)
					Log.Debug("IP Address not found among network adapters: " + Address.ToString());

				return false;
			}
			catch (Exception ex)
			{
				if (DoLog)
					Log.Debug(ex.Message);

				return false;
			}
		}

#if !MONO
		[DllImport("kernel32.dll")]
		static extern int WTSGetActiveConsoleSessionId();
#endif

		/// <summary>
		/// Authentication mechanism that makes sure the call is made from a session with a valid authenticated user with
		/// the given set of privileges.
		/// </summary>
		/// <param name="Privileges">Required user privileges.</param>
		public static RequiredUserPrivileges LoggedIn(string[] Privileges)
		{
			return new RequiredUserPrivileges(webServer, Privileges);
		}

		/// <summary>
		/// Authentication mechanism that makes sure the call is made from a session with a valid authenticated user with
		/// the given set of privileges.
		/// </summary>
		/// <param name="UserVariable">Name of user variable.</param>
		/// <param name="Privileges">Required user privileges.</param>
		public static RequiredUserPrivileges LoggedIn(string UserVariable, string[] Privileges)
		{
			return new RequiredUserPrivileges(UserVariable, webServer, Privileges);
		}

		/// <summary>
		/// Authentication mechanism that makes sure the call is made from a session with a valid authenticated user with
		/// the given set of privileges.
		/// </summary>
		/// <param name="UserVariable">Name of user variable.</param>
		/// <param name="LoginPage">Login page.</param>
		/// <param name="Privileges">Required user privileges.</param>
		public static RequiredUserPrivileges LoggedIn(string UserVariable, string LoginPage, string[] Privileges)
		{
			return new RequiredUserPrivileges(UserVariable, LoginPage, webServer, Privileges);
		}

		/// <summary>
		/// Current Login Auditor. Should be used by modules accepting user logins, to protect the system from
		/// unauthorized access by malicious users.
		/// </summary>
		public static LoginAuditor LoginAuditor
		{
			get { return loginAuditor; }
		}

		/// <summary>
		/// Makes sure a request is being made from a session with a successful user login.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Privilege">Required user privilege.</param>
		/// <returns>Logged in user.</returns>
		public static IUser AssertUserAuthenticated(HttpRequest Request, string Privilege)
		{
			return AssertUserAuthenticated(Request.Session, Privilege);
		}

		/// <summary>
		/// Makes sure a request is being made from a session with a successful user login.
		/// </summary>
		/// <param name="Session">Session Variables</param>
		/// <param name="Privilege">Required user privilege.</param>
		/// <returns>Logged in user.</returns>
		public static IUser AssertUserAuthenticated(Variables Session, string Privilege)
		{
			IUser User = null;

			if (Session is null ||
				!Session.TryGetVariable("User", out Variable v) ||
				((User = v.ValueObject as IUser) is null) ||
				!User.HasPrivilege(Privilege))
			{
				throw ForbiddenException.AccessDenied(string.Empty, User?.UserName, Privilege);
			}

			return User;
		}

		/// <summary>
		/// Makes sure a request is being made from a session with a successful user login.
		/// </summary>
		/// <param name="Request">HTTP Request</param>
		/// <param name="Privileges">Required user privileges.</param>
		/// <returns>Logged in user.</returns>
		public static IUser AssertUserAuthenticated(HttpRequest Request, string[] Privileges)
		{
			return AssertUserAuthenticated(Request.Session, Privileges);
		}

		/// <summary>
		/// Makes sure a request is being made from a session with a successful user login.
		/// </summary>
		/// <param name="Session">Session Variables</param>
		/// <param name="Privileges">Required user privileges.</param>
		/// <returns>Logged in user.</returns>
		public static IUser AssertUserAuthenticated(Variables Session, string[] Privileges)
		{
			if (Session is null ||
				!Session.TryGetVariable("User", out Variable v) ||
				(!(v.ValueObject is IUser User)))
			{
				throw ForbiddenException.AccessDenied(string.Empty, string.Empty, string.Empty);
			}

			foreach (string Privilege in Privileges)
			{
				if (!User.HasPrivilege(Privilege))
					throw ForbiddenException.AccessDenied(string.Empty, User.UserName, Privilege);
			}

			return User;
		}

		#endregion

		#region Thing Registry

		private static Task ThingRegistryClient_Claimed(object Sender, ClaimedEventArgs e)
		{
			if (e.Node.IsEmpty)
			{
				ownerJid = e.JID;
				Log.Informational("Gateway has been claimed.", ownerJid, new KeyValuePair<string, object>("Public", e.IsPublic));
			}

			return Task.CompletedTask;
		}

		private static Task ThingRegistryClient_Disowned(object Sender, Networking.XMPP.Provisioning.NodeEventArgs e)
		{
			if (e.Node.IsEmpty)
			{
				Log.Informational("Gateway has been disowned.", ownerJid);
				ownerJid = string.Empty;
				Task.Run(Register);
			}

			return Task.CompletedTask;
		}

		private static Task ThingRegistryClient_Removed(object Sender, Networking.XMPP.Provisioning.NodeEventArgs e)
		{
			if (e.Node.IsEmpty)
				Log.Informational("Gateway has been removed from the public registry.", ownerJid);

			return Task.CompletedTask;
		}

		private static async Task Register()
		{
			string Key = Guid.NewGuid().ToString().Replace("-", string.Empty);

			// For info on tag names, see: http://xmpp.org/extensions/xep-0347.html#tags
			MetaDataTag[] MetaData = new MetaDataTag[]
			{
				new MetaDataStringTag("KEY", Key),
				new MetaDataStringTag("CLASS", "Gateway"),
				new MetaDataStringTag("MAN", "waher.se"),
				new MetaDataStringTag("MODEL", "Waher.IoTGateway"),
				new MetaDataStringTag("PURL", "https://github.com/PeterWaher/IoTGateway#iotgateway"),
				new MetaDataNumericTag("V", 1.0)
			};

			if (!(GetMetaData is null))
				MetaData = await GetMetaData(MetaData);

			thingRegistryClient.RegisterThing(MetaData, async (sender2, e2) =>
			{
				if (e2.Ok)
				{
					registered = true;

					if (e2.IsClaimed)
						ownerJid = e2.OwnerJid;
					else
						ownerJid = string.Empty;

					RegistrationEventHandler h = RegistrationSuccessful;
					if (!(h is null))
						await h(MetaData, e2);
				}
			}, null);
		}

		/// <summary>
		/// Event raised when meta data is compiled for registration.
		/// </summary>
		public static event GetRegistryMetaDataEventHandler GetMetaData = null;

		/// <summary>
		/// Event raised when the gateway has performed a successful registration in a thing registry.
		/// </summary>
		public static event RegistrationEventHandler RegistrationSuccessful = null;

		/// <summary>
		/// XMPP Client connection of gateway.
		/// </summary>
		public static XmppClient XmppClient
		{
			get { return xmppClient; }
		}

		/// <summary>
		/// XMPP Thing Registry Client.
		/// </summary>
		public static ThingRegistryClient ThingRegistryClient
		{
			get { return thingRegistryClient; }
		}

		/// <summary>
		/// XMPP Provisioning Client.
		/// </summary>
		public static ProvisioningClient ProvisioningClient
		{
			get { return provisioningClient; }
		}

		/// <summary>
		/// XMPP Concentrator Server.
		/// </summary>
		public static ConcentratorServer ConcentratorServer
		{
			get { return concentratorServer; }
		}

		/// <summary>
		/// XMPP Concentrator Server.
		/// </summary>
		public static Networking.XMPP.Avatar.AvatarClient AvatarClient
		{
			get { return avatarClient; }
		}

		/// <summary>
		/// XMPP Sensor Client.
		/// </summary>
		public static SensorClient SensorClient
		{
			get { return sensorClient; }
		}

		/// <summary>
		/// XMPP Control Client.
		/// </summary>
		public static ControlClient ControlClient
		{
			get { return controlClient; }
		}

		/// <summary>
		/// XMPP Concentrator Client.
		/// </summary>
		public static ConcentratorClient ConcentratorClient
		{
			get { return concentratorClient; }
		}

		/// <summary>
		/// XMPP Synchronization Client.
		/// </summary>
		public static SynchronizationClient SynchronizationClient
		{
			get { return synchronizationClient; }
		}

		/// <summary>
		/// XMPP Personal Eventing Protocol (PEP) Client.
		/// </summary>
		public static PepClient PepClient
		{
			get { return pepClient; }
		}

		/// <summary>
		/// XMPP Multi-User Chat Protocol (MUC) Client.
		/// </summary>
		public static MultiUserChatClient MucClient
		{
			get { return mucClient; }
		}

		/// <summary>
		/// XMPP Publish/Subscribe (PubSub) Client, if such a component is available on the XMPP broker.
		/// </summary>
		public static PubSubClient PubSubClient
		{
			get { return pepClient.PubSubClient; }
		}

		/// <summary>
		/// XMPP Software Updates Client, if such a compoent is available on the XMPP broker.
		/// </summary>
		public static SoftwareUpdateClient SoftwareUpdateClient
		{
			get { return softwareUpdateClient; }
		}

		/// <summary>
		/// XMPP Mail Client, if support for mail-extensions is available on the XMPP broker.
		/// </summary>
		public static MailClient MailClient
		{
			get { return mailClient; }
		}

		/// <summary>
		/// HTTP Server
		/// </summary>
		public static HttpServer HttpServer
		{
			get { return webServer; }
		}

		/// <summary>
		/// HTTPX Server
		/// </summary>
		public static HttpxServer HttpxServer
		{
			get { return httpxServer; }
		}

		/// <summary>
		/// HTTPX Proxy resource
		/// </summary>
		public static HttpxProxy HttpxProxy
		{
			get { return httpxProxy; }
		}

		/// <summary>
		/// SOCKS5 Proxy
		/// </summary>
		public static Socks5Proxy Socks5Proxy
		{
			get { return socksProxy; }
		}

		/// <summary>
		/// CoAP Endpoint
		/// </summary>
		public static CoapEndpoint CoapEndpoint
		{
			get { return coapEndpoint; }
		}


		// TODO: Teman: http://mmistakes.github.io/skinny-bones-jekyll/, http://jekyllrb.com/

		#endregion

		#region Service Commands

		/// <summary>
		/// Executes a service command.
		/// 
		/// Command must have been registered with <see cref="RegisterServiceCommand(EventHandler)"/> before being executed.
		/// </summary>
		/// <param name="CommandNr">Command number.</param>
		/// <returns>If a service command with the given number was found and executed.</returns>
		public static bool ExecuteServiceCommand(int CommandNr)
		{
			EventHandler h;

			lock (serviceCommandByNr)
			{
				if (!serviceCommandByNr.TryGetValue(CommandNr, out h))
					h = null;
			}

			if (h is null)
			{
				Log.Warning("Service command lacking command handler invoked.", CommandNr.ToString());
				return false;
			}
			else
			{
				try
				{
					h(null, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}

				return true;
			}
		}

		/// <summary>
		/// Registers an administrative service command.
		/// </summary>
		/// <param name="Callback">Method to call when service command is invoked.</param>
		/// <returns>Command number assigned to the command.</returns>
		public static int RegisterServiceCommand(EventHandler Callback)
		{
			int i;

			lock (serviceCommandByNr)
			{
				if (serviceCommandNrByCallback.TryGetValue(Callback, out i))
					return i;

				i = nextServiceCommandNr++;

				serviceCommandNrByCallback[Callback] = i;
				serviceCommandByNr[i] = Callback;
			}

			return i;
		}

		/// <summary>
		/// Unregisters an administrative service command.
		/// </summary>
		/// <param name="Callback">Method serving the service command.</param>
		/// <returns>If the command was found and unregistered.</returns>
		public static bool UnregisterServiceCommand(EventHandler Callback)
		{
			lock (serviceCommandByNr)
			{
				if (serviceCommandNrByCallback.TryGetValue(Callback, out int i))
				{
					serviceCommandByNr.Remove(i);
					serviceCommandNrByCallback.Remove(Callback);

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Service command number related to the BeforeUninstall Service command registered by the gateway.
		/// </summary>
		public static int BeforeUninstallCommandNr
		{
			get { return beforeUninstallCommandNr; }
		}

		/// <summary>
		/// Event raised before the application is uninstalled.
		/// </summary>
		public static event EventHandler OnBeforeUninstall = null;

		private static void BeforeUninstall(object Sender, EventArgs e)
		{
			try
			{
				OnBeforeUninstall?.Invoke(Sender, e);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		#endregion

		#region Scheduling

		/// <summary>
		/// Schedules a one-time event.
		/// </summary>
		/// <param name="Callback">Method to call when event is due.</param>
		/// <param name="When">When the event is to be executed.</param>
		/// <param name="State">State object</param>
		/// <returns>Timepoint of when event was scheduled.</returns>
		public static DateTime ScheduleEvent(ScheduledEventCallback Callback, DateTime When, object State)
		{
			return scheduler?.Add(When, Callback, State) ?? DateTime.MinValue;
		}

		/// <summary>
		/// Schedules a one-time event.
		/// </summary>
		/// <param name="Callback">Method to call when event is due.</param>
		/// <param name="When">When the event is to be executed.</param>
		/// <param name="State">State object</param>
		/// <returns>Timepoint of when event was scheduled.</returns>
		public static DateTime ScheduleEvent(ScheduledEventCallbackAsync Callback, DateTime When, object State)
		{
			return scheduler?.Add(When, Callback, State) ?? DateTime.MinValue;
		}

		/// <summary>
		/// Cancels a scheduled event.
		/// </summary>
		/// <param name="When">When event is scheduled</param>
		/// <returns>If event was found and removed.</returns>
		public static bool CancelScheduledEvent(DateTime When)
		{
			return scheduler?.Remove(When) ?? false;
		}

		#endregion

		#region Random number generation

		/// <summary>
		/// Generates a new floating-point value between 0 and 1, using a cryptographic random number generator.
		/// </summary>
		/// <returns>Random number.</returns>
		public static double NextDouble()
		{
			byte[] b = new byte[8];

			lock (rnd)
			{
				rnd.GetBytes(b);
			}

			double d = BitConverter.ToUInt64(b, 0);
			d /= ulong.MaxValue;

			return d;
		}

		/// <summary>
		/// Returns a non-negative random integer that is less than the specified maximum.
		/// </summary>
		/// <param name="Max">The exclusive upper bound of the random number to be generated. maxValue must
		/// be greater than or equal to 0.</param>
		/// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="Max"/>;
		/// that is, the range of return values ordinarily includes 0 but not <paramref name="Max"/>. However,
		/// if <paramref name="Max"/> equals 0, <paramref name="Max"/> is returned.</returns>
		public static int NextInteger(int Max)
		{
			if (Max < 0)
				throw new ArgumentOutOfRangeException("Must be non-negative.", nameof(Max));

			if (Max == 0)
				return 0;

			int Result;

			do
			{
				Result = (int)(NextDouble() * Max);
			}
			while (Result >= Max);

			return Result;
		}

		/// <summary>
		/// Generates an array of random bytes.
		/// </summary>
		/// <param name="NrBytes">Number of random bytes to generate.</param>
		/// <returns>Random bytes.</returns>
		public static byte[] NextBytes(int NrBytes)
		{
			if (NrBytes < 0)
				throw new ArgumentException("Number of bytes must be non-negative.", nameof(NrBytes));

			byte[] Result = new byte[NrBytes];

			lock (rnd)
			{
				rnd.GetBytes(Result);
			}

			return Result;
		}

		#endregion

		#region Momentary values

		/// <summary>
		/// Reports newly measured values.
		/// </summary>
		/// <param name="Values">New momentary values.</param>
		public static void NewMomentaryValues(params Field[] Values)
		{
			concentratorServer?.SensorServer?.NewMomentaryValues(Values);
		}

		/// <summary>
		/// Reports newly measured values.
		/// </summary>
		/// <param name="Reference">Optional node reference</param>
		/// <param name="Values">New momentary values.</param>
		public static void NewMomentaryValues(IThingReference Reference, params Field[] Values)
		{
			concentratorServer?.SensorServer?.NewMomentaryValues(Reference, Values);
		}

		/// <summary>
		/// Reports newly measured values.
		/// </summary>
		/// <param name="Values">New momentary values.</param>
		public static void NewMomentaryValues(IEnumerable<Field> Values)
		{
			concentratorServer?.SensorServer?.NewMomentaryValues(Values);
		}

		/// <summary>
		/// Reports newly measured values.
		/// </summary>
		/// <param name="Reference">Optional node reference</param>
		/// <param name="Values">New momentary values.</param>
		public static void NewMomentaryValues(IThingReference Reference, IEnumerable<Field> Values)
		{
			concentratorServer?.SensorServer?.NewMomentaryValues(Reference, Values);
		}

		#endregion

		#region Personal Eventing Protocol

		/// <summary>
		/// Publishes a personal event on the XMPP network.
		/// </summary>
		/// <param name="PersonalEvent">Personal event to publish.</param>
		public static void PublishPersonalEvent(IPersonalEvent PersonalEvent)
		{
			pepClient?.Publish(PersonalEvent, null, null);
		}

		/// <summary>
		/// Registers an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		public static void RegisterHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler)
		{
			pepClient?.RegisterHandler(PersonalEventType, Handler);
		}

		/// <summary>
		/// Unregisters an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		/// <returns>If the event handler was found and removed.</returns>
		public static bool UnregisterHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler)
		{
			if (pepClient is null)
				return false;
			else
				return pepClient.UnregisterHandler(PersonalEventType, Handler);
		}

		/// <summary>
		/// Event raisen when an item notification has been received, that is not a personal event, but received from the
		/// Publish/Subscribe component.
		/// </summary>
		public static event ItemNotificationEventHandler PubSubItemNotification
		{
			add => pepClient.NonPepItemNotification += value;
			remove => pepClient.NonPepItemNotification -= value;
		}

		#endregion

		#region Backups

		private static async Task<bool> CheckBackup()
		{
			bool Result = false;

			try
			{
				if (await Export.GetAutomaticBackupsAsync())
				{
					DateTime Now = DateTime.Now;
					TimeSpan Timepoint = await Export.GetBackupTimeAsync();
					DateTime EstimatedTime = Now.Date + Timepoint;
					DateTime LastBackup = await Export.GetLastBackupAsync();

					if ((Timepoint.Hours == Now.Hour && Timepoint.Minutes == Now.Minute) ||
						(lastBackupTimeCheck.HasValue && lastBackupTimeCheck.Value < EstimatedTime && Now >= EstimatedTime) ||
						LastBackup.AddDays(1) < EstimatedTime)
					{
						lastBackupTimeCheck = Now;
						await DoBackup();

						Result = true;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}

			return Result;
		}

		/// <summary>
		/// Performs a backup of the system.
		/// </summary>
		/// <returns></returns>
		public static async Task DoBackup()
		{
			DateTime Now = DateTime.Now;

			await Export.SetLastBackupAsync(Now);

			StartExport.ExportInfo ExportInfo = await StartExport.GetExporter("Encrypted", false, new string[0]);

			ExportFormat.UpdateClientsFileUpdated(ExportInfo.LocalBackupFileName, -1, Now);

			List<string> Folders = new List<string>();

			foreach (Export.FolderCategory FolderCategory in Export.GetRegisteredFolders())
				Folders.AddRange(FolderCategory.Folders);

			await StartExport.DoExport(ExportInfo, true, false, true, Folders.ToArray());

			long KeepDays = await Export.GetKeepDaysAsync();
			long KeepMonths = await Export.GetKeepMonthsAsync();
			long KeepYears = await Export.GetKeepYearsAsync();
			string ExportFolder = await Export.GetFullExportFolderAsync();
			string KeyFolder = await Export.GetFullKeyExportFolderAsync();

			DeleteOldFiles(ExportFolder, KeepDays, KeepMonths, KeepYears, Now);
			if (ExportFolder != KeyFolder)
				DeleteOldFiles(KeyFolder, KeepDays, KeepMonths, KeepYears, Now);

			try
			{
				OnAfterBackup?.Invoke(typeof(Gateway), EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private static DateTime? lastBackupTimeCheck = null;

		/// <summary>
		/// Event raised after a backup has been performed. This event can be used by services to purge the system of old data, without
		/// affecting any ongoing backup.
		/// </summary>
		public static event EventHandler OnAfterBackup = null;

		private static void DeleteOldFiles(string Path, long KeepDays, long KeepMonths, long KeepYears, DateTime Now)
		{
			try
			{
				string[] Files = Directory.GetFiles(Path, "*.*", SearchOption.AllDirectories);
				DateTime CreationTime;

				foreach (string FileName in Files)
				{
					try
					{
						CreationTime = File.GetCreationTime(FileName);

						if (CreationTime.Day == 1)
						{
							if (CreationTime.Month == 1)    // Yearly
							{
								if (Now.Year - CreationTime.Year <= KeepYears)
									continue;
							}
							else    // Monthly
							{
								if (((Now.Year * 12 + Now.Month) - (CreationTime.Year * 12 + Now.Month)) <= KeepMonths)
									continue;
							}
						}
						else    // Daily
						{
							if ((Now.Date - CreationTime.Date).TotalDays <= KeepDays)
								continue;
						}

						File.Delete(FileName);
						Log.Informational("Backup file deleted.", FileName);

						ExportFormat.UpdateClientsFileDeleted(System.IO.Path.GetFileName(FileName));
					}
					catch (Exception ex)
					{
						Log.Critical(ex, FileName);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private static void Database_CollectionRepaired(object Sender, CollectionRepairedEventArgs e)
		{
			StringBuilder Msg = new StringBuilder();

			Msg.Append("Collection repaired: ");
			Msg.AppendLine(e.Collection);

			if (!(e.Flagged is null))
			{
				foreach (FlagSource Source in e.Flagged)
				{
					Msg.AppendLine();
					Msg.Append("Reason: ");
					Msg.Append(MarkdownDocument.Encode(Source.Reason));

					if (Source.Count > 1)
					{
						Msg.Append(" (");
						Msg.Append(Source.Count.ToString());
						Msg.Append(" times)");
					}

					Msg.AppendLine();
					Msg.AppendLine();
					Msg.AppendLine("StackTrace:");
					Msg.AppendLine();
					Msg.AppendLine("```");
					Msg.AppendLine(Source.StackTrace);
					Msg.AppendLine("```");
				}
			}

			Log.Alert(Msg.ToString(), e.Collection);
		}

		#endregion

		#region Notifications

		private static async Task MailClient_MailReceived(object Sender, MailEventArgs e)
		{
			MailEventHandler h = MailReceived;
			if (!(h is null))
				await h(Sender, e);
		}

		/// <summary>
		/// Event raised when a mail has been received.
		/// </summary>
		public static event MailEventHandler MailReceived = null;

		/// <summary>
		/// Sends a graph as a notification message to configured notification recipients.
		/// </summary>
		/// <param name="Graph">Graph to send.</param>
		public static Task SendNotification(Graph Graph)
		{
			return SendNotification(Content.Markdown.Functions.ToMarkdown.GraphToMarkdown(Graph));
		}

		/// <summary>
		/// Sends an image as a notification message to configured notification recipients.
		/// </summary>
		/// <param name="Pixels">Pixels to send.</param>
		public static Task SendNotification(PixelInformation Pixels)
		{
			return SendNotification(Content.Markdown.Functions.ToMarkdown.PixelsToMarkdown(Pixels));
		}

		/// <summary>
		/// Sends a notification message to configured notification recipients.
		/// </summary>
		/// <param name="Markdown">Markdown of message.</param>
		public static Task SendNotification(string Markdown)
		{
			return SendNotification(Markdown, string.Empty, false);
		}

		/// <summary>
		/// Sends a notification message to configured notification recipients.
		/// </summary>
		/// <param name="Markdown">Markdown of message.</param>
		/// <param name="MessageId">Message ID</param>
		public static Task SendNotification(string Markdown, string MessageId)
		{
			return SendNotification(Markdown, MessageId, false);
		}

		/// <summary>
		/// Sends a notification message to configured notification recipients.
		/// </summary>
		/// <param name="Markdown">Markdown of message.</param>
		/// <param name="MessageId">Message ID</param>
		public static Task SendNotificationUpdate(string Markdown, string MessageId)
		{
			return SendNotification(Markdown, MessageId, true);
		}

		/// <summary>
		/// Sends a notification message to configured notification recipients.
		/// </summary>
		/// <param name="Markdown">Markdown of message.</param>
		/// <param name="MessageId">Message ID</param>
		/// <param name="Update">If its an update notification</param>
		private static async Task SendNotification(string Markdown, string MessageId, bool Update)
		{
			try
			{
				CaseInsensitiveString[] Addresses = GetNotificationAddresses();
				(string Text, string Html) = await ConvertMarkdown(Markdown);

				foreach (CaseInsensitiveString Admin in Addresses)
					SendNotification(Admin, Markdown, Text, Html, MessageId, Update);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private static Task<(string, string)> ConvertMarkdown(string Markdown)
		{
			return ConvertMarkdown(Markdown, true, true);
		}

		private static async Task<(string, string)> ConvertMarkdown(string Markdown, bool TextVersion, bool HtmlVersion)
		{
			if (TextVersion || HtmlVersion)
			{
				MarkdownSettings Settings = new MarkdownSettings()
				{
					ParseMetaData = false
				};
				HtmlSettings HtmlSettings = new HtmlSettings()
				{
					XmlEntitiesOnly = true
				};
				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown, Settings);
				string Text = TextVersion ? await Doc.GeneratePlainText() : null;
				string Html = HtmlVersion ? HtmlDocument.GetBody(await Doc.GenerateHTML(HtmlSettings)) : null;

				return (Text, Html);
			}
			else
				return (null, null);
		}

		/// <summary>
		/// Returns configured notification addresses.
		/// </summary>
		/// <returns>Array of addresses</returns>
		public static CaseInsensitiveString[] GetNotificationAddresses()
		{
			return NotificationConfiguration.Instance?.Addresses ?? new CaseInsensitiveString[0];
		}

		private static void SendNotification(string To, string Markdown, string Text, string Html, string MessageId, bool Update)
		{
			if (!(XmppClient is null) && XmppClient.State == XmppState.Connected)
			{
				RosterItem Item = XmppClient.GetRosterItem(To);
				if (Item is null || (Item.State != SubscriptionState.To && Item.State != SubscriptionState.Both))
				{
					xmppClient.RequestPresenceSubscription(To);
					ScheduleEvent(Resend, DateTime.Now.AddMinutes(15), new object[] { To, Markdown, Text, Html, MessageId, Update });
				}
				else
					SendChatMessage(MessageType.Chat, Markdown, Text, Html, To, MessageId, string.Empty, Update);
			}
			else
				ScheduleEvent(Resend, DateTime.Now.AddSeconds(30), new object[] { To, Markdown, Text, Html, MessageId, Update });
		}

		/// <summary>
		/// Sends a chat message to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		public static Task SendChatMessage(string Markdown, string To)
		{
			return SendChatMessage(Markdown, To, string.Empty);
		}

		/// <summary>
		/// Sends a chat message to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID to use.</param>
		public static Task SendChatMessage(string Markdown, string To, string MessageId)
		{
			return SendChatMessage(Markdown, To, MessageId, string.Empty);
		}

		/// <summary>
		/// Sends a chat message to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID to use.</param>
		/// <param name="ThreadId">Thread ID</param>
		public static async Task SendChatMessage(string Markdown, string To, string MessageId, string ThreadId)
		{
			(string Text, string Html) = await ConvertMarkdown(Markdown);
			SendChatMessage(MessageType.Chat, Markdown, Text, Html, To, MessageId, ThreadId, false);
		}

		/// <summary>
		/// Sends a chat message update to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID of message to update.</param>
		public static Task SendChatMessageUpdate(string Markdown, string To, string MessageId)
		{
			return SendChatMessageUpdate(Markdown, To, MessageId, string.Empty);
		}

		/// <summary>
		/// Sends a chat message update to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID of message to update.</param>
		/// <param name="ThreadId">Thread ID</param>
		public static async Task SendChatMessageUpdate(string Markdown, string To, string MessageId, string ThreadId)
		{
			(string Text, string Html) = await ConvertMarkdown(Markdown);
			SendChatMessage(MessageType.Chat, Markdown, Text, Html, To, MessageId, ThreadId, true);
		}

		/// <summary>
		/// Sends a group chat message to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		public static Task SendGroupChatMessage(string Markdown, string To)
		{
			return SendGroupChatMessage(Markdown, To, string.Empty);
		}

		/// <summary>
		/// Sends a group chat message to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID to use.</param>
		public static Task SendGroupChatMessage(string Markdown, string To, string MessageId)
		{
			return SendGroupChatMessage(Markdown, To, MessageId, string.Empty);
		}

		/// <summary>
		/// Sends a group chat message to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID to use.</param>
		/// <param name="ThreadId">Thread ID</param>
		public static async Task SendGroupChatMessage(string Markdown, string To, string MessageId, string ThreadId)
		{
			(string Text, string Html) = await ConvertMarkdown(Markdown);
			SendChatMessage(MessageType.GroupChat, Markdown, Text, Html, To, MessageId, ThreadId, false);
		}

		/// <summary>
		/// Sends a group chat message update to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID of message to update.</param>
		public static Task SendGroupChatMessageUpdate(string Markdown, string To, string MessageId)
		{
			return SendGroupChatMessageUpdate(Markdown, To, MessageId, string.Empty);
		}

		/// <summary>
		/// Sends a group chat message update to a recipient.
		/// </summary>
		/// <param name="Markdown">Markdown containing text to send.</param>
		/// <param name="To">Recipient of chat message.</param>
		/// <param name="MessageId">Message ID of message to update.</param>
		/// <param name="ThreadId">Thread ID</param>
		public static async Task SendGroupChatMessageUpdate(string Markdown, string To, string MessageId, string ThreadId)
		{
			(string Text, string Html) = await ConvertMarkdown(Markdown);
			SendChatMessage(MessageType.GroupChat, Markdown, Text, Html, To, MessageId, ThreadId, true);
		}

		/// <summary>
		/// Gets XML for a multi-formatted chat message.
		/// </summary>
		/// <param name="Markdown">Markdown containing message text. Used to generate plain text and HTML copies of the same content.</param>
		/// <returns>Multi-format XML for chat message.</returns>
		public static Task<string> GetMultiFormatChatMessageXml(string Markdown)
		{
			return GetMultiFormatChatMessageXml(Markdown, true, true);
		}

		/// <summary>
		/// Gets XML for a multi-formatted chat message.
		/// </summary>
		/// <param name="Markdown">Markdown containing message text. Used to generate plain text and HTML copies of the same content.</param>
		/// <param name="TextVersion">If a Text version of the message should be included.</param>
		/// <param name="HtmlVersion">If a HTML version of the message should be included.</param>
		/// <returns>Multi-format XML for chat message.</returns>
		public static async Task<string> GetMultiFormatChatMessageXml(string Markdown, bool TextVersion, bool HtmlVersion)
		{
			(string Text, string Html) = await ConvertMarkdown(Markdown, TextVersion, HtmlVersion);
			return GetMultiFormatChatMessageXml(Text, Html, Markdown);
		}

		/// <summary>
		/// Gets XML for a multi-formatted chat message.
		/// </summary>
		/// <param name="Text">Plain-text version of message. If empty or null, plain text is excluded from message.</param>
		/// <param name="Html">HTML version of message. If empty or null, HTML is excluded from message.</param>
		/// <param name="Markdown">Markdown containing message text. If empty or null, markdown is excluded from message.</param>
		/// <returns>Multi-format XML for chat message.</returns>
		public static string GetMultiFormatChatMessageXml(string Text, string Html, string Markdown)
		{
			StringBuilder Xml = new StringBuilder();
			AppendMultiFormatChatMessageXml(Xml, Text, Html, Markdown);
			return Xml.ToString();
		}

		/// <summary>
		/// Appends the XML for a multi-formatted chat message to a string being built.
		/// </summary>
		/// <param name="Xml">XML output.</param>
		/// <param name="Text">Plain-text version of message. If empty or null, plain text is excluded from message.</param>
		/// <param name="Html">HTML version of message. If empty or null, HTML is excluded from message.</param>
		/// <param name="Markdown">Markdown containing message text. If empty or null, markdown is excluded from message.</param>
		public static void AppendMultiFormatChatMessageXml(StringBuilder Xml, string Text, string Html, string Markdown)
		{
			if (string.IsNullOrEmpty(Text))
				Xml.Append("<body/>");
			else
			{
				Xml.Append("<body><![CDATA[");
				Xml.Append(Text.Replace("]]>", "]] >"));
				Xml.Append("]]></body>");
			}

			if (!string.IsNullOrEmpty(Markdown))
			{
				Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\"><![CDATA[");
				Xml.Append(Markdown.Replace("]]>", "]] >"));
				Xml.Append("]]></content>");
			}

			if (!string.IsNullOrEmpty(Html))
			{
				Xml.Append("<html xmlns='http://jabber.org/protocol/xhtml-im'>");
				Xml.Append("<body xmlns='http://www.w3.org/1999/xhtml'>");

				HtmlDocument Doc = new HtmlDocument("<root>" + Html + "</root>");
				IEnumerable<HtmlNode> Children = (Doc.Body ?? Doc.Root).Children;

				if (!(Children is null))
				{
					foreach (HtmlNode N in Children)
						N.Export(Xml);
				}

				Xml.Append("</body></html>");
			}
		}

		private static void SendChatMessage(MessageType Type, string Markdown, string Text, string Html, string To, string MessageId, string ThreadId, bool Update)
		{
			if (!(XmppClient is null) && XmppClient.State == XmppState.Connected)
			{
				StringBuilder Xml = new StringBuilder();

				AppendMultiFormatChatMessageXml(Xml, Text, Html, Markdown);

				if (Update && !string.IsNullOrEmpty(MessageId))
				{
					Xml.Append("<replace id='");
					Xml.Append(MessageId);
					Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");

					MessageId = string.Empty;
				}

				xmppClient.SendMessage(QoSLevel.Unacknowledged, Type, MessageId, To, Xml.ToString(), string.Empty,
					string.Empty, string.Empty, ThreadId, string.Empty, null, null);
			}
		}

		/// <summary>
		/// Gets a URL for a resource.
		/// </summary>
		/// <param name="LocalResource">Local resource.</param>
		/// <returns>URL</returns>
		public static string GetUrl(string LocalResource)
		{
			return GetUrl(LocalResource, webServer);
		}

		/// <summary>
		/// Gets a URL for a resource.
		/// </summary>
		/// <param name="LocalResource">Local resource.</param>
		/// <param name="Server">HTTP Server to get URL to. (By default is equal to the HTTP Server hosted by the gateway.)</param>
		/// <returns>URL</returns>
		public static string GetUrl(string LocalResource, HttpServer Server)
		{
			StringBuilder sb = new StringBuilder();
			int DefaultPort;
			int[] Ports;

			sb.Append("http");

			if (certificate is null)
			{
				Ports = Server?.OpenHttpPorts ?? GetConfigPorts("HTTP");
				DefaultPort = 80;
			}
			else
			{
				sb.Append('s');
				Ports = Server?.OpenHttpsPorts ?? GetConfigPorts("HTTPS");
				DefaultPort = 443;
			}

			sb.Append("://");
			if (DomainConfiguration.Instance?.UseDomainName ?? false)
				sb.Append(domain);
			/*else if (httpxProxy?.ServerlessMessaging?.Network.State == PeerToPeerNetworkState.Ready)
				sb.Append(httpxProxy.ServerlessMessaging.Network.ExternalAddress.ToString());
			
				TODO: P2P & Serverless messaging: Recognize HTTP request, and redirect to local HTTP Server, and return response.
			 */
			else
			{
				IPAddress IP4 = null;
				IPAddress IP6 = null;

				if (!(Server is null))
				{
					foreach (IPAddress Addr in Server.LocalIpAddresses)
					{
						if (IPAddress.IsLoopback(Addr))
							continue;

						switch (Addr.AddressFamily)
						{
							case System.Net.Sockets.AddressFamily.InterNetwork:
								if (IP4 is null)
									IP4 = Addr;
								break;

							case System.Net.Sockets.AddressFamily.InterNetworkV6:
								if (IP6 is null)
									IP6 = Addr;
								break;
						}
					}
				}

				if (!(IP4 is null))
					sb.Append(IP4.ToString());
				else if (!(IP6 is null))
					sb.Append(IP6.ToString());
				else
					sb.Append(Dns.GetHostName());
			}

			if (Array.IndexOf(Ports, DefaultPort) < 0 && Ports.Length > 0)
			{
				sb.Append(":");
				sb.Append(Ports[0].ToString());
			}

			sb.Append(LocalResource);

			return sb.ToString();
		}

		/// <summary>
		/// If a domain or host name represents the gateway.
		/// </summary>
		/// <param name="DomainOrHost">Domain or host name.</param>
		/// <param name="IncludeAlternativeDomains">If alternative domains are to be checked as well.</param>
		/// <returns>If the name represents the gateway.</returns>
		public static bool IsDomain(string DomainOrHost, bool IncludeAlternativeDomains)
		{
			if (DomainConfiguration.Instance?.UseDomainName ?? false)
			{
				if (DomainOrHost == domain)
					return true;

				if (IncludeAlternativeDomains && !(alternativeDomains is null))
				{
					foreach (CaseInsensitiveString s in alternativeDomains)
					{
						if (s == DomainOrHost)
							return true;
					}
				}
			}
			else
			{
				if (DomainOrHost == "localhost" || string.IsNullOrEmpty(DomainOrHost))
					return true;

				if (!(webServer is null))
				{
					foreach (IPAddress Addr in webServer.LocalIpAddresses)
					{
						if (Addr.ToString() == DomainOrHost)
							return true;
					}
				}

				if (DomainOrHost == Dns.GetHostName())
					return true;
			}

			return false;
		}

		private static void Resend(object P)
		{
			object[] P2 = (object[])P;
			SendNotification((string)P2[0], (string)P2[1], (string)P2[2], (string)P2[3], (string)P2[4], (bool)P2[5]);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Simplified configuration.
		/// </summary>
		internal static async Task SimplifiedConfiguration()
		{
			foreach (SystemConfiguration Configuration in configurations)
			{
				if (!Configuration.Complete)
				{
					bool Updated = await Configuration.EnvironmentConfiguration();

					if (!Configuration.Complete && await Configuration.SimplifiedConfiguration())
						Updated = true;

					if (Updated)
					{
						await Configuration.MakeCompleted();
						await Database.Update(Configuration);
					}
				}
			}
		}

		/// <summary>
		/// Gets the settings menu.
		/// </summary>
		/// <param name="Request">Current HTTP Request</param>
		/// <param name="UserVariable">Name of user variable</param>
		/// <returns></returns>
		public static WebMenuItem[] GetSettingsMenu(HttpRequest Request, string UserVariable)
		{
			List<WebMenuItem> Result = new List<WebMenuItem>();
			Variables Session = Request.Session;
			if (Session is null)
				return new WebMenuItem[0];

			Language Language = ScriptExtensions.Constants.Language.GetLanguageAsync(Session).Result;

			if (Session is null ||
				!Session.TryGetVariable(UserVariable, out Variable v) ||
				!(v.ValueObject is IUser User))
			{
				Result.Add(new WebMenuItem("Login", "/Login.md"));
			}
			else
			{
				if (!(configurations is null))
				{
					foreach (SystemConfiguration Configuration in configurations)
					{
						if (User.HasPrivilege("Settings." + Configuration.GetType().FullName))
							Result.Add(new WebMenuItem(Configuration.Title(Language).Result, Configuration.Resource));
					}
				}

				if (!Session.TryGetVariable(" AutoLogin ", out v) || !(v.ValueObject is bool AutoLogin) || !AutoLogin)
					Result.Add(new WebMenuItem("Logout", "/Logout"));
			}

			return Result.ToArray();
		}

		#endregion

		#region Smart Contracts

		/// <summary>
		/// XMPP Contracts Client, if such a compoent is available on the XMPP broker.
		/// </summary>
		public static ContractsClient ContractsClient
		{
			get { return contractsClient; }
			set
			{
				if (contractsClient is null ||
					contractsClient == value ||
					(value is null && LegalIdentityConfiguration.Instance is null))
				{
					contractsClient = value;
				}
				else
					throw new InvalidOperationException("Not allowed to set a new Contracts Client class.");
			}
		}

		/// <summary>
		/// Latest approved Legal Identity ID.
		/// </summary>
		public static string LatestApprovedLegalIdentityId
		{
			get { return LegalIdentityConfiguration.LatestApprovedLegalIdentityId; }
		}

		/// <summary>
		/// Requests the operator to sign a smart contract.
		/// </summary>
		/// <param name="Contract">Contract to sign. Must be ready to sign.</param>
		/// <param name="Role">Role to sign for. Must be available in contract.</param>
		/// <param name="Purpose">Purpose of contract. Must be one row.</param>
		/// <exception cref="ArgumentException">Any of the arguments are invalid.</exception>
		public static async Task RequestContractSignature(Contract Contract, string Role, string Purpose)
		{
			bool RoleFound = false;

			if (Contract is null)
				throw new ArgumentException("Contract cannot be null.", nameof(Contract));

			// TODO: Check contract server signature is valid.

			foreach (Networking.XMPP.Contracts.Role R in Contract.Roles)
			{
				if (R.Name == Role)
				{
					RoleFound = true;
					break;
				}
			}

			if (!RoleFound)
				throw new ArgumentException("Invalid role.", nameof(Role));

			if (string.IsNullOrEmpty(Purpose) || Purpose.IndexOfAny(CommonTypes.CRLF) >= 0)
				throw new ArgumentException("Invalid purpose.", nameof(Purpose));

			try
			{
				string Module = string.Empty;
				int Skip = 1;

				while (true)
				{
					StackFrame Frame = new StackFrame(Skip);
					MethodBase Method = Frame.GetMethod();
					if (Method is null)
						break;

					Type Type = Method.DeclaringType;
					Assembly Assembly = Type.Assembly;
					Module = Assembly.GetName().Name;

					if (Type != typeof(Gateway) && !Module.StartsWith("System."))
						break;

					Skip++;
				}

				string Markdown;
				ContractSignatureRequest Request = await Database.FindFirstIgnoreRest<ContractSignatureRequest>(new FilterAnd(
					new FilterFieldEqualTo("ContractId", Contract.ContractId),
					new FilterFieldEqualTo("Role", Role),
					new FilterFieldEqualTo("Module", Module),
					new FilterFieldEqualTo("Provider", Contract.Provider),
					new FilterFieldEqualTo("Purpose", Purpose)));


				if (Request is null)
				{
					Request = new ContractSignatureRequest()
					{
						Received = DateTime.Now,
						Signed = null,
						ContractId = Contract.ContractId,
						Role = Role,
						Module = Module,
						Provider = Contract.Provider,
						Purpose = Purpose
					};

					Request.SetContract(Contract);

					await Database.Insert(Request);

					Markdown = await Resources.ReadAllTextAsync(Path.Combine(rootFolder, "Settings", "SignatureRequest.md"));

					int i = Markdown.IndexOf("~~~~~~");
					int c = Markdown.Length;

					if (i >= 0)
					{
						i += 6;
						while (i < c && Markdown[i] == '~')
							i++;

						Markdown = Markdown.Substring(i).TrimStart();
					}

					i = Markdown.IndexOf("~~~~~~");
					if (i > 0)
						Markdown = Markdown.Substring(0, i).TrimEnd();

					Variables Variables = HttpServer.CreateVariables();
					Variables["RequestId"] = Request.ObjectId;
					Variables["Request"] = Request;

					MarkdownSettings Settings = new MarkdownSettings(emoji1_24x24, false, Variables);
					Markdown = await MarkdownDocument.Preprocess(Markdown, Settings);

					StringBuilder sb = new StringBuilder(Markdown);

					sb.AppendLine();
					sb.Append("Link: [`");
					sb.Append(Request.ContractId);
					sb.Append("](");
					sb.Append(GetUrl("/SignatureRequest.md?RequestId=" + Request.ObjectId));
					sb.AppendLine(")");

					Markdown = sb.ToString();
				}
				else
				{
					Markdown = "**Reminder**: Smart Contract [" + Request.ContractId + "](" +
						GetUrl("/SignatureRequest.md?RequestId=" + Request.ObjectId) + ") is waiting for your signature.";
				}

				await SendNotification(Markdown);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Petitions information about a legal identity from its owner.
		/// </summary>
		/// <param name="LegalId">ID of petitioned legal identity.</param>
		/// <param name="PetitionId">A petition ID string used to identity request when response is returned.</param>
		/// <param name="Purpose">String containing purpose of petition. Can be seen by owner, as well as the legal identity of the current machine.</param>
		/// <param name="Callback">Method to call when response is returned. If timed out, or declined, identity will be null.</param>
		/// <param name="Timeout">Maximum time to wait for a response.</param>
		/// <returns>If a legal identity was found that could be used to sign the petition.</returns>
		public static Task<bool> PetitionLegalIdentity(string LegalId, string PetitionId, string Purpose,
			LegalIdentityPetitionResponseEventHandler Callback, TimeSpan Timeout)
		{
			return LegalIdentityConfiguration.Instance.PetitionLegalIdentity(LegalId, PetitionId, Purpose, Callback, Timeout);
		}

		/// <summary>
		/// Petitions information about a legal identity from its owner.
		/// </summary>
		/// <param name="LegalId">ID of petitioned legal identity.</param>
		/// <param name="PetitionId">A petition ID string used to identity request when response is returned.</param>
		/// <param name="Purpose">String containing purpose of petition. Can be seen by owner, as well as the legal identity of the current machine.</param>
		/// <param name="Password">Password of legal identity on the current machine used to sign the petition.</param>
		/// <param name="Callback">Method to call when response is returned. If timed out, or declined, identity will be null.</param>
		/// <param name="Timeout">Maximum time to wait for a response.</param>
		/// <returns>If a legal identity was found that could be used to sign the petition, and the password matched (if protected by password).</returns>
		public static Task<bool> PetitionLegalIdentity(string LegalId, string PetitionId, string Purpose, string Password,
			LegalIdentityPetitionResponseEventHandler Callback, TimeSpan Timeout)
		{
			return LegalIdentityConfiguration.Instance.PetitionLegalIdentity(LegalId, PetitionId, Purpose, Password, Callback, Timeout);
		}

		/// <summary>
		/// Petitions information about a smart contract from its owner.
		/// </summary>
		/// <param name="ContractId">ID of petitioned smart contract.</param>
		/// <param name="PetitionId">A petition ID string used to contract request when response is returned.</param>
		/// <param name="Purpose">String containing purpose of petition. Can be seen by owner, as well as the smart contract of the current machine.</param>
		/// <param name="Callback">Method to call when response is returned. If timed out, or declined, identity will be null.</param>
		/// <param name="Timeout">Maximum time to wait for a response.</param>
		/// <returns>If a smart contract was found that could be used to sign the petition.</returns>
		public static Task<bool> PetitionContract(string ContractId, string PetitionId, string Purpose,
			ContractPetitionResponseEventHandler Callback, TimeSpan Timeout)
		{
			return LegalIdentityConfiguration.Instance.PetitionContract(ContractId, PetitionId, Purpose, Callback, Timeout);
		}

		/// <summary>
		/// Petitions information about a smart contract from its owner.
		/// </summary>
		/// <param name="ContractId">ID of petitioned smart contract.</param>
		/// <param name="PetitionId">A petition ID string used to contract request when response is returned.</param>
		/// <param name="Purpose">String containing purpose of petition. Can be seen by owner, as well as the smart contract of the current machine.</param>
		/// <param name="Password">Password of smart contract on the current machine used to sign the petition.</param>
		/// <param name="Callback">Method to call when response is returned. If timed out, or declined, identity will be null.</param>
		/// <param name="Timeout">Maximum time to wait for a response.</param>
		/// <returns>If a smart contract was found that could be used to sign the petition, and the password matched (if protected by password).</returns>
		public static Task<bool> PetitionContract(string ContractId, string PetitionId, string Purpose, string Password,
			ContractPetitionResponseEventHandler Callback, TimeSpan Timeout)
		{
			return LegalIdentityConfiguration.Instance.PetitionContract(ContractId, PetitionId, Purpose, Password, Callback, Timeout);
		}

		#endregion

		#region Finding Files

		/// <summary>
		/// Finds files in a set of folders, and optionally, their subfolders. This method only finds files in folders
		/// the application as access rights to.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="IncludeSubfolders">If subfolders are to be included.</param>
		/// <param name="BreakOnFirst">If the search should break when it finds the first file.</param>
		/// <returns>Files matching the pattern found in the corresponding folders.</returns>
		public static string[] FindFiles(Environment.SpecialFolder[] Folders, string Pattern, bool IncludeSubfolders, bool BreakOnFirst)
		{
			return FileSystem.FindFiles(Folders, Pattern, IncludeSubfolders, BreakOnFirst);
		}

		/// <summary>
		/// Finds files in a set of folders, and optionally, their subfolders. This method only finds files in folders
		/// the application as access rights to.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="IncludeSubfolders">If subfolders are to be included.</param>
		/// <param name="BreakOnFirst">If the search should break when it finds the first file.</param>
		/// <returns>Files matching the pattern found in the corresponding folders.</returns>
		public static string[] FindFiles(string[] Folders, string Pattern, bool IncludeSubfolders, bool BreakOnFirst)
		{
			return FileSystem.FindFiles(Folders, Pattern, IncludeSubfolders, BreakOnFirst);
		}

		/// <summary>
		/// Finds files in a set of folders, and optionally, their subfolders. This method only finds files in folders
		/// the application as access rights to.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="IncludeSubfolders">If subfolders are to be included.</param>
		/// <param name="MaxCount">Maximum number of files to return.</param>
		/// <returns>Files matching the pattern found in the corresponding folders.</returns>
		public static string[] FindFiles(string[] Folders, string Pattern, bool IncludeSubfolders, int MaxCount)
		{
			return FileSystem.FindFiles(Folders, Pattern, IncludeSubfolders, MaxCount);
		}

		/// <summary>
		/// Finds files in a set of folders, and optionally, their subfolders. This method only finds files in folders
		/// the application as access rights to.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="SubfolderDepth">Maximum folder depth to search.</param>
		/// <param name="MaxCount">Maximum number of files to return.</param>
		/// <returns>Files matching the pattern found in the corresponding folders.</returns>
		public static string[] FindFiles(string[] Folders, string Pattern, int SubfolderDepth, int MaxCount)
		{
			return FileSystem.FindFiles(Folders, Pattern, SubfolderDepth, MaxCount);
		}

		/// <summary>
		/// Gets the physical locations of special folders.
		/// </summary>
		/// <param name="Folders">Special folders.</param>
		/// <param name="AppendWith">Append result with this array of folders.</param>
		/// <returns>Physical locations. Only the physical locations of defined special folders are returned.</returns>
		public static string[] GetFolders(Environment.SpecialFolder[] Folders, params string[] AppendWith)
		{
			return FileSystem.GetFolders(Folders, AppendWith);
		}

		/// <summary>
		/// Finds the latest file matching a search pattern, by searching in a set of folders, and optionally, their subfolders. 
		/// This method only finds files in folders the application as access rights to. If no file is found, the empty string
		/// is returned.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="IncludeSubfolders">If subfolders are to be included.</param>
		/// <returns>Latest file if found, empty string if not.</returns>
		public static string FindLatestFile(Environment.SpecialFolder[] Folders, string Pattern, bool IncludeSubfolders)
		{
			return FileSystem.FindLatestFile(Folders, Pattern, IncludeSubfolders);
		}

		/// <summary>
		/// Finds the latest file matching a search pattern, by searching in a set of folders, and optionally, their subfolders. 
		/// This method only finds files in folders the application as access rights to. If no file is found, the empty string
		/// is returned.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="IncludeSubfolders">If subfolders are to be included.</param>
		/// <returns>Latest file if found, empty string if not.</returns>
		public static string FindLatestFile(string[] Folders, string Pattern, bool IncludeSubfolders)
		{
			return FileSystem.FindLatestFile(Folders, Pattern, IncludeSubfolders);
		}

		/// <summary>
		/// Finds the latest file matching a search pattern, by searching in a set of folders, and optionally, their subfolders. 
		/// This method only finds files in folders the application as access rights to. If no file is found, the empty string
		/// is returned.
		/// </summary>
		/// <param name="Folders">Folders to search in.</param>
		/// <param name="Pattern">Pattern to search for.</param>
		/// <param name="SubfolderDepth">Maximum folder depth to search.</param>
		/// <returns>Latest file if found, empty string if not.</returns>
		public static string FindLatestFile(string[] Folders, string Pattern, int SubfolderDepth)
		{
			return FileSystem.FindLatestFile(Folders, Pattern, SubfolderDepth);
		}

		#endregion

		#region Custom Errors

		private static readonly Dictionary<string, KeyValuePair<DateTime, MarkdownDocument>> defaultDocuments = new Dictionary<string, KeyValuePair<DateTime, MarkdownDocument>>();

		private static async Task WebServer_CustomError(object Sender, CustomErrorEventArgs e)
		{
			HttpFieldAccept Accept = e.Request?.Header?.Accept;
			if (Accept is null || Accept.Value == "*/*")
				return;

			if (Accept.IsAcceptable("text/html"))
			{
				string Html = await GetCustomErrorHtml(e.Request, e.StatusCode.ToString() + ".md", e.ContentType, e.Content);

				if (!string.IsNullOrEmpty(Html))
					e.SetContent("text/html; charset=utf-8", System.Text.Encoding.UTF8.GetBytes(Html));
			}
		}

		/// <summary>
		/// Gets a custom error HTML document.
		/// </summary>
		/// <param name="Request">Request for which the error document is to be prepared.</param>
		/// <param name="LocalFileName">Local file name of the custom error markdown document.</param>
		/// <param name="ContentType">Content-Type of embedded content.</param>
		/// <param name="Content">Binary encoding of embedded content.</param>
		/// <returns>HTML document, if it could be found and generated, or null otherwise.</returns>
		public static async Task<string> GetCustomErrorHtml(HttpRequest Request, string LocalFileName, string ContentType, byte[] Content)
		{
			bool IsText;
			bool IsMarkdown;
			bool IsEmpty;

			if (string.IsNullOrEmpty(ContentType))
			{
				IsText = IsMarkdown = false;
				IsEmpty = true;
			}
			else
			{
				IsText = ContentType.StartsWith(PlainTextCodec.DefaultContentType);
				IsMarkdown = ContentType.StartsWith("text/markdown");
				IsEmpty = false;
			}

			if (IsEmpty || IsText || IsMarkdown)
			{
				MarkdownDocument Doc;
				DateTime TP;

				lock (defaultDocuments)
				{
					if (defaultDocuments.TryGetValue(LocalFileName, out KeyValuePair<DateTime, MarkdownDocument> P))
					{
						TP = P.Key;
						Doc = P.Value;
					}
					else
					{
						TP = DateTime.MinValue;
						Doc = null;
					}
				}

				string FullFileName = Path.Combine(appDataFolder, "Default", LocalFileName);

				if (File.Exists(FullFileName))
				{
					DateTime TP2 = File.GetLastWriteTime(FullFileName);
					MarkdownSettings Settings;
					MarkdownDocument Detail;
					string Markdown;

					if (Doc is null || TP2 > TP)
					{
						Markdown = await Resources.ReadAllTextAsync(FullFileName);
						Settings = new MarkdownSettings(emoji1_24x24, true)
						{
							RootFolder = rootFolder,
							Variables = Request.Session ?? HttpServer.CreateVariables()
						};

						if (!Settings.Variables.ContainsVariable("Request"))
							Settings.Variables["Request"] = Request;

						Doc = await MarkdownDocument.CreateAsync(Markdown, Settings, RootFolder, string.Empty, string.Empty);

						lock (defaultDocuments)
						{
							defaultDocuments[LocalFileName] = new KeyValuePair<DateTime, MarkdownDocument>(TP2, Doc);
						}
					}

					if (IsEmpty || Content is null)
						Detail = null;
					else
					{
						System.Text.Encoding Encoding = null;
						int i = ContentType.IndexOf(';');

						if (i > 0)
						{
							KeyValuePair<string, string>[] Fields = CommonTypes.ParseFieldValues(ContentType.Substring(i + 1).TrimStart());

							foreach (KeyValuePair<string, string> Field in Fields)
							{
								if (Field.Key.ToUpper() == "CHARSET")
									Encoding = InternetContent.GetEncoding(Field.Value);
							}
						}

						if (Encoding is null)
							Encoding = System.Text.Encoding.UTF8;

						Markdown = CommonTypes.GetString(Content, Encoding);
						if (IsText)
						{
							MarkdownSettings Settings2 = new MarkdownSettings(null, false);
							Detail = await MarkdownDocument.CreateAsync("```\r\n" + Markdown + "\r\n```", Settings2);
						}
						else
							Detail = await MarkdownDocument.CreateAsync(Markdown, Doc.Settings);
					}

					if (!(Doc.Tag is MultiReadSingleWriteObject DocSynchObj))
					{
						DocSynchObj = new MultiReadSingleWriteObject();
						Doc.Tag = DocSynchObj;
					}

					if (await DocSynchObj.TryBeginWrite(30000))
					{
						try
						{
							Doc.Detail = Detail;
							return await Doc.GenerateHTML();
						}
						finally
						{
							await DocSynchObj.EndWrite();
						}
					}
					else
						throw new ServiceUnavailableException("Unable to generate custom HTML error document.");
				}
				else
				{
					if (!(Doc is null))
					{
						lock (defaultDocuments)
						{
							defaultDocuments.Remove(LocalFileName);
						}
					}
				}
			}

			return null;
		}

		#endregion

		#region Sniffers & Events

		/// <summary>
		/// Creates a web sniffer, and adds it to a sniffable object.
		/// </summary>
		/// <param name="SnifferId">Sniffer ID</param>
		/// <param name="Request">Current HTTP request fetching a page displaying the sniffer.</param>
		/// <param name="Sniffable">Object being sniffed</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer Markdown.</returns>
		public static string AddWebSniffer(string SnifferId, HttpRequest Request, ISniffable Sniffable, string UserVariable, params string[] Privileges)
		{
			return AddWebSniffer(SnifferId, Request, BinaryPresentationMethod.ByteCount, Sniffable, UserVariable, Privileges);
		}

		/// <summary>
		/// Creates a web sniffer, and adds it to a sniffable object.
		/// </summary>
		/// <param name="SnifferId">Sniffer ID</param>
		/// <param name="Request">Current HTTP request fetching a page displaying the sniffer.</param>
		/// <param name="BinaryPresentationMethod">How binary data is to be presented.</param>
		/// <param name="Sniffable">Object being sniffed</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer Markdown.</returns>
		public static string AddWebSniffer(string SnifferId, HttpRequest Request, BinaryPresentationMethod BinaryPresentationMethod,
			ISniffable Sniffable, string UserVariable, params string[] Privileges)
		{
			return AddWebSniffer(SnifferId, Request, TimeSpan.FromHours(1), BinaryPresentationMethod, Sniffable, UserVariable, Privileges);
		}

		/// <summary>
		/// Creates a web sniffer, and adds it to a sniffable object.
		/// </summary>
		/// <param name="SnifferId">Sniffer ID</param>
		/// <param name="Request">Current HTTP request fetching a page displaying the sniffer.</param>
		/// <param name="MaxLife">Maximum life of sniffer.</param>
		/// <param name="BinaryPresentationMethod">How binary data is to be presented.</param>
		/// <param name="Sniffable">Object being sniffed</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer Markdown.</returns>
		public static string AddWebSniffer(string SnifferId, HttpRequest Request, TimeSpan MaxLife,
			BinaryPresentationMethod BinaryPresentationMethod, ISniffable Sniffable, string UserVariable, params string[] Privileges)
		{
			string Resource = Request.Header.ResourcePart;
			int i = Resource.IndexOfAny(new char[] { '?', '#' });
			if (i > 0)
				Resource = Resource.Substring(0, i);

			return AddWebSniffer(SnifferId, Resource, MaxLife, BinaryPresentationMethod, Sniffable, UserVariable, Privileges);
		}

		/// <summary>
		/// Creates a web sniffer, and adds it to a sniffable object.
		/// </summary>
		/// <param name="SnifferId">Sniffer ID</param>
		/// <param name="PageResource">Resource of page displaying the sniffer.</param>
		/// <param name="MaxLife">Maximum life of sniffer.</param>
		/// <param name="BinaryPresentationMethod">How binary data is to be presented.</param>
		/// <param name="Sniffable">Object being sniffed</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer Markdown.</returns>
		public static string AddWebSniffer(string SnifferId, string PageResource, TimeSpan MaxLife,
			BinaryPresentationMethod BinaryPresentationMethod, ISniffable Sniffable, string UserVariable, params string[] Privileges)
		{
			bool Found = false;

			foreach (ISniffer Sniffer in Sniffable)
			{
				if (Sniffer is WebSniffer WebSniffer && WebSniffer.SnifferId == SnifferId)
				{
					Found = true;
					break;
				}
			}

			if (!Found)
			{
				WebSniffer Sniffer = new WebSniffer(SnifferId, PageResource, MaxLife, BinaryPresentationMethod, Sniffable, UserVariable, Privileges);
				Sniffable.Add(Sniffer);
			}

			return "\r\n\r\n![Sniffer](/Sniffers/Sniffer.md)\r\n\r\n";
		}

		/// <summary>
		/// Creates a web event sink, and registers it with <see cref="Log"/>.
		/// </summary>
		/// <param name="SinkId">Event Sink ID</param>
		/// <param name="Request">Current HTTP request fetching a page displaying the sniffer.</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer object.</returns>
		public static void AddWebEventSink(string SinkId, HttpRequest Request, string UserVariable, params string[] Privileges)
		{
			AddWebEventSink(SinkId, Request, TimeSpan.FromHours(1), UserVariable, Privileges);
		}

		/// <summary>
		/// Creates a web event sink, and registers it with <see cref="Log"/>.
		/// </summary>
		/// <param name="SinkId">Event Sink ID</param>
		/// <param name="Request">Current HTTP request fetching a page displaying the sniffer.</param>
		/// <param name="MaxLife">Maximum life of event sink.</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer object.</returns>
		public static void AddWebEventSink(string SinkId, HttpRequest Request, TimeSpan MaxLife, string UserVariable, params string[] Privileges)
		{
			string Resource = Request.Header.ResourcePart;
			int i = Resource.IndexOfAny(new char[] { '?', '#' });
			if (i > 0)
				Resource = Resource.Substring(0, i);

			AddWebEventSink(SinkId, Resource, MaxLife, UserVariable, Privileges);
		}

		/// <summary>
		/// Creates a web event sink, and registers it with <see cref="Log"/>.
		/// </summary>
		/// <param name="SinkId">Event Sink ID</param>
		/// <param name="PageResource">Resource of page displaying the events.</param>
		/// <param name="MaxLife">Maximum life of event sink.</param>
		/// <param name="UserVariable">Event is only pushed to clients with a session contining a variable 
		/// named <paramref name="UserVariable"/> having a value derived from <see cref="IUser"/>.</param>
		/// <param name="Privileges">Event is only pushed to clients with a user variable having the following set of privileges.</param>
		/// <returns>Web Sniffer object.</returns>
		public static void AddWebEventSink(string SinkId, string PageResource, TimeSpan MaxLife, string UserVariable, params string[] Privileges)
		{
			bool Found = false;

			foreach (IEventSink Sink in Log.Sinks)
			{
				if (Sink is WebEventSink WebEventSink && WebEventSink.ObjectID == SinkId)
				{
					Found = true;
					break;
				}
			}

			if (!Found)
			{
				WebEventSink Sink = new WebEventSink(SinkId, PageResource, MaxLife, UserVariable, Privileges);
				Log.Register(Sink);
			}
		}

		#endregion

		#region Script Resources

		/// <summary>
		/// Adds a script resource to the web server hosted by the gateway.
		/// </summary>
		/// <param name="ResourceName">Resource name of script resource.</param>
		/// <param name="Expression">Expression to be executed when resource is accessed.</param>
		/// <param name="ReferenceFileName">Optional reference file name for script.</param>
		/// <returns>If script resource could be added.</returns>
		public static async Task<bool> AddScriptResource(string ResourceName, Expression Expression, string ReferenceFileName)
		{
			if (!await RemoveScriptResource(ResourceName, true))
				return false;

			webServer.Register(new HttpScriptResource(ResourceName, Expression, ReferenceFileName, true));

			await RuntimeSettings.SetAsync("Gateway.ScriptResource." + ResourceName, ReferenceFileName + " ||| " + Expression.Script);

			return true;
		}

		/// <summary>
		/// Adds a script resource to the web server hosted by the gateway.
		/// </summary>
		/// <param name="ResourceName">Resource name of script resource.</param>
		/// <param name="Expression">Expression to be executed when resource is accessed.</param>
		/// <param name="ReferenceFileName">Optional reference file name for script.</param>
		/// <returns>If script resource could be added.</returns>
		public static async Task<bool> AddScriptResource(string ResourceName, ScriptNode Expression, string ReferenceFileName)
		{
			if (!await RemoveScriptResource(ResourceName, true))
				return false;

			webServer.Register(new HttpScriptResource(ResourceName, Expression, ReferenceFileName, true));

			await RuntimeSettings.SetAsync("Gateway.ScriptResource." + ResourceName, ReferenceFileName + " ||| " + Expression.SubExpression);

			return true;
		}

		/// <summary>
		/// Removes a script resource from the web server hosted by the gateway.
		/// </summary>
		/// <param name="ResourceName">Resource name of script resource.</param>
		/// <returns>If a script resource with the given resource name was found and removed.</returns>
		public static Task<bool> RemoveScriptResource(string ResourceName)
		{
			return RemoveScriptResource(ResourceName, false);
		}

		/// <summary>
		/// Removes a script resource from the web server hosted by the gateway.
		/// </summary>
		/// <param name="ResourceName">Resource name of script resource.</param>
		/// <param name="ConsiderNonexistantRemoved">How to treat the case if a resource does not exist.</param>
		/// <returns>If a script resource with the given resource name was found and removed.</returns>
		private static async Task<bool> RemoveScriptResource(string ResourceName, bool ConsiderNonexistantRemoved)
		{
			if (!webServer.TryGetResource(ResourceName, false, out HttpResource Resource, out string SubPath))
				return false;

			if (!string.IsNullOrEmpty(SubPath))
				return ConsiderNonexistantRemoved;

			if (!(Resource is HttpScriptResource))
				return false;

			webServer.Unregister(Resource);

			await RuntimeSettings.DeleteAsync("Gateway.ScriptResource." + ResourceName);

			return true;
		}

		private static async Task LoadScriptResources()
		{
			Dictionary<string, object> Settings = await RuntimeSettings.GetWhereKeyLikeAsync("Gateway.ScriptResource.*", "*");

			foreach (KeyValuePair<string, object> Setting in Settings)
			{
				if (!(Setting.Value is string Value))
				{
					Log.Error("Invalid Runtime setting found and ignored.",
						new KeyValuePair<string, object>("Key", Setting.Key),
						new KeyValuePair<string, object>("Value", Setting.Value));

					continue;
				}

				string ResourceName = Setting.Key.Substring(23);
				string ReferenceFileName;
				int i;
				Expression Exp;

				i = Value.IndexOf(" ||| ");
				if (i < 0)
					ReferenceFileName = string.Empty;
				else
				{
					ReferenceFileName = Value.Substring(0, i);
					Value = Value.Substring(i + 5);
				}

				try
				{
					Exp = new Expression(Value);
					webServer.Register(new HttpScriptResource(ResourceName, Exp, ReferenceFileName, true));
				}
				catch (Exception ex)
				{
					Log.Error("Invalid Runtime setting script. Resource could not be added.",
						new KeyValuePair<string, object>("Resource", ResourceName),
						new KeyValuePair<string, object>("Error", ex.Message),
						new KeyValuePair<string, object>("ReferenceFileName", ReferenceFileName),
						new KeyValuePair<string, object>("Script", Value));

					continue;
				}
			}
		}

		/// <summary>
		/// Processes new Service Configuration Files. This method should be called after installation of new
		/// services (or updating such services) containg such service configuration files in the root application 
		/// data folder.
		/// </summary>
		/// <returns>Number of configuration files executed.</returns>
		public static Task<int> ProcessNewServiceConfigurations()
		{
			return ProcessServiceConfigurations(true);
		}

		private static async Task<int> ProcessServiceConfigurations(bool OnlyIfChanged)
		{
			string[] ConfigurationFiles = Directory.GetFiles(appDataFolder, "*.config", SearchOption.TopDirectoryOnly);
			int NrExecuted = 0;

			foreach (string ConfigurationFile in ConfigurationFiles)
			{
				if (await ProcessServiceConfigurationFile(ConfigurationFile, OnlyIfChanged))
					NrExecuted++;
			}

			return NrExecuted;
		}

		private const string ServiceConfigurationRoot = "ServiceConfiguration";
		private const string ServiceConfigurationNamespace = "http://waher.se/Schema/ServiceConfiguration.xsd";

		/// <summary>
		/// Processes a Service Configuration File. This method should be called for each service configuration file,
		/// either when service starts, or when file is updated or is installed.
		/// </summary>
		/// <param name="ConfigurationFileName">File Name of Service Configuration file.</param>
		/// <param name="OnlyIfChanged">Only execute contents in file, if file has changed.</param>
		/// <returns>If file was successfully loaded and executed.</returns>
		public static async Task<bool> ProcessServiceConfigurationFile(string ConfigurationFileName, bool OnlyIfChanged)
		{
			try
			{
				ConfigurationFileName = Path.GetFullPath(ConfigurationFileName);

				string DirectoryName = Path.GetDirectoryName(ConfigurationFileName);
				if (!DirectoryName.EndsWith(new string(Path.DirectorySeparatorChar, 1)))
					DirectoryName += Path.DirectorySeparatorChar;

				if (string.Compare(DirectoryName, appDataFolder, true) != 0)
					return false;

				if (!File.Exists(ConfigurationFileName))
					return false;

				XmlDocument Doc = new XmlDocument();
				Doc.Load(ConfigurationFileName);

				if (Doc.DocumentElement.LocalName != ServiceConfigurationRoot || Doc.DocumentElement.NamespaceURI != ServiceConfigurationNamespace)
					return false;

				XSL.Validate(Path.GetFileName(ConfigurationFileName), Doc, ServiceConfigurationRoot, ServiceConfigurationNamespace,
					XSL.LoadSchema(typeof(Gateway).Namespace + ".Schema.ServiceConfiguration.xsd", typeof(Gateway).Assembly));

				bool ExecuteInitScript = await Content.Markdown.Functions.InitScriptFile.NeedsExecution(ConfigurationFileName);

				if (OnlyIfChanged && !ExecuteInitScript)
					return false;

				Log.Notice("Applying Service Configurations.", ConfigurationFileName);

				webServer.UnregisterVanityResources(ConfigurationFileName);

				foreach (XmlNode N in Doc.DocumentElement.ChildNodes)
				{
					if (!(N is XmlElement E))
						continue;

					switch (E.LocalName)
					{
						case "VanityResources":
							foreach (XmlNode N2 in E.ChildNodes)
							{
								if (N2 is XmlElement E2 && E2.LocalName == "VanityResource")
								{
									string RegEx = XML.Attribute(E2, "regex");
									string Url = XML.Attribute(E2, "url");

									try
									{
										webServer.RegisterVanityResource(RegEx, Url, ConfigurationFileName);
									}
									catch (Exception ex)
									{
										Log.Error("Unable to register vanity resource: " + ex.Message,
											new KeyValuePair<string, object>("RegEx", RegEx),
											new KeyValuePair<string, object>("Url", Url));
									}
								}
							}
							break;

						case "StartupScript":           // Always execute
							Expression Exp = new Expression(E.InnerText);
							Variables v = HttpServer.CreateVariables();
							await Exp.EvaluateAsync(v);
							break;

						case "InitializationScript":    // Execute, only if changed
							if (ExecuteInitScript)
							{
								Exp = new Expression(E.InnerText);
								v = HttpServer.CreateVariables();
								await Exp.EvaluateAsync(v);
							}
							break;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.Critical(ex, ConfigurationFileName);
				return false;
			}
		}

		#endregion
	}
}
