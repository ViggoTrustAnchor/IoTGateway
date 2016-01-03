﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Sensor;
using Waher.Things;
using Waher.Things.SensorData;
using Waher.Client.WPF.Controls;
using Waher.Client.WPF.Controls.Chat;
using Waher.Client.WPF.Controls.Sniffers;
using Waher.Client.WPF.Dialogs;
using Waher.Client.WPF.Model;

namespace Waher.Client.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const string WindowTitle = "Simple XMPP IoT Client";

		public static RoutedUICommand Add = new RoutedUICommand("Add", "Add", typeof(MainWindow));
		public static RoutedUICommand ConnectTo = new RoutedUICommand("Connect To", "ConnectTo", typeof(MainWindow));
		public static RoutedUICommand Sniff = new RoutedUICommand("Sniff", "Sniff", typeof(MainWindow));
		public static RoutedUICommand CloseTab = new RoutedUICommand("Close Tab", "CloseTab", typeof(MainWindow));
		public static RoutedUICommand Chat = new RoutedUICommand("Chat", "Chat", typeof(MainWindow));
		public static RoutedUICommand ReadMomentary = new RoutedUICommand("Read Momentary", "ReadMomentary", typeof(MainWindow));
		public static RoutedUICommand ReadDetailed = new RoutedUICommand("Read Detailed", "ReadDetailed", typeof(MainWindow));
		internal static MainWindow currentInstance = null;


		private Connections connections;
		private string fileName = string.Empty;

		public MainWindow()
		{
			if (currentInstance == null)
				currentInstance = this;

			InitializeComponent();

			this.connections = new Connections(this);
		}

		private static readonly string registryKey = Registry.CurrentUser + @"\Software\Waher Data AB\Waher.Client.WPF";

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			object Value;

			try
			{
				Value = Registry.GetValue(registryKey, "WindowLeft", (int)this.Left);
				if (Value != null && Value is int)
					this.Left = (int)Value;

				Value = Registry.GetValue(registryKey, "WindowTop", (int)this.Top);
				if (Value != null && Value is int)
					this.Top = (int)Value;

				Value = Registry.GetValue(registryKey, "WindowWidth", (int)this.Width);
				if (Value != null && Value is int)
					this.Width = (int)Value;

				Value = Registry.GetValue(registryKey, "WindowHeight", (int)this.Height);
				if (Value != null && Value is int)
					this.Height = (int)Value;

				Value = Registry.GetValue(registryKey, "ConnectionTreeWidth", (int)this.ConnectionTree.Width);
				if (Value != null && Value is int)
					this.ConnectionsGrid.ColumnDefinitions[0].Width = new GridLength((int)Value);

				Value = Registry.GetValue(registryKey, "WindowState", this.WindowState.ToString());
				if (Value != null && Value is string)
					this.WindowState = (WindowState)Enum.Parse(typeof(WindowState), (string)Value);

				Value = Registry.GetValue(registryKey, "FileName", string.Empty);
				if (Value != null && Value is string)
				{
					this.FileName = (string)Value;
					if (!string.IsNullOrEmpty(this.fileName))
						this.Load(this.fileName);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Unable to load values from registry.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private bool CheckSaved()
		{
			if (this.connections.Modified)
			{
				switch (MessageBox.Show(this, "You have unsaved changes. Do you want to save these changes before closing the application?",
					"Save unsaved changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
				{
					case MessageBoxResult.Yes:
						if (this.SaveNewFile())
							break;
						else
							return false;

					case MessageBoxResult.No:
						break;

					case MessageBoxResult.Cancel:
						return false;
				}
			}

			return true;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!this.CheckSaved())
			{
				e.Cancel = true;
				return;
			}

			Registry.SetValue(registryKey, "WindowLeft", (int)this.Left, RegistryValueKind.DWord);
			Registry.SetValue(registryKey, "WindowTop", (int)this.Top, RegistryValueKind.DWord);
			Registry.SetValue(registryKey, "WindowWidth", (int)this.Width, RegistryValueKind.DWord);
			Registry.SetValue(registryKey, "WindowHeight", (int)this.Height, RegistryValueKind.DWord);
			Registry.SetValue(registryKey, "ConnectionTreeWidth", (int)this.ConnectionsGrid.ColumnDefinitions[0].Width.Value, RegistryValueKind.DWord);
			Registry.SetValue(registryKey, "WindowState", this.WindowState.ToString(), RegistryValueKind.String);
			Registry.SetValue(registryKey, "FileName", this.fileName, RegistryValueKind.String);

			this.connections.New();
		}

		private void ConnectTo_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ConnectToForm Dialog = new ConnectToForm();
			Dialog.Owner = this;
			bool? Result = Dialog.ShowDialog();

			if (Result.HasValue && Result.Value)
			{
				string Host = Dialog.XmppServer.Text;
				int Port = int.Parse(Dialog.XmppPort.Text);
				string Account = Dialog.AccountName.Text;
				string PasswordHash = Dialog.PasswordHash;
				string PasswordHashMethod = Dialog.PasswordHashMethod;
				bool TrustCertificate = Dialog.TrustServerCertificate.IsChecked.HasValue && Dialog.TrustServerCertificate.IsChecked.Value;

				XmppAccountNode Node = new XmppAccountNode(this.connections, null, Host, Port, Account, PasswordHash, PasswordHashMethod, TrustCertificate);
				this.connections.Add(Node);
				this.AddNode(Node);
			}
		}

		private void AddNode(TreeNode Node)
		{
			this.ConnectionTree.Items.Add(Node);
			this.NodeAdded(null, Node);
		}

		public void NodeAdded(TreeNode Parent, TreeNode ChildNode)
		{
			ChildNode.Updated += this.Node_Updated;
			ChildNode.Added(this);
		}

		public void NodeRemoved(TreeNode Parent, TreeNode ChildNode)
		{
			ChildNode.Updated -= this.Node_Updated;
			ChildNode.Removed(this);
		}

		private void Node_Updated(object sender, EventArgs e)
		{
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.RefreshTree), sender);
		}

		private void RefreshTree(object P)
		{
			TreeNode Node = (TreeNode)P;
			this.ConnectionTree.Items.Refresh();
		}

		private bool SaveNewFile()
		{
			SaveFileDialog Dialog = new SaveFileDialog();
			Dialog.AddExtension = true;
			Dialog.CheckPathExists = true;
			Dialog.CreatePrompt = false;
			Dialog.DefaultExt = "xml";
			Dialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
			Dialog.Title = "Save connection file";

			bool? Result = Dialog.ShowDialog(this);

			if (Result.HasValue && Result.Value)
			{
				this.FileName = Dialog.FileName;
				this.SaveFile();
				return true;
			}
			else
				return false;
		}

		private void SaveFile()
		{
			if (string.IsNullOrEmpty(this.fileName))
				this.SaveNewFile();
			else
				this.connections.Save(this.fileName);
		}

		private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.SaveFile();
		}

		private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.SaveNewFile();
		}

		private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!this.CheckSaved())
				return;

			try
			{
				OpenFileDialog Dialog = new OpenFileDialog();
				Dialog.AddExtension = true;
				Dialog.CheckFileExists = true;
				Dialog.CheckPathExists = true;
				Dialog.DefaultExt = "xml";
				Dialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
				Dialog.Multiselect = false;
				Dialog.ShowReadOnly = true;
				Dialog.Title = "Open connection file";

				bool? Result = Dialog.ShowDialog(this);

				if (Result.HasValue && Result.Value)
					this.Load(Dialog.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Unable to load file.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Load(string FileName)
		{
			try
			{
				XmlDocument Xml = new XmlDocument();
				Xml.Load(FileName);

				switch (Xml.DocumentElement.LocalName)
				{
					case "ClientConnections":
						this.connections.Load(FileName, Xml);
						this.FileName = FileName;

						ConnectionTree.Items.Clear();
						foreach (TreeNode Node in this.connections.RootNodes)
							this.AddNode(Node);
						break;

					case "Sniff":
						TabItem TabItem = new TabItem();
						this.Tabs.Items.Add(TabItem);

						SnifferView SnifferView = new SnifferView(null, this);

						TabItem.Header = System.IO.Path.GetFileName(FileName);
						TabItem.Content = SnifferView;

						SnifferView.Sniffer = new TabSniffer(TabItem, SnifferView);

						this.Tabs.SelectedItem = TabItem;

						SnifferView.Load(Xml, FileName);
						break;

					case "Chat":
						TabItem = new TabItem();
						this.Tabs.Items.Add(TabItem);

						ChatView ChatView = new ChatView(null, this);
						ChatView.Input.IsEnabled = false;
						ChatView.SendButton.IsEnabled = false;

						TabItem.Header = System.IO.Path.GetFileName(FileName);
						TabItem.Content = ChatView;

						this.Tabs.SelectedItem = TabItem;

						ChatView.Load(Xml, FileName);
						break;

					default:
						throw new Exception("Unrecognized file format.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void New_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!this.CheckSaved())
				return;

			ConnectionTree.Items.Clear();
			this.connections.New();
			this.FileName = string.Empty;
		}

		public string FileName
		{
			get { return this.fileName; }
			set
			{
				this.fileName = value;
				if (string.IsNullOrEmpty(this.fileName))
					this.Title = WindowTitle;
				else
					this.Title = this.fileName + " - " + WindowTitle;
			}
		}

		private void ConnectionTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;

			if (Node == null)
			{
				this.AddButton.IsEnabled = false;
				this.DeleteButton.IsEnabled = false;
				this.RefreshButton.IsEnabled = false;
				this.ChatButton.IsEnabled = false;
				this.ReadMomentaryButton.IsEnabled = false;
				this.ReadDetailedButton.IsEnabled = false;
			}
			else
			{
				this.AddButton.IsEnabled = Node.CanAddChildren;
				this.DeleteButton.IsEnabled = true;
				this.RefreshButton.IsEnabled = Node.CanRecycle;
				this.SniffButton.IsEnabled = Node.IsSniffable;
				this.ChatButton.IsEnabled = Node.CanChat;
				this.ReadMomentaryButton.IsEnabled = Node.CanReadSensorData;
				this.ReadDetailedButton.IsEnabled = Node.CanReadSensorData;
			}
		}

		private TreeNode SelectedNode
		{
			get
			{
				if (this.ConnectionTree == null)
					return null;

				return this.ConnectionTree.SelectedItem as TreeNode;
			}
		}

		private void Add_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null && Node.CanAddChildren);
		}

		private void Add_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null || !Node.CanAddChildren)
				return;

			Node.Add();
		}

		private void Refresh_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null && Node.CanRecycle);
		}

		private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null || !Node.CanRecycle)
				return;

			Node.Recycle();
		}

		private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null);
		}

		private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null)
				return;

			if (MessageBox.Show(this, "Are you sure you want to remove " + Node.Header + "?", "Are you sure?", MessageBoxButton.YesNo,
				MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				this.NodeRemoved(Node.Parent, Node);

				if (Node.Parent == null)
				{
					this.connections.Delete(Node);
					this.ConnectionTree.Items.Remove(this.ConnectionTree.SelectedItem);
				}
				else
					Node.Parent.Delete(Node);

				this.ConnectionTree.Items.Refresh();
			}
		}

		private void Sniff_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null && Node.IsSniffable);
		}

		private void Sniff_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null || !Node.IsSniffable)
				return;

			SnifferView View;

			foreach (TabItem Tab in this.Tabs.Items)
			{
				View = Tab.Content as SnifferView;
				if (View == null)
					continue;

				if (View.Node == Node)
				{
					Tab.Focus();
					return;
				}
			}
			
			TabItem TabItem = new TabItem();
			this.Tabs.Items.Add(TabItem);

			View = new SnifferView(Node, this);

			TabItem.Header = Node.Header;
			TabItem.Content = View;

			View.Sniffer = new TabSniffer(TabItem, View);
			Node.AddSniffer(View.Sniffer);

			this.Tabs.SelectedItem = TabItem;
		}

		private void CloseTab_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = this.Tabs.SelectedIndex > 0;
		}

		private void CloseTab_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			int i = this.Tabs.SelectedIndex;
			if (i > 0)
			{
				TabItem TabItem = this.Tabs.Items[i] as TabItem;
				if (TabItem != null)
				{
					object Content = TabItem.Content;
					if (Content != null && Content is IDisposable)
						((IDisposable)Content).Dispose();
				}

				this.Tabs.Items.RemoveAt(i);
			}
		}

		private void Chat_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null && Node.CanChat);
		}

		private void Chat_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null || !Node.CanChat)
				return;

			ChatView View;

			foreach (TabItem Tab in this.Tabs.Items)
			{
				View = Tab.Content as ChatView;
				if (View == null)
					continue;

				if (View.Node == Node)
				{
					Tab.Focus();
					return;
				}
			}

			TabItem TabItem = new TabItem();
			this.Tabs.Items.Add(TabItem);

			View = new ChatView(Node, this);

			TabItem.Header = Node.Header;
			TabItem.Content = View;

			this.Tabs.SelectedItem = TabItem;

			Thread T = new Thread(this.FocusChatInput);
			T.Start(View);
		}

		private void FocusChatInput(object P)
		{
			Thread.Sleep(50);
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.FocusChatInput2), P);
		}

		private void FocusChatInput2(object P)
		{
			ChatView View = (ChatView)P;
			View.Input.Focus();
		}

		public void OnChatMessage(object Sender, MessageEventArgs e)
		{
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.ChatMessageReceived), e);
		}

		private void ChatMessageReceived(object P)
		{
			MessageEventArgs e = (MessageEventArgs)P;
			ChatView ChatView;

			foreach (TabItem TabItem in this.Tabs.Items)
			{
				ChatView = TabItem.Content as ChatView;
				if (ChatView == null)
					continue;

				XmppContact XmppContact = ChatView.Node as XmppContact;
				if (XmppContact == null)
					continue;

				if (XmppContact.BareJID != e.FromBareJID)
					continue;

				XmppAccountNode XmppAccountNode = XmppContact.XmppAccountNode;
				if (XmppAccountNode == null)
					continue;

				if (XmppAccountNode.BareJID != XmppClient.GetBareJID(e.To))
					continue;

				ChatView.ChatMessageReceived(e.Body);
				break;
			}
		}

		private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TabItem Item = this.Tabs.SelectedItem as TabItem;
			if (Item != null)
			{
				ChatView View = Item.Content as ChatView;
				if (View != null)
				{
					Thread T = new Thread(this.FocusChatInput);
					T.Start(View);
				}
			}
		}
		
		private void ReadMomentary_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null && Node.CanReadSensorData);
		}

		private void ReadMomentary_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null || !Node.CanReadSensorData)
				return;

			SensorDataClientRequest Request = Node.StartSensorDataMomentaryReadout();
			if (Request == null)
				return;

			TabItem TabItem = new TabItem();
			this.Tabs.Items.Add(TabItem);

			SensorDataView View = new SensorDataView(Request, Node, this);

			TabItem.Header = Node.Header;
			TabItem.Content = View;

			this.Tabs.SelectedItem = TabItem;
		}

		private void ReadDetailed_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			TreeNode Node = this.SelectedNode;
			e.CanExecute = (Node != null && Node.CanReadSensorData);
		}

		private void ReadDetailed_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			TreeNode Node = this.ConnectionTree.SelectedItem as TreeNode;
			if (Node == null || !Node.CanReadSensorData)
				return;

			SensorDataClientRequest Request = Node.StartSensorDataFullReadout();
			if (Request == null)
				return;

			TabItem TabItem = new TabItem();
			this.Tabs.Items.Add(TabItem);

			SensorDataView View = new SensorDataView(Request, Node, this);

			TabItem.Header = Node.Header;
			TabItem.Content = View;

			this.Tabs.SelectedItem = TabItem;
		}

	}
}
