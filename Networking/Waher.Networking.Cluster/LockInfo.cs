﻿using System;
using System.Collections.Generic;
using System.Net;
using Waher.Events;

namespace Waher.Networking.Cluster
{
	internal class LockInfo
	{
		public string Resource;
		public bool Locked;
		public LinkedList<LockInfoRec> Queue = new LinkedList<LockInfoRec>();
	}

	internal class LockInfoRec
	{
		public LockInfo Info;
		public DateTime Timeout;
		public EventHandlerAsync<ClusterResourceLockEventArgs> Callback;
		public IPEndPoint LockedBy;
		public object State;
		public bool TimeoutScheduled = false;
	}
}
