﻿using System;
using System.IO;
using Waher.Things.Ieee1451.Ieee1451_0.TEDS;
using Waher.Things.Ieee1451.Ieee1451_0.TEDS.FieldTypes;
using Waher.Things.Metering;

namespace Waher.Things.Ieee1451.Ieee1451_0.Messages
{
	/// <summary>
	/// IEEE 1451.0 TEDS Access Message
	/// </summary>
	public class TedsAccessMessage : Message
	{
		private Teds data;
		private ushort errorCode;

		/// <summary>
		/// IEEE 1451.0 TEDS Access Message
		/// </summary>
		/// <param name="NetworkServiceType">Network Service Type</param>
		/// <param name="TedsAccessService">Network Service ID</param>
		/// <param name="MessageType">Message Type</param>
		/// <param name="Body">Binary Body</param>
		/// <param name="Tail">Bytes that are received after the body.</param>
		public TedsAccessMessage(NetworkServiceType NetworkServiceType, TedsAccessService TedsAccessService,
			MessageType MessageType, byte[] Body, byte[] Tail)
			: base(NetworkServiceType, (byte)TedsAccessService, MessageType, Body, Tail)
		{
			this.TedsAccessService = TedsAccessService;
		}

		/// <summary>
		/// TEDS Access Service
		/// </summary>
		public TedsAccessService TedsAccessService { get; }

		/// <summary>
		/// Name of <see cref="Message.NetworkServiceId"/>
		/// </summary>
		public override string NetworkServiceIdName => this.TedsAccessService.ToString();

		/// <summary>
		/// Tries to parse a TEDS from the message.
		/// </summary>
		/// <param name="ErrorCode">Error code, if available.</param>
		/// <param name="Teds">TEDS object, if successful.</param>
		/// <returns>If able to parse a TEDS object.</returns>
		public bool TryParseTeds(out ushort ErrorCode, out Teds Teds)
		{
			return this.TryParseTeds(true, out ErrorCode, out Teds);
		}

		/// <summary>
		/// Tries to parse a TEDS from the message.
		/// </summary>
		/// <param name="CheckChecksum">If checksum should be checked.</param>
		/// <param name="ErrorCode">Error code, if available.</param>
		/// <param name="Teds">TEDS object, if successful.</param>
		/// <returns>If able to parse a TEDS object.</returns>
		public bool TryParseTeds(bool CheckChecksum, out ushort ErrorCode, out Teds Teds)
		{
			if (!(this.data is null))
			{
				ErrorCode = this.errorCode;
				Teds = this.data;
				return true;
			}

			Teds = null;

			try
			{
				if (this.MessageType == MessageType.Reply)
					ErrorCode = this.NextUInt16();
				else
					ErrorCode = 0;

				ChannelAddress ChannelInfo = this.NextChannelId(true);
				uint TedsOffset = this.NextUInt32();
				int Start = this.Position;
				uint Len = this.NextUInt32();
				if (Len < 2)
					return this.MessageType == MessageType.Command;

				Len -= 2;
				if (Len > int.MaxValue)
					return false;

				byte[] Data = this.NextUInt8Array((int)Len);
				ushort CheckSum = 0;

				while (Start < this.Position)
					CheckSum += this.Body[Start++];

				CheckSum ^= 0xffff;

				ushort CheckSum2 = this.NextUInt16();
				if (CheckChecksum && CheckSum != CheckSum2)
					return false;

				Binary TedsBlock = new Binary(Data);
				ParsingState State = new ParsingState();

				this.data = Teds = new Teds(ChannelInfo, TedsBlock.ParseTedsRecords(State));
				this.errorCode = ErrorCode;
				return true;
			}
			catch (Exception)
			{
				ErrorCode = 0xffff;
				return false;
			}
		}

		/// <summary>
		/// Tries to parse a TEDS request from the message.
		/// </summary>
		/// <param name="Channel">Channel information.</param>
		/// <param name="TedsAccessCode">What TED is requested.</param>
		/// <param name="TedsOffset">TEDS offset.</param>
		/// <param name="TimeoutSeconds">Timeout, in seconds.</param>
		/// <returns>If able to parse a TEDS object.</returns>
		public bool TryParseRequest(out ChannelAddress Channel, out TedsAccessCode TedsAccessCode,
			out uint TedsOffset, out double TimeoutSeconds)
		{
			try
			{
				Channel = this.NextChannelId(true);
				TedsAccessCode = (TedsAccessCode)this.NextUInt8();
				TedsOffset = this.NextUInt32();
				TimeoutSeconds = this.NextTimeDurationSeconds();

				return true;
			}
			catch (Exception)
			{
				Channel = null;
				TedsAccessCode = 0;
				TedsOffset = 0;
				TimeoutSeconds = 0;

				return false;
			}
		}

