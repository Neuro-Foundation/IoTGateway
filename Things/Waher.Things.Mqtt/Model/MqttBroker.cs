﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.MQTT;
using Waher.Runtime.Inventory;
using Waher.Runtime.Timing;

namespace Waher.Things.Mqtt.Model
{
	/// <summary>
	/// TODO
	/// </summary>
	public class MqttBroker : IDisposable
	{
		private static Scheduler scheduler = null;
		private readonly SortedDictionary<string, MqttTopic> topics = new SortedDictionary<string, MqttTopic>();
		private readonly SemaphoreSlim topicSemaphore = new SemaphoreSlim(1);
		private readonly MqttBrokerNode node;
		private MqttClient mqttClient;
		private bool connectionOk = false;
		private readonly string host;
		private readonly int port;
		private readonly bool tls;
		private readonly bool trustServer;
		private readonly string userName;
		private readonly string password;
		private readonly string connectionSubscription;
		private string willTopic;
		private string willData;
		private bool willRetain;
		private MqttQualityOfService willQoS;
		private DateTime nextCheck;

		/// <summary>
		/// TODO
		/// </summary>
		public MqttBroker(MqttBrokerNode Node, string Host, int Port, bool Tls, bool TrustServer, string UserName, string Password,
			string ConnectionSubscription, string WillTopic, string WillData, bool WillRetain, MqttQualityOfService WillQoS)
		{
			this.node = Node;
			this.host = Host;
			this.port = Port;
			this.tls = Tls;
			this.trustServer = TrustServer;
			this.userName = UserName;
			this.password = Password;
			this.connectionSubscription = ConnectionSubscription;
			this.willTopic = WillTopic;
			this.willData = WillData;
			this.willRetain = WillRetain;
			this.willQoS = WillQoS;

			this.Open();
		}

		internal MqttClient Client => this.mqttClient;

		private void Open()
		{
			this.mqttClient = new MqttClient(this.host, this.port, this.tls, this.userName, this.password, this.willTopic,
				this.willQoS, this.willRetain, Encoding.UTF8.GetBytes(this.willData))
			{
				TrustServer = this.trustServer
			};

			this.mqttClient.OnConnectionError += this.MqttClient_OnConnectionError;
			this.mqttClient.OnContentReceived += this.MqttClient_OnContentReceived;
			this.mqttClient.OnStateChanged += this.MqttClient_OnStateChanged;

			this.nextCheck = Scheduler.Add(DateTime.Now.AddMinutes(1), this.CheckOnline, null);
		}

		private void Close()
		{
			if (!(this.mqttClient is null))
			{
				Scheduler.Remove(this.nextCheck);

				this.mqttClient.OnConnectionError -= this.MqttClient_OnConnectionError;
				this.mqttClient.OnContentReceived -= this.MqttClient_OnContentReceived;
				this.mqttClient.OnStateChanged -= this.MqttClient_OnStateChanged;

				this.mqttClient.Dispose();
				this.mqttClient = null;
			}
		}

		/// <summary>
		/// TODO
		/// </summary>
		public void Dispose()
		{
			this.Close();
		}

