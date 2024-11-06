﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.Modbus;
using Waher.Networking.Sniffers;
using Waher.Runtime.Cache;
using Waher.Runtime.Language;
using Waher.Runtime.Threading;
using Waher.Things.Ip;

namespace Waher.Things.Modbus
{
	/// <summary>
	/// Node representing a TCP/IP connection to a Modbus Gateway
	/// </summary>
	public class ModbusGatewayNode : IpHostPort, ISniffable
	{
		private readonly Sniffable sniffers = new Sniffable();

		/// <summary>
		/// Node representing a TCP/IP connection to a Modbus Gateway
		/// </summary>
		public ModbusGatewayNode()
			: base()
		{
			this.Port = 502;
			this.Tls = false;
		}

		/// <summary>
		/// Gets the type name of the node.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <returns>Localized type node.</returns>
		public override Task<string> GetTypeNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(ModbusGatewayNode), 1, "Modbus Gateway");
		}

		/// <summary>
		/// If the node accepts a presumptive child, i.e. can receive as a child (if that child accepts the node as a parent).
		/// </summary>
		/// <param name="Child">Presumptive child node.</param>
		/// <returns>If the child is acceptable.</returns>
		public override Task<bool> AcceptsChildAsync(INode Child)
		{
			return Task.FromResult(Child is ModbusUnitNode);
		}

		#region ISniffable

		/// <summary>
		/// Registered sniffers
		/// </summary>
		public ISniffer[] Sniffers => this.sniffers.Sniffers;

		/// <summary>
		/// If sniffers are registered
		/// </summary>
		public bool HasSniffers => this.sniffers.HasSniffers;

		/// <summary>
		/// Adds a sniffer
		/// </summary>
		/// <param name="Sniffer">Sniffer</param>
		public void Add(ISniffer Sniffer) => this.sniffers.Add(Sniffer);

		/// <summary>
		/// Adds a range of sniffers
		/// </summary>
		/// <param name="Sniffers">Sniffrs</param>
		public void AddRange(IEnumerable<ISniffer> Sniffers) => this.sniffers.AddRange(Sniffers);

		/// <summary>
		/// Removes a sniffer
		/// </summary>
		/// <param name="Sniffer">Sniffer</param>
		/// <returns>If sniffer was found and removed.</returns>
		public bool Remove(ISniffer Sniffer) => this.sniffers.Remove(Sniffer);

		/// <summary>
		/// Gets an enumerator of registered sniffers.
		/// </summary>
		/// <returns>Enumerator</returns>
		public IEnumerator<ISniffer> GetEnumerator() => this.sniffers.GetEnumerator();

		/// <summary>
		/// Gets an enumerator of registered sniffers.
		/// </summary>
		/// <returns>Enumerator</returns>
		IEnumerator IEnumerable.GetEnumerator() => this.sniffers.GetEnumerator();

		/// <summary>
		/// Called when binary data has been received.
		/// </summary>
		/// <param name="Data">Binary Data.</param>
		public Task ReceiveBinary(byte[] Data) => this.sniffers.ReceiveBinary(Data);

		/// <summary>
		/// Called when binary data has been transmitted.
		/// </summary>
		/// <param name="Data">Binary Data.</param>
		public Task TransmitBinary(byte[] Data) => this.sniffers.TransmitBinary(Data);

		/// <summary>
		/// Called when text has been received.
		/// </summary>
		/// <param name="Text">Text</param>
		public Task ReceiveText(string Text) => this.sniffers.ReceiveText(Text);

		/// <summary>
		/// Called when text has been transmitted.
		/// </summary>
		/// <param name="Text">Text</param>
		public Task TransmitText(string Text) => this.sniffers.TransmitText(Text);

		/// <summary>
		/// Called to inform the viewer of something.
		/// </summary>
		/// <param name="Comment">Comment.</param>
		public Task Information(string Comment) => this.sniffers.Information(Comment);

		/// <summary>
		/// Called to inform the viewer of a warning state.
		/// </summary>
		/// <param name="Warning">Warning.</param>
		public Task Warning(string Warning) => this.sniffers.Warning(Warning);

		/// <summary>
		/// Called to inform the viewer of an error state.
		/// </summary>
		/// <param name="Error">Error.</param>
		public Task Error(string Error) => this.sniffers.Error(Error);

		/// <summary>
		/// Called to inform the viewer of an exception state.
		/// </summary>
		/// <param name="Exception">Exception.</param>
		public Task Exception(Exception Exception) => this.sniffers.Exception(Exception);

		#endregion

		#region TCP/IP connections

		private readonly static Cache<string, ModbusTcpClient> clients = GetCache();
		private readonly static MultiReadSingleWriteObject clientsSynchObject = new MultiReadSingleWriteObject();

		private static Cache<string, ModbusTcpClient> GetCache()
		{
			Cache<string, ModbusTcpClient> Result = new Cache<string, ModbusTcpClient>(int.MaxValue, TimeSpan.MaxValue, TimeSpan.FromMinutes(5));

			Result.Removed += Result_Removed;

			return Result;
		}

		private static Task Result_Removed(object Sender, CacheItemEventArgs<string, ModbusTcpClient> e)
		{
			try
			{
				e.Value.Dispose();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets the TCP/IP connection associated with this gateway.
		/// </summary>
		/// <returns>TCP/IP connection</returns>
		public async Task<ModbusTcpClient> GetTcpIpConnection()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(this.Host);
			sb.Append(this.Port.ToString());
			sb.Append(this.Tls.ToString());

			string Key = sb.ToString();

			if (clients.TryGetValue(Key, out ModbusTcpClient Client))
			{
				if (Client.Connected)
				{
					this.CheckSniffers(Client);
					return Client;
				}
				else
					clients.Remove(Key);
			}

			await clientsSynchObject.BeginWrite();
			try
			{
				if (clients.TryGetValue(Key, out Client))
					return Client;

				Client = await ModbusTcpClient.Connect(this.Host, this.Port, this.Tls, this.sniffers.Sniffers);

				clients[Key] = Client;
			}
			finally
			{
				await clientsSynchObject.EndWrite();
			}

			return Client;
		}

		private void CheckSniffers(ModbusTcpClient Client)
		{
			if (!Client.HasSniffers && !this.sniffers.HasSniffers)
				return;

			foreach (ISniffer Sniffer in Client.Sniffers)
			{
				bool Found = false;

				foreach (ISniffer Sniffer2 in this.sniffers.Sniffers)
				{
					if (Sniffer == Sniffer2)
					{
						Found = true;
						break;
					}
				}

				if (!Found)
					Client.Remove(Sniffer);
			}

			foreach (ISniffer Sniffer in this.sniffers.Sniffers)
			{
				bool Found = false;

				foreach (ISniffer Sniffer2 in Client.Sniffers)
				{
					if (Sniffer == Sniffer2)
					{
						Found = true;
						break;
					}
				}

				if (!Found)
					Client.Add(Sniffer);
			}
		}

		#endregion
	}
}