		/// <summary>
		/// Serializes a request for TEDS.
		/// </summary>
		/// <param name="NcapId">NCAP ID</param>
		/// <param name="TimId">TIM ID, or null if none.</param>
		/// <param name="ChannelId">Channel ID, or 0 if none.</param>
		/// <param name="TedsAccessCode">TEDS access code.</param>
		/// <param name="TedsOffset">TEDS offset.</param>
		/// <param name="TimeoutSeconds">Timeout, in seconds.</param>
		/// <returns>Binary serialization.</returns>
		public static byte[] SerializeRequest(byte[] NcapId, byte[] TimId, ushort ChannelId,
			TedsAccessCode TedsAccessCode, uint TedsOffset, double TimeoutSeconds)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				ms.Write(MeteringTopology.Root.ObjectId.ToByteArray(), 0, 16); // App ID
				ms.Write(NcapId ?? EmptyUuid, 0, 16);
				ms.Write(TimId ?? EmptyUuid, 0, 16);
				ms.WriteByte((byte)(ChannelId >> 8));
				ms.WriteByte((byte)ChannelId);
				ms.WriteByte((byte)TedsAccessCode);

				byte[] Bin = BitConverter.GetBytes(TedsOffset);
				Array.Reverse(Bin);
				ms.Write(Bin, 0, 4);

				TimeoutSeconds *= 1e9 * 65536;
				ulong l = (ulong)TimeoutSeconds;
				Bin = BitConverter.GetBytes(l);
				Array.Reverse(Bin);
				ms.Write(Bin, 0, 8);

				return Ieee1451Parser.SerializeMessage(TedsAccessService.Read,
					MessageType.Command, ms.ToArray());
			}
		}

		/// <summary>
		/// Serializes a response to a TEDS request.
		/// </summary>
		/// <param name="ErrorCode">Error code.</param>
		/// <param name="NcapId">NCAP ID</param>
		/// <param name="TimId">TIM ID</param>
		/// <param name="ChannelId">Channel ID</param>
		/// <param name="TedsHeader">TEDS header record.</param>
		/// <param name="Records">Records</param>
		/// <returns>Binary serialization.</returns>
		public static byte[] SerializeResponse(ushort ErrorCode, byte[] NcapId, byte[] TimId,
			ushort ChannelId, TedsId TedsHeader, params TedsRecord[] Records)
		{
			if (NcapId is null)
				NcapId = new byte[16];
			else if (NcapId.Length != 16)
				throw new ArgumentException("Invalid NCAP UUID.", nameof(NcapId));

			if (TimId is null)
				TimId = new byte[16];
			else if (TimId.Length != 16)
				throw new ArgumentException("Invalid TIM UUID.", nameof(TimId));

			using (MemoryStream ms = new MemoryStream())
			{
				ms.WriteByte((byte)(ErrorCode >> 8));
				ms.WriteByte((byte)ErrorCode);
				ms.Write(MeteringTopology.Root.ObjectId.ToByteArray(), 0, 16); // App ID
				ms.Write(NcapId, 0, 16); // NCAP ID
				ms.Write(TimId, 0, 16); // TIM ID
				ms.WriteByte((byte)(ChannelId >> 8));
				ms.WriteByte((byte)ChannelId);
				ms.WriteByte(0);    // TedsOffset MSB
				ms.WriteByte(0);
				ms.WriteByte(0);
				ms.WriteByte(0);    // TedsOffset LSB

				using (MemoryStream ms2 = new MemoryStream())
				{
					Append(ms2, TedsHeader);

					foreach (TedsRecord Record in Records)
						Append(ms2, Record);

					byte[] Bin = ms2.ToArray();
					int Len = Bin.Length;
					ushort Checksum = 0;
					int i, j, k;
					byte b;

					j = Len + 2;
					k = 0;
					for (i = 0; i < 4; i++)
					{
						k <<= 8;
						b = (byte)j;
						k |= b;
						Checksum += b;
						j >>= 8;
					}

					for (i = 0; i < 4; i++)
					{
						ms.WriteByte((byte)k);
						k >>= 8;
					}

					if (Len > 0)
					{
						ms.Write(Bin, 0, Len);

						for (i = 0; i < Len; i++)
							Checksum += Bin[i];
					}

					Checksum ^= 0xffff;

					ms.WriteByte((byte)(Checksum >> 8));
					ms.WriteByte((byte)Checksum);
				}

				return Ieee1451Parser.SerializeMessage(TedsAccessService.Read, MessageType.Reply, ms.ToArray());
			}
		}

		private static void Append(MemoryStream ms2, TedsRecord Record)
		{
			byte[] Bin = Record.RawValue;
			int RecordLen = Bin?.Length ?? 0;
			if (RecordLen > 255)
				throw new Exception("Record length exceeds 255 bytes in length.");

			ms2.WriteByte(Record.Type);
			ms2.WriteByte((byte)RecordLen);

			if (RecordLen > 0)
				ms2.Write(Bin, 0, RecordLen);
		}
	}
}
