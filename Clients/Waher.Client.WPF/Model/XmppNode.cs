﻿using System;
using System.Windows.Controls;
using Waher.Networking.XMPP;
using System.Windows;
using Waher.Client.WPF.Dialogs.Xmpp;
using System.Threading.Tasks;

namespace Waher.Client.WPF.Model
{
	public abstract class XmppNode : TreeNode
	{
		public XmppNode(TreeNode Parent)
			: base(Parent)
		{
		}

		public abstract string FullJID
		{
			get;
		}

		public XmppAccountNode Account
		{
			get { return this.Parent as XmppAccountNode; }
		}

		public override void AddContexMenuItems(ref string CurrentGroup, ContextMenu Menu)
		{
			base.AddContexMenuItems(ref CurrentGroup, Menu);

			MenuItem Item;

			GroupSeparator(ref CurrentGroup, "XMPP", Menu);

			Menu.Items.Add(Item = new MenuItem()
			{
				Header = "Send XMPP Message...",
				IsEnabled = true
			});

			Item.Click += this.SendXmppMessage_Click;

			Menu.Items.Add(Item = new MenuItem()
			{
				Header = "Send XMPP IQ GET...",
				IsEnabled = true
			});

			Item.Click += this.SendXmppIqGet_Click;

			Menu.Items.Add(Item = new MenuItem()
			{
				Header = "Send XMPP IQ SET...",
				IsEnabled = true
			});

			Item.Click += this.SendXmppIqSet_Click;
		}

		private async void SendXmppMessage_Click(object Sender, RoutedEventArgs e)
		{
			try
			{
				MessageForm Form = new MessageForm
				{
					Owner = MainWindow.currentInstance
				};

				Form.To.Text = this.FullJID;

				bool? Result = Form.ShowDialog();

				if (Result.HasValue && Result.Value)
				{
					await this.Account.Client.SendMessage((MessageType)Enum.Parse(typeof(MessageType), Form.Type.Text),
						Form.To.Text.Trim(), Form.CustomXml.Text, Form.Body.Text, Form.Subject.Text, Form.MessageLanguage.Text,
						Form.ThreadId.Text, Form.ParentThreadId.Text);
				}
			}
			catch (Exception ex)
			{
				MainWindow.ErrorBox(ex.Message);
			}
		}

		private async void SendXmppIqGet_Click(object Sender, RoutedEventArgs e)
		{
			try
			{
				IqForm Form = new IqForm
				{
					Owner = MainWindow.currentInstance
				};

				Form.Type.SelectedIndex = 0;
				Form.To.Text = this.FullJID;

				bool? Result = Form.ShowDialog();

				if (Result.HasValue && Result.Value)
				{
					await this.Account.Client.SendIqGet(Form.To.Text.Trim(), Form.CustomXml.Text, (sender2, e2) =>
					{
						if (e2.Ok)
						{
							MainWindow.UpdateGui(() =>
							{
								IqResultForm ResultForm = new IqResultForm()
								{
									Owner = MainWindow.currentInstance
								};

								ResultForm.From.Text = e2.From;
								ResultForm.XmlResponse.Text = e2.Response.OuterXml;

								ResultForm.ShowDialog();

								return Task.CompletedTask;
							});
						}
						else
							MainWindow.ErrorBox(string.IsNullOrEmpty(e2.ErrorText) ? "Error returned." : e2.ErrorText);

						return Task.CompletedTask;
					}, null);
				}
			}
			catch (Exception ex)
			{
				MainWindow.ErrorBox(ex.Message);
			}
		}

		private async void SendXmppIqSet_Click(object Sender, RoutedEventArgs e)
		{
			try
			{
				IqForm Form = new IqForm
				{
					Owner = MainWindow.currentInstance
				};

				Form.Type.SelectedIndex = 1;
				Form.To.Text = this.FullJID;

				bool? Result = Form.ShowDialog();

				if (Result.HasValue && Result.Value)
				{
					await this.Account.Client.SendIqSet(Form.To.Text.Trim(), Form.CustomXml.Text, (sender2, e2) =>
					{
						if (e2.Ok)
						{
							MainWindow.UpdateGui(() =>
							{
								IqResultForm ResultForm = new IqResultForm()
								{
									Owner = MainWindow.currentInstance
								};

								ResultForm.From.Text = e2.From;
								ResultForm.XmlResponse.Text = e2.Response.OuterXml;

								ResultForm.ShowDialog();

								return Task.CompletedTask;
							});
						}
						else
							MainWindow.ErrorBox(string.IsNullOrEmpty(e2.ErrorText) ? "Error returned." : e2.ErrorText);

						return Task.CompletedTask;
					}, null);
				}
			}
			catch (Exception ex)
			{
				MainWindow.ErrorBox(ex.Message);
			}
		}
	}
}
