﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Waher.Client.WPF.Model;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.RDP;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for RemoteDesktopView.xaml
	/// </summary>
	public partial class RemoteDesktopView : UserControl, ITabView
	{
		private readonly LinkedList<(string, int, int)> queue = new LinkedList<(string, int, int)>();
		private readonly TreeNode node;
		private readonly XmppClient client;
		private readonly RemoteDesktopClient rdpClient;
		private readonly RemoteDesktopSession session;
		private readonly object synchObj = new object();
		private string[,] pendingTiles = null;
		private WriteableBitmap desktop = null;
		private Timer timer;
		private int columns;
		private int rows;
		private bool drawing = false;

		public RemoteDesktopView(TreeNode Node, XmppClient Client, RemoteDesktopClient RdpClient, RemoteDesktopSession Session)
		{
			this.node = Node;
			this.client = Client;
			this.rdpClient = RdpClient;
			this.session = Session;

			this.session.StateChanged += Session_StateChanged;
			this.session.TileUpdated += Session_TileUpdated;

			InitializeComponent();
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
					this.pendingTiles = new string[this.rows, this.columns];

					foreach ((string TileBase64, int X, int Y) in this.queue)
						this.pendingTiles[Y, X] = TileBase64;

					this.queue.Clear();
				}

				this.timer = new Timer(this.UpdateScreen, null, 250, 250);
			}
		}

		private void Session_TileUpdated(object sender, TileEventArgs e)
		{
			lock (this.synchObj)
			{
				if (this.pendingTiles is null)
					this.queue.AddLast((e.TileBase64, e.X, e.Y));
				else
					this.pendingTiles[e.Y, e.X] = e.TileBase64;
			}
		}

		private void UpdateScreen(object _)
		{
			MainWindow.UpdateGui(this.UpdateScreenGuiThread);
		}

		private void UpdateScreenGuiThread()
		{
			MemoryStream ms = null;
			Bitmap Tile = null;
			BitmapData Data = null;
			bool Locked = false;
			int Size = this.session.TileSize;
			string s;
			int x, y;

			try
			{
				if (this.drawing)
					return;

				this.drawing = true;

				for (y = 0; y < this.rows; y++)
				{
					for (x = 0; x < this.columns; x++)
					{
						lock (this.synchObj)
						{
							s = this.pendingTiles[y, x];
							if (s is null)
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
						ms = new MemoryStream(Convert.FromBase64String(s));

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
		}

		public async void Dispose()
		{
			try
			{
				this.timer?.Dispose();
				this.timer = null;

				if (this.session.State != RemoteDesktopSessionState.Stopped && this.session.State != RemoteDesktopSessionState.Stopping)
					await this.rdpClient.StopSessionAsync(this.session.RemoteJid, this.session.SessionId);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}

			this.Node?.ViewClosed();
		}

		public TreeNode Node => this.node;
		public XmppClient Client => this.client;
		public RemoteDesktopClient RdpClient => this.rdpClient;
		public RemoteDesktopSession Session => this.session;

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Refresh screen?
		}

		public void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: screen capture?
		}

		public void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: screen capture?
		}

		public void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: ?
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// TODO: ?
		}

	}
}
