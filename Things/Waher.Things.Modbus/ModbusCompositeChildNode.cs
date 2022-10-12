﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Sensor;
using Waher.Runtime.Language;
using Waher.Things.ControlParameters;

namespace Waher.Things.Modbus
{
	/// <summary>
	/// Represents a composition of nodes, under a unit.
	/// </summary>
	public class ModbusCompositeChildNode : ModbusUnitChildNode, ISensor, IActuator
	{
		/// <summary>
		/// Represents a composition of nodes, under a unit.
		/// </summary>
		public ModbusCompositeChildNode()
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
			return Language.GetStringAsync(typeof(ModbusGatewayNode), 28, "Composite Node");
		}

		/// <summary>
		/// If the node accepts a presumptive parent, i.e. can be added to that parent (if that parent accepts the node as a child).
		/// </summary>
		/// <param name="Parent">Presumptive parent node.</param>
		/// <returns>If the parent is acceptable.</returns>
		public override Task<bool> AcceptsParentAsync(INode Parent)
		{
			return Task.FromResult<bool>(Parent is ModbusUnitNode);
		}

		/// <summary>
		/// If the node accepts a presumptive child, i.e. can receive as a child (if that child accepts the node as a parent).
		/// </summary>
		/// <param name="Child">Presumptive child node.</param>
		/// <returns>If the child is acceptable.</returns>
		public override Task<bool> AcceptsChildAsync(INode Child)
		{
			return Task.FromResult<bool>(Child is ModbusUnitChildNode);
		}

		/// <summary>
		/// Starts the readout of the sensor.
		/// </summary>
		/// <param name="Request">Request object. All fields and errors should be reported to this interface.</param>
		public async Task StartReadout(ISensorReadout Request)
		{
			foreach (INode Child in await this.ChildNodes)
			{
				if (Child is ISensor Sensor)
				{
					TaskCompletionSource<bool> ReadoutCompleted = new TaskCompletionSource<bool>();

					InternalReadoutRequest InternalReadout = new InternalReadoutRequest(this.LogId, null, Request.Types, Request.FieldNames,
						Request.From, Request.To,
						(sender, e) =>
						{
							Request.ReportFields(false, e.Fields);

							if (e.Done)
								ReadoutCompleted.TrySetResult(true);

							return Task.CompletedTask;
						},
						(sender, e) =>
						{
							if (!(e.Errors is ThingError[] Errors))
							{
								List<ThingError> List = new List<ThingError>();

								foreach (ThingError Error in e.Errors)
									List.Add(Error);

								Errors = List.ToArray();
							}

							Request.ReportErrors(false, Errors);

							if (e.Done)
								ReadoutCompleted.TrySetResult(true);

							return Task.CompletedTask;
						}, null);

					await Sensor.StartReadout(InternalReadout);

					Task Timeout = Task.Delay(60000);

					Task T = await Task.WhenAny(ReadoutCompleted.Task, Timeout);

					if (ReadoutCompleted.Task.IsCompleted)
						Request.ReportFields(true);
					else
						Request.ReportErrors(true, new ThingError(this, "Timeout."));
				}
			}
		}

		/// <summary>
		/// Get control parameters for the actuator.
		/// </summary>
		/// <returns>Collection of control parameters for actuator.</returns>
		public async Task<ControlParameter[]> GetControlParameters()
		{
			List<ControlParameter> Result = new List<ControlParameter>();

			foreach (INode Child in await this.ChildNodes)
			{
				if (Child is IActuator Actuator)
					Result.AddRange(await Actuator.GetControlParameters());
			}

			return Result.ToArray();
		}

	}
}