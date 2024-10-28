﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Waher.Content;
using Waher.Content.Emoji.Emoji1;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Consolidation;
using Waher.Content.Xml;
using Waher.Content.Xsl;
using Waher.Events;
using Waher.Client.WPF.Controls.Chat;
using Waher.Client.WPF.Model;
using Waher.Client.WPF.Model.Muc;
using Waher.Content.Markdown.Rendering;
using Waher.Content.Markdown.Wpf;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for ChatView.xaml
	/// </summary>
	public partial class ChatView : UserControl, ITabView
	{
		private static readonly RandomNumberGenerator rnd = RandomNumberGenerator.Create();
		private static Emoji1LocalFiles emoji1_24x24 = null;

		private readonly Dictionary<string, Consolidator> threads = new Dictionary<string, Consolidator>();
		private readonly TreeNode node;
		private DateTime timer = DateTime.MinValue;
		private bool muc;
		private bool consolidate = true;

		public ChatView(TreeNode Node, bool Muc)
		{
			this.node = Node;
			this.muc = Muc;

			this.InitializeComponent();

			if (Muc)
			{
				double w = this.ReceivedColumn.Width * 2;
				this.FromColumn.Width = w;
				this.ContentColumn.Width -= w;

				this.timer = MainWindow.Scheduler.Add(DateTime.Now.AddMinutes(1), this.MucSelfPing, null);
			}

			this.DataContext = this;
		}

		public bool Muc => this.muc;

		public bool Consolidate
		{
			get => this.consolidate;
			set => this.consolidate = value;
		}

		internal static void InitEmojis()
		{
			if (emoji1_24x24 is null)
			{
				string Folder = Assembly.GetExecutingAssembly().Location;
				if (string.IsNullOrEmpty(Folder))
					Folder = AppDomain.CurrentDomain.BaseDirectory;

				emoji1_24x24 = new Emoji1LocalFiles(Emoji1SourceFileType.Png64, 24, 24,
					Path.Combine(MainWindow.AppDataFolder, "Graphics", "Emoji1", ImageCodec.FileExtensionPng, "64x64", "%FILENAME%"),
					Path.Combine(Path.GetDirectoryName(Folder), "Graphics", "Emoji1.zip"),
					Path.Combine(MainWindow.AppDataFolder, "Graphics"));
			}
		}

		public static Emoji1LocalFiles Emoji1_24x24
		{
			get { return emoji1_24x24; }
		}

		public void Dispose()
		{
			if (this.timer > DateTime.MinValue)
			{
				MainWindow.Scheduler?.Remove(this.timer);
				this.timer = DateTime.MinValue;
			}

			this.Node?.ViewClosed();
		}

		public TreeNode Node => this.node;

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.ChatListView.View is GridView GridView)
			{
				GridView.Columns[2].Width = Math.Max(this.ActualWidth - GridView.Columns[0].ActualWidth -
					GridView.Columns[1].ActualWidth - GridView.Columns[3].ActualWidth -
					SystemParameters.VerticalScrollBarWidth - 8, 10);
			}

			this.Input.Width = Math.Max(this.ActualWidth - this.SendButton.ActualWidth - 16, 10);
		}

		public static MarkdownSettings GetMarkdownSettings()
		{
			return new MarkdownSettings(Emoji1_24x24, false);
		}

		public static HtmlSettings GetHtmlSettings()
		{
			return new HtmlSettings()
			{
				XmlEntitiesOnly = true
			};
		}

		public static XamlSettings GetXamlSettings()
		{
			return new XamlSettings()
			{
				TableCellRowBackgroundColor1 = "#20404040",
				TableCellRowBackgroundColor2 = "#10808080"
			};
		}

		private async void Send_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string Msg = this.Input.Text;
				string ThreadId;

				this.Input.Text = string.Empty;
				this.Input.Focus();

				if (this.muc)
				{
					byte[] Bin = new byte[16];

					lock (rnd)
					{
						rnd.GetBytes(Bin);
					}

					ThreadId = Convert.ToBase64String(Bin);
				}
				else
					ThreadId = string.Empty;

				MarkdownDocument Markdown = await this.ChatMessageTransmitted(Msg, ThreadId);
				await this.node.SendChatMessage(Msg, ThreadId, Markdown);
			}
			catch (Exception ex)
			{
				MainWindow.ErrorBox(ex.Message);
			}
		}

		public async Task<MarkdownDocument> ChatMessageTransmitted(string Message, string ThreadId)
		{
			MarkdownDocument Markdown;

			if (Message.IndexOf('|') >= 0)
			{
				string s;
				int c = this.ChatListView.Items.Count;

				if (c > 0 &&
					this.ChatListView.Items[c - 1] is ChatItem Item &&
					Item.Type == ChatItemType.Transmitted &&
					string.IsNullOrWhiteSpace(Item.From) &&
					(DateTime.Now - Item.LastUpdated).TotalSeconds < 10 &&
					(s = Item.Message).IndexOf('|') >= 0)
				{
					try
					{
						if (!s.EndsWith("\n"))
							s += Environment.NewLine;

						s += Message;
						Markdown = await MarkdownDocument.CreateAsync(s, GetMarkdownSettings());
						await Item.Update(s, Markdown);
						this.ChatListView.Items.Refresh();
						this.ChatListView.ScrollIntoView(Item);
						return Markdown;
					}
					catch (Exception)
					{
						// Ignore.
					}
				}
			}

			try
			{
				new Waher.Script.Expression(Message);
				Markdown = null;   // Expressions should not be parsed as markdown.
			}
			catch (Exception)
			{
				try
				{
					Markdown = await MarkdownDocument.CreateAsync(Message, GetMarkdownSettings());
				}
				catch (Exception)
				{
					Markdown = null;
				}
			}

			this.AddItem(ChatItemType.Transmitted, DateTime.Now, Message, string.Empty, Markdown, ThreadId, Colors.Black, Colors.Honeydew);

			return Markdown;
		}

		private async void AddItem(ChatItemType Type, DateTime Timestamp, string Message, string From, MarkdownDocument Markdown, string ThreadId, Color FgColor, Color BgColor)
		{
			try
			{
				if (this.muc && !string.IsNullOrEmpty(ThreadId))
				{
					switch (Type)
					{
						case ChatItemType.Transmitted:
							int N;

							if (this.node is RoomNode RoomNode)
								N = RoomNode.NrOccupants;
							else
								N = 12;

							lock (this.threads)
							{
								if (!this.threads.ContainsKey(ThreadId))
								{
									this.threads[ThreadId] = new Consolidator(ThreadId, N)
									{
										Tag = new ConsolidationTag()
									};
								}
							}
							break;

						case ChatItemType.Received:
							Consolidator Consolidation;

							if (!this.consolidate)
								break;

							lock (this.threads)
							{
								if (!this.threads.TryGetValue(ThreadId, out Consolidation))
									break;
							}

							ConsolidationTag Rec = (ConsolidationTag)Consolidation.Tag;
							MainWindow.Scheduler?.Remove(Rec.UpdateTP);

							await Consolidation.Add(ChatItem.GetShortFrom(From), Markdown);

							Rec.UpdateTP = MainWindow.Scheduler.Add(DateTime.Now.AddMilliseconds(100), async (_) =>
							{
								StringBuilder sb = new StringBuilder();
								bool First = true;

								Message = await Consolidation.GenerateMarkdownAsync();
								Markdown = await MarkdownDocument.CreateAsync(Message, Markdown.Settings, Markdown.TransparentExceptionTypes);

								foreach (string Source in await Consolidation.GetSources())
								{
									if (First)
										First = false;
									else
										sb.AppendLine();

									sb.Append(Source);
								}

								MainWindow.UpdateGui(async () =>
								{
									bool Added;

									if (Added = (Rec.Item is null))
										Rec.Item = await ChatItem.CreateAsync(Type, Timestamp, string.Empty, string.Empty, null, ThreadId, FgColor, BgColor);

									Rec.Item.From = sb.ToString();
									await Rec.Item.Update(Message, Markdown);

									if (Added)
										Rec.ListViewIndex = this.AddListItem(Rec.Item);
									else
										this.ChatListView.Items[Rec.ListViewIndex] = this.CreateItem(Rec.Item);
								});

							}, null);

							return;
					}
				}

				this.AddListItem(await ChatItem.CreateAsync(Type, Timestamp, Message, From, Markdown, ThreadId, FgColor, BgColor));
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		private class ConsolidationTag
		{
			public ChatItem Item = null;
			public DateTime UpdateTP = DateTime.MinValue;
			public int ListViewIndex;
		}

		private int AddListItem(ChatItem Item)
		{
			ListViewItem ListViewItem = this.CreateItem(Item);
			int Index = this.ChatListView.Items.Add(ListViewItem);
			this.ChatListView.ScrollIntoView(ListViewItem);

			return Index;
		}

		private ListViewItem CreateItem(ChatItem Item)
		{
			return new ListViewItem()
			{
				Content = Item,
				Foreground = new SolidColorBrush(Item.ForegroundColor),
				Background = new SolidColorBrush(Item.BackgroundColor),
				Margin = new Thickness(0)
			};
		}

		public async Task ChatMessageReceived(string Message, string From, string ThreadId, bool IsMarkdown, DateTime Timestamp, MainWindow MainWindow)
		{
			MarkdownDocument Markdown;

			if (IsMarkdown)
			{
				if (Message.IndexOf('|') >= 0)
				{
					int c = this.ChatListView.Items.Count;

					if (c > 0 &&
						this.ChatListView.Items[c - 1] is ChatItem Item &&
						Item.Type == ChatItemType.Received &&
						Item.From == From &&
						(DateTime.Now - Item.LastUpdated).TotalSeconds < 10 &&
						Item.LastIsTable)
					{
						Item.Append(Message, this.ChatListView, MainWindow);
						return;
					}
				}

				try
				{
					Markdown = await MarkdownDocument.CreateAsync(Message, GetMarkdownSettings());
				}
				catch (Exception)
				{
					Markdown = null;
				}
			}
			else
				Markdown = null;

			MainWindow.UpdateGui(() =>
			{
				this.AddItem(ChatItemType.Received, Timestamp, Message, From, Markdown, ThreadId, Colors.Black, Colors.AliceBlue);
				return Task.CompletedTask;
			});
		}

		private void UserControl_GotFocus(object sender, RoutedEventArgs e)
		{
			this.Input.Focus();
		}

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			this.ChatListView.Items.Clear();
		}

		public void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			this.SaveAsButton_Click(sender, e);
		}

		public void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog Dialog = new SaveFileDialog()
			{
				AddExtension = true,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "html",
				Filter = "XML Files (*.xml)|*.xml|HTML Files (*.html,*.htm)|*.html,*.htm|All Files (*.*)|*.*",
				Title = "Save chat session"
			};

			bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

			if (Result.HasValue && Result.Value)
			{
				try
				{
					if (Dialog.FilterIndex == 2)
					{
						StringBuilder Xml = new StringBuilder();
						using (XmlWriter w = XmlWriter.Create(Xml, XML.WriterSettings(true, true)))
						{
							this.SaveAsXml(w);
						}

						string Html = XSL.Transform(Xml.ToString(), chatToHtml);

						File.WriteAllText(Dialog.FileName, Html, System.Text.Encoding.UTF8);
					}
					else
					{
						using (FileStream f = File.Create(Dialog.FileName))
						{
							using (XmlWriter w = XmlWriter.Create(f, XML.WriterSettings(true, false)))
							{
								this.SaveAsXml(w);
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(MainWindow.FindWindow(this), ex.Message, "Unable to save file.", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private static readonly XslCompiledTransform chatToHtml = XSL.LoadTransform("Waher.Client.WPF.Transforms.ChatToHTML.xslt");
		private static readonly XmlSchema schema = XSL.LoadSchema("Waher.Client.WPF.Schema.Chat.xsd");
		private const string chatNamespace = "http://waher.se/Schema/Chat.xsd";
		private const string chatRoot = "Chat";

		private void SaveAsXml(XmlWriter w)
		{
			w.WriteStartElement(chatRoot, chatNamespace);
			w.WriteAttributeString("muc", CommonTypes.Encode(this.muc));

			foreach (ListViewItem Item in this.ChatListView.Items)
			{
				if (Item.Content is ChatItem ChatItem)
				{
					w.WriteStartElement(ChatItem.Type.ToString());
					w.WriteAttributeString("timestamp", XML.Encode(ChatItem.Timestamp));

					if (!string.IsNullOrEmpty(ChatItem.From))
						w.WriteAttributeString("from", ChatItem.FromStr);

					if (!string.IsNullOrEmpty(ChatItem.ThreadId))
						w.WriteAttributeString("thread", ChatItem.ThreadId);

					w.WriteValue(ChatItem.Message);
					w.WriteEndElement();
				}
			}

			w.WriteEndElement();
			w.Flush();
		}

		public async void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				OpenFileDialog Dialog = new OpenFileDialog()
				{
					AddExtension = true,
					CheckFileExists = true,
					CheckPathExists = true,
					DefaultExt = "xml",
					Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
					Multiselect = false,
					ShowReadOnly = true,
					Title = "Open chat session"
				};

				bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

				if (Result.HasValue && Result.Value)
				{
					XmlDocument Xml = new XmlDocument()
					{
						PreserveWhitespace = true
					};
					Xml.Load(Dialog.FileName);

					await this.Load(Xml, Dialog.FileName);
				}
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(ex.Message, "Unable to load file.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public async Task Load(XmlDocument Xml, string FileName)
		{
			MarkdownDocument Markdown;
			XmlElement E;
			DateTime Timestamp;
			Color ForegroundColor;
			Color BackgroundColor;
			string From;
			string ThreadId;

			XSL.Validate(FileName, Xml, chatRoot, chatNamespace, schema);

			this.ChatListView.Items.Clear();

			bool PrevMuc = this.muc;
			this.muc = XML.Attribute(Xml.DocumentElement, "muc", this.muc);

			if (this.muc != PrevMuc)
			{
				if (this.muc)
				{
					double w = this.ReceivedColumn.Width * 2;
					this.FromColumn.Width = w;
					this.ContentColumn.Width -= w;
				}
				else
				{
					this.ContentColumn.Width += this.FromColumn.Width;
					this.FromColumn.Width = 0;
				}
			}

			foreach (XmlNode N in Xml.DocumentElement.ChildNodes)
			{
				E = N as XmlElement;
				if (E is null)
					continue;

				if (!Enum.TryParse(E.LocalName, out ChatItemType Type))
					continue;

				Timestamp = XML.Attribute(E, "timestamp", DateTime.MinValue);
				From = XML.Attribute(E, "from");
				ThreadId = XML.Attribute(E, "thread");

				switch (Type)
				{
					case ChatItemType.Received:
						ForegroundColor = Colors.Black;
						BackgroundColor = Colors.AliceBlue;
						break;

					case ChatItemType.Transmitted:
						ForegroundColor = Colors.Black;
						BackgroundColor = Colors.Honeydew;
						break;

					case ChatItemType.Event:
						ForegroundColor = EventFgColor;
						BackgroundColor = EventBgColor;
						break;

					default:
						continue;
				}

				try
				{
					Markdown = await MarkdownDocument.CreateAsync(E.InnerText, GetMarkdownSettings());
				}
				catch (Exception)
				{
					Markdown = null;
				}

				this.AddItem(Type, Timestamp, E.InnerText, From, Markdown, ThreadId, ForegroundColor, BackgroundColor);
			}
		}

		private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
				{
					this.Send_Click(sender, e);
					e.Handled = true;
				}
			}
		}

		private void ChatListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (this.ChatListView.SelectedItem is ListViewItem ListViewItem &&
				ListViewItem.Content is ChatItem Item)
			{
				this.Input.Text = Item.Message;
				e.Handled = true;
			}
			else if (e.OriginalSource is Run Run)
			{
				this.Input.Text = Run.Text;
				e.Handled = true;
			}
			else if (e.OriginalSource is TextBlock TextBlock)
			{
				this.Input.Text = TextBlock.Text;
				e.Handled = true;
			}
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			string Uri = ((Hyperlink)sender).NavigateUri.ToString();
			System.Diagnostics.Process.Start(Uri);
		}

		public void Event(string Message, string From, string ThreadId)
		{
			this.Event(Message, From, null, DateTime.Now, ThreadId);
		}

		public void Event(string Message, string From, MarkdownDocument Markdown, DateTime Timestamp, string ThreadId)
		{
			MainWindow.UpdateGui(() =>
			{
				this.AddItem(ChatItemType.Event, Timestamp, Message, From, Markdown, ThreadId, EventFgColor, EventBgColor);
				return Task.CompletedTask;
			});
		}

		public static readonly Color EventFgColor = Color.FromRgb(32, 32, 32);
		public static readonly Color EventBgColor = Color.FromRgb(240, 240, 240);

		private void MucSelfPing(object State)
		{
			this.timer = MainWindow.Scheduler.Add(DateTime.Now.AddMinutes(1), this.MucSelfPing, null);

			if (this.node is RoomNode RoomNode)
			{
				RoomNode.MucClient.SelfPing(RoomNode.RoomId, RoomNode.Domain, RoomNode.NickName, (sender, e) =>
				{
					if (!e.Ok)
					{
						if (e.StanzaError is Networking.XMPP.StanzaErrors.NotAcceptableException)   // Need to reconnect with room.
						{
							RoomNode.MucClient.EnterRoom(RoomNode.RoomId, RoomNode.Domain, RoomNode.NickName, RoomNode.Password, (sender2, e2) =>
							{
								if (e2.Ok)
									RoomNode.MucClient.SetPresence(RoomNode.RoomId, RoomNode.Domain, RoomNode.NickName, Networking.XMPP.Availability.Chat, null, null);

								return Task.CompletedTask;
							}, null);
						}
					}

					return Task.CompletedTask;
				}, null);
			}
		}

		public bool ContainsThread(string ThreadId)
		{
			return this.threads.ContainsKey(ThreadId);
		}

	}
}
