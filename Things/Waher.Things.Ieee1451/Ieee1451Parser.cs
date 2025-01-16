﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking;
using Waher.Security;
using Waher.Things.Ieee1451.Ieee1451_0;
using Waher.Things.Ieee1451.Ieee1451_0.Messages;

namespace Waher.Things.Ieee1451
{
    /// <summary>
    /// Static class for IEEE 1451-related parsing tasks.
    /// </summary>
    public static class Ieee1451Parser
	{
		/// <summary>
		/// Tries to parse an IEEE 1451.0-encoded data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <returns>Message object, if able to parse the message, null otherwise.</returns>
		public static Message TryParseMessage(byte[] Data)
		{
			return TryParseMessage(Data, null);
		}

		/// <summary>
		/// Tries to parse an IEEE 1451.0-encoded data.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <param name="ComLayer">Sniffable interface on which the message was received.</param>
		/// <returns>Message object, if able to parse the message, null otherwise.</returns>
		public static Message TryParseMessage(byte[] Data, ICommunicationLayer ComLayer)
		{
			NetworkServiceType NetworkServiceType = 0;
			MessageType MessageType = 0;
			byte NetworkServiceId = 0;
			int Length = 0;
			int TailLength = 0;
			byte[] Body = null;
			byte[] Tail = null;
			int i = 0;
			int c = Data.Length;
			int Pos = 0;
			int State = 0;
			byte b;

			while (i < c)
			{
				b = Data[i++];

				switch (State)
				{
					case 0: // Network Service Type
						if (b < 1 || b > 4)
						{
							if (ComLayer?.HasSniffers ?? false)
								ComLayer.Error("Invalid Network Service Type: " + b.ToString());

							return null;
						}

						NetworkServiceType = (NetworkServiceType)b;
						State += b;
						break;

					case 1: // Discovery services
						if (b < 1 || b > 10)
						{
							if (ComLayer?.HasSniffers ?? false)
								ComLayer.Error("Invalid Discovery Service: " + b.ToString());

							return null;
						}

						NetworkServiceId = b;
						State = 5;
						break;

					case 2: // Transducer Access Service
						if (b < 1 || b > 20)
						{
							if (ComLayer?.HasSniffers ?? false)
								ComLayer.Error("Invalid Transducer Access Service: " + b.ToString());

							return null;
						}

						NetworkServiceId = b;
						State = 5;
						break;

					case 3: // TEDS Access Service
						if (b < 1 || b > 4)
						{
							if (ComLayer?.HasSniffers ?? false)
								ComLayer.Error("Invalid TEDS Access Service: " + b.ToString());

							return null;
						}

						NetworkServiceId = b;
						State = 5;
						break;

					case 4: // Event Notification Service
						if (b < 1 || b > 12)
						{
							if (ComLayer?.HasSniffers ?? false)
								ComLayer.Error("Invalid Event Notification Service: " + b.ToString());

							return null;
						}

						NetworkServiceId = b;
						State = 5;
						break;

					case 5: // Message Type
						MessageType = (MessageType)b;
						State++;
						break;

					case 6: // Length MSB
						Length = b;
						State++;
						break;

					case 7: // Length LSB
						Length <<= 8;
						Length |= b;

						TailLength = c - i - Length;
						if (TailLength < 0)
						{
							if (ComLayer?.HasSniffers ?? false)
								ComLayer.Error("Missing data bytes.");

							return null;
						}

						Body = new byte[Length];
						Tail = TailLength == 0 ? Array.Empty<byte>() : new byte[TailLength];

						if (Length > 0)
							State++;
						else if (TailLength > 0)
							State += 2;
						else
							State += 3;
						break;

					case 8:
						Body[Pos++] = b;
						Length--;
						if (Length == 0)
						{
							if (TailLength > 0)
							{
								Pos = 0;
								State++;
							}
							else
								State += 2;
						}
						break;

					case 9:
						Tail[Pos++] = b;
						TailLength--;
						if (TailLength == 0)
							State++;
						break;

					case 10:
					default:
						if (ComLayer?.HasSniffers ?? false)
							ComLayer.Error("Invalid Parsing State: " + State.ToString());

						return null;
				}
			}

			if (State != 10)
			{
				if (ComLayer?.HasSniffers ?? false)
					ComLayer.Error("Unexpected end of message.");

				return null;
			}

			switch (NetworkServiceType)
			{
				case NetworkServiceType.DiscoveryServices:
					if (ComLayer?.HasSniffers ?? false)
					{
						ComLayer.Information(nameof(DiscoveryMessage) + "(" + NetworkServiceType.ToString() + "," +
							((DiscoveryService)NetworkServiceId).ToString() + "," + MessageType.ToString() + "," +
							Hashes.BinaryToString(Body, true) + ")");
					}

					return new DiscoveryMessage(NetworkServiceType, (DiscoveryService)NetworkServiceId, MessageType, Body, Tail, ComLayer);

				case NetworkServiceType.TransducerAccessServices:
					if (ComLayer?.HasSniffers ?? false)
					{
						ComLayer.Information(nameof(TransducerAccessMessage) + "(" + NetworkServiceType.ToString() + "," +
							((TransducerAccessService)NetworkServiceId).ToString() + "," + MessageType.ToString() + "," +
							Hashes.BinaryToString(Body, true) + ")");
					}

					return new TransducerAccessMessage(NetworkServiceType, (TransducerAccessService)NetworkServiceId, MessageType, Body, Tail, ComLayer);

				case NetworkServiceType.TedsAccessServices:
					if (ComLayer?.HasSniffers ?? false)
					{
						ComLayer.Information(nameof(TedsAccessMessage) + "(" + NetworkServiceType.ToString() + "," +
							((TedsAccessService)NetworkServiceId).ToString() + "," + MessageType.ToString() + "," +
							Hashes.BinaryToString(Body, true) + ")");
					}

					return new TedsAccessMessage(NetworkServiceType, (TedsAccessService)NetworkServiceId, MessageType, Body, Tail, ComLayer);

				case NetworkServiceType.EventNotificationServices:
					if (ComLayer?.HasSniffers ?? false)
					{
						ComLayer.Information(nameof(EventNotificationMessage) + "(" + NetworkServiceType.ToString() + "," +
							((EventNotificationService)NetworkServiceId).ToString() + "," + MessageType.ToString() + "," +
							Hashes.BinaryToString(Body, true) + ")");
					}

					return new EventNotificationMessage(NetworkServiceType, (EventNotificationService)NetworkServiceId, MessageType, Body, Tail, ComLayer);

				default:
					if (ComLayer?.HasSniffers ?? false)
						ComLayer.Error("Unsupported Network Service Type: " + NetworkServiceType.ToString());

					return null;
			}
		}

