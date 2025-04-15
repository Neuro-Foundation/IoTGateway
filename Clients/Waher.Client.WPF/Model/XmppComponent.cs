﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Waher.Networking.XMPP;
using Waher.Client.WPF.Dialogs;
using Waher.Client.WPF.Controls;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Sensor;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Things.SensorData;

namespace Waher.Client.WPF.Model
{
	public class XmppComponent : XmppNode
	{
		private readonly Dictionary<string, bool> features;
		private readonly string jid;
		private readonly string name;
		private readonly string node;
		private readonly bool canSearch;

		public XmppComponent(TreeNode Parent, string JID, string Name, string Node, Dictionary<string, bool> Features)
			: base(Parent)
		{
			this.jid = JID;
			this.name = Name;
			this.node = Node;
			this.features = Features;
			this.canSearch = this.features.ContainsKey(XmppClient.NamespaceSearch);

			this.Account?.RegisterComponent(this);
		}

		public override void Dispose()
		{
			this.Account?.UnregisterComponent(this);
			base.Dispose();
		}

		public string JID => this.jid;
		public string Name => this.name;
		public string Node => this.node;

		public override string FullJID => this.jid;
		public override string Key => this.jid;
		public override ImageSource ImageResource => XmppAccountNode.component;
		public override string TypeName => "XMPP Server component";
		public override bool CanAddChildren => false;
		public override bool CanEdit => false;
		public override bool CanDelete => false;
		public override bool CanRecycle => false;

		public override string Header
		{
			get
			{
				if (string.IsNullOrEmpty(this.name))
					return this.jid;
				else
					return this.name;
			}
		}

		public override string ToolTip
		{
			get
			{
				if (string.IsNullOrEmpty(this.node))
					return "XMPP Server component";
				else
					return "XMPP Server component (" + this.node + ")";
			}
		}

		public override void Write(XmlWriter Output)
		{
			// Don't output.
		}

		public override bool CanSearch => this.canSearch;

		public override void Search()
		{
			this.Account?.Client?.SendSearchFormRequest(null, this.jid, (Sender, e) =>
			{
				if (e.Ok)
					_ = MainWindow.ShowParameterDialog(e.SearchForm);
				else
					MainWindow.ErrorBox(string.IsNullOrEmpty(e.ErrorText) ? "Unable to get search form." : e.ErrorText);

				return Task.CompletedTask;

			}, (Sender, e) =>
			{
				if (e.Ok)
				{
					MainWindow.UpdateGui(() =>
					{
						TabItem TabItem = MainWindow.NewTab("Search Result");
						MainWindow.currentInstance.Tabs.Items.Add(TabItem);

						SearchResultView View = new SearchResultView(e.Headers, e.Records);
						TabItem.Content = View;

						MainWindow.currentInstance.Tabs.SelectedItem = TabItem;

						return Task.CompletedTask;
					});
				}
				else
					MainWindow.ErrorBox(string.IsNullOrEmpty(e.ErrorText) ? "Unable to perform search." : e.ErrorText);

				return Task.CompletedTask;

			}, null);
		}

		public void NodesAdded(IEnumerable<TreeNode> Nodes, TreeNode Parent)
		{
			XmppAccountNode XmppAccountNode = this.Account;
			if (XmppAccountNode is null)
				return;

			ConnectionView View = XmppAccountNode.View;
			if (View is null)
				return;

			foreach (TreeNode Node in Nodes)
				View.NodeAdded(Parent, Node);
		}

		public void NodesRemoved(IEnumerable<TreeNode> Nodes, TreeNode Parent)
		{
			XmppAccountNode XmppAccountNode = this.Account;
			if (XmppAccountNode is null)
				return;

			Controls.ConnectionView View = XmppAccountNode.View;
			if (View is null)
				return;

			LinkedList<KeyValuePair<TreeNode, TreeNode>> ToRemove = new LinkedList<KeyValuePair<TreeNode, TreeNode>>();

			foreach (TreeNode Node in Nodes)
				ToRemove.AddLast(new KeyValuePair<TreeNode, TreeNode>(Parent, Node));

			while (!(ToRemove.First is null))
			{
				KeyValuePair<TreeNode, TreeNode> P = ToRemove.First.Value;
				ToRemove.RemoveFirst();

				Parent = P.Key;
				TreeNode Node = P.Value;

				if (Node.HasChildren.HasValue && Node.HasChildren.Value)
				{
					foreach (TreeNode Child in Node.Children)
						ToRemove.AddLast(new KeyValuePair<TreeNode, TreeNode>(Node, Child));
				}

				MainWindow.UpdateGui(() =>
				{
					View.NodeRemoved(Parent, Node);
					return Task.CompletedTask;
				});
			}
		}

		public bool HasFeature(string Feature)
		{
			return this.features?.ContainsKey(Feature) ?? false;
		}

		public override bool CanReadSensorData => this.Account.IsOnline;

		public override Task<SensorDataClientRequest> StartSensorDataFullReadout()
		{
			return this.DoReadout(FieldType.All);
		}

		public override Task<SensorDataClientRequest> StartSensorDataMomentaryReadout()
		{
			return this.DoReadout(FieldType.Momentary);
		}

		private async Task<SensorDataClientRequest> DoReadout(FieldType Types)
		{
			string Id = Guid.NewGuid().ToString();

			CustomSensorDataClientRequest Request = new CustomSensorDataClientRequest(Id, string.Empty, string.Empty, null,
				Types, null, DateTime.MinValue, DateTime.MaxValue, DateTime.Now, string.Empty, string.Empty, string.Empty);

			await Request.Accept(false);
			await Request.Started();

			await this.Account.Client.SendServiceDiscoveryRequest(this.jid, (Sender, e) =>
			{
				if (e.Ok)
				{
					List<Field> Fields = new List<Field>();
					DateTime Now = DateTime.Now;

					foreach (KeyValuePair<string, bool> Feature in e.Features)
					{
						Fields.Add(new BooleanField(Waher.Things.ThingReference.Empty, Now,
							Feature.Key, Feature.Value, FieldType.Momentary, FieldQoS.AutomaticReadout));
					}

					if ((Types & FieldType.Identity) != 0)
					{
						foreach (Identity Identity in e.Identities)
						{
							Fields.Add(new StringField(Waher.Things.ThingReference.Empty, Now,
								Identity.Type, Identity.Category + (string.IsNullOrEmpty(Identity.Name) ? string.Empty : " (" + Identity.Name + ")"),
								FieldType.Identity,
								FieldQoS.AutomaticReadout));
						}
					}

					Request.LogFields(Fields);
					Request.Done();
				}
				else
					Request.Fail("Unable to perform a service discovery.");

				return Task.CompletedTask;

			}, null);

			return Request;
		}

	}
}
