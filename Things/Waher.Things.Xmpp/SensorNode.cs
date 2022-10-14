﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Sensor;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;

namespace Waher.Things.Xmpp
{
	/// <summary>
	/// A node in a concentrator.
	/// </summary>
	public class SensorNode : XmppNode, ISensor
	{
		/// <summary>
		/// A node in a concentrator.
		/// </summary>
		public SensorNode()
			: base()
		{
		}

		/// <summary>
		/// Gets the type name of the node.
		/// </summary>
		/// <param name="Language">Language to use.</param>
		/// <returns>Localized type node.</returns>
		public override Task<string> GetTypeNameAsync(Language Language)
		{
			return Language.GetStringAsync(typeof(ConcentratorDevice), 12, "Sensor Node");
		}

		/// <summary>
		/// Starts the readout of the sensor.
		/// </summary>
		/// <param name="Request">Request object. All fields and errors should be reported to this interface.</param>
		public Task StartReadout(ISensorReadout Request)
		{
			if (Types.TryGetModuleParameter("XMPP", out object Obj) && Obj is XmppClient Client &&
				Client.TryGetExtension<SensorClient>(out SensorClient SensorClient))
			{
				string JID = string.Empty;
				string NID = this.RemoteNodeID;
				string SID = string.Empty;
				string PID = string.Empty;

				IThingReference Loop = this.Parent;
				while (!(Loop is null))
				{
					if (Loop is SourceNode SourceNode)
						SID = SourceNode.RemoteSourceID;
					else if (Loop is PartitionNode PartitionNode)
						PID = PartitionNode.RemotePartitionID;
					else if (Loop is ConcentratorDevice ConcentratorDevice)
					{
						JID = ConcentratorDevice.JID;
						break;
					}
				}

				RosterItem Item = Client.GetRosterItem(JID);
				if (Item is null)
				{
					Request.ReportErrors(true, new ThingError(this, "JID not available in roster."));
					return Task.CompletedTask;
				}

				if (!Item.HasLastPresence || !Item.LastPresence.IsOnline)
				{
					Request.ReportErrors(true, new ThingError(this, "Concentrator not online."));
					return Task.CompletedTask;
				}

				TaskCompletionSource<bool> Done = new TaskCompletionSource<bool>();
				SensorDataClientRequest Request2 = SensorClient.RequestReadout(Item.LastPresenceFullJid,
					new IThingReference[] { new ThingReference(NID, SID, PID) }, Request.Types, Request.FieldNames,
					Request.From, Request.To, Request.When, Request.ServiceToken, Request.DeviceToken, Request.UserToken);

				Request2.OnFieldsReceived += (sender, Fields) =>
				{
					Request.ReportFields(false, Fields);
					return Task.CompletedTask;
				};

				Request2.OnErrorsReceived += (sender, Errors) =>
				{
					List<ThingError> Errors2 = new List<ThingError>();
					Errors2.AddRange(Errors);
					Request.ReportErrors(false, Errors2.ToArray());
					return Task.CompletedTask;
				};

				Request2.OnStateChanged += (sender, State) =>
				{
					switch (State)
					{
						case SensorDataReadoutState.Cancelled:
							Request.ReportErrors(true, new ThingError(this, "Readout was cancelled."));
							Done.TrySetResult(false);
							break;

						case SensorDataReadoutState.Done:
							Request.ReportFields(true);
							Done.TrySetResult(true);
							break;

						case SensorDataReadoutState.Failure:
							Request.ReportErrors(true, new ThingError(this, "Readout failed."));
							Done.TrySetResult(false);
							break;
					}

					return Task.CompletedTask;
				};

				return Done.Task;
			}
			else
			{
				Request.ReportErrors(true, new ThingError(this, "No XMPP Sensor Client available."));
				return Task.CompletedTask;
			}
		}
	}
}