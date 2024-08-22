﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Temporary;
using Waher.Runtime.Threading;

namespace Waher.Networking.XMPP.InBandBytestreams
{
	/// <summary>
	/// Class managing the transmission of an in-band bytestream.
	/// </summary>
	public class OutgoingStream : IDisposable
	{
		private readonly XmppClient client;
		private TemporaryStream tempStream;
		private MultiReadSingleWriteObject syncObject;
		private readonly IEndToEndEncryption e2e;
		private readonly string to;
		private readonly string streamId;
		private object state = null;
		private long pos = 0;
		private readonly int blockSize;
		private ushort seq;
		private int seqAcknowledged = -1;
		private bool isWriting;
		private bool done;
		private bool aborted = false;
		private bool opened = false;

		internal OutgoingStream(XmppClient Client, string To, string StreamId, int BlockSize, IEndToEndEncryption E2E)
		{
			this.client = Client;
			this.streamId = StreamId;
			this.to = To;
			this.blockSize = BlockSize;
			this.e2e = E2E;
			this.isWriting = false;
			this.seq = 0;
			this.done = false;
			this.tempStream = new TemporaryStream();
		}

		/// <summary>
		/// Recipient of stream.
		/// </summary>
		public string To => this.to;

		/// <summary>
		/// Stream ID
		/// </summary>
		public string StreamId => this.streamId;

		/// <summary>
		/// Block Size
		/// </summary>
		public int BlockSize => this.blockSize;

		/// <summary>
		/// If the stream has been aborted.
		/// </summary>
		public bool Aborted => this.aborted;

		/// <summary>
		/// State object.
		/// </summary>
		public object State
		{
			get => this.state;
			set => this.state = value;
		}

		/// <summary>
		/// Disposes allocated resources.
		/// </summary>
		public void Dispose()
		{
			this.aborted = true;

			this.tempStream?.Dispose();
			this.tempStream = null;

			this.syncObject?.Dispose();
			this.syncObject = null;
		}

		/// <summary>
		/// Writes data to the stram.
		/// </summary>
		/// <param name="Data">Data</param>
		public Task Write(byte[] Data)
		{
			return this.Write(Data, 0, Data.Length);
		}

		/// <summary>
		/// Writes data to the stram.
		/// </summary>
		/// <param name="Data">Data</param>
		/// <param name="Offset">Offset into array where writing is to start.</param>
		/// <param name="Count">Number of bytes to start.</param>
		public async Task Write(byte[] Data, int Offset, int Count)
		{
			if (this.tempStream is null || this.aborted || this.done)
				throw new IOException("Stream not open");

			if (!await this.syncObject.TryBeginWrite(10000))
				throw new TimeoutException();

			try
			{
				this.tempStream.Position = this.tempStream.Length;
				await this.tempStream.WriteAsync(Data, Offset, Count);

				if (this.opened && !this.isWriting && this.tempStream.Length - this.pos >= this.blockSize)
					await this.WriteBlockLocked();
			}
			finally
			{
				await this.syncObject.EndWrite();
			}
		}

		private async Task WriteBlockLocked()
		{
			int BlockSize;

			if (this.done)
				BlockSize = (int)Math.Min(this.tempStream.Length - this.pos, this.blockSize);
			else
				BlockSize = this.blockSize;

			if (BlockSize == 0)
				this.SendClose();
			else
			{
				byte[] Block = new byte[BlockSize];

				this.tempStream.Position = this.pos;
				int NrRead = await this.tempStream.ReadAsync(Block, 0, BlockSize);
				if (NrRead <= 0)
				{
					await this.Close();
					this.Dispose();

					throw new IOException("Unable to read from temporary file.");
				}

				this.pos += NrRead;
#if WINDOWS_UWP
				string Base64 = Convert.ToBase64String(Block, 0, NrRead);
#else
				string Base64 = Convert.ToBase64String(Block, 0, NrRead);
#endif
				StringBuilder Xml = new StringBuilder();
				int Seq = this.seq++;

				Xml.Append("<data xmlns='");
				Xml.Append(IbbClient.Namespace);
				Xml.Append("' seq='");
				Xml.Append(Seq.ToString());
				Xml.Append("' sid='");
				Xml.Append(this.streamId);
				Xml.Append("'>");
				Xml.Append(Base64);
				Xml.Append("</data>");

				this.isWriting = true;

				if (!(this.e2e is null))
					this.e2e.SendIqSet(this.client, E2ETransmission.NormalIfNotE2E, this.to, Xml.ToString(), this.BlockAck, Seq);
				else
					this.client.SendIqSet(this.to, Xml.ToString(), this.BlockAck, Seq);
			}
		}

		private async Task BlockAck(object Sender, IqResultEventArgs e)
		{
			if (this.tempStream is null || this.aborted)
				return;

			if (!e.Ok)
			{
				this.Dispose();
				return;
			}

			if (!await this.syncObject.TryBeginWrite(10000))
				throw new TimeoutException();

			try
			{
				int Seq2 = (int)e.State;
				if (Seq2 <= this.seqAcknowledged)
					return; // Response to a retry

				this.seqAcknowledged = Seq2;

				long NrLeft = this.tempStream.Length - this.pos;

				if (NrLeft >= this.blockSize || (this.done && NrLeft > 0))
					await this.WriteBlockLocked();
				else
				{
					this.isWriting = false;

					if (this.done)
					{
						this.SendClose();
						this.Dispose();
					}
				}
			}
			finally
			{
				await this.syncObject.EndWrite();
			}
		}

		internal async Task Opened(IqResultEventArgs e)
		{
			this.opened = true;

			OpenStreamEventHandler h = this.OnOpened;
			if (!(h is null))
			{
				try
				{
					OpenStreamEventArgs e2 = new OpenStreamEventArgs(e, this);
					await h(this, e2);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}

			if (!this.isWriting && this.tempStream.Length - this.pos >= this.blockSize)
				await this.WriteBlockLocked();
		}

		/// <summary>
		/// Event raised when stream han been opened.
		/// </summary>
		public OpenStreamEventHandler OnOpened = null;

		/// <summary>
		/// Closes the session.
		/// </summary>
		public async Task Close()
		{
			this.done = true;

			if (this.opened && !this.isWriting)
			{
				if (this.tempStream.Length > this.pos)
					await this.WriteBlockLocked();
				else
					this.SendClose();
			}
		}

		private void SendClose()
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<close xmlns='");
			Xml.Append(IbbClient.Namespace);
			Xml.Append("' sid='");
			Xml.Append(this.streamId);
			Xml.Append("'/>");

			if (!(this.e2e is null))
				this.e2e.SendIqSet(this.client, E2ETransmission.NormalIfNotE2E, this.to, Xml.ToString(), null, null);
			else
				this.client.SendIqSet(this.to, Xml.ToString(), null, null);
		}

		internal void Abort()
		{
			this.aborted = true;

			EventHandler h = this.OnAbort;
			if (!(h is null))
			{
				try
				{
					h(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when stream is aborted.
		/// </summary>
		public event EventHandler OnAbort = null;

	}
}
