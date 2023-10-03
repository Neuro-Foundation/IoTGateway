﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Waher.Content.Markdown.Model.BlockElements
{
	/// <summary>
	/// Represents a numbered list in a markdown document.
	/// </summary>
	public class NumberedList : BlockElementChildren
	{
		/// <summary>
		/// Represents a numbered list in a markdown document.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Children">Child elements.</param>
		public NumberedList(MarkdownDocument Document, IEnumerable<MarkdownElement> Children)
			: base(Document, Children)
		{
		}

		/// <summary>
		/// Represents a numbered list in a markdown document.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Children">Child elements.</param>
		public NumberedList(MarkdownDocument Document, params MarkdownElement[] Children)
			: base(Document, Children)
		{
		}

		/// <summary>
		/// Generates Markdown for the markdown element.
		/// </summary>
		/// <param name="Output">Markdown will be output here.</param>
		public override async Task GenerateMarkdown(StringBuilder Output)
		{
			await base.GenerateMarkdown(Output);
			Output.AppendLine();
		}

		/// <summary>
		/// Generates HTML for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		public override async Task GenerateHTML(StringBuilder Output)
		{
			NumberedItem Item;
			int Expected = 0;

			Output.AppendLine("<ol>");

			foreach (MarkdownElement E in this.Children)
			{
				Expected++;
				Item = E as NumberedItem;

				if (Item is null)
					await E.GenerateHTML(Output);
				else if (Item.Number == Expected)
				{
					Output.Append("<li>");
					await Item.Child.GenerateHTML(Output);
					Output.AppendLine("</li>");
				}
				else
				{
					await Item.GenerateHTML(Output);
					Expected = Item.Number;
				}
			}

			Output.AppendLine("</ol>");
		}

		/// <summary>
		/// Generates plain text for the markdown element.
		/// </summary>
		/// <param name="Output">Plain text will be output here.</param>
		public override async Task GeneratePlainText(StringBuilder Output)
		{
			StringBuilder sb = new StringBuilder();
			string s;
			string s2 = Environment.NewLine + Environment.NewLine;
			bool LastIsParagraph = false;

			s = Output.ToString();
			if (!s.EndsWith(Environment.NewLine) && !string.IsNullOrEmpty(s))
				Output.AppendLine();

			foreach (MarkdownElement E in this.Children)
			{
				await E.GeneratePlainText(sb);
				s = sb.ToString();
				sb.Clear();
				Output.Append(s);

				LastIsParagraph = s.EndsWith(s2);
			}

			if (!LastIsParagraph)
				Output.AppendLine();
		}

		/// <summary>
		/// Generates WPF XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="TextAlignment">Alignment of text in element.</param>
		public override async Task GenerateXAML(XmlWriter Output, TextAlignment TextAlignment)
		{
			XamlSettings Settings = this.Document.Settings.XamlSettings;
			NumberedItem Item;
			int Expected = 0;
			int Row = 0;
			bool ParagraphBullet;

			Output.WriteStartElement("Grid");
			Output.WriteAttributeString("Margin", Settings.ParagraphMargins);
			Output.WriteStartElement("Grid.ColumnDefinitions");

			Output.WriteStartElement("ColumnDefinition");
			Output.WriteAttributeString("Width", "Auto");
			Output.WriteEndElement();

			Output.WriteStartElement("ColumnDefinition");
			Output.WriteAttributeString("Width", "*");
			Output.WriteEndElement();

			Output.WriteEndElement();
			Output.WriteStartElement("Grid.RowDefinitions");

			foreach (MarkdownElement _ in this.Children)
			{
				Output.WriteStartElement("RowDefinition");
				Output.WriteAttributeString("Height", "Auto");
				Output.WriteEndElement();
			}

			Output.WriteEndElement();

			foreach (MarkdownElement E in this.Children)
			{
				Expected++;
				Item = E as NumberedItem;

				ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
				E.GetMargins(out int TopMargin, out int BottomMargin);

				Output.WriteStartElement("TextBlock");
				Output.WriteAttributeString("TextWrapping", "Wrap");
				Output.WriteAttributeString("Grid.Column", "0");
				Output.WriteAttributeString("Grid.Row", Row.ToString());
				if (TextAlignment != TextAlignment.Left)
					Output.WriteAttributeString("TextAlignment", TextAlignment.ToString());

				Output.WriteAttributeString("Margin", "0," + TopMargin.ToString() + "," +
					Settings.ListContentMargin.ToString() + "," + BottomMargin.ToString());

				if (!(Item is null))
					Output.WriteValue((Expected = Item.Number).ToString());
				else
					Output.WriteValue(Expected.ToString());

				Output.WriteValue(".");
				Output.WriteEndElement();

				Output.WriteStartElement("StackPanel");
				Output.WriteAttributeString("Grid.Column", "1");
				Output.WriteAttributeString("Grid.Row", Row.ToString());

				if (ParagraphBullet)
					await E.GenerateXAML(Output, TextAlignment);
				else
				{
					Output.WriteStartElement("TextBlock");
					Output.WriteAttributeString("TextWrapping", "Wrap");
					if (TextAlignment != TextAlignment.Left)
						Output.WriteAttributeString("TextAlignment", TextAlignment.ToString());

					await E.GenerateXAML(Output, TextAlignment);

					Output.WriteEndElement();
				}

				Output.WriteEndElement();

				Row++;
			}

			Output.WriteEndElement();
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="State">Xamarin Forms XAML Rendering State.</param>
		public override async Task GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State)
		{
			XamlSettings Settings = this.Document.Settings.XamlSettings;
			int Expected = 0;
			int Row = 0;
			bool ParagraphBullet;

			Output.WriteStartElement("ContentView");
			Output.WriteAttributeString("Padding", Settings.ParagraphMargins);

			Output.WriteStartElement("Grid");
			Output.WriteAttributeString("RowSpacing", "0");
			Output.WriteAttributeString("ColumnSpacing", "0");

			Output.WriteStartElement("Grid.ColumnDefinitions");

			Output.WriteStartElement("ColumnDefinition");
			Output.WriteAttributeString("Width", "Auto");
			Output.WriteEndElement();

			Output.WriteStartElement("ColumnDefinition");
			Output.WriteAttributeString("Width", "*");
			Output.WriteEndElement();

			Output.WriteEndElement();
			Output.WriteStartElement("Grid.RowDefinitions");

			foreach (MarkdownElement _ in this.Children)
			{
				Output.WriteStartElement("RowDefinition");
				Output.WriteAttributeString("Height", "Auto");
				Output.WriteEndElement();
			}

			Output.WriteEndElement();

			foreach (MarkdownElement E in this.Children)
			{
				if (E is BlockElementSingleChild Item)
				{
					Expected++;

					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
					E.GetMargins(out int TopMargin, out int BottomMargin);

					Paragraph.GenerateXamarinFormsContentView(Output, State.TextAlignment, "0," + TopMargin.ToString() + "," +
						Settings.ListContentMargin.ToString() + "," + BottomMargin.ToString());
					Output.WriteAttributeString("Grid.Column", "0");
					Output.WriteAttributeString("Grid.Row", Row.ToString());

					Output.WriteStartElement("Label");

					if (Item is NumberedItem NumberedItem)
						Output.WriteValue((Expected = NumberedItem.Number).ToString());
					else
						Output.WriteValue(Expected.ToString());

					Output.WriteValue(".");
					Output.WriteEndElement();
					Output.WriteEndElement();

					Output.WriteStartElement("StackLayout");
					Output.WriteAttributeString("Grid.Column", "1");
					Output.WriteAttributeString("Grid.Row", Row.ToString());
					Output.WriteAttributeString("Orientation", "Vertical");

					if (ParagraphBullet)
						await E.GenerateXamarinForms(Output, State);
					else
						await Paragraph.GenerateXamarinFormsLabel(Output, Item, false, State);

					Output.WriteEndElement();
				}

				Row++;
			}

			Output.WriteEndElement();
			Output.WriteEndElement();
		}

		/// <summary>
		/// Generates Human-Readable XML for Smart Contracts from the markdown text.
		/// Ref: https://gitlab.com/IEEE-SA/XMPPI/IoT/-/blob/master/SmartContracts.md#human-readable-text
		/// </summary>
		/// <param name="Output">Smart Contract XML will be output here.</param>
		/// <param name="State">Current rendering state.</param>
		public override async Task GenerateSmartContractXml(XmlWriter Output, SmartContractRenderState State)
		{
			Output.WriteStartElement("numberedItems");

			foreach (MarkdownElement E in this.Children)
				await E.GenerateSmartContractXml(Output, State);

			Output.WriteEndElement();
		}

		/// <summary>
		/// Generates LaTeX for the markdown element.
		/// </summary>
		/// <param name="Output">LaTeX will be output here.</param>
		public override async Task GenerateLaTeX(StringBuilder Output)
		{
			Output.AppendLine("\\begin{enumerate}");

			foreach (MarkdownElement E in this.Children)
				await E.GenerateLaTeX(Output);

			Output.AppendLine("\\end{enumerate}");
			Output.AppendLine();
		}

		/// <summary>
		/// If the element is an inline span element.
		/// </summary>
		internal override bool InlineSpanElement => false;

		/// <summary>
		/// Exports the element to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		public override void Export(XmlWriter Output)
		{
			this.Export(Output, "NumberedList");
		}

		/// <summary>
		/// Creates an object of the same type, and meta-data, as the current object,
		/// but with content defined by <paramref name="Children"/>.
		/// </summary>
		/// <param name="Children">New content.</param>
		/// <param name="Document">Document that will contain the element.</param>
		/// <returns>Object of same type and meta-data, but with new content.</returns>
		public override MarkdownElementChildren Create(IEnumerable<MarkdownElement> Children, MarkdownDocument Document)
		{
			return new NumberedList(Document, Children);
		}

		/// <summary>
		/// Increments the property or properties in <paramref name="Statistics"/> corresponding to the element.
		/// </summary>
		/// <param name="Statistics">Contains statistics about the Markdown document.</param>
		public override void IncrementStatistics(MarkdownStatistics Statistics)
		{
			Statistics.NrNumberedLists++;
			Statistics.NrLists++;
		}


		/// <summary>
		/// Adds children to the element.
		/// </summary>
		/// <param name="NewChildren">New children to add.</param>
		public override void AddChildren(IEnumerable<MarkdownElement> NewChildren)
		{
			if (this.LastChild is NumberedItem LastItem)
			{
				int Next = LastItem.Number + 1;

				foreach (MarkdownElement E in NewChildren)
				{
					if (E is NumberedItem NewItem && !NewItem.NumberExplicit)
						NewItem.Number = Next++;
					else
						break;
				}
			}

			base.AddChildren(NewChildren);
		}
	}
}
