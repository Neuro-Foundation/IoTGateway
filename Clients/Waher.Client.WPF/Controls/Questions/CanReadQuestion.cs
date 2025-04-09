﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Events;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Things;
using Waher.Things.SensorData;

namespace Waher.Client.WPF.Controls.Questions
{
	public enum OperationRange
	{
		Caller,
		Domain,
		ServiceToken,
		DeviceToken,
		UserToken,
		All
	}

	public class CanReadQuestion : NodeQuestion
	{
		private SortedDictionary<string, bool> fieldsSorted = null;
		private ListBox typesListBox = null;
		private ListBox fieldsListBox = null;
		private ProvisioningClient client;
		private QuestionView questionView;
		private OperationRange range;
		private string parameter = null;
		private string[] fieldNames = null;
		private string[] availableFieldNames = null;
		private FieldType categories = FieldType.All;
		private bool registered = false;

		public CanReadQuestion()
			: base()
		{
		}

		[DefaultValueNull]
		public string[] FieldNames
		{
			get => this.fieldNames;
			set => this.fieldNames = value;
		}

		[DefaultValueNull]
		public string[] AvailableFieldNames
		{
			get => this.availableFieldNames;
			set => this.availableFieldNames = value;
		}

		[DefaultValue(FieldType.All)]
		public FieldType Categories
		{
			get => this.categories;
			set => this.categories = value;
		}

		public override string QuestionString => "Allowed to read?";

		public override void PopulateDetailsDialog(QuestionView QuestionView, ProvisioningClient ProvisioningClient)
		{
			StackPanel Details = QuestionView.Details;
			TextBlock TextBlock;
			ListBox ListBox;
			Button Button;

			this.client = ProvisioningClient;
			this.questionView = QuestionView;

			Details.Children.Add(new TextBlock()
			{
				FontSize = 18,
				FontWeight = FontWeights.Bold,
				Text = "Allowed to read?"
			});

			Details.Children.Add(TextBlock = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 6, 0, 6)
			});

			TextBlock.Inlines.Add("Device: ");
			this.AddJidName(this.JID, ProvisioningClient, TextBlock);

			this.AddNodeInfo(Details);

