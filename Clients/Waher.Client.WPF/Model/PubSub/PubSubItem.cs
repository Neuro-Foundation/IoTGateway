﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Input;
using System.Xml;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using Waher.Networking.XMPP.DataForms.DataTypes;
using Waher.Things.DisplayableParameters;
using Waher.Client.WPF.Dialogs;
using System.Threading.Tasks;

namespace Waher.Client.WPF.Model.PubSub
{
	/// <summary>
	/// Represents a node in a Publish/Subscribe service.
	/// </summary>
	public class PubSubItem : TreeNode
	{
		private XmlDocument xml;
		private DateTime? published = null;
		private readonly string jid = null;
		private readonly string node = null;
		private readonly string itemId = null;
		private string payload = null;
		private string publisher = null;
		private string title = null;
		private string summary = null;
		private string link = null;

		public PubSubItem(TreeNode Parent, string Jid, string Node, string ItemId, string Payload, string Publisher)
			: base(Parent)
		{
			this.jid = Jid;
			this.node = Node;
			this.itemId = ItemId;
			this.publisher = Publisher;

			this.Init(Payload);
		}

		internal void Init(string Payload)
		{
			XmlElement E;

			this.payload = Payload;
			this.publisher = null;
			this.title = null;
			this.summary = null;
			this.link = null;

			this.xml = new XmlDocument()
			{
				PreserveWhitespace = true
			};

			try
			{
				this.xml.LoadXml(Payload);

				if (!(this.xml is null) && !((E = this.xml.DocumentElement) is null))
				{
					if (E.LocalName == "entry" && E.NamespaceURI == "http://www.w3.org/2005/Atom")
					{
						foreach (XmlNode N in E.ChildNodes)
						{
							if (N is XmlElement E2)
							{
								switch (E2.LocalName.ToLower())
								{
									case "title":
										this.title = E2.InnerText;
										break;

									case "summary":
										this.summary = E2.InnerText;
										break;

									case "published":
										if (XML.TryParse(E2.InnerText, out DateTime TP))
											this.published = TP;
										break;

									case "link":
										this.link = XML.Attribute(E2, "href");
										break;
								}
							}
						}
					}
				}

			}
			catch (Exception)
			{
				this.xml = null;    // Not XML payload.
			}

			List<Parameter> Parameters = new List<Parameter>();

			if (!string.IsNullOrEmpty(this.jid))
				Parameters.Add(new StringParameter("JID", "JID", this.jid));

			if (!string.IsNullOrEmpty(this.node))
				Parameters.Add(new StringParameter("Node", "Node", this.node));

			if (!string.IsNullOrEmpty(this.publisher))
				Parameters.Add(new StringParameter("Publisher", "Publisher", this.publisher));

			if (!string.IsNullOrEmpty(this.title))
				Parameters.Add(new StringParameter("Title", "Title", this.title));

			if (!(this.published is null))
				Parameters.Add(new DateTimeParameter("Published", "Published", this.published.Value));

			this.parameters = new DisplayableParameters(Parameters.ToArray());
		}

		public override string Key => this.itemId;
		public override string Header => this.itemId;
		public override string ToolTip => "Item ID " + this.itemId;
		public override bool CanRecycle => false;

		public string Summary => this.summary;
		public string Link => this.link;
		public string Payload => this.payload;

		public override string TypeName => "Publish/Subscribe Item";
		public override ImageSource ImageResource => XmppAccountNode.box;

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
		public override bool CanAddChildren => false;
		public override bool CanDelete => true;
		public override bool CanEdit => true;

		public override void Edit()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			this.Service.PubSubClient.GetItems(this.node, new string[] { this.itemId }, (sender, e) =>
			{
				MainWindow.MouseDefault();

				if (e.Ok)
				{
					if (e.Items.Length == 1)
					{
						Networking.XMPP.PubSub.PubSubItem Item = e.Items[0];
						DataForm Form = null;
						ParameterDialog Dialog = null;

						Form = new DataForm(this.Service.PubSubClient.Client,
							(sender2, e2) =>
							{
								string Payload = Form["Payload"].ValueString;

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

								this.Service.PubSubClient.Publish(this.node, this.itemId, Payload, (sender3, e3) =>
								{
									MainWindow.MouseDefault();

									if (e3.Ok)
									{
										this.Init(Payload);
										this.OnUpdated();
									}
									else
										MainWindow.ErrorBox("Unable to update item: " + e3.ErrorText);

									return Task.CompletedTask;

								}, null);

								return Task.CompletedTask;

							},
							(sender2, e2) =>
							{
								// Do nothing.
								return Task.CompletedTask;
							}, e.From, e.To,
							new JidSingleField(null, "Publisher", "Publisher:", false, new string[] { Item.Publisher }, null, "JID of publisher.",
								null, null, string.Empty, false, true, false),
							new TextMultiField(null, "Payload", "XML:", false, new string[] { Item.Payload }, null, "XML payload of item.",
								StringDataType.Instance, null, string.Empty, false, false, false));

						MainWindow.UpdateGui(async () =>
						{
							Dialog = await ParameterDialog.CreateAsync(Form);
							Dialog.ShowDialog();
						});
					}
				}
				else
					MainWindow.ErrorBox("Unable to get item from server: " + e.ErrorText);

				return Task.CompletedTask;

			}, null);
		}

		public override void Delete(TreeNode Parent, EventHandler OnDeleted)
		{
			Mouse.OverrideCursor = Cursors.Wait;

			this.Service.PubSubClient.Retract(this.node, this.itemId, (sender, e) =>
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
					MainWindow.ErrorBox("Unable to delete item: " + e.ErrorText);

				return Task.CompletedTask;

			}, null);
		}

	}
}
