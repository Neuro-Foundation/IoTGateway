﻿using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Waher.Client.WPF.Model;
using Waher.Client.WPF.Dialogs;
using Waher.Client.WPF.Controls.Sniffers;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Things.DisplayableParameters;
using System.Threading.Tasks;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for ConnectionView.xaml
	/// </summary>
	public partial class ConnectionView : UserControl, ITabView
	{
		private string fileName = string.Empty;
		private Connections connections;
		private TreeNode selectedNode = null;

		public ConnectionView()
		{
			this.InitializeComponent();
		}

		public void Load(MainWindow Owner)
		{
			this.connections = new Connections(Owner);
		}

		public void Dispose()
		{
			this.connections.New();
		}

		public MainWindow MainWindow
		{
			get { return MainWindow.FindWindow(this); }
		}

		public Connections Connections => this.connections;

		public string FileName
		{
			get => this.fileName;
			set
			{
				this.fileName = value;
				if (string.IsNullOrEmpty(this.fileName))
					this.MainWindow.Title = MainWindow.WindowTitle;
				else
					this.MainWindow.Title = this.fileName + " - " + MainWindow.WindowTitle;
			}
		}

		private void ConnectionTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			this.ConnectionListView.Items.Clear();

			if (this.ConnectionListView.View is GridView GridView)
			{
				while (GridView.Columns.Count > 2)
					GridView.Columns.RemoveAt(2);
			}
			else
				GridView = null;

			this.selectedNode = this.ConnectionTree.SelectedItem as TreeNode;
			if (!(this.selectedNode is null))
			{
				TreeNode[] Children = this.selectedNode.Children;
				Dictionary<string, bool> Headers = null;
				DisplayableParameters Parameters;

				if (!(Children is null))
				{
					foreach (TreeNode Child in this.selectedNode.Children)
					{
						this.ConnectionListView.Items.Add(Child);

						if (!(GridView is null))
						{
							Parameters = Child.DisplayableParameters;
							if (!(Parameters is null))
							{
								foreach (Parameter P in Parameters.Ordered)
								{
									if (P.Id == "NodeId" || P.Id == "Type")
										continue;

									if (Headers is null)
										Headers = new Dictionary<string, bool>();

									if (!Headers.ContainsKey(P.Id))
									{
										Headers[P.Id] = true;

										GridViewColumn Column = new GridViewColumn()
										{
											Header = P.Name,
											Width = double.NaN,
											DisplayMemberBinding = new Binding("DisplayableParameters[" + P.Id + "]")
										};

										GridView.Columns.Add(Column);
									}
								}
							}
						}
					}
				}
			}

			MainWindow MainWindow = MainWindow.FindWindow(this);
			MainWindow?.SelectionChanged();
		}

		public bool SaveFile()
		{
			if (string.IsNullOrEmpty(this.fileName))
				return this.SaveNewFile();
			else
			{
				this.connections.Save(this.fileName);
				return true;
			}
		}

		public bool CheckSaved()
		{
			if (this.connections.Modified)
			{
				switch (MessageBox.Show(MainWindow.FindWindow(this), "You have unsaved changes. Do you want to save these changes before closing the application?",
					"Save unsaved changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
				{
					case MessageBoxResult.Yes:
						if (this.SaveFile())
							break;
						else
							return false;

					case MessageBoxResult.No:
						break;

					case MessageBoxResult.Cancel:
						return false;
				}
			}

			return true;
		}

		public bool SaveNewFile()
		{
			SaveFileDialog Dialog = new SaveFileDialog()
			{
				AddExtension = true,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "xml",
				Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
				Title = "Save connection file"
			};

			bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

			if (Result.HasValue && Result.Value)
			{
				this.FileName = Dialog.FileName;
				this.SaveFile();
				return true;
			}
			else
				return false;
		}

		public async Task Load(string FileName)
		{
			try
			{
				XmlDocument Xml = new XmlDocument()
				{
					PreserveWhitespace = true
				};
				Xml.Load(FileName);

				switch (Xml.DocumentElement.LocalName)
				{
					case "ClientConnections":
						this.connections.Load(FileName, Xml);
						this.FileName = FileName;

						this.ConnectionTree.Items.Clear();
						foreach (TreeNode Node in this.connections.RootNodes)
							this.AddNode(Node);
						break;

					case "Sniff":
						TabItem TabItem = MainWindow.NewTab(Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						SnifferView SnifferView = new SnifferView(null, null, true);
						TabItem.Content = SnifferView;

						SnifferView.Sniffer = new TabSniffer(SnifferView);

						this.MainWindow.Tabs.SelectedItem = TabItem;

						SnifferView.Load(Xml, FileName);
						break;

					case "Chat":
						TabItem = MainWindow.NewTab(Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						bool Muc = XML.Attribute(Xml.DocumentElement, "muc", false);
						ChatView ChatView = new ChatView(null, Muc);
						ChatView.Input.IsEnabled = false;
						ChatView.SendButton.IsEnabled = false;

						TabItem.Content = ChatView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						await ChatView.Load(Xml, FileName);
						break;

					case "SensorData":
						TabItem = MainWindow.NewTab(Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						SensorDataView SensorDataView = new SensorDataView(null, null, false);
						TabItem.Content = SensorDataView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						SensorDataView.Load(Xml, FileName);
						break;

					case "SearchResult":
						TabItem = MainWindow.NewTab(Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						SearchResultView SearchResultView = new SearchResultView();
						TabItem.Content = SearchResultView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						SearchResultView.Load(Xml, FileName);
						break;

					case "Script":
						TabItem = MainWindow.NewTab(Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						ScriptView ScriptView = new ScriptView();
						TabItem.Content = ScriptView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						ScriptView.Load(Xml, FileName);
						break;

					case "EventOutput":
						TabItem = MainWindow.NewTab(Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						LogView LogView = new LogView(null, false);
						TabItem.Content = LogView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						LogView.Load(Xml, FileName);
						break;

					case "Report":
						TabItem = MainWindow.NewTab(Path.GetFileName(FileName), out TextBlock HeaderLabel);
						this.MainWindow.Tabs.Items.Add(TabItem);

						QueryResultView ReportView = await QueryResultView.CreateAsync(null, null, HeaderLabel);
						TabItem.Content = ReportView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						ReportView.Load(Xml, FileName);
						break;

					default:
						throw new Exception("Unrecognized file format.");
				}
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(ex.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			if (!this.CheckSaved())
				return;

			this.ConnectionTree.Items.Clear();
			this.connections.New();
			this.FileName = string.Empty;
		}

		public void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			this.SaveFile();
		}

		public void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			this.SaveNewFile();
		}

		public async void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!this.CheckSaved())
					return;

				OpenFileDialog Dialog = new OpenFileDialog()
				{
					AddExtension = true,
					CheckFileExists = true,
					CheckPathExists = true,
					DefaultExt = "xml",
					Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
					Multiselect = false,
					ShowReadOnly = true,
					Title = "Open connection file"
				};

				bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

				if (Result.HasValue && Result.Value)
					await this.Load(Dialog.FileName);
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(ex.Message, "Unable to load file.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void ConnectTo_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ConnectToForm Dialog = new ConnectToForm()
			{
				Owner = this.MainWindow
			};
			bool? Result = Dialog.ShowDialog();

			if (Result.HasValue && Result.Value)
			{
				if (!int.TryParse(Dialog.XmppPort.Text, out int Port))
					Port = XmppCredentials.DefaultPort;

				XmppAccountNode Node = new XmppAccountNode(this.connections, null, Dialog.XmppServer.Text,
					(TransportMethod)Dialog.ConnectionMethod.SelectedIndex, Port, Dialog.UrlEndpoint.Text,
					Dialog.AccountName.Text, Dialog.PasswordHash, Dialog.PasswordHashMethod,
					Dialog.TrustServerCertificate.IsChecked.HasValue && Dialog.TrustServerCertificate.IsChecked.Value,
					Dialog.AllowInsecureAuthentication.IsChecked.HasValue && Dialog.AllowInsecureAuthentication.IsChecked.Value);

				this.connections.Add(Node);
				this.AddNode(Node);
			}
		}

		private void AddNode(TreeNode Node)
		{
			this.ConnectionTree.Items.Add(Node);
			this.NodeAdded(null, Node);
		}

		public void NodeAdded(TreeNode _, TreeNode ChildNode)
		{
			ChildNode.Updated += this.Node_Updated;
			ChildNode.Added(this.MainWindow);
		}

		public void NodeRemoved(TreeNode Parent, TreeNode ChildNode)
		{
			ChildNode.Updated -= this.Node_Updated;
			ChildNode.Removed(this.MainWindow);

			if (Parent is null)
			{
				this.connections.Delete(ChildNode);
				MainWindow.UpdateGui(() =>
				{
					this.ConnectionTree.Items.Remove(ChildNode);
					return Task.CompletedTask;
				});
			}
			else
				Parent.RemoveChild(ChildNode);

			this.Node_Updated(this, EventArgs.Empty);
		}

		private void Node_Updated(object sender, EventArgs e)
		{
			lock (this.syncObj)
			{
				this.refreshTimer?.Dispose();
				this.refreshTimer = new Timer(this.RefreshTree, null, 250, Timeout.Infinite);
			}
		}

		public void ShowStatus(string Message)
		{
			lock (this.syncObj)
			{
				if (this.status == Message)
					return;

				this.status = Message;

				this.statusTimer?.Dispose();
				this.statusTimer = new Timer(this.SetStatus, null, 250, Timeout.Infinite);
			}
		}

		private string status = string.Empty;
		private Timer refreshTimer = null;
		private Timer statusTimer = null;
		private readonly object syncObj = new object();

		private void RefreshTree(object _)
		{
			lock (this.syncObj)
			{
				this.refreshTimer?.Dispose();
				this.refreshTimer = null;
			}

			MainWindow.UpdateGui(() =>
			{
				this.ConnectionTree.Items.Refresh();
				return Task.CompletedTask;
			});
		}

		private void SetStatus(object _)
		{
			lock (this.syncObj)
			{
				this.statusTimer?.Dispose();
				this.statusTimer = null;
			}

			MainWindow.UpdateGui(() =>
			{
				this.ConnectionStatus.Content = this.status;
				return Task.CompletedTask;
			});
		}

		private void ConnectionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.ConnectionListView_GotFocus(sender, e);
		}

		public TreeNode SelectedNode => this.selectedNode;

		private void TreeContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			this.PopulateConextMenu(this.TreeContextMenu);
		}

		private void ListViewContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			this.PopulateConextMenu(this.ListViewContextMenu);
		}

		private void PopulateConextMenu(ContextMenu Menu)
		{
			string Group = string.Empty;

			Menu.Items.Clear();

			this.selectedNode?.AddContexMenuItems(ref Group, Menu);
		}

		private void ConnectionListView_GotFocus(object sender, RoutedEventArgs e)
		{
			this.selectedNode = this.ConnectionListView.SelectedItem as TreeNode;

			MainWindow MainWindow = MainWindow.FindWindow(this);
			MainWindow?.SelectionChanged();
		}

		private void ConnectionTree_GotFocus(object sender, RoutedEventArgs e)
		{
			this.selectedNode = this.ConnectionTree.SelectedItem as TreeNode;

			MainWindow MainWindow = MainWindow.FindWindow(this);
			MainWindow?.SelectionChanged();
		}

	}
}