			Details.Children.Add(TextBlock = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 6, 0, 6)
			});

			TextBlock.Inlines.Add("Caller: ");
			this.AddJidName(this.RemoteJID, ProvisioningClient, TextBlock);

			Details.Children.Add(new Label()
			{
				Content = "Categories:",
				Margin = new Thickness(0, 6, 0, 0)
			});

			Details.Children.Add(ListBox = new ListBox()
			{
				SelectionMode = SelectionMode.Multiple,
				Margin = new Thickness(0, 0, 0, 6)
			});

			this.typesListBox = ListBox;

			ListBox.Items.Add(new ListBoxItem()
			{
				Content = "Momentary values",
				IsSelected = (this.categories & FieldType.Momentary) != 0,
				Tag = FieldType.Momentary
			});

			ListBox.Items.Add(new ListBoxItem()
			{
				Content = "Identity values",
				IsSelected = (this.categories & FieldType.Identity) != 0,
				Tag = FieldType.Identity
			});

			ListBox.Items.Add(new ListBoxItem()
			{
				Content = "Status values",
				IsSelected = (this.categories & FieldType.Status) != 0,
				Tag = FieldType.Status
			});

			ListBox.Items.Add(new ListBoxItem()
			{
				Content = "Computed values",
				IsSelected = (this.categories & FieldType.Computed) != 0,
				Tag = FieldType.Computed
			});

			ListBox.Items.Add(new ListBoxItem()
			{
				Content = "Peak values",
				IsSelected = (this.categories & FieldType.Peak) != 0,
				Tag = FieldType.Peak
			});

			ListBox.Items.Add(new ListBoxItem()
			{
				Content = "Historical values",
				IsSelected = (this.categories & FieldType.Historical) != 0,
				Tag = FieldType.Historical
			});

			AddAllClearButtons(Details, ListBox);


			Details.Children.Add(new Label()
			{
				Content = "Field restriction:",
				Margin = new Thickness(0, 6, 0, 0)
			});

			Details.Children.Add(ListBox = new ListBox()
			{
				MaxHeight = 150,
				SelectionMode = SelectionMode.Multiple,
				Margin = new Thickness(0, 0, 0, 6)
			});

			this.fieldsListBox = ListBox;

			if (this.availableFieldNames is null)
			{
				if (!(this.fieldNames is null))
				{
					foreach (string FieldName in this.fieldNames)
					{
						ListBox.Items.Add(new ListBoxItem()
						{
							Content = FieldName,
							IsSelected = true,
							Tag = FieldName
						});
					}
				}
			}
			else
			{
				foreach (string FieldName in this.availableFieldNames)
				{
					ListBox.Items.Add(new ListBoxItem()
					{
						Content = FieldName,
						IsSelected = (this.fieldNames is null || Array.IndexOf<string>(this.fieldNames, FieldName) >= 0),
						Tag = FieldName
					});
				}
			}

			StackPanel StackPanel = AddAllClearButtons(Details, ListBox);

			if (this.availableFieldNames is null)
			{
				StackPanel.Children.Add(Button = new Button()
				{
					Margin = new Thickness(6, 6, 6, 6),
					Padding = new Thickness(20, 0, 20, 0),
					Content = "Get List",
					Tag = ListBox
				});

				Button.Click += this.GetListButton_Click;
			}

			Details.Children.Add(TextBlock = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 6, 0, 6),
				Text = "Is the caller allowed to read your device?"
			});

			Details.Children.Add(Button = new Button()
			{
				Margin = new Thickness(0, 6, 0, 6),
				Content = "Yes"
			});

			Button.Click += this.YesButton_Click;

			Details.Children.Add(Button = new Button()
			{
				Margin = new Thickness(0, 6, 0, 6),
				Content = "No"
			});

			Button.Click += this.NoButton_Click;

			string s = this.RemoteJID;
			int i = s.IndexOf('@');
			if (i >= 0)
			{
				s = s.Substring(i + 1);

				Details.Children.Add(Button = new Button()
				{
					Margin = new Thickness(0, 6, 0, 6),
					Content = "Yes, to anyone from " + s
				});

				Button.Click += this.YesDomainButton_Click;

				Details.Children.Add(Button = new Button()
				{
					Margin = new Thickness(0, 6, 0, 6),
					Content = "No, to no one from " + s
				});

				Button.Click += this.NoDomainButton_Click;
			}

			Details.Children.Add(Button = new Button()
			{
				Margin = new Thickness(0, 6, 0, 6),
				Content = "Yes, to anyone"
			});

			Button.Click += this.YesAllButton_Click;

			Details.Children.Add(Button = new Button()
			{
				Margin = new Thickness(0, 6, 0, 6),
				Content = "No, to no one"
			});

			Button.Click += this.NoAllButton_Click;

			this.AddTokens(Details, this.client, this.YesTokenButton_Click, this.NoTokenButton_Click);
		}

		internal static StackPanel AddAllClearButtons(StackPanel Details, ListBox ListBox)
		{
			StackPanel StackPanel;
			Button Button;

			Details.Children.Add(StackPanel = new StackPanel()
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Center
			});

			StackPanel.Children.Add(Button = new Button()
			{
				Margin = new Thickness(6, 6, 6, 6),
				Padding = new Thickness(20, 0, 20, 0),
				Content = "All",
				Tag = ListBox
			});

			Button.Click += AllButton_Click;

			StackPanel.Children.Add(Button = new Button()
			{
				Margin = new Thickness(6, 6, 6, 6),
				Padding = new Thickness(20, 0, 20, 0),
				Content = "Clear",
				Tag = ListBox
			});

			Button.Click += ClearButton_Click;

			return StackPanel;
		}

		internal static void ClearButton_Click(object Sender, RoutedEventArgs e)
		{
			if (Sender is Button Button && Button.Tag is ListBox ListBox)
			{
				foreach (ListBoxItem Item in ListBox.Items)
					Item.IsSelected = false;
			}
		}

		internal static void AllButton_Click(object Sender, RoutedEventArgs e)
		{
			if (Sender is Button Button && Button.Tag is ListBox ListBox)
			{
				foreach (ListBoxItem Item in ListBox.Items)
					Item.IsSelected = true;
			}
		}

		private async void GetListButton_Click(object Sender, RoutedEventArgs e)
		{
			try
			{
				XmppClient Client = this.client.Client;

				((Button)Sender).IsEnabled = false;

				RosterItem Item = Client[this.JID];
				if (Item is null || Item.State == SubscriptionState.None || Item.State == SubscriptionState.From)
				{
					if (!this.registered)
					{
						this.registered = true;
						this.questionView.Owner.RegisterRosterEventHandler(this.JID, this.RosterItemUpdated);
					}

					await Client.RequestPresenceSubscription(this.JID);
				}
				else if (!Item.HasLastPresence || !Item.LastPresence.IsOnline)
					await Client.RequestPresenceSubscription(this.JID);
				else
					await this.DoRequest(Item);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		public override void Dispose()
		{
			if (this.registered)
			{
				this.registered = false;
				this.questionView.Owner.UnregisterRosterEventHandler(this.JID, this.RosterItemUpdated);
			}

			base.Dispose();
		}

		private async Task RosterItemUpdated(object Sender, RosterItem Item)
		{
			if ((Item.State == SubscriptionState.Both || Item.State == SubscriptionState.To) && Item.HasLastPresence && Item.LastPresence.IsOnline)
			{
				this.questionView.Owner.UnregisterRosterEventHandler(this.JID, this.RosterItemUpdated);
				await this.DoRequest(Item);
			}
		}

		private async Task DoRequest(RosterItem Item)
		{
			SensorDataClientRequest Request;

			this.fieldsSorted = new SortedDictionary<string, bool>();

			if (this.IsNode)
				Request = await this.questionView.Owner.SensorClient.RequestReadout(Item.LastPresenceFullJid,
					new ThingReference[] { this.GetNodeReference() }, FieldType.All);
			else
				Request = await this.questionView.Owner.SensorClient.RequestReadout(Item.LastPresenceFullJid, FieldType.All);

			Request.OnStateChanged += this.Request_OnStateChanged;
			Request.OnFieldsReceived += this.Request_OnFieldsReceived;
		}

		private async Task Request_OnStateChanged(object Sender, SensorDataReadoutState NewState)
		{
			if (NewState == SensorDataReadoutState.Done)
			{
				string[] Fields;

				lock (this.fieldsSorted)
				{
					Fields = new string[this.fieldsSorted.Count];
					this.fieldsSorted.Keys.CopyTo(Fields, 0);
				}

				this.availableFieldNames = Fields;
				await Database.Update(this);

				MainWindow.UpdateGui(() =>
				{
					SortedDictionary<string, bool> Selected = null;
					bool AllSelected = this.fieldNames is null;

					if (!AllSelected)
					{
						Selected = new SortedDictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);

						foreach (ListBoxItem Item in this.fieldsListBox.Items)
						{
							if (Item.IsSelected)
								Selected[(string)Item.Tag] = true;
						}
					}

					this.fieldsListBox.Items.Clear();

					foreach (string FieldName in this.availableFieldNames)
					{
						this.fieldsListBox.Items.Add(new ListBoxItem()
						{
							Content = FieldName,
							IsSelected = AllSelected || Selected.ContainsKey(FieldName),
							Tag = FieldName
						});
					}

					return Task.CompletedTask;
				});
			}
		}

		private Task Request_OnFieldsReceived(object _, IEnumerable<Field> NewFields)
		{
			lock (this.fieldsSorted)
			{
				foreach (Field F in NewFields)
					this.fieldsSorted[F.Name] = true;
			}

			return Task.CompletedTask;
		}

		private FieldType GetFieldType()
		{
			FieldType Result = (FieldType)0;

			foreach (ListBoxItem Item in this.typesListBox.Items)
			{
				if (Item.IsSelected)
					Result |= (FieldType)Item.Tag;
			}

			return Result;
		}

		private string[] GetFields()
		{
			List<string> Result = new List<string>();
			bool All = true;

			foreach (ListBoxItem Item in this.fieldsListBox.Items)
			{
				if (Item.IsSelected)
					Result.Add((string)Item.Tag);
				else
					All = false;
			}

			if (this.availableFieldNames is null || !All)
				return Result.ToArray();
			else
				return null;
		}

		private async Task RuleCallback(object Sender, IqResultEventArgs e)
		{
			try
			{
				if (e.Ok)
					await this.Processed(this.questionView);
				else
					MainWindow.ErrorBox(string.IsNullOrEmpty(e.ErrorText) ? "Unable to set rule." : e.ErrorText);
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MainWindow.ErrorBox(ex.Message);
			}
		}

		private void NoAllButton_Click(object Sender, RoutedEventArgs e)
		{
			this.range = OperationRange.All;
			this.client.CanReadResponseAll(this.Sender, this.JID, this.RemoteJID, this.Key, false, this.GetFieldType(), this.GetFields(), this.GetNodeReference(), this.RuleCallback, null);
		}

		private void YesAllButton_Click(object Sender, RoutedEventArgs e)
		{
			this.range = OperationRange.All;
			this.client.CanReadResponseAll(this.Sender, this.JID, this.RemoteJID, this.Key, true, this.GetFieldType(), this.GetFields(), this.GetNodeReference(), this.RuleCallback, null);
		}

		private void NoDomainButton_Click(object Sender, RoutedEventArgs e)
		{
			this.range = OperationRange.Domain;
			this.client.CanReadResponseDomain(this.Sender, this.JID, this.RemoteJID, this.Key, false, this.GetFieldType(), this.GetFields(), this.GetNodeReference(), this.RuleCallback, null);
		}

		private void YesDomainButton_Click(object Sender, RoutedEventArgs e)
		{
			this.range = OperationRange.Domain;
			this.client.CanReadResponseDomain(this.Sender, this.JID, this.RemoteJID, this.Key, true, this.GetFieldType(), this.GetFields(), this.GetNodeReference(), this.RuleCallback, null);
		}

		private void NoButton_Click(object Sender, RoutedEventArgs e)
		{
			this.range = OperationRange.Caller;
			this.client.CanReadResponseCaller(this.Sender, this.JID, this.RemoteJID, this.Key, false, this.GetFieldType(), this.GetFields(), this.GetNodeReference(), this.RuleCallback, null);
		}

		private void YesButton_Click(object Sender, RoutedEventArgs e)
		{
			this.range = OperationRange.Caller;
			this.client.CanReadResponseCaller(this.Sender, this.JID, this.RemoteJID, this.Key, true, this.GetFieldType(), this.GetFields(), this.GetNodeReference(), this.RuleCallback, null);
		}

		private void NoTokenButton_Click(object Sender, RoutedEventArgs e)
		{
			this.TokenButtonClick(Sender, e, false);
		}

		private void TokenButtonClick(object Sender, RoutedEventArgs _, bool Result)
		{
			Button Button = (Button)Sender;
			object[] P = (object[])Button.Tag;
			this.parameter = (string)P[0];
			this.range = (OperationRange)P[1];

			switch (this.range)
			{
				case OperationRange.ServiceToken:
					this.client.CanReadResponseService(this.Sender, this.JID, this.RemoteJID, this.Key, Result, this.GetFieldType(), this.GetFields(), this.parameter, this.GetNodeReference(), this.RuleCallback, null);
					break;

				case OperationRange.DeviceToken:
					this.client.CanReadResponseDevice(this.Sender, this.JID, this.RemoteJID, this.Key, Result, this.GetFieldType(), this.GetFields(), this.parameter, this.GetNodeReference(), this.RuleCallback, null);
					break;

				case OperationRange.UserToken:
					this.client.CanReadResponseUser(this.Sender, this.JID, this.RemoteJID, this.Key, Result, this.GetFieldType(), this.GetFields(), this.parameter, this.GetNodeReference(), this.RuleCallback, null);
					break;
			}
		}

		private void YesTokenButton_Click(object Sender, RoutedEventArgs e)
		{
			this.TokenButtonClick(Sender, e, true);
		}

		public override bool IsResolvedBy(Question Question)
		{
			if (Question is CanReadQuestion CanReadQuestion)
			{
				if (this.JID != CanReadQuestion.JID)
					return false;

				switch (this.range)
				{
					case OperationRange.Caller:
						return (this.RemoteJID == CanReadQuestion.RemoteJID);

					case OperationRange.Domain:
						return (IsFriendQuestion.GetDomain(this.RemoteJID) == IsFriendQuestion.GetDomain(CanReadQuestion.RemoteJID));

					case OperationRange.All:
						return true;

					case OperationRange.ServiceToken:
						return MatchesToken(this.parameter, CanReadQuestion.ServiceTokens);

					case OperationRange.DeviceToken:
						return MatchesToken(this.parameter, CanReadQuestion.DeviceTokens);

					case OperationRange.UserToken:
						return MatchesToken(this.parameter, CanReadQuestion.UserTokens);

					default:
						return false;
				}
			}
			else
				return false;
		}

		internal static bool MatchesToken(string Token, string[] Tokens)
		{
			if (Tokens is not null)
			{
				foreach (string Token2 in Tokens)
				{
					if (Token == Token2)
						return true;
				}
			}

			return false;
		}

	}
}