		/// <summary>
		/// Creates a binary IEEE 1451.0 message.
		/// </summary>
		/// <param name="NetworkServiceType">Network service type.</param>
		/// <param name="NetworkServiceId">Network service ID</param>
		/// <param name="MessageType">Message Type</param>
		/// <param name="Payload">Binary payload.</param>
		/// <returns>Binary message</returns>
		/// <exception cref="ArgumentException"></exception>
		public static byte[] SerializeMessage(NetworkServiceType NetworkServiceType,
			byte NetworkServiceId, MessageType MessageType, byte[] Payload)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				ms.WriteByte((byte)NetworkServiceType);
				ms.WriteByte(NetworkServiceId);
				ms.WriteByte((byte)MessageType);

				int Length = Payload?.Length ?? 0;
				if (Length > ushort.MaxValue)
					throw new ArgumentException("Payload too large.", nameof(Payload));

				ms.WriteByte((byte)(Length >> 8));
				ms.WriteByte((byte)Length);

				if (Length > 0)
					ms.Write(Payload, 0, Length);

				return ms.ToArray();
			}
		}

		/// <summary>
		/// Creates a binary IEEE 1451.0 message.
		/// </summary>
		/// <param name="NetworkServiceId">Network service ID</param>
		/// <param name="MessageType">Message Type</param>
		/// <param name="Payload">Binary payload.</param>
		/// <param name="SnifferOutput">Optional sniffer output.</param>
		/// <returns>Binary message</returns>
		/// <exception cref="ArgumentException"></exception>
		public static byte[] SerializeMessage(DiscoveryService NetworkServiceId,
			MessageType MessageType, byte[] Payload, StringBuilder SnifferOutput)
		{
			if (!(SnifferOutput is null))
			{
				SnifferOutput.AppendLine("NetworkServiceType: DiscoveryServices");
				SnifferOutput.Append("NetworkServiceId: ");
				SnifferOutput.AppendLine(NetworkServiceId.ToString());
				SnifferOutput.Append("MessageType: ");
				SnifferOutput.AppendLine(MessageType.ToString());
			}

			return SerializeMessage(NetworkServiceType.DiscoveryServices, (byte)NetworkServiceId, MessageType, Payload);
		}

		/// <summary>
		/// Creates a binary IEEE 1451.0 message.
		/// </summary>
		/// <param name="NetworkServiceId">Network service ID</param>
		/// <param name="MessageType">Message Type</param>
		/// <param name="Payload">Binary payload.</param>
		/// <param name="SnifferOutput">Optional sniffer output.</param>
		/// <returns>Binary message</returns>
		/// <exception cref="ArgumentException"></exception>
		public static byte[] SerializeMessage(TransducerAccessService NetworkServiceId,
			MessageType MessageType, byte[] Payload, StringBuilder SnifferOutput)
		{
			if (!(SnifferOutput is null))
			{
				SnifferOutput.AppendLine("NetworkServiceType: TransducerAccessServices");
				SnifferOutput.Append("NetworkServiceId: ");
				SnifferOutput.AppendLine(NetworkServiceId.ToString());
				SnifferOutput.Append("MessageType: ");
				SnifferOutput.AppendLine(MessageType.ToString());
			}

			return SerializeMessage(NetworkServiceType.TransducerAccessServices,
				(byte)NetworkServiceId, MessageType, Payload);
		}

		/// <summary>
		/// Creates a binary IEEE 1451.0 message.
		/// </summary>
		/// <param name="NetworkServiceId">Network service ID</param>
		/// <param name="MessageType">Message Type</param>
		/// <param name="Payload">Binary payload.</param>
		/// <param name="SnifferOutput">Optional sniffer output.</param>
		/// <returns>Binary message</returns>
		/// <exception cref="ArgumentException"></exception>
		public static byte[] SerializeMessage(TedsAccessService NetworkServiceId,
			MessageType MessageType, byte[] Payload, StringBuilder SnifferOutput)
		{
			if (!(SnifferOutput is null))
			{
				SnifferOutput.AppendLine("NetworkServiceType: TedsAccessServices");
				SnifferOutput.Append("NetworkServiceId: ");
				SnifferOutput.AppendLine(NetworkServiceId.ToString());
				SnifferOutput.Append("MessageType: ");
				SnifferOutput.AppendLine(MessageType.ToString());
			}

			return SerializeMessage(NetworkServiceType.TedsAccessServices,
				(byte)NetworkServiceId, MessageType, Payload);
		}

		/// <summary>
		/// Creates a binary IEEE 1451.0 message.
		/// </summary>
		/// <param name="NetworkServiceId">Network service ID</param>
		/// <param name="MessageType">Message Type</param>
		/// <param name="Payload">Binary payload.</param>
		/// <param name="SnifferOutput">Optional sniffer output.</param>
		/// <returns>Binary message</returns>
		/// <exception cref="ArgumentException"></exception>
		public static byte[] SerializeMessage(EventNotificationService NetworkServiceId,
			MessageType MessageType, byte[] Payload, StringBuilder SnifferOutput)
		{
			if (!(SnifferOutput is null))
			{
				SnifferOutput.AppendLine("NetworkServiceType: EventNotificationServices");
				SnifferOutput.Append("NetworkServiceId: ");
				SnifferOutput.AppendLine(NetworkServiceId.ToString());
				SnifferOutput.Append("MessageType: ");
				SnifferOutput.AppendLine(MessageType.ToString());
			}

			return SerializeMessage(NetworkServiceType.EventNotificationServices,
				(byte)NetworkServiceId, MessageType, Payload);
		}

	}
}
