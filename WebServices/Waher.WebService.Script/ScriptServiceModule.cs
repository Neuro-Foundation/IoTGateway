﻿using System.Threading.Tasks;
using Waher.Networking.HTTP;
using Waher.Runtime.Inventory;

namespace Waher.WebService.Script
{
	/// <summary>
	/// Pluggable module registering the script service to the web server.
	/// </summary>
	[Singleton]
	public class ScriptServiceModule : IModule
	{
		private HttpServer webServer;
		private ScriptService instance;

		/// <summary>
		/// Pluggable module registering the script service to the web server.
		/// </summary>
		public ScriptServiceModule()
		{
		}

		/// <summary>
		/// Starts the module.
		/// </summary>
		public Task Start()
		{
			if (Types.TryGetModuleParameter("HTTP", out HttpServer WebServer))
			{
				this.webServer = WebServer;
				this.instance = new ScriptService("/Evaluate");
				this.webServer.Register(this.instance);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the module.
		/// </summary>
		public Task Stop()
		{
			if (!(this.webServer is null))
			{
				this.webServer.Unregister(this.instance);

				this.webServer = null;
				this.instance = null;
			}

			return Task.CompletedTask;
		}
	}
}
