﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP.Control;
using Waher.Runtime.Inventory;
using Waher.Things.ControlParameters;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppControlTests : CommunicationTests
	{
		private ControlClient controlClient;
		private ControlServer controlServer;
		private bool b = false;
		private ColorReference cl = new(0, 0, 0);
		private DateTime d = DateTime.Today;
		private DateTime dt = DateTime.Now;
		private double db = 0;
		private Duration dr = Duration.Zero;
		private TypeCode e = TypeCode.Boolean;
		private int i = 0;
		private long l = 0;
		private string s = string.Empty;
		private TimeSpan t = TimeSpan.Zero;

		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext _)
		{
			Types.Initialize(
				typeof(XmppClient).Assembly,
				typeof(InternetContent).Assembly,
				typeof(BOSH.HttpBinding).Assembly,
				typeof(WebSocket.WebSocketBinding).Assembly,
				typeof(P2P.EndpointSecurity).Assembly);
		}

		[ClassInitialize]
		public static void ClassInitialize(TestContext _)
		{
			SetupSnifferAndLog();
		}

		[ClassCleanup]
		public static async Task ClassCleanup()
		{
			await DisposeSnifferAndLog();
		}

		public override async Task ConnectClients()
		{
			await base.ConnectClients();

			Assert.AreEqual(XmppState.Connected, this.client1.State);
			Assert.AreEqual(XmppState.Connected, this.client2.State);

			this.controlClient = new ControlClient(this.client1);
			this.controlServer = new ControlServer(this.client2,
				new BooleanControlParameter("Bool", "Page1", "Bool:", "Boolean value",
					(sender) => Task.FromResult<bool?>(this.b),
					(sender, value) => { this.b = value; return Task.CompletedTask; }),
				new ColorControlParameter("Color", "Page1", "Color:", "Color value",
					(sender) => Task.FromResult<ColorReference>(this.cl),
					(sender, value) => { this.cl = value; return Task.CompletedTask; }),
				new DateControlParameter("Date", "Page1", "Date:", "Date value", DateTime.MinValue, DateTime.MaxValue,
					(sender) => Task.FromResult<DateTime?>(this.d),
					(sender, value) => { this.d = value; return Task.CompletedTask; }),
				new DateTimeControlParameter("DateTime", "Page1", "DateTime:", "DateTime value", DateTime.MinValue, DateTime.MaxValue,
					(sender) => Task.FromResult<DateTime?>(this.dt),
					(sender, value) => { this.dt = value; return Task.CompletedTask; }),
				new DoubleControlParameter("Double", "Page1", "Double:", "Double value",
					(sender) => Task.FromResult<double?>(this.db),
					(sender, value) => { this.db = value; return Task.CompletedTask; }),
				new DurationControlParameter("Duration", "Page1", "Duration:", "Duration value",
					(sender) => Task.FromResult<Duration>(this.dr),
					(sender, value) => { this.dr = value; return Task.CompletedTask; }),
				new EnumControlParameter("Enum", "Page1", "Enum:", "Enum value", typeof(TypeCode),
					(sender) => Task.FromResult<Enum>(this.e),
					(sender, value) => { this.e = (TypeCode)value; return Task.CompletedTask; }),
				new Int32ControlParameter("Int32", "Page1", "Int32:", "Int32 value",
					(sender) => Task.FromResult<int?>(this.i),
					(sender, value) => { this.i = value; return Task.CompletedTask; }),
				new Int64ControlParameter("Int64", "Page1", "Int64:", "Int64 value",
					(sender) => Task.FromResult<long?>(this.l),
					(sender, value) => { this.l = value; return Task.CompletedTask; }),
				new StringControlParameter("String", "Page1", "String:", "String value",
					(sender) => Task.FromResult<string>(this.s),
					(sender, value) => { this.s = value; return Task.CompletedTask; }),
				new TimeControlParameter("Time", "Page1", "Time:", "Time value",
					(sender) => Task.FromResult<TimeSpan?>(this.t),
					(sender, value) => { this.t = value; return Task.CompletedTask; }));
		}

		public override Task DisposeClients()
		{
			if (this.controlServer is not null)
			{
				this.controlServer.Dispose();
				this.controlServer = null;
			}

			if (this.controlClient is not null)
			{
				this.controlClient.Dispose();
				this.controlClient = null;
			}

			return base.DisposeClients();
		}

		[TestMethod]
		public async Task Control_Test_01_Bool()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.b = false;

				await this.controlClient.Set(this.client2.FullJID, "Bool", true, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();

					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(true, this.b);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_02_Color()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.cl = new ColorReference(0, 0, 0);

				await this.controlClient.Set(this.client2.FullJID, "Color", new ColorReference(1, 2, 3), (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(1, this.cl.Red);
				Assert.AreEqual(2, this.cl.Green);
				Assert.AreEqual(3, this.cl.Blue);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_03_Date()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.d = DateTime.MinValue;

				await this.controlClient.Set(this.client2.FullJID, "Date", DateTime.Today, true, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(DateTime.Today, this.d);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_04_DateTime()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				DateTime Now = DateTime.Now;
				this.dt = DateTime.MinValue;

				Now = new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, Now.Second, Now.Millisecond);

				await this.controlClient.Set(this.client2.FullJID, "DateTime", Now, false, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(Now.ToUniversalTime().Ticks, this.dt.ToUniversalTime().Ticks);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_05_Double()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.db = 0;

				await this.controlClient.Set(this.client2.FullJID, "Double", 3.1415927, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(3.1415927, this.db);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_06_Duration()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.dr = Duration.Zero;

				await this.controlClient.Set(this.client2.FullJID, "Duration", new Duration(true, 1, 2, 3, 4, 5, 6), (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(new Duration(true, 1, 2, 3, 4, 5, 6), this.dr);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_07_Enum()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.e = TypeCode.Boolean;

				await this.controlClient.Set(this.client2.FullJID, "Enum", TypeCode.Int16, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(TypeCode.Int16, this.e);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_08_Int32()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.i = 0;

				await this.controlClient.Set(this.client2.FullJID, "Int32", int.MinValue, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(int.MinValue, this.i);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_09_Int64()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.l = 0;

				await this.controlClient.Set(this.client2.FullJID, "Int64", long.MinValue, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(long.MinValue, this.l);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_10_String()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.s = string.Empty;

				await this.controlClient.Set(this.client2.FullJID, "String", "ABC", (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual("ABC", this.s);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task Control_Test_11_Time()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				TimeSpan Time = DateTime.Now.TimeOfDay;
				this.t = TimeSpan.Zero;

				await this.controlClient.Set(this.client2.FullJID, "Time", Time, (Sender, e) =>
				{
					if (e.Ok)
						Done.Set();
					else
						Error.Set();
				
					return Task.CompletedTask;
				}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Configuration not performed correctly");
				Assert.AreEqual(Time, this.t);
			}
			finally
			{
				await this.DisposeClients();
			}
		}

	}
}
