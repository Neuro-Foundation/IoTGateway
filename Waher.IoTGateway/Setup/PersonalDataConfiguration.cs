﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking.HTTP;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Runtime.Inventory;
using Waher.IoTGateway.Setup.PersonalData;
using Waher.Runtime.Language;

namespace Waher.IoTGateway.Setup
{
	/// <summary>
	/// Provides information about personal data processing.
	/// </summary>
	public class PersonalDataConfiguration : SystemConfiguration
	{
		private static PersonalDataConfiguration instance = null;
		private static IProcessingActivity[] processingActivities = null;
		private HttpResource consent = null;

		private string informationHash = string.Empty;
		private bool consented = false;
		private DateTime consentedTimestamp = DateTime.MinValue;

		/// <summary>
		/// Current instance of configuration.
		/// </summary>
		public static PersonalDataConfiguration Instance => instance;

		/// <summary>
		/// Available processing activities.
		/// </summary>
		public static IProcessingActivity[] ProcessingActivities => processingActivities;

		/// <summary>
		/// Digest of transparent information presented to which consent is asked.
		/// </summary>
		[DefaultValueStringEmpty]
		public string InformationHash
		{
			get => this.informationHash;
			set => this.informationHash = value;
		}

		/// <summary>
		/// If consent to personal data processing has been received.
		/// </summary>
		[DefaultValue(false)]
		public bool Consented
		{
			get => this.consented;
			set => this.consented = value;
		}

		/// <summary>
		/// When consent to personal data processing has been received.
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime ConsentedTimestamp
		{
			get => this.consentedTimestamp;
			set => this.consentedTimestamp = value;
		}

		/// <summary>
		/// Resource to be redirected to, to perform the configuration.
		/// </summary>
		public override string Resource => "/Settings/PersonalData.md";

		/// <summary>
		/// Priority of the setting. Configurations are sorted in ascending order.
		/// </summary>
		public override int Priority => 100;

		/// <summary>
		/// Gets a title for the system configuration.
		/// </summary>
		/// <param name="Language">Current language.</param>
		/// <returns>Title string</returns>
		public override Task<string> Title(Language Language)
		{
			return Language.GetStringAsync(typeof(Gateway), 4, "Personal Data");
		}

		/// <summary>
		/// Is called during startup to configure the system.
		/// </summary>
		public override Task ConfigureSystem()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Sets the static instance of the configuration.
		/// </summary>
		/// <param name="Configuration">Configuration object</param>
		public override void SetStaticInstance(ISystemConfiguration Configuration)
		{
			instance = Configuration as PersonalDataConfiguration;
		}

		/// <summary>
		/// Initializes the setup object.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		public override async Task InitSetup(HttpServer WebServer)
		{
			await base.InitSetup(WebServer);

			this.consent = WebServer.Register("/Settings/Consent", null, this.Consent, true, false, true);

			List<IProcessingActivity> Activities = new List<IProcessingActivity>();

			foreach (Type T in Types.GetTypesImplementingInterface(typeof(IProcessingActivity)))
			{
				if (T.IsAbstract || T.IsInterface || T.IsGenericTypeDefinition)
					continue;

				try
				{
					Activities.Add((IProcessingActivity)Types.Instantiate(T));
				}
				catch (Exception ex)
				{
					Log.Exception(ex, T.FullName);
				}
			}

			Activities.Sort((A1, A2) =>
			{
				int i = A1.Priority - A2.Priority;
				if (i != 0)
					return i;

				return A1.GetType().FullName.CompareTo(A2.GetType().FullName);
			});

			processingActivities = Activities.ToArray();

			/* Removed since it might affect remote updates.
             
            StringBuilder sb = new StringBuilder();

			foreach (IProcessingActivity A in processingActivities)
			{
				string FileName = Path.Combine(Gateway.RootFolder, "Settings", "PersonalData", A.TransparentInformationMarkdownFileName);

				try
				{
					string Markdown = await Resources.ReadAllTextAsync(FileName);
					sb.AppendLine(Markdown);
				}
				catch (Exception ex)
				{
					Log.Exception(ex, A.TransparentInformationMarkdownFileName);
				}
			}

			string Hash = Security.Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(sb.ToString()));

			if (Hash != this.informationHash || (!this.consented && this.Complete))
			{
				this.InformationHash = Hash;
				this.consented = false;
				this.consentedTimestamp = DateTime.MinValue;
				this.Complete = false;
				this.Completed = DateTime.MinValue;
				this.Updated = DateTime.Now;

				await Database.Update(this);
			}*/
		}

		/// <summary>
		/// Unregisters the setup object.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		public override Task UnregisterSetup(HttpServer WebServer)
		{
			WebServer.Unregister(this.consent);

			return base.UnregisterSetup(WebServer);
		}

		/// <summary>
		/// Minimum required privilege for a user to be allowed to change the configuration defined by the class.
		/// </summary>
		protected override string ConfigPrivilege => "Admin.Legal.PersonalData";

		private async Task Consent(HttpRequest Request, HttpResponse Response)
		{
			Gateway.AssertUserAuthenticated(Request, this.ConfigPrivilege);

			if (!Request.HasData)
				throw new BadRequestException();

			object Obj = await Request.DecodeDataAsync();
			if (!(Obj is Dictionary<string, object> Parameters))
				throw new BadRequestException();

			if (!Parameters.TryGetValue("consent", out Obj) || !(Obj is bool Consent))
				throw new BadRequestException();

			string TabID = Request.Header["X-TabID"];
			if (string.IsNullOrEmpty(TabID))
				throw new BadRequestException();

			if (this.consented != Consent)
			{
				this.consented = Consent;
				this.consentedTimestamp = Consent ? DateTime.Now : DateTime.MinValue;

				await Database.Update(this);

				await ClientEvents.PushEvent(new string[] { TabID }, "ShowNext", JSON.Encode(new Dictionary<string, object>()
				{
					{ "consent", Consent }
				}, false), true, "User");
			}

			Response.StatusCode = 200;
		}

		/// <summary>
		/// Environment variable name for personal data consent configuration.
		/// </summary>
		public const string GATEWAY_PII_CONSENT = nameof(GATEWAY_PII_CONSENT);

		/// <summary>
		/// Environment configuration by configuring values available in environment variables.
		/// </summary>
		/// <returns>If the configuration was changed, and can be considered completed.</returns>
		public override Task<bool> EnvironmentConfiguration()
		{
			if (!this.TryGetEnvironmentVariable(GATEWAY_PII_CONSENT, false, out this.consented) || !this.consented)
				return Task.FromResult(false);

			this.consentedTimestamp = DateTime.Now;
			return Task.FromResult(true);
		}

	}
}
