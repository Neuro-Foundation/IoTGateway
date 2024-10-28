﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Waher.Client.WPF.Model;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.P2P.SOCKS5;
using Waher.Networking.XMPP.RDP;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for RemoteDesktopView.xaml
	/// </summary>
	public partial class RemoteDesktopView : UserControl, ITabView
	{
		private readonly LinkedList<(Pending, int, int)> queue = new LinkedList<(Pending, int, int)>();
		private readonly XmppContact node;
		private readonly XmppClient client;
		private readonly RemoteDesktopClient rdpClient;
		private readonly object synchObj = new object();
		private RemoteDesktopSession session;
		private Pending[,] pendingTiles = null;
		private WriteableBitmap desktop = null;
		private DateTime updateScreenTimer;
		private int columns;
		private int rows;
		private bool drawing = false;
		private bool disposeRdpClient;

		private class Pending
		{
			public string Base64;
			public byte[] Bin;

			public Pending(string Base64)
			{
				this.Base64 = Base64;
			}

			public Pending(byte[] Bin)
			{
				this.Bin = Bin;
			}
		}

		public RemoteDesktopView(XmppContact Node, XmppClient Client, RemoteDesktopClient RdpClient, bool DisposeRdpClient)
		{
			this.node = Node;
			this.client = Client;
			this.rdpClient = RdpClient;
			this.disposeRdpClient = DisposeRdpClient;

			this.InitializeComponent();

			this.Focusable = true;
			Keyboard.Focus(this);
		}

		public RemoteDesktopSession Session
		{
			get => this.session;
			internal set
			{
				if (!(this.session is null))
				{
					this.session.StateChanged -= this.Session_StateChanged;
					this.session.TileUpdated -= this.Session_TileUpdated;
					this.session.ScanComplete -= this.Session_ScanComplete;
				}

				this.session = value;

				this.session.StateChanged += this.Session_StateChanged;
				this.session.TileUpdated += this.Session_TileUpdated;
				this.session.ScanComplete += this.Session_ScanComplete;
			}
		}

		private void Session_StateChanged(object sender, EventArgs e)
		{
			if (this.session.State == RemoteDesktopSessionState.Started && this.desktop is null)
			{
				int ScreenWidth = this.session.Width;
				int ScreenHeight = this.session.Height;
				this.columns = (ScreenWidth + this.session.TileSize - 1) / this.session.TileSize;
				this.rows = (ScreenHeight + this.session.TileSize - 1) / this.session.TileSize;

				lock (this.synchObj)
				{
					this.pendingTiles = new Pending[this.rows, this.columns];

					foreach ((Pending Tile, int X, int Y) in this.queue)
						this.pendingTiles[Y, X] = Tile;

					this.queue.Clear();
				}
			}
		}

		private void Session_TileUpdated(object sender, TileEventArgs e)
		{
			lock (this.synchObj)
			{
				if (this.pendingTiles is null)
					this.queue.AddLast((new Pending(e.TileBase64), e.X, e.Y));
				else
					this.pendingTiles[e.Y, e.X] = new Pending(e.TileBase64);

				if (this.updateScreenTimer == DateTime.MinValue)
					this.updateScreenTimer = MainWindow.Scheduler.Add(DateTime.Now.AddMilliseconds(250), this.UpdateScreen, null);
			}
		}

		private void Session_ScanComplete(object sender, EventArgs e)
		{
			this.UpdateScreen(null);
		}

		private byte[] buffer;
		private byte[] block;
		private int blockState = 0;
		private int blockLen = 0;
		private int blockLeft = 0;
		private int blockPos = 0;
		private int state = 0;
		private byte command = 0;
		private int len = 0;
		private int left = 0;
		private int pos = 0;
		private int x = 0;
		private int y = 0;

		internal Task Socks5DataReceived(object Sender, DataReceivedEventArgs e)
		{
			byte[] Data = e.Buffer;
			int i = e.Offset;
			int c = e.Count;

			while (c > 0)
			{
				switch (this.blockState)
				{
					case 0:
						this.blockLen = Data[i++];
						this.blockState++;
						c--;
						break;

					case 1:
						this.blockLen <<= 8;
						this.blockLen |= Data[i++];
						this.blockLeft = this.blockLen;
						if ((this.block?.Length ?? 0) != this.blockLen)
							this.block = new byte[this.blockLen];
						this.blockPos = 0;
						this.blockState++;
						c--;
						break;

					case 2:
						int j = Math.Min(this.blockLeft, c);
						Array.Copy(Data, i, this.block, this.blockPos, j);
						this.blockPos += j;
						this.blockLeft -= j;
						i += j;
						c -= j;

						if (this.blockLeft == 0)
						{
							this.BlockReceived(this.block);
							this.blockState = 0;
						}
						break;
				}
			}

			return Task.CompletedTask;
		}

		private void BlockReceived(byte[] Data)
		{
			int i = 0;
			int c = Data.Length;
			int j;

			while (c > 0)
			{
				switch (this.state)
				{
					case 0:
						this.command = Data[i++];
						c--;
						this.state++;
						break;

					case 1:
						this.len = Data[i++];
						c--;
						this.state++;
						break;

					case 2:
						this.len |= Data[i++] << 8;
						c--;
						this.state++;
						break;

					case 3:
						this.len |= Data[i++] << 16;
						c--;
						this.left = this.len;
						this.buffer = new byte[this.len];
						this.pos = 0;
						this.state++;
						break;

					case 4:
						this.x = Data[i++];
						c--;
						this.state++;
						break;

					case 5:
						this.x |= Data[i++] << 8;
						c--;
						this.state++;
						break;

					case 6:
						this.y = Data[i++];
						c--;
						this.state++;
						break;

					case 7:
						this.y |= Data[i++] << 8;
						c--;

						if (this.left > 0)
							this.state++;
						else
						{
							this.ProcessCommand();
							this.state = 0;
						}
						break;

					case 8:
						j = Math.Min(this.left, c);
						Array.Copy(Data, i, this.buffer, this.pos, j);
						this.pos += j;
						this.left -= j;
						i += j;
						c -= j;

						if (this.left == 0)
						{
							this.ProcessCommand();
							this.state = 0;
						}
						break;

					default:
						c = 0;
						break;
				}
			}
		}

		private void ProcessCommand()
		{
			switch (this.command)
			{
				case 0:
					lock (this.synchObj)
					{
						if (this.pendingTiles is null)
							this.queue.AddLast((new Pending(this.buffer), this.x, this.y));
						else
							this.pendingTiles[this.y, this.x] = new Pending(this.buffer);

						if (this.updateScreenTimer == DateTime.MinValue)
							this.updateScreenTimer = MainWindow.Scheduler.Add(DateTime.Now.AddMilliseconds(250), this.UpdateScreen, null);
					}
					break;

				case 1:
					this.UpdateScreen(null);
					break;
			}
		}

		internal Task Socks5StreamClosed(object Sender, StreamEventArgs e)
		{
			return Task.CompletedTask;  // TODO
		}

		private void UpdateScreen(object _)
		{
			this.updateScreenTimer = DateTime.MinValue;
			MainWindow.UpdateGui(this.UpdateScreenGuiThread);
		}

		private Task UpdateScreenGuiThread()
		{
			MemoryStream ms = null;
			Bitmap Tile = null;
			BitmapData Data = null;
			Pending PendingTile;
			bool Locked = false;
			int Size = this.session.TileSize;
			int x, y;

			try
			{
				if (this.drawing)
					return Task.CompletedTask;

				this.drawing = true;

				for (y = 0; y < this.rows; y++)
				{
					for (x = 0; x < this.columns; x++)
					{
						lock (this.synchObj)
						{
							PendingTile = this.pendingTiles[y, x];
							if (PendingTile is null)
								continue;

							this.pendingTiles[y, x] = null;
						}

						if (this.desktop is null)
						{
							this.desktop = new WriteableBitmap(this.session.Width, this.session.Height, 96, 96, PixelFormats.Bgra32, null);
							this.DesktopImage.Source = this.desktop;
						}

						ms?.Dispose();
						ms = null;
						ms = new MemoryStream(PendingTile.Bin ?? Convert.FromBase64String(PendingTile.Base64));

						Tile = (Bitmap)Bitmap.FromStream(ms);
						Data = Tile.LockBits(new Rectangle(0, 0, Tile.Width, Tile.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

						if (!Locked)
						{
							this.desktop.Lock();
							Locked = true;
						}

						this.desktop.WritePixels(new Int32Rect(0, 0, Tile.Width, Tile.Height), Data.Scan0, Data.Stride * Data.Height,
							Data.Stride, x * Size, y * Size);

						Tile.UnlockBits(Data);
						Data = null;

						Tile.Dispose();
						Tile = null;
					}
				}
			}
			finally
			{
				this.drawing = false;

				if (Locked)
					this.desktop.Unlock();

				if (!(Data is null))
					Tile.UnlockBits(Data);

				ms?.Dispose();
				Tile?.Dispose();
			}

			return Task.CompletedTask;
		}

		public async void Dispose()
		{
			try
			{
				this.node?.XmppAccountNode?.UnregisterView(this);

				if (this.updateScreenTimer > DateTime.MinValue)
				{
					MainWindow.Scheduler?.Remove(this.updateScreenTimer);
					this.updateScreenTimer = DateTime.MinValue;
				}

				if (!(this.session is null) &&
					this.session.State != RemoteDesktopSessionState.Stopped &&
					this.session.State != RemoteDesktopSessionState.Stopping)
				{
					await this.rdpClient.StopSessionAsync(this.session.RemoteJid, this.session.SessionId);
				}

				if (this.disposeRdpClient)
				{
					this.rdpClient.Dispose();
					this.disposeRdpClient = false;
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}

			this.node?.ViewClosed();
		}

		public XmppContact Node => this.node;
		public XmppClient Client => this.client;
		public RemoteDesktopClient RdpClient => this.rdpClient;

		private void UserControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (!(this.session is null))
			{
				this.GetPosition(e, out int X, out int Y);
				this.session.MouseMoved(X, Y);
				e.Handled = true;
			}
		}

		private void GetPosition(MouseEventArgs e, out int X, out int Y)
		{
			System.Windows.Point P = e.GetPosition(this.DesktopImage);

			X = (int)(this.session.Width * P.X / this.DesktopImage.ActualWidth + 0.5);
			Y = (int)(this.session.Height * P.Y / this.DesktopImage.ActualHeight + 0.5);
		}

		private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!(this.session is null))
			{
				this.GetPosition(e, out int X, out int Y);

				switch (e.ChangedButton)
				{
					case System.Windows.Input.MouseButton.Left:
						this.session.MouseDown(X, Y, Networking.XMPP.RDP.MouseButton.Left);
						e.Handled = true;
						break;

					case System.Windows.Input.MouseButton.Middle:
						this.session.MouseDown(X, Y, Networking.XMPP.RDP.MouseButton.Middle);
						e.Handled = true;
						break;

					case System.Windows.Input.MouseButton.Right:
						this.session.MouseDown(X, Y, Networking.XMPP.RDP.MouseButton.Right);
						e.Handled = true;
						break;

					default:
						e.Handled = false;
						break;
				}
			}
		}

		private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!(this.session is null))
			{
				this.GetPosition(e, out int X, out int Y);

				switch (e.ChangedButton)
				{
					case System.Windows.Input.MouseButton.Left:
						this.session.MouseUp(X, Y, Networking.XMPP.RDP.MouseButton.Left);
						e.Handled = true;
						break;

					case System.Windows.Input.MouseButton.Middle:
						this.session.MouseUp(X, Y, Networking.XMPP.RDP.MouseButton.Middle);
						e.Handled = true;
						break;

					case System.Windows.Input.MouseButton.Right:
						this.session.MouseUp(X, Y, Networking.XMPP.RDP.MouseButton.Right);
						e.Handled = true;
						break;

					default:
						e.Handled = false;
						break;
				}
			}
		}

		private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!(this.session is null))
			{
				this.GetPosition(e, out int X, out int Y);
				this.session.MouseWheel(X, Y, e.Delta);
				e.Handled = true;
			}
		}

		private void UserControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (!(this.session is null))
			{
				int KeyCode = KeyInterop.VirtualKeyFromKey(e.Key);
				this.session.KeyDown(KeyCode);
				e.Handled = true;
			}
		}

		private void UserControl_KeyUp(object sender, KeyEventArgs e)
		{
			if (!(this.session is null))
			{
				int KeyCode = KeyInterop.VirtualKeyFromKey(e.Key);
				this.session.KeyUp(KeyCode);
				e.Handled = true;
			}
		}

		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (this.IsVisible)
				Keyboard.Focus(this);
		}

		public void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: screen capture?
		}

		public void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: screen capture?
		}

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Refresh screen?
		}

		public void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: ?
		}
	}
}
