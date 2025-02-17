﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Security.CallStack;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Waher.IoTGateway.App
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
			this.Suspending += this.OnSuspending;
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (!(Window.Current.Content is Frame rootFrame))
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();

				rootFrame.NavigationFailed += this.OnNavigationFailed;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					//TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}

			if (e.PrelaunchActivated == false)
			{
				if (rootFrame.Content is null)
				{
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation
					// parameter
					rootFrame.Navigate(typeof(MainPage), e.Arguments);
				}
				// Ensure the current window is active
				Window.Current.Activate();
				Task.Run((Action)this.Init);
			}
		}

		private async void Init()
		{
			try
			{
				Log.RegisterAlertExceptionType(true,
					typeof(OutOfMemoryException),
					typeof(StackOverflowException),
					typeof(AccessViolationException),
					typeof(InsufficientMemoryException),
					typeof(UnauthorizedCallstackException));

				Log.RegisterExceptionToUnnest(typeof(System.Runtime.InteropServices.ExternalException));
				Log.RegisterExceptionToUnnest(typeof(System.Security.Authentication.AuthenticationException));

				AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
				{
					if (e.IsTerminating)
					{
						string FileName = Path.Combine(Gateway.AppDataFolder, "UnhandledException.txt");
						Networking.Sniffers.XmlFileSniffer.MakeUnique(ref FileName);

						using (StreamWriter w = File.CreateText(FileName))
						{
							w.Write("Type: ");

							if (!(e.ExceptionObject is null))
								w.WriteLine(e.ExceptionObject.GetType().FullName);
							else
								w.WriteLine("null");

							w.Write("Time: ");
							w.WriteLine(DateTime.Now.ToString());

							w.WriteLine();
							if (e.ExceptionObject is Exception ex)
							{
								while (!(ex is null))
								{
									w.WriteLine(ex.Message);
									w.WriteLine();
									w.WriteLine(Log.CleanStackTrace(ex.StackTrace));
									w.WriteLine();

									ex = ex.InnerException;
								}
							}
							else
							{
								if (!(e.ExceptionObject is null))
									w.WriteLine(e.ExceptionObject.ToString());

								w.WriteLine();
								w.WriteLine(Log.CleanStackTrace(Environment.StackTrace));
							}

							w.Flush();
						}

						if (e.ExceptionObject is Exception ex2)
							Log.Emergency(ex2);
						else if (!(e.ExceptionObject is null))
							Log.Emergency(e.ExceptionObject.ToString());
						else
							Log.Emergency("Unexpected null exception thrown.");

						Gateway.Stop().Wait();
						Log.Terminate();
					}
					else
					{
						if (e.ExceptionObject is Exception ex2)
							Log.Alert(ex2);
						else if (!(e.ExceptionObject is null))
							Log.Alert(e.ExceptionObject.ToString());
						else
							Log.Alert("Unexpected null exception thrown.");
					}
				};

				TaskScheduler.UnobservedTaskException += (sender, e) =>
				{
					Exception ex = Log.UnnestException(e.Exception);
					string StackTrace = Log.CleanStackTrace(ex.StackTrace);

					Log.Alert("Unobserved Task Exception\r\n============================\r\n\r\n" + ex.Message + "\r\n\r\n```\r\n" + StackTrace + "\r\n```");
					
					e.SetObserved();
				};

				Gateway.GetDatabaseProvider += GetDatabase;
				Gateway.RegistrationSuccessful += RegistrationSuccessful;
				Gateway.GetMetaData += this.GetMetaData;
				Gateway.OnTerminate += this.Gateway_OnTerminate;

				if (!await Gateway.Start(false, false, string.Empty))
					throw new Exception("Gateway being started in another process.");
			}
			catch (Exception ex)
			{
				Log.Emergency(ex);

				MessageDialog Dialog = new MessageDialog(ex.Message, "Error");
				await MainPage.Instance.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
					async () => await Dialog.ShowAsync());
			}
		}

		private void Gateway_OnTerminate(object sender, EventArgs e)
		{
			Gateway.Stop().Wait();
			Log.Terminate();
			Window.Current.Close();
		}

		private static async Task<IDatabaseProvider> GetDatabase(XmlElement DatabaseConfig)
		{
			if (CommonTypes.TryParse(DatabaseConfig.Attributes["encrypted"].Value, out bool Encrypted) && Encrypted)
				throw new Exception("Encrypted database storage not supported on this platform.");

			FilesProvider Result = await FilesProvider.CreateAsync(Gateway.AppDataFolder + DatabaseConfig.Attributes["folder"].Value,
				DatabaseConfig.Attributes["defaultCollectionName"].Value,
				int.Parse(DatabaseConfig.Attributes["blockSize"].Value),
				int.Parse(DatabaseConfig.Attributes["blocksInCache"].Value),
				int.Parse(DatabaseConfig.Attributes["blobBlockSize"].Value), System.Text.Encoding.UTF8,
				int.Parse(DatabaseConfig.Attributes["timeoutMs"].Value));

			return Result;
		}

		private async Task<MetaDataTag[]> GetMetaData(MetaDataTag[] MetaData)
		{
			List<MetaDataTag> Result = new List<MetaDataTag>(MetaData);
			string s;

			if (await RuntimeSettings.GetAsync("ThingRegistry.Location", false))
			{
				s = await RuntimeSettings.GetAsync("ThingRegistry.Country", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("COUNTRY", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Region", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("REGION", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.City", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("CITY", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Area", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("AREA", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Street", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("STREET", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.StreetNr", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("STREETNR", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Building", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("BLD", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Apartment", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("APT", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Room", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("ROOM", s));

				s = await RuntimeSettings.GetAsync("ThingRegistry.Name", string.Empty);
				if (!string.IsNullOrEmpty(s))
					Result.Add(new MetaDataStringTag("NAME", s));
			}
			else
			{
				TaskCompletionSource<bool> UserInput = new TaskCompletionSource<bool>();

				await MainPage.Instance.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					try
					{
						RegistrationDialog Dialog = new RegistrationDialog();

						switch (await Dialog.ShowAsync())
						{
							case ContentDialogResult.Primary:
								await RuntimeSettings.SetAsync("ThingRegistry.Country", s = Dialog.Reg_Country);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("COUNTRY", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Region", s = Dialog.Reg_Region);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("REGION", s));

								await RuntimeSettings.SetAsync("ThingRegistry.City", s = Dialog.Reg_City);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("CITY", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Area", s = Dialog.Reg_Area);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("AREA", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Street", s = Dialog.Reg_Street);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("STREET", s));

								await RuntimeSettings.SetAsync("ThingRegistry.StreetNr", s = Dialog.Reg_StreetNr);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("STREETNR", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Building", s = Dialog.Reg_Building);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("BLD", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Apartment", s = Dialog.Reg_Apartment);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("APT", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Room", s = Dialog.Reg_Room);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("ROOM", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Name", s = Dialog.Name);
								if (!string.IsNullOrEmpty(s))
									Result.Add(new MetaDataStringTag("NAME", s));

								await RuntimeSettings.SetAsync("ThingRegistry.Location", true);

								UserInput.SetResult(true);
								break;

							case ContentDialogResult.Secondary:
								UserInput.SetResult(false);
								break;
						}
					}
					catch (Exception ex)
					{
						Log.Exception(ex);
					}
				});

				await UserInput.Task;
			}

			return Result.ToArray();
		}

		private static async Task RegistrationSuccessful(MetaDataTag[] MetaData, RegistrationEventArgs e)
		{
			if (!e.IsClaimed && Types.TryGetModuleParameter("Registry", out object Obj) && Obj is ThingRegistryClient ThingRegistryClient)
			{
				string ClaimUrl = ThingRegistryClient.EncodeAsIoTDiscoURI(MetaData);
				string FilePath = Path.Combine(Gateway.AppDataFolder, "Gateway.iotdisco");

				Log.Informational("Registration successful.");
				Log.Informational(ClaimUrl, new KeyValuePair<string, object>("Path", FilePath));

				await File.WriteAllTextAsync(FilePath, ClaimUrl);
			}
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();

			Gateway.Terminate();

			deferral.Complete();
		}
	}
}
