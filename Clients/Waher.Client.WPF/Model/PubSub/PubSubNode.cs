﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Xml;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.DataForms.DataTypes;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Things.DisplayableParameters;
using Waher.Client.WPF.Dialogs;
using System.Windows.Controls;
using Waher.Client.WPF.Dialogs.PubSub;

namespace Waher.Client.WPF.Model.PubSub
{
	/// <summary>
	/// Represents a node in a Publish/Subscribe service.
	/// </summary>
	public class PubSubNode : TreeNode
	{
		private NodeType nodeType;
		private readonly string jid;
		private readonly string node;
		private string name;

		public PubSubNode(TreeNode Parent, string Jid, string Node, string Name, NodeType NodeType)
			: base(Parent)
		{
			this.jid = Jid;
			this.node = Node;
			this.name = Name;
			this.nodeType = NodeType;

			this.SetParameters();
		}

		private void SetParameters()
		{
			List<Parameter> Parameters = new List<Parameter>();

			if (!string.IsNullOrEmpty(this.jid))
				Parameters.Add(new StringParameter("JID", "JID", this.jid));

			if (!string.IsNullOrEmpty(this.name))
				Parameters.Add(new StringParameter("Name", "Name", this.name));

			if (this.nodeType == NodeType.collection)
				Parameters.Add(new StringParameter("Type", "Type", "Collection"));
			else
				Parameters.Add(new StringParameter("Type", "Type", "Leaf"));

			this.children = new SortedDictionary<string, TreeNode>()
			{
				{ string.Empty, new Loading(this) }
			};

			this.parameters = new DisplayableParameters(Parameters.ToArray());
		}

		public override string Key => this.node;
		public override string Header => this.node;
		public override string ToolTip => this.name;
		public override bool CanRecycle => false;

		public override string TypeName
		{
			get
			{
				return "Publish/Subscribe Node";
			}
		}

		public override ImageSource ImageResource
		{
			get
			{
				if (this.IsExpanded)
					return XmppAccountNode.folderYellowOpen;
				else
					return XmppAccountNode.folderYellowClosed;
			}
		}

		public override void Write(XmlWriter Output)
		{
			// Don't output.
		}

		public PubSubService Service
		{
			get
			{
				TreeNode Loop = this.Parent;

				while (!(Loop is null))
				{
					if (Loop is PubSubService PubSubService)
						return PubSubService;

					Loop = Loop.Parent;
				}

				return null;
			}
		}

		private bool loadingChildren = false;

		public PubSubClient PubSubClient
		{
			get
			{
				return this.Service?.PubSubClient;
			}
		}

		public override bool CanAddChildren => true;
		public override bool CanDelete => true;
		public override bool CanEdit => true;

