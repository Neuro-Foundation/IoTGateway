﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking;
using Waher.Networking.MQTT;
using Waher.Runtime.Inventory;
using Waher.Runtime.Timing;

namespace Waher.Things.Mqtt.Model
{
	/// <summary>
	/// MQTT Broker connection object.
	/// </summary>
	public class MqttBroker : IDisposable
	{
		private static Scheduler scheduler = null;
		private readonly SortedDictionary<string, MqttTopic> topics = new SortedDictionary<string, MqttTopic>();
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
		/// MQTT Broker connection object.
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

		/// <summary>
		/// Reference to broker node.
		/// </summary>
		public MqttBrokerNode Node => this.node;

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

		private async Task Close()
		{
			if (!(this.mqttClient is null))
			{
				Scheduler.Remove(this.nextCheck);

				this.mqttClient.OnConnectionError -= this.MqttClient_OnConnectionError;
				this.mqttClient.OnContentReceived -= this.MqttClient_OnContentReceived;
				this.mqttClient.OnStateChanged -= this.MqttClient_OnStateChanged;

				await this.mqttClient.DisposeAsync();
				this.mqttClient = null;
			}
		}

		/// <summary>
		/// TODO
		/// </summary>
		[Obsolete("Use the DisposeAsync() method.")]
		public async void Dispose()
		{
			try
			{
				await this.DisposeAsync();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// Closes the connection and disposes of all resources.
		/// </summary>
		public Task DisposeAsync()
		{
			return this.Close();
		}

		private async void CheckOnline(object _)
		{
			try
			{
				if (!(this.mqttClient is null) && !NetworkingModule.Stopping)
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
		/// Publishes binary data to a topic.
		/// </summary>
		/// <param name="Topic">Topic</param>
		/// <param name="QoS">Quality of Service</param>
		/// <param name="Retain">If data should be retained</param>
		/// <param name="Data">Binary Data</param>
		public Task Publish(string Topic, MqttQualityOfService QoS, bool Retain, byte[] Data)
		{
			return this.Client.PUBLISH(Topic, QoS, Retain, Data);
		}

		/// <summary>
		/// Publishes text data to a topic.
		/// </summary>
		/// <param name="Topic">Topic</param>
		/// <param name="QoS">Quality of Service</param>
		/// <param name="Retain">If data should be retained</param>
		/// <param name="Data">Text Data</param>
		public Task Publish(string Topic, MqttQualityOfService QoS, bool Retain, string Data)
		{
			return this.Publish(Topic, QoS, Retain, Encoding.UTF8.GetBytes(Data));
		}

		/// <summary>
		/// TODO
		/// </summary>
		public async Task DataReceived(MqttContent Content)
		{
			MqttTopic Topic = await this.GetTopic(Content.Topic, true, true);
			if (!(Topic is null))
				await Topic.DataReported(Content);
		}

		/// <summary>
		/// TODO
		/// </summary>
		public async Task SetWill(string WillTopic, string WillData, bool WillRetain, MqttQualityOfService WillQoS)
		{
			if (this.willTopic != WillTopic || this.willData != WillData || this.willRetain != WillRetain || this.willQoS != WillQoS)
			{
				await this.Close();

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

						if (!NetworkingModule.Stopping)
							await this.Reconnect();
						break;

					case MqttState.Offline:
						await this.node.LogErrorAsync("Offline", "Connection is closed.");

						if (!!NetworkingModule.Stopping)
							await this.Reconnect();
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private Task MqttClient_OnContentReceived(object Sender, MqttContent Content)
		{
			lock (this.topics)
			{
				if (this.processing)
				{
					this.queue.AddLast(Content);
					return Task.CompletedTask;
				}
				else
					this.processing = true;
			}

			this.Process(Content);

			return Task.CompletedTask;
		}

		private readonly LinkedList<MqttContent> queue = new LinkedList<MqttContent>();
		private bool processing = false;

		private async void Process(MqttContent Content)
		{
			try
			{
				while (true)
				{
					MqttTopic Topic = await this.GetTopic(Content.Topic, true, true);
					if (!(Topic is null))
						await Topic.DataReported(Content);

					lock (this.topics)
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
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				this.processing = false;
			}
		}

		private async Task MqttClient_OnConnectionError(object Sender, Exception Exception)
		{
			if (!NetworkingModule.Stopping)
				await this.Reconnect();
		}

		private async Task Reconnect()
		{
			if (this.connectionOk)
			{
				this.connectionOk = false;

				if (!(this.mqttClient is null) && !NetworkingModule.Stopping)
					await this.mqttClient.Reconnect();
			}
		}

		/// <summary>
		/// Gets the Node responsible for managing a Topic
		/// </summary>
		public async Task<MqttTopic> GetTopic(string TopicString, bool CreateNew, bool IgnoreGuids)
		{
			if (string.IsNullOrEmpty(TopicString))
				return null;

			MqttTopicRepresentation Representation = new MqttTopicRepresentation(TopicString, TopicString.Split('/'), 0);
			MqttTopic Topic = await this.GetLocalTopic(Representation, CreateNew);

			if (Topic is null)
				return null;
			else if (Representation.MoveNext(Topic))
				return await Topic.GetTopic(Representation, CreateNew, IgnoreGuids, this);
			else
				return Topic;
		}

		private async Task<MqttTopic> GetLocalTopic(MqttTopicRepresentation Representation, bool CreateNew)
		{
			string CurrentSegment = Representation.CurrentSegment;
			MqttTopic Topic, Topic2;

			lock (this.topics)
			{
				if (this.topics.TryGetValue(CurrentSegment, out Topic))
					return Topic;
			}

			if (Guid.TryParse(CurrentSegment.Replace('_', '-'), out Guid _))
				return null;

			if (this.node.HasChildren)
			{
				foreach (INode Child in await this.node.ChildNodes)
				{
					if (Child is IMqttTopicNode TopicNode && TopicNode.LocalTopic == CurrentSegment)
					{
						lock (this.topics)
						{
							if (this.topics.TryGetValue(CurrentSegment, out Topic2))
								return Topic2;
							else
							{
								Topic = new MqttTopic(TopicNode, CurrentSegment, CurrentSegment, null, this);
								this.topics[CurrentSegment] = Topic;
								return Topic;
							}
						}
					}
				}
			}

			if (!CreateNew)
				return null;

			IMqttTopicNode AddNode = Types.FindBest<IMqttTopicNode, MqttTopicRepresentation>(Representation);
			if (AddNode is null)
				return null;

			AddNode = await AddNode.CreateNew(Representation);
			Topic = new MqttTopic(AddNode, AddNode.LocalTopic, AddNode.LocalTopic, null, this);

			lock (this.topics)
			{
				if (this.topics.TryGetValue(CurrentSegment, out Topic2))
					return Topic2;
				else
					this.topics[CurrentSegment] = Topic;
			}

			await this.node.AddAsync(AddNode);

			return Topic;
		}

		/// <summary>
		/// Removes a child topic
		/// </summary>
		/// <param name="LocalTopic">Local topic name.</param>
		/// <returns>If local topic was found and removed.</returns>
		public bool Remove(string LocalTopic)
		{
			if (!(LocalTopic is null))
			{
				lock (this.topics)
				{
					return this.topics.Remove(LocalTopic);
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

						Log.Terminating += (Sender, e) =>
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
