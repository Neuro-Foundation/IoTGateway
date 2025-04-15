﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SkiaSharp;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Wpf;
using Waher.Content.Xml;
using Waher.Content.Xsl;
using Waher.Events;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Graphs;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for ScriptView.xaml
	/// </summary>
	public partial class ScriptView : UserControl, ITabView
	{
		private readonly Variables variables;

		public ScriptView()
		{
			this.InitializeComponent();

			this.variables = new Variables()
			{
				ConsoleOut = new Script.PrintOutput(this)
			};

			this.Input.Focus();
		}

		private void Input_PreviewKeyDown(object Sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
				{
					this.ExecuteButton_Click(Sender, e);
					e.Handled = true;
				}
			}
		}

		private void ExecuteButton_Click(object Sender, RoutedEventArgs e)
		{
			Waher.Script.Expression Exp;
			TextBlock ScriptBlock;
			UIElement ResultBlock = null;

			try
			{
				Exp = new Waher.Script.Expression(this.Input.Text);

				ScriptBlock = new TextBlock()
				{
					Text = this.Input.Text,
					FontFamily = new FontFamily("Courier New"),
					TextWrapping = TextWrapping.Wrap,
					Tag = Exp
				};

				ScriptBlock.PreviewMouseDown += this.TextBlock_PreviewMouseDown;

				this.HistoryPanel.Children.Add(ScriptBlock);
				this.HistoryScrollViewer.ScrollToBottom();

				this.Input.Text = string.Empty;
				this.Input.Focus();
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(MainWindow.currentInstance, ex.Message, "Unable to parse script.", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Task.Run(async () =>
			{
				try
				{
					IElement Ans;

					Task Preview(object sender2, PreviewEventArgs e2)
					{
						this.Dispatcher.Invoke(async () =>
							ResultBlock = await this.ShowResult(ResultBlock, e2.Preview, ScriptBlock));

						return Task.CompletedTask;
					};

					this.variables.OnPreview += Preview;
					try
					{
						Ans = await Exp.Root.EvaluateAsync(this.variables);
					}
					catch (ScriptReturnValueException ex)
					{
						Ans = ex.ReturnValue;
					}
					catch (Exception ex)
					{
						Ans = new ObjectValue(ex);
					}
					finally
					{
						this.variables.OnPreview -= Preview;
					}

					this.variables["Ans"] = Ans;

					MainWindow.UpdateGui(async () =>
					{
						ResultBlock = await this.ShowResult(ResultBlock, Ans, ScriptBlock);
					});
				}
				catch (Exception ex)
				{
					ex = Log.UnnestException(ex);
					MainWindow.MessageBox(ex.Message, "Unable to parse script.", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			});
		}

		private async Task<UIElement> ShowResult(UIElement ResultBlock, IElement Ans, TextBlock ScriptBlock)
		{
			try
			{
				if (Ans is Graph G)
				{
					PixelInformation Pixels = G.CreatePixels(this.variables, out object[] States);
					return this.AddImageBlock(ScriptBlock, Pixels, G, States, ResultBlock);
				}
				else if (Ans.AssociatedObjectValue is SKImage Img)
					return this.AddImageBlock(ScriptBlock, PixelInformation.FromImage(Img), null, null, ResultBlock);
				else if (Ans.AssociatedObjectValue is Exception ex)
				{
					UIElement Last = ResultBlock ?? ScriptBlock;

					ex = Log.UnnestException(ex);

					if (ex is AggregateException ex2)
					{
						foreach (Exception ex3 in ex2.InnerExceptions)
							Last = this.AddTextBlock(Last, ex3.Message, Colors.Red, FontWeights.Bold, null, ex3);
					}
					else
						Last = this.AddTextBlock(ScriptBlock, ex.Message, Colors.Red, FontWeights.Bold, ResultBlock, ex);

					return Last;
				}
				else if (Ans.AssociatedObjectValue is ObjectMatrix M && !(M.ColumnNames is null))
				{
					StringBuilder Markdown = new StringBuilder();

					foreach (string s2 in M.ColumnNames)
					{
						Markdown.Append("| ");
						Markdown.Append(MarkdownDocument.Encode(s2));
					}

					Markdown.AppendLine(" |");

					foreach (string s2 in M.ColumnNames)
						Markdown.Append("|---");

					Markdown.AppendLine("|");

					int x, y;

					for (y = 0; y < M.Rows; y++)
					{
						for (x = 0; x < M.Columns; x++)
						{
							Markdown.Append("| ");

							object Item = M.GetElement(x, y).AssociatedObjectValue;
							if (Item is not null)
							{
								if (!(Item is string s2))
									s2 = Waher.Script.Expression.ToString(Item);

								s2 = s2.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>");
								Markdown.Append(MarkdownDocument.Encode(s2));
							}
						}

						Markdown.AppendLine(" |");
					}

					MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown.ToString(), ChatView.GetMarkdownSettings());
					string XAML = await Doc.GenerateXAML(ChatView.GetXamlSettings());

					if (XamlReader.Parse(XAML) is UIElement Parsed)
						return this.AddBlock(ScriptBlock, Parsed);

					return null;
				}
				else
					return this.AddTextBlock(ScriptBlock, Ans.ToString(), Colors.Red, FontWeights.Normal, ResultBlock, Ans);
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				Ans = new ObjectValue(ex);
				this.variables["Ans"] = Ans;

				if (ex is AggregateException ex2)
				{
					foreach (Exception ex3 in ex2.InnerExceptions)
						ScriptBlock = this.AddTextBlock(ScriptBlock, ex3.Message, Colors.Red, FontWeights.Bold, null, ex3);
				}
				else
					this.AddTextBlock(ScriptBlock, ex.Message, Colors.Red, FontWeights.Bold, ResultBlock, ex);

				return null;
			}
		}

		private TextBlock AddTextBlock(UIElement ScriptBlock, string s, Color cl, FontWeight Weight, UIElement ResultBlock, object Tag)
		{
			if (ResultBlock is TextBlock TextBlock)
			{
				TextBlock.Text = s;
				TextBlock.Tag = Tag;
			}
			else
			{
				TextBlock = new TextBlock()
				{
					Text = s,
					FontFamily = new FontFamily("Courier New"),
					Foreground = new SolidColorBrush(cl),
					TextWrapping = TextWrapping.Wrap,
					FontWeight = Weight,
					Tag = Tag
				};

				this.AddBlock(ScriptBlock, TextBlock);
			}

			return TextBlock;
		}

		private UIElement AddBlock(UIElement ScriptBlock, UIElement ResultBlock)
		{
			if (ScriptBlock is null)
				this.HistoryPanel.Children.Add(ResultBlock);
			else
				this.HistoryPanel.Children.Insert(this.HistoryPanel.Children.IndexOf(ScriptBlock) + 1, ResultBlock);

			return ResultBlock;
		}

		private void TextBlock_PreviewMouseDown(object Sender, MouseButtonEventArgs e)
		{
			this.Input.Text = ((TextBlock)Sender).Text;
			this.Input.SelectAll();
			this.Input.Focus();
			e.Handled = true;
		}

		private UIElement AddImageBlock(UIElement ScriptBlock, PixelInformation Pixels, Graph Graph, object[] States, UIElement ResultBlock)
		{
			BitmapImage BitmapImage;
			byte[] Bin = Pixels.EncodeAsPng();

			using (MemoryStream ms = new MemoryStream(Bin))
			{
				BitmapImage = new BitmapImage();
				BitmapImage.BeginInit();
				BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				BitmapImage.StreamSource = ms;
				BitmapImage.EndInit();
			}

			if (ResultBlock is Image ImageBlock)
			{
				ImageBlock.Source = BitmapImage;
				ImageBlock.Width = Pixels.Width;
				ImageBlock.Height = Pixels.Height;
				ImageBlock.Tag = new Tuple<byte[], int, int, Graph, object[]>(Bin, Pixels.Width, Pixels.Height, Graph, States);
			}
			else
			{
				ImageBlock = new Image()
				{
					Source = BitmapImage,
					Width = Pixels.Width,
					Height = Pixels.Height,
					Tag = new Tuple<byte[], int, int, Graph, object[]>(Bin, Pixels.Width, Pixels.Height, Graph, States)
				};

				ImageBlock.PreviewMouseDown += this.ImageBlock_PreviewMouseDown;

				this.HistoryPanel.Children.Insert(this.HistoryPanel.Children.IndexOf(ScriptBlock) + 1, ImageBlock);
			}

			return ImageBlock;
		}

		private void ImageBlock_PreviewMouseDown(object Sender, MouseButtonEventArgs e)
		{
			Image ImageBlock = (Image)Sender;

			if (e.ChangedButton == MouseButton.Left)
			{
				Point P = e.GetPosition(ImageBlock);
				string Script;

				if (ImageBlock.Tag is Tuple<byte[], int, int, Graph, object[]> Image && !(Image.Item4 is null) && !(Image.Item5 is null))
				{
					double X = ((double)P.X) * Image.Item2 / ImageBlock.ActualWidth;
					double Y = ((double)P.Y) * Image.Item3 / ImageBlock.ActualHeight;

					Script = Image.Item4.GetBitmapClickScript(X, Y, Image.Item5);
				}
				else
					Script = "[" + P.X.ToString() + "," + P.Y.ToString() + "]";

				this.Input.Text = Script;
				this.ExecuteButton_Click(this, e);
			}
			else if (e.ChangedButton == MouseButton.Right)
			{
				BitmapImage Image = (BitmapImage)ImageBlock.Source;

				SaveFileDialog Dialog = new SaveFileDialog()
				{
					Title = "Save Image",
					DefaultExt = ImageCodec.FileExtensionPng,
					Filter = "PNG files (*.png)|*.png|All Image files (*.bmp, *.gif, *.jpg, *.jpeg, *.png, *.tif, *.tiff)|*.bmp, *.gif, *.jpg, *.jpeg, *.png, *.tif, *.tiff|All files (*.*)|*.*",
					OverwritePrompt = true
				};

				bool? Result = Dialog.ShowDialog();
				if (Result.HasValue && Result.Value)
				{
					BitmapEncoder Encoder;

					string Extension = System.IO.Path.GetExtension(Dialog.FileName).ToLower();
					if (Extension.StartsWith("."))
						Extension = Extension.Substring(1);

					switch (Extension)
					{
						case ImageCodec.FileExtensionJpg:
						case "jpeg":
							Encoder = new JpegBitmapEncoder();
							break;

						case ImageCodec.FileExtensionBmp:
							Encoder = new BmpBitmapEncoder();
							break;

						case ImageCodec.FileExtensionGif:
							Encoder = new GifBitmapEncoder();
							break;

						case ImageCodec.FileExtensionTiff:
						case "tiff":
							Encoder = new TiffBitmapEncoder();
							break;

						case ImageCodec.FileExtensionPng:
						default:
							Encoder = new PngBitmapEncoder();
							break;
					}

					try
					{
						Encoder.Frames.Add(BitmapFrame.Create(Image));

						using (FileStream File = new FileStream(Dialog.FileName, System.IO.FileMode.Create))
						{
							Encoder.Save(File);
						}
					}
					catch (Exception ex)
					{
						ex = Log.UnnestException(ex);
						MessageBox.Show(MainWindow.currentInstance, ex.Message, "Unable to save image.", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				e.Handled = true;
			}
		}

		internal void Print(string Output)
		{
			MainWindow.UpdateGui(() =>
			{
				this.AddTextBlock(null, Output, Colors.Blue, FontWeights.Normal, null, false);
				return Task.CompletedTask;
			});
		}

		public void NewButton_Click(object Sender, RoutedEventArgs e)
		{
			this.HistoryPanel.Children.Clear();
		}

		public void SaveButton_Click(object Sender, RoutedEventArgs e)
		{
			this.SaveAsButton_Click(Sender, e);
		}

		public void SaveAsButton_Click(object Sender, RoutedEventArgs e)
		{
			SaveFileDialog Dialog = new SaveFileDialog()
			{
				AddExtension = true,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "html",
				Filter = "Script Files (*.xml)|*.xml|Script Files (*.script)|*.script|HTML Files (*.html,*.htm)|*.html,*.htm|All Files (*.*)|*.*",
				Title = "Save Script"
			};

			bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

			if (Result.HasValue && Result.Value)
			{
				try
				{
					switch (Dialog.FilterIndex)
					{
						case 1:
							using (FileStream f = File.Create(Dialog.FileName))
							{
								using (XmlWriter w = XmlWriter.Create(f, XML.WriterSettings(true, false)))
								{
									this.SaveAsXml(w);
								}
							}
							break;

						case 3:
							StringBuilder Xml = new StringBuilder();
							using (XmlWriter w = XmlWriter.Create(Xml, XML.WriterSettings(true, true)))
							{
								this.SaveAsXml(w);
							}

							string Html = XSL.Transform(Xml.ToString(), scriptToHtml);

							File.WriteAllText(Dialog.FileName, Html, System.Text.Encoding.UTF8);
							break;

						case 2:
						default:
							using (StreamWriter w = File.CreateText(Dialog.FileName))
							{
								foreach (Object Obj in this.HistoryPanel.Children)
								{
									if (Obj is TextBlock TextBlock && TextBlock.Tag is bool b && b)
									{
										string s = TextBlock.Text.TrimEnd();
										if (!string.IsNullOrEmpty(s) && !s.EndsWith(";"))
											s += ";";

										w.WriteLine(s);
									}
								}

								w.Flush();
							}
							break;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(MainWindow.FindWindow(this), ex.Message, "Unable to save file.", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private static readonly XslCompiledTransform scriptToHtml = XSL.LoadTransform("Waher.Client.WPF.Transforms.ScriptToHTML.xslt");
		private static readonly XmlSchema schema = XSL.LoadSchema(typeof(MainWindow).Namespace + ".Schema.Script.xsd");
		private const string scriptNamespace = "http://waher.se/Schema/Script.xsd";
		private const string scriptRoot = "Script";

		private void SaveAsXml(XmlWriter w)
		{
			w.WriteStartElement(scriptRoot, scriptNamespace);

			foreach (object Object in this.HistoryPanel.Children)
			{
				if (Object is TextBlock TextBlock)
				{
					if (TextBlock.Tag is Waher.Script.Expression Exp)
						w.WriteElementString("Expression", Exp.Script);
					else if (TextBlock.Tag is Exception ex)
						w.WriteElementString("Error", ex.Message);
					else if (TextBlock.Tag is bool b)
					{
						if (b)
							w.WriteElementString("Result", TextBlock.Text);
						else
							w.WriteElementString("Print", TextBlock.Text);
					}
				}
				else if (Object is Image ImageBlock)
				{
					if (ImageBlock.Tag is Tuple<byte[], int, int, Graph, object[]> Image)
					{
						w.WriteStartElement("Image");
						w.WriteAttributeString("width", Image.Item2.ToString());
						w.WriteAttributeString("height", Image.Item3.ToString());
						w.WriteValue(Convert.ToBase64String(Image.Item1));
						w.WriteEndElement();
					}
				}
			}

			w.WriteEndElement();
			w.Flush();
		}

		public void OpenButton_Click(object Sender, RoutedEventArgs e)
		{
			try
			{
				OpenFileDialog Dialog = new OpenFileDialog()
				{
					AddExtension = true,
					CheckFileExists = true,
					CheckPathExists = true,
					DefaultExt = "xml",
					Filter = "Script Files (*.xml)|*.xml|Script Files (*.script)|*.script|All Files (*.*)|*.*",
					Multiselect = false,
					ShowReadOnly = true,
					Title = "Load Script"
				};

				bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

				if (Result.HasValue && Result.Value)
				{
					if (Dialog.FilterIndex == 1)
					{
						XmlDocument Xml = new XmlDocument()
						{
							PreserveWhitespace = true
						};
						Xml.Load(Dialog.FileName);

						this.Load(Xml, Dialog.FileName);
					}
					else
					{
						string Script = File.ReadAllText(Dialog.FileName);

						this.Input.Text = Script;
						this.ExecuteButton_Click(this, new RoutedEventArgs());
					}
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
			XSL.Validate(FileName, Xml, scriptRoot, scriptNamespace, schema);

			this.HistoryPanel.Children.Clear();

			foreach (XmlNode N in Xml.DocumentElement.ChildNodes)
			{
				if (N is XmlElement E)
				{
					switch (E.LocalName)
					{
						case "Expression":
							Waher.Script.Expression Exp = new Waher.Script.Expression(this.Input.Text);

							TextBlock ScriptBlock = new TextBlock()
							{
								Text = E.InnerText,
								FontFamily = new FontFamily("Courier New"),
								TextWrapping = TextWrapping.Wrap,
								Tag = Exp
							};

							ScriptBlock.PreviewMouseDown += this.TextBlock_PreviewMouseDown;

							this.HistoryPanel.Children.Add(ScriptBlock);
							break;

						case "Error":
							this.AddTextBlock(null, E.InnerText, Colors.Red, FontWeights.Bold, null, new Exception(E.InnerText));
							break;

						case "Result":
							this.AddTextBlock(null, E.InnerText, Colors.Red, FontWeights.Normal, null, true);
							break;

						case "Print":
							this.AddTextBlock(null, E.InnerText, Colors.Blue, FontWeights.Normal, null, false);
							break;

						case "Image":
							BitmapImage BitmapImage;
							byte[] Bin = Convert.FromBase64String(E.InnerText);
							int Width = XML.Attribute(E, "width", 0);
							int Height = XML.Attribute(E, "height", 0);

							using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(E.InnerText)))
							{
								BitmapImage = new BitmapImage();
								BitmapImage.BeginInit();
								BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
								BitmapImage.StreamSource = ms;
								BitmapImage.EndInit();
							}

							Image ImageBlock = new System.Windows.Controls.Image()
							{
								Source = BitmapImage,
								Width = Width,
								Height = Height,
								Tag = new Tuple<byte[], int, int, Graph, object[]>(Bin, Width, Height, null, null)
							};

							ImageBlock.PreviewMouseDown += this.ImageBlock_PreviewMouseDown;

							this.HistoryPanel.Children.Add(ImageBlock);
							break;
					}
				}
			}

			this.HistoryScrollViewer.ScrollToBottom();
		}


		public void Dispose()
		{
			this.HistoryPanel.Children.Clear();
		}

		private void Hyperlink_Click(object Sender, RoutedEventArgs e)
		{
			string Uri = ((Hyperlink)Sender).NavigateUri.ToString();
			System.Diagnostics.Process.Start(Uri);
		}
	}
}