		protected override void LoadChildren()
		{
			if (!this.loadingChildren && !this.IsLoaded)
			{
				SortedDictionary<string, TreeNode> Children = new SortedDictionary<string, TreeNode>();

				Mouse.OverrideCursor = Cursors.Wait;
				this.loadingChildren = true;

				if (this.nodeType == NodeType.leaf && this.Service.SupportsLastPublished)
				{
					this.Service.PubSubClient.GetLatestItems(this.node, 50, (sender, e) =>
					{
						this.loadingChildren = false;
						MainWindow.MouseDefault();

						if (e.Ok)
						{
							foreach (Networking.XMPP.PubSub.PubSubItem Item in e.Items)
								Children[Item.ItemId] = new PubSubItem(this, this.jid, Item.Node, Item.ItemId, Item.Payload, Item.Publisher);

							this.children = Children;

							this.OnUpdated();
							this.Service.NodesAdded(this.children.Values, this);

							this.Service.PubSubClient.Subscribe(this.node, (sender2, e2) =>
							{
								if (!e2.Ok)
									MainWindow.ErrorBox("Unable to subscribe to new items: " + e.ErrorText);

								return Task.CompletedTask;
							}, null);
						}
						else
							MainWindow.ErrorBox(string.IsNullOrEmpty(e.ErrorText) ? "Unable to get latest items." : e.ErrorText);

						return Task.CompletedTask;

					}, null);
				}
				else
				{
					this.Service.Account.Client.SendServiceItemsDiscoveryRequest(this.PubSubClient.ComponentAddress, this.node, (sender, e) =>
					{
						this.loadingChildren = false;
						MainWindow.MouseDefault();

						if (e.Ok)
						{
							this.Service.NodesRemoved(this.children.Values, this);

							if (this.nodeType == NodeType.leaf)
							{
								List<string> ItemIds = new List<string>();

								foreach (Item Item in e.Items)
									ItemIds.Add(Item.Name);

								this.Service.PubSubClient.GetItems(this.node, ItemIds.ToArray(), (sender2, e2) =>
								{
									if (e2.Ok)
									{
										if (e2.Items.Length == ItemIds.Count)
										{
											foreach (Networking.XMPP.PubSub.PubSubItem Item in e2.Items)
												Children[Item.ItemId] = new PubSubItem(this, this.jid, Item.Node, Item.ItemId, Item.Payload, Item.Publisher);

											this.children = Children;

											this.OnUpdated();
											this.Service.NodesAdded(this.children.Values, this);
										}
										else
										{
											foreach (Item Item in e.Items)
											{
												this.Service.PubSubClient.GetItems(this.node, new string[] { Item.Name }, (sender3, e3) =>
												{
													if (e3.Ok)
													{
														if (e3.Items.Length == 1)
														{
															Networking.XMPP.PubSub.PubSubItem Item2 = e3.Items[0];
															TreeNode NewNode;

															lock (Children)
															{
																NewNode = new PubSubItem(this, this.jid, Item2.Node, Item2.ItemId, Item2.Payload, Item2.Publisher);
																Children[Item2.ItemId] = NewNode;
																this.children = new SortedDictionary<string, TreeNode>(Children);
															}

															this.OnUpdated();
															this.Service.NodesAdded(new TreeNode[] { NewNode }, this);
														}
													}
													else
														MainWindow.ErrorBox(string.IsNullOrEmpty(e2.ErrorText) ? "Unable to get item." : e3.ErrorText);

													return Task.CompletedTask;

												}, Item);
											}
										}
									}
									else
										MainWindow.ErrorBox(string.IsNullOrEmpty(e2.ErrorText) ? "Unable to get items." : e2.ErrorText);

									return Task.CompletedTask;

								}, null);
							}
							else
							{
								foreach (Item Item in e.Items)
								{
									this.Service.Account.Client.SendServiceDiscoveryRequest(this.PubSubClient.ComponentAddress, Item.Node, (sender2, e2) =>
									{
										if (e2.Ok)
										{
											Item Item2 = (Item)e2.State;
											string Jid = Item2.JID;
											string Node = Item2.Node;
											string Name = Item2.Name;
											NodeType NodeType = NodeType.leaf;
											TreeNode NewNode;

											foreach (Identity Identity in e2.Identities)
											{
												if (Identity.Category == "pubsub")
												{
													if (!Enum.TryParse(Identity.Type, out NodeType))
														NodeType = NodeType.leaf;

													if (!string.IsNullOrEmpty(Identity.Name))
														Name = Identity.Name;
												}
											}

											lock (Children)
											{
												NewNode = new PubSubNode(this, Jid, Node, Name, NodeType);
												Children[Item2.Node] = NewNode;
												this.children = new SortedDictionary<string, TreeNode>(Children);
											}

											this.OnUpdated();
											this.Service.NodesAdded(new TreeNode[] { NewNode }, this);
										}
										else
											MainWindow.ErrorBox(string.IsNullOrEmpty(e2.ErrorText) ? "Unable to get information." : e2.ErrorText);

										return Task.CompletedTask;

									}, Item);
								}
							}
						}
						else
							MainWindow.ErrorBox(string.IsNullOrEmpty(e.ErrorText) ? "Unable to get information." : e.ErrorText);

						return Task.CompletedTask;

					}, null);
				}
			}

			base.LoadChildren();
		}

		protected override void UnloadChildren()
		{
			base.UnloadChildren();

			if (this.IsLoaded)
			{
				if (!(this.children is null))
					this.Service?.NodesRemoved(this.children.Values, this);

				this.children = new SortedDictionary<string, TreeNode>()
				{
					{ string.Empty, new Loading(this) }
				};

				this.OnUpdated();

				this.Service.PubSubClient.Unsubscribe(this.node, null, null);
			}
		}

