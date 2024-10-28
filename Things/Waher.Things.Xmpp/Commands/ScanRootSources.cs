﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.Concentrator;
using Waher.Runtime.Language;
using Waher.Things.Metering;

namespace Waher.Things.Xmpp.Commands
{
	/// <summary>
	/// Scans a concentrator node for its root sources.
	/// </summary>
	public class ScanRootSources : ConcentratorCommand
	{
		/// <summary>
		/// Scans a concentrator node for its root sources.
		/// </summary>
		/// <param name="Concentrator">Concentrator node.</param>
		public ScanRootSources(ConcentratorDevice Concentrator)
			: base(Concentrator, "1")
		{
		}

		/// <summary>
		/// ID of command.
		/// </summary>
		public override string CommandID => nameof(ScanRootSources);

		/// <summary>
		/// Type of command.
		/// </summary>
		public override CommandType Type => CommandType.Simple;

		/// <summary>
		/// Gets the name of data source.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		public override Task<string> GetNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(ConcentratorDevice), 52, "Scan Root Sources");
		}

		/// <summary>
		/// Executes the command.
		/// </summary>
		public override Task ExecuteCommandAsync()
		{
			this.StartSearch();
			return Task.CompletedTask;
		}

		private async void StartSearch()
		{
			try
			{
				ConcentratorClient Client = await this.GetConcentratorClient();
				string FullJid = this.GetRemoteFullJid(Client.Client);

				DataSourceReference[] Sources = await Client.GetRootDataSourcesAsync(FullJid);
				Dictionary<string, ConcentratorSourceNode> BySourceId = new Dictionary<string, ConcentratorSourceNode>();

				foreach (INode Child in await this.Concentrator.ChildNodes)
				{
					if (Child is ConcentratorSourceNode SourceNode)
						BySourceId[SourceNode.RemoteSourceID] = SourceNode;
				}

				LinkedList<ScanSource> NewScans = null;

				foreach (DataSourceReference Source in Sources)
				{
					if (BySourceId.ContainsKey(Source.SourceID))
						continue;

					ConcentratorSourceNode SourceNode = new ConcentratorSourceNode()
					{
						NodeId = await MeteringNode.GetUniqueNodeId(Source.SourceID),
						RemoteSourceID = Source.SourceID
					};

					await this.Concentrator.AddAsync(SourceNode);

					BySourceId[Source.SourceID] = SourceNode;

					if (NewScans is null)
						NewScans = new LinkedList<ScanSource>();

					NewScans.AddLast(new ScanSource(this.Concentrator, SourceNode));
				}

				if (!(NewScans is null))
				{
					foreach (ScanSource ScanSource in NewScans)
						await ScanSource.ExecuteCommandAsync();
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// Creates a copy of the command object.
		/// </summary>
		/// <returns>Copy of command object.</returns>
		public override ICommand Copy()
		{
			return new ScanRootSources(this.Concentrator);
		}
	}
}
