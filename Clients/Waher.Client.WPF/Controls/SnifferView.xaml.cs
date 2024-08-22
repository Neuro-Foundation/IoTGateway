﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Waher.Content.Xml;
using Waher.Content.Xsl;
using Waher.Events;
using Waher.Client.WPF.Model;
using Waher.Client.WPF.Controls.Sniffers;
using System.Threading.Tasks;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for SnifferView.xaml
	/// </summary>
	public partial class SnifferView : UserControl, ITabView
	{
		private readonly TreeNode node;
		private readonly string identifier;
		private readonly bool custom;
		private TabSniffer sniffer;

		public SnifferView(TreeNode Node, string Identifier, bool Custom)
		{
			this.node = Node;
			this.identifier = Identifier;
			this.sniffer = null;
			this.custom = Custom;

			this.InitializeComponent();
		}

		public void Dispose()
		{
			this.node?.RemoveSniffer(this.sniffer);
			this.node?.ViewClosed();
		}

		public TreeNode Node => this.node;
		public string Identifier => this.identifier;
		public bool Custom => this.custom;

		public TabSniffer Sniffer
		{
			get => this.sniffer;
			internal set => this.sniffer = value;
		}

		public void Add(SniffItem Item)
		{
			MainWindow.UpdateGui(this.AddItem, Item);
		}

		private Task AddItem(object P)
		{
			this.SnifferListView.Items.Add((SniffItem)P);
			this.SnifferListView.ScrollIntoView(P);
			return Task.CompletedTask;
		}

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			this.SnifferListView.Items.Clear();
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
				DefaultExt = "xml",
				Filter = "XML Files (*.xml)|*.xml|HTML Files (*.html,*.htm)|*.html,*.htm|All Files (*.*)|*.*",
				Title = "Save sniff file"
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

						string Html = XSL.Transform(Xml.ToString(), sniffToHtml);

						File.WriteAllText(Dialog.FileName, Html, Encoding.UTF8);
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

		private static readonly XslCompiledTransform sniffToHtml = XSL.LoadTransform("Waher.Client.WPF.Transforms.SniffToHTML.xslt");
		private static readonly XmlSchema schema = XSL.LoadSchema("Waher.Client.WPF.Schema.Sniff.xsd");
		private const string sniffNamespace = "http://waher.se/Schema/Sniff.xsd";
		private const string sniffRoot = "Sniff";

		private void SaveAsXml(XmlWriter w)
		{
			w.WriteStartElement(sniffRoot, sniffNamespace);

			foreach (SniffItem Item in this.SnifferListView.Items)
			{
				w.WriteStartElement(Item.Type.ToString());
				w.WriteAttributeString("timestamp", XML.Encode(Item.Timestamp));

				if (!(Item.Data is null))
					w.WriteValue(Convert.ToBase64String(Item.Data));
				else
					w.WriteValue(Item.Message);

				w.WriteEndElement();
			}

			w.WriteEndElement();
			w.Flush();
		}

		public void OpenButton_Click(object sender, RoutedEventArgs e)
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
					Title = "Open sniff file"
				};

				bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

				if (Result.HasValue && Result.Value)
				{
					XmlDocument Xml = new XmlDocument()
					{
						PreserveWhitespace = true
					};
					Xml.Load(Dialog.FileName);

					this.Load(Xml, Dialog.FileName);
				}
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(ex.Message, "Unable to load file.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void Load(XmlDocument Xml, string FileName)
		{
			XmlElement E;
			DateTime Timestamp;
			Color ForegroundColor;
			Color BackgroundColor;
			string Message;
			byte[] Data;
			bool IsData;

			XSL.Validate(FileName, Xml, sniffRoot, sniffNamespace, schema);

			this.SnifferListView.Items.Clear();

			foreach (XmlNode N in Xml.DocumentElement.ChildNodes)
			{
				E = N as XmlElement;
				if (E is null)
					continue;

				if (!Enum.TryParse(E.LocalName, out SniffItemType Type))
					continue;

				Timestamp = XML.Attribute(E, "timestamp", DateTime.MinValue);

				switch (Type)
				{
					case SniffItemType.DataReceived:
						ForegroundColor = Colors.White;
						BackgroundColor = Colors.Navy;
						IsData = true;
						break;

					case SniffItemType.DataTransmitted:
						ForegroundColor = Colors.Black;
						BackgroundColor = Colors.White;
						IsData = true;
						break;

					case SniffItemType.TextReceived:
						ForegroundColor = Colors.White;
						BackgroundColor = Colors.Navy;
						IsData = false;
						break;

					case SniffItemType.TextTransmitted:
						ForegroundColor = Colors.Black;
						BackgroundColor = Colors.White;
						IsData = false;
						break;

					case SniffItemType.Information:
						ForegroundColor = Colors.Yellow;
						BackgroundColor = Colors.DarkGreen;
						IsData = false;
						break;

					case SniffItemType.Warning:
						ForegroundColor = Colors.Black;
						BackgroundColor = Colors.Yellow;
						IsData = false;
						break;

					case SniffItemType.Error:
						ForegroundColor = Colors.Yellow;
						BackgroundColor = Colors.Red;
						IsData = false;
						break;

					case SniffItemType.Exception:
						ForegroundColor = Colors.Yellow;
						BackgroundColor = Colors.DarkRed;
						IsData = false;
						break;

					default:
						continue;
				}

				if (IsData)
				{
					Data = Convert.FromBase64String(E.InnerText);
					Message = TabSniffer.HexToString(Data);
				}
				else
				{
					Data = null;
					Message = E.InnerText;
				}

				this.Add(new SniffItem(Timestamp, Type, Message, Data, ForegroundColor, BackgroundColor));
			}
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.SnifferListView.View is GridView GridView)
				GridView.Columns[1].Width = Math.Max(this.ActualWidth - GridView.Columns[0].ActualWidth - SystemParameters.VerticalScrollBarWidth - 8, 10);
		}

	}
}