		public override void Add()
		{
			DataForm Form = null;
			ParameterDialog Dialog = null;

			Form = new DataForm(this.Service.PubSubClient.Client,
				(sender2, e2) =>
				{
					string Payload = Form["Payload"].ValueString;
					string ItemId = Form["ItemId"].ValueString;

					try
					{
						XmlDocument Xml = new XmlDocument()
						{
							PreserveWhitespace = true
						};
						Xml.LoadXml(Payload);
					}
					catch (XmlException ex)
					{
						ex = XML.AnnotateException(ex, Payload);
						Form["Payload"].Error = ex.Message;

						MainWindow.UpdateGui(async () =>
						{
							Dialog = await ParameterDialog.CreateAsync(Form);
							Dialog.ShowDialog();
						});

						return Task.CompletedTask;
					}
					catch (Exception ex)
					{
						Form["Payload"].Error = ex.Message;

						MainWindow.UpdateGui(async () =>
						{
							Dialog = await ParameterDialog.CreateAsync(Form);
							Dialog.ShowDialog();
						});

						return Task.CompletedTask;
					}

					Mouse.OverrideCursor = Cursors.Wait;

					this.Service.PubSubClient.Publish(this.node, ItemId, Payload, (sender3, e3) =>
					{
						MainWindow.MouseDefault();

						if (!e3.Ok)
							MainWindow.ErrorBox("Unable to add item: " + e3.ErrorText);

						return Task.CompletedTask;
					}, null);

					return Task.CompletedTask;
				},
				(sender2, e2) =>
				{
					// Do nothing.
					return Task.CompletedTask;
				}, string.Empty, string.Empty,
				new TextSingleField(null, "ItemId", "Item ID:", false, new string[] { string.Empty }, null, "ID of item to create. If not provided, one will be generated for you.",
					StringDataType.Instance, null, string.Empty, false, false, false),
				new TextMultiField(null, "Payload", "XML:", false, new string[] { string.Empty }, null, "XML payload of item.",
					StringDataType.Instance, null, string.Empty, false, false, false));

			MainWindow.UpdateGui(async () =>
			{
				Dialog = await ParameterDialog.CreateAsync(Form);
				Dialog.ShowDialog();
			});
		}

		internal void ItemNotification(ItemNotificationEventArgs e)
		{
			if (this.IsLoaded)
			{
				if (this.TryGetChild(e.ItemId, out TreeNode N) && N is PubSubItem Item)
				{
					Item.Init(e.Item.InnerXml);
					Item.OnUpdated();
				}
				else
				{
					Item = new PubSubItem(this, e.Publisher, e.NodeName, e.ItemId, e.Item.InnerText, e.Publisher);

					if (this.children is null)
						this.children = new SortedDictionary<string, TreeNode>() { { Item.Key, Item } };
					else
					{
						lock (this.children)
						{
							this.children[Item.Key] = Item;
						}
					}

					MainWindow.UpdateGui(() =>
					{
						this.Service?.Account?.View?.NodeAdded(this, Item);
						this.OnUpdated();
						return Task.CompletedTask;
					});
				}
			}
		}

		internal void ItemRetracted(ItemNotificationEventArgs e)
		{
			if (this.TryGetChild(e.ItemId, out TreeNode N) && this.RemoveChild(N))
			{
				MainWindow.UpdateGui(() =>
				{
					this.Service?.Account?.View?.NodeRemoved(this, N);
					this.OnUpdated();
					return Task.CompletedTask;
				});
			}
		}

		internal void Purged(NodeNotificationEventArgs _)
		{
			if (this.children is null)
				return;

			TreeNode[] ToRemove;

			lock (this.children)
			{
				ToRemove = new TreeNode[this.children.Count];
				this.children.Values.CopyTo(ToRemove, 0);
				this.children = null;
			}

			MainWindow.UpdateGui(() =>
			{
				foreach (TreeNode Node in ToRemove)
					this.Service?.Account?.View?.NodeRemoved(this, Node);

				this.OnUpdated();

				return Task.CompletedTask;
			});
		}

		public override void AddContexMenuItems(ref string CurrentGroup, ContextMenu Menu)
		{
			MenuItem MenuItem;

			base.AddContexMenuItems(ref CurrentGroup, Menu);

			this.GroupSeparator(ref CurrentGroup, "PubSubNode", Menu);

			Menu.Items.Add(MenuItem = new MenuItem()
			{
				Header = "Affiliations...",
				IsEnabled = true,
			});

			MenuItem.Click += this.Affiliations_Click;

			Menu.Items.Add(MenuItem = new MenuItem()
			{
				Header = "Purge...",
				IsEnabled = true,
			});

			MenuItem.Click += this.Purge_Click;
		}

		private void Purge_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(MainWindow.currentInstance, "Are you sure you want to purge all items in " + this.node + "?", "Are you sure?", MessageBoxButton.YesNo,
				MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				try
				{
					this.Service.PubSubClient.PurgeNode(this.node, (sender2, e2) =>
					{
						if (!e2.Ok)
							MainWindow.ErrorBox("Unable to purge the node: " + e2.ErrorText);

						return Task.CompletedTask;

					}, null);
				}
				catch (Exception ex)
				{
					MainWindow.ErrorBox(ex.Message);
				}
			}
		}

