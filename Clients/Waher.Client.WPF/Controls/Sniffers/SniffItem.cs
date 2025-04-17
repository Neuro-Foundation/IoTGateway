﻿using System;
using System.Windows.Media;
using Waher.Client.WPF.Model;

namespace Waher.Client.WPF.Controls.Sniffers
{
	public enum SniffItemType
	{
		DataReceived,
		DataTransmitted,
		TextReceived,
		TextTransmitted,
		Information,
		Warning,
		Error,
		Exception
	}

	/// <summary>
	/// Represents one item in a sniffer output.
	/// </summary>
	public class SniffItem : ColorableItem
	{
		private readonly SniffItemType type;
		private readonly DateTime timestamp;
		private readonly string message;
		private readonly byte[]? data;

		/// <summary>
		/// Represents one item in a sniffer output.
		/// </summary>
		/// <param name="Timestamp">Timestamp of event.</param>
		/// <param name="Type">Type of sniff record.</param>
		/// <param name="Message">Message</param>
		/// <param name="Data">Optional binary data.</param>
		/// <param name="ForegroundColor">Foreground Color</param>
		/// <param name="BackgroundColor">Background Color</param>
		public SniffItem(DateTime Timestamp, SniffItemType Type, string Message, byte[]? Data, Color ForegroundColor, Color BackgroundColor)
			: base(ForegroundColor, BackgroundColor)
		{
			this.type = Type;
			this.timestamp = Timestamp;
			this.message = Message;
			this.data = Data;
		}

		/// <summary>
		/// Timestamp of event.
		/// </summary>
		public DateTime Timestamp => this.timestamp;

		/// <summary>
		/// Sniff item type.
		/// </summary>
		public SniffItemType Type => this.type;

		/// <summary>
		/// Time of day of event, as a string.
		/// </summary>
		public string Time { get { return this.timestamp.ToLongTimeString(); } }

		/// <summary>
		/// Message
		/// </summary>
		public string Message => this.message;

		/// <summary>
		/// Optional binary data.
		/// </summary>
		public byte[]? Data => this.data;

	}
}
