﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Waher.Networking.Sniffers;

namespace Waher.Client.WPF.Controls.Sniffers
{
	public class TabSniffer : SnifferBase
	{
		private readonly SnifferView view;
		private string snifferId = null;

		public TabSniffer(SnifferView View)
		{
			this.view = View;
		}

		public SnifferView View => this.view;

		public string SnifferId
		{
			get => this.snifferId;
			set => this.snifferId = value;
		}

		public override Task ReceiveBinary(DateTime Timestamp, byte[] Data)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.DataReceived, HexToString(Data), Data, Colors.White, Colors.Navy));
			return Task.CompletedTask;
		}

		public override Task TransmitBinary(DateTime Timestamp, byte[] Data)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.DataTransmitted, HexToString(Data), Data, Colors.Black, Colors.White));
			return Task.CompletedTask;
		}

		internal static string HexToString(byte[] Data)
		{
			StringBuilder Output = new StringBuilder();
			int i = 0;

			foreach (byte b in Data)
			{
				if (i > 0)
					Output.Append(' ');

				Output.Append(b.ToString("X2"));

				i = (i + 1) & 31;
				if (i == 0)
					Output.AppendLine();
			}

			return Output.ToString().TrimEnd();
		}

		public override Task ReceiveText(DateTime Timestamp, string Text)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.TextReceived, Text, null, Colors.White, Colors.Navy));
			return Task.CompletedTask;
		}

		public override Task TransmitText(DateTime Timestamp, string Text)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.TextTransmitted, Text, null, Colors.Black, Colors.White));
			return Task.CompletedTask;
		}

		public override Task Information(DateTime Timestamp, string Comment)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.Information, Comment, null, Colors.Yellow, Colors.DarkGreen));
			return Task.CompletedTask;
		}

		public override Task Warning(DateTime Timestamp, string Warning)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.Warning, Warning, null, Colors.Black, Colors.Yellow));
			return Task.CompletedTask;
		}

		public override Task Error(DateTime Timestamp, string Error)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.Error, Error, null, Colors.White, Colors.Red));
			return Task.CompletedTask;
		}

		public override Task Exception(DateTime Timestamp, string Exception)
		{
			this.view.Add(new SniffItem(Timestamp, SniffItemType.Exception, Exception, null, Colors.White, Colors.DarkRed));
			return Task.CompletedTask;
		}
	}
}