		private void Affiliations_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Mouse.OverrideCursor = Cursors.Wait;

				this.Service.PubSubClient.GetAffiliations(this.node, (sender2, e2) =>
				{
					MainWindow.MouseDefault();

					MainWindow.UpdateGui(() =>
					{
						if (!e2.Ok)
							MainWindow.ErrorBox("Unable to get the affiliations for the node: " + e2.ErrorText);

						AffiliationsForm Form = new AffiliationsForm(this.node)
						{
							Owner = MainWindow.currentInstance
						};

						foreach (Affiliation Affiliation in e2.Affiliations)
						{
							ListViewItem Item = new ListViewItem()
							{
								Content = new AffiliationItem(Affiliation)
							};

							Form.AffiliationView.Items.Add(Item);
						}

						bool? b = Form.ShowDialog();
						if (b.HasValue && b.Value)
						{
							List<KeyValuePair<string, AffiliationStatus>> Affiliations = new List<KeyValuePair<string, AffiliationStatus>>();

							foreach (ListViewItem Item in Form.AffiliationView.Items)
							{
								if (Item.Content is AffiliationItem AffiliationItem)
								{
									Affiliations.Add(new KeyValuePair<string, AffiliationStatus>(
										AffiliationItem.Affiliation.Jid,
										AffiliationItem.Affiliation.Status));
								}
							}

							Mouse.OverrideCursor = Cursors.Wait;
							try
							{
								this.Service.PubSubClient.UpdateAffiliations(this.node, Affiliations, (sender3, e3) =>
								{
									MainWindow.MouseDefault();

									if (e3.Ok)
										MainWindow.SuccessBox("Affiliations updated for the node.");
									else
										MainWindow.ErrorBox("Unable to update affiliations for the node: " + e3.ErrorText);

									return Task.CompletedTask;
								}, null);
							}
							catch (Exception ex)
							{
								MainWindow.ErrorBox(ex.Message);
							}
						}

						return Task.CompletedTask;
					});

					return Task.CompletedTask;

				}, null);
			}
			catch (Exception ex)
			{
				MainWindow.ErrorBox(ex.Message);
			}
		}

		public override void Edit()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			this.PubSubClient.GetNodeConfiguration(this.node, (sender, e) =>
			{
				MainWindow.MouseDefault();

				if (e.Ok)
				{
					DataForm Form = null;

					Form = new DataForm(this.PubSubClient.Client,
						(sender2, e2) =>
						{
							Mouse.OverrideCursor = Cursors.Wait;

							this.PubSubClient.ConfigureNode(this.node, e.Form, (sender3, e3) =>
							{
								MainWindow.MouseDefault();

								if (e3.Ok)
								{
									this.name = Form["pubsub#title"]?.ValueString ?? string.Empty;

									if (!Enum.TryParse(Form["pubsub#node_type"]?.ValueString ?? string.Empty, out this.nodeType))
										this.nodeType = NodeType.leaf;

									this.SetParameters();
									this.OnUpdated();
								}
								else
									MainWindow.ErrorBox("Unable to update node: " + e3.ErrorText);

								return Task.CompletedTask;
							}, null);

							return Task.CompletedTask;
						},
						(sender2, e2) =>
						{
							// Do nothing.
							return Task.CompletedTask;
						}, string.Empty, string.Empty, e.Form.Fields);

					MainWindow.UpdateGui(async () =>
					{
						ParameterDialog Dialog = await ParameterDialog.CreateAsync(Form);
						Dialog.ShowDialog();
					});
				}
				else
					MainWindow.ErrorBox("Unable to get node properties: " + e.ErrorText);

				return Task.CompletedTask;

			}, null);
		}

		public override void Delete(TreeNode Parent, EventHandler OnDeleted)
		{
			Mouse.OverrideCursor = Cursors.Wait;

			this.Service.PubSubClient.DeleteNode(this.node, (sender, e) =>
			{
				MainWindow.MouseDefault();

				if (e.Ok)
				{
					try
					{
						base.Delete(Parent, OnDeleted);
					}
					catch (Exception ex)
					{
						Log.Exception(ex);
					}
				}
				else
					MainWindow.ErrorBox("Unable to delete node: " + e.ErrorText);

				return Task.CompletedTask;

			}, null);
		}

	}
}
