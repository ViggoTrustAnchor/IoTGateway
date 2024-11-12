﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.Events;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppStanzaTests : CommunicationTests
	{
		[ClassInitialize]
		public static void ClassInitialize(TestContext _)
		{
			SetupSnifferAndLog();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			DisposeSnifferAndLog();
		}

		[TestMethod]
		public async Task Stanza_Test_01_ChatMessage()
		{
			await this.ConnectClients();
			ManualResetEvent Done = new(false);
			this.client2.OnChatMessage += (Sender, e) =>
			{
				Done.Set();
				return Task.CompletedTask;
			};

			await this.client1.SendChatMessage(this.client2.FullJID, "Hello");

			Assert.IsTrue(Done.WaitOne(10000), "Message not delivered properly.");
		}

		[TestMethod]
		public async Task Stanza_Test_02_ChatMessageWithSubject()
		{
			await this.ConnectClients();
			ManualResetEvent Done = new(false);
			this.client2.OnChatMessage += (Sender, e) =>
			{
				Done.Set();
				return Task.CompletedTask;
			};

			await this.client1.SendChatMessage(this.client2.FullJID, "Hello", "Greeting");

			Assert.IsTrue(Done.WaitOne(10000), "Message not delivered properly.");
		}

		[TestMethod]
		public async Task Stanza_Test_03_ChatMessageWithSubjectAndLanguage()
		{
			await this.ConnectClients();
			ManualResetEvent Done = new(false);
			this.client2.OnChatMessage += (Sender, e) =>
			{
				Done.Set();
				return Task.CompletedTask;
			};

			await this.client1.SendChatMessage(this.client2.FullJID, "Hello", "Greeting", "en");

			Assert.IsTrue(Done.WaitOne(10000), "Message not delivered properly.");
		}

		[TestMethod]
		public async Task Stanza_Test_04_Presence()
		{
			await this.ConnectClients();
			ManualResetEvent Done = new(false);
			this.client1.OnPresence += (Sender, e) =>
			{
				if (e.From == this.client2.FullJID && e.Availability == Availability.Chat)
					Done.Set();
			
				return Task.CompletedTask;
			};

			RosterItem Item1 = this.client1.GetRosterItem(this.client2.BareJID);
			RosterItem Item2 = this.client2.GetRosterItem(this.client1.BareJID);
			if (Item1 is null || (Item1.State != SubscriptionState.Both && Item1.State != SubscriptionState.To) ||
				Item2 is null || (Item2.State != SubscriptionState.Both && Item2.State != SubscriptionState.From))
			{
				ManualResetEvent Done2 = new(false);
				ManualResetEvent Error2 = new(false);

				this.client2.OnPresenceSubscribe += async (Sender, e) =>
				{
					if (e.FromBareJID == this.client1.BareJID)
					{
						await e.Accept();
						Done2.Set();
					}
					else
					{
						await e.Decline();
						Error2.Set();
					}
				};

				await this.client1.RequestPresenceSubscription(this.client2.BareJID);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done2, Error2 }, 10000));
			}

			Thread.Sleep(500);

			this.client2.CustomPresenceXml += this.AddCustomXml;
			try
			{
				await this.client2.SetPresence(Availability.Chat);
			}
			finally
			{
				this.client2.CustomPresenceXml -= this.AddCustomXml;
			}

			Assert.IsTrue(Done.WaitOne(10000), "Presence not delivered properly.");
		}

		private Task AddCustomXml(object Sender, CustomPresenceEventArgs e)
		{
			e.Stanza.Append("<hola xmlns='bandola'>abc</hola>");
			return Task.CompletedTask;
		}

		[TestMethod]
		public async Task Stanza_Test_05_IQ_Get()
		{
			await this.ConnectClients();
			this.client1.RegisterIqGetHandler("query", "test", (Sender, e) =>
			{
				return e.IqResult("<response xmlns='test'/>");
			}, true);

			this.client2.IqGet(this.client1.FullJID, "<query xmlns='test'/>", 10000);
		}

		[TestMethod]
		public async Task Stanza_Test_06_IQ_Set()
		{
			await this.ConnectClients();
			this.client1.RegisterIqSetHandler("query", "test", (Sender, e) =>
			{
				return e.IqResult("<response xmlns='test'/>");
			}, true);

			this.client2.IqSet(this.client1.FullJID, "<query xmlns='test'/>", 10000);
		}
	}
}
