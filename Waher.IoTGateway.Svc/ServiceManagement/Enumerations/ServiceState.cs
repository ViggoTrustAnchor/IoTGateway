﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.IoTGateway.Svc.ServiceManagement.Enumerations
{
	public enum ServiceState : uint
	{
		Stopped = 0x00000001,
		StartPending = 0x00000002,
		StopPending = 0x00000003,
		Running = 0x00000004,
		ContinuePending = 0x00000005,
		PausePending = 0x00000006,
		Paused = 0x00000007
	}
}