		private async void CheckOnline(object _)
		{
			try
			{
				if (!(this.mqttClient is null))
				{
					MqttState State = this.mqttClient.State;
					if (State == MqttState.Offline || State == MqttState.Error || State == MqttState.Authenticating)
						await this.mqttClient.Reconnect();
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
			finally
			{
				this.nextCheck = Scheduler.Add(DateTime.Now.AddMinutes(1), this.CheckOnline, null);
			}
		}

		/// <summary>
		/// TODO
		/// </summary>
		public async Task DataReceived(MqttContent Content)
		{
			MqttTopic Topic = await this.GetTopic(Content.Topic, true);
			if (!(Topic is null))
				await Topic.DataReported(Content);
		}

		/// <summary>
		/// TODO
		/// </summary>
		public void SetWill(string WillTopic, string WillData, bool WillRetain, MqttQualityOfService WillQoS)
		{
			if (this.willTopic != WillTopic || this.willData != WillData || this.willRetain != WillRetain || this.willQoS != WillQoS)
			{
				this.Close();

				this.willTopic = WillTopic;
				this.willData = WillData;
				this.willRetain = WillRetain;
				this.willQoS = WillQoS;

				this.Open();
			}
		}

		private async Task MqttClient_OnStateChanged(object Sender, MqttState NewState)
		{
			try
			{
				switch (NewState)
				{
					case MqttState.Connected:
						this.connectionOk = true;
						await this.node.RemoveErrorAsync("Offline");
						await this.node.RemoveErrorAsync("Error");

						if (!string.IsNullOrEmpty(this.connectionSubscription))
						{
							string[] Parts = this.connectionSubscription.Split(',');
							int i, c = Parts.Length;

							for (i = 0; i < c; i++)
								Parts[i] = Parts[i].Trim();

							await this.mqttClient.SUBSCRIBE(MqttQualityOfService.AtLeastOnce, Parts);
						}
						break;

					case MqttState.Error:
						await this.node.LogErrorAsync("Error", "Connection to broker failed.");
						this.Reconnect();
						break;

					case MqttState.Offline:
						await this.node.LogErrorAsync("Offline", "Connection is closed.");
						this.Reconnect();
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private async Task MqttClient_OnContentReceived(object Sender, MqttContent Content)
		{
			await this.topicSemaphore.WaitAsync();
			try
			{
				if (this.processing)
				{
					this.queue.AddLast(Content);
					return;
				}
				else
					this.processing = true;
			}
			finally
			{
				this.topicSemaphore.Release();
			}

			this.Process(Content);
		}

		private readonly LinkedList<MqttContent> queue = new LinkedList<MqttContent>();
		private bool processing = false;

		private async void Process(MqttContent Content)
		{
			try
			{
				while (true)
				{
					MqttTopic Topic = await this.GetTopic(Content.Topic, true);
					if (!(Topic is null))
						await Topic.DataReported(Content);

					await this.topicSemaphore.WaitAsync();
					try
					{
						if (this.queue.First is null)
						{
							this.processing = false;
							break;
						}
						else
						{
							Content = this.queue.First.Value;
							this.queue.RemoveFirst();
						}
					}
					finally
					{
						this.topicSemaphore.Release();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				this.processing = false;
			}
		}

		private Task MqttClient_OnConnectionError(object Sender, Exception Exception)
		{
			this.Reconnect();
			return Task.CompletedTask;
		}

		private void Reconnect()
		{
			if (this.connectionOk)
			{
				this.connectionOk = false;
				this.mqttClient?.Reconnect();
			}
		}

		/// <summary>
		/// Gets the Node responsible for managing a Topic
		/// </summary>
		public async Task<MqttTopic> GetTopic(string TopicString, bool CreateNew)
		{
			if (string.IsNullOrEmpty(TopicString))
				return null;

			MqttTopicRepresentation Representation = new MqttTopicRepresentation(TopicString, TopicString.Split('/'), 0);
			string CurrentSegment = Representation.CurrentSegment;
			MqttTopic Topic;

			await this.topicSemaphore.WaitAsync();
			try
			{
				if (!this.topics.TryGetValue(CurrentSegment, out Topic))
					Topic = null;

				if (Topic is null)
				{
					if (Guid.TryParse(CurrentSegment.Replace('_', '-'), out Guid _))
						return null;

					if (this.node.HasChildren)
					{
						foreach (INode Child in await this.node.ChildNodes)
						{
							if (Child is IMqttTopicNode Topic2 && Topic2.LocalTopic == CurrentSegment)
							{
								Topic = new MqttTopic(Topic2, CurrentSegment, CurrentSegment, null, this);
								break;
							}
						}
					}

					if (Topic is null)
					{
						if (!CreateNew)
							return null;

						IMqttTopicNode Node = Types.FindBest<IMqttTopicNode, MqttTopicRepresentation>(Representation);
						if (Node is null)
							return null;

						Node = await Node.CreateNew(Representation);
						Topic = new MqttTopic(Node, Node.LocalTopic, Node.LocalTopic, null, this);

						await this.node.AddAsync(Node);
					}

					this.topics[Topic.LocalTopic] = Topic;
				}
			}
			finally
			{
				this.topicSemaphore.Release();
			}

			if (Representation.MoveNext())
				return await Topic.GetTopic(Representation, CreateNew, this);
			else
				return Topic;
		}

		/// <summary>
		/// Removes a child topic
		/// </summary>
		/// <param name="LocalTopic">Local topic name.</param>
		public async Task<bool> Remove(string LocalTopic)
		{
			if (!(LocalTopic is null))
			{
				await this.topicSemaphore.WaitAsync();
				try
				{
					return this.topics.Remove(LocalTopic);
				}
				finally
				{
					this.topicSemaphore.Release();
				}
			}
			else
				return false;
		}

		/// <summary>
		/// Scheduler for asynchronous tasks.
		/// </summary>
		public static Scheduler Scheduler
		{
			get
			{
				if (scheduler is null)
				{
					if (Types.TryGetModuleParameter("Scheduler", out object Obj) && Obj is Scheduler Scheduler)
						scheduler = Scheduler;
					else
					{
						scheduler = new Scheduler();

						Log.Terminating += (sender, e) =>
						{
							scheduler?.Dispose();
							scheduler = null;
						};
					}
				}

				return scheduler;
			}
		}

	}
}
