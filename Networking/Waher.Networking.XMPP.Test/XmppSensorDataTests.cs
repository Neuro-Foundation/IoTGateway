﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP.Sensor;
using Waher.Runtime.Console;
using Waher.Things;
using Waher.Things.SensorData;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppSensorDataTests : CommunicationTests
	{
		private SensorClient sensorClient;
		private SensorServer sensorServer;
		private double temp;

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

			this.sensorClient = new SensorClient(this.client1);
			this.sensorServer = new SensorServer(this.client2, true);

			this.temp = 12.3;

			this.sensorServer.OnExecuteReadoutRequest += async (Sender, e) =>
			{
				DateTime Now = DateTime.Now;

				await e.ReportFields(true,
					new QuantityField(ThingReference.Empty, Now, "Temperature", this.temp, 1, "C", FieldType.Momentary, FieldQoS.AutomaticReadout),
					new BooleanField(ThingReference.Empty, Now, "Bool", true, FieldType.Momentary, FieldQoS.AutomaticReadout),
					new DateField(ThingReference.Empty, Now, "Date", DateTime.Today, FieldType.Momentary, FieldQoS.AutomaticReadout),
					new DateTimeField(ThingReference.Empty, Now, "DateTime", Now, FieldType.Momentary, FieldQoS.AutomaticReadout),
					new DurationField(ThingReference.Empty, Now, "Duration", new Duration(true, 1, 2, 3, 4, 5, 6), FieldType.Momentary, FieldQoS.AutomaticReadout),
					new EnumField(ThingReference.Empty, Now, "Enum", TypeCode.Boolean, FieldType.Momentary, FieldQoS.AutomaticReadout),
					new Int32Field(ThingReference.Empty, Now, "Int32", int.MinValue, FieldType.Momentary, FieldQoS.AutomaticReadout),
					new Int64Field(ThingReference.Empty, Now, "Int64", long.MinValue, FieldType.Momentary, FieldQoS.AutomaticReadout),
					new StringField(ThingReference.Empty, Now, "String", "Hello world.", FieldType.Momentary, FieldQoS.AutomaticReadout),
					new TimeField(ThingReference.Empty, Now, "Time", Now.TimeOfDay, FieldType.Momentary, FieldQoS.AutomaticReadout));
			};
		}

		public override Task DisposeClients()
		{
			if (this.sensorServer is not null)
			{
				this.sensorServer.Dispose();
				this.sensorServer = null;
			}

			if (this.sensorClient is not null)
			{
				this.sensorClient.Dispose();
				this.sensorClient = null;
			}

			return base.DisposeClients();
		}

		[TestMethod]
		public async Task SensorData_Test_01_ReadAll()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);
				IEnumerable<Field> Fields = null;

				SensorDataClientRequest Request = await this.sensorClient.RequestReadout(this.client2.FullJID, FieldType.All);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Error.Set();
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Done.Set();
					return Task.CompletedTask;
				};

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 20000), "Readout not performed correctly");

				foreach (Field Field in Fields)
					ConsoleOut.WriteLine(Field.ToString());
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task SensorData_Test_02_Subscribe_MaxInterval()
		{
			await this.ConnectClients();
			try
			{
				TaskCompletionSource<bool> Result = new();
				IEnumerable<Field> Fields = null;

				SensorDataSubscriptionRequest Request = await this.sensorClient.Subscribe(this.client2.FullJID, FieldType.All,
					Duration.Parse("PT1S"), Duration.Parse("PT5S"), false);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Result.TrySetResult(false);
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Result.TrySetResult(true);
					return Task.CompletedTask;
				};

				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Task _ = Task.Delay(10000).ContinueWith((_) => Result.TrySetException(new TimeoutException()));

				Assert.IsTrue(await Result.Task, "Subscription not performed correctly");

				foreach (Field Field in Fields)
					ConsoleOut.WriteLine(Field.ToString());
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task SensorData_Test_03_Subscribe_ChangeBy()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);
				IEnumerable<Field> Fields = null;

				SensorDataSubscriptionRequest Request = await this.sensorClient.Subscribe(this.client2.FullJID, FieldType.All,
					new FieldSubscriptionRule[]
					{
						new("Temperature", this.temp, 1)
					},
					Duration.Parse("PT1S"), Duration.Parse("PT5S"), false);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Error.Set();
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Done.Set();
					return Task.CompletedTask;
				};

				this.temp += 0.5;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Thread.Sleep(2000);

				this.temp += 0.5;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Subscription not performed correctly");

				foreach (Field Field in Fields)
					ConsoleOut.WriteLine(Field.ToString());

				Done.Reset();

				this.temp -= 1;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Subscription not performed correctly");

				foreach (Field Field in Fields)
					ConsoleOut.WriteLine(Field.ToString());
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task SensorData_Test_04_Subscribe_ChangeUp()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);
				IEnumerable<Field> Fields = null;

				SensorDataSubscriptionRequest Request = await this.sensorClient.Subscribe(this.client2.FullJID, FieldType.All,
					new FieldSubscriptionRule[]
					{
						new("Temperature", this.temp, 1, null)
					},
					Duration.Parse("PT1S"), Duration.Parse("PT5S"), false);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Error.Set();
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Done.Set();
					return Task.CompletedTask;
				};

				this.temp -= 1;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Thread.Sleep(2000);

				this.temp += 2;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Subscription not performed correctly");

				foreach (Field Field in Fields)
					ConsoleOut.WriteLine(Field.ToString());
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task SensorData_Test_05_Subscribe_ChangeDown()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);
				IEnumerable<Field> Fields = null;

				SensorDataSubscriptionRequest Request = await this.sensorClient.Subscribe(this.client2.FullJID, FieldType.All,
					new FieldSubscriptionRule[]
					{
						new("Temperature", this.temp, 1, null)
					},
					Duration.Parse("PT1S"), Duration.Parse("PT5S"), false);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Error.Set();
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Done.Set();
					return Task.CompletedTask;
				};

				this.temp += 1;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Thread.Sleep(2000);

				this.temp -= 2;
				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Subscription not performed correctly");

				foreach (Field Field in Fields)
					ConsoleOut.WriteLine(Field.ToString());
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task SensorData_Test_06_Subscribe_MinInterval()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);
				IEnumerable<Field> Fields = null;

				SensorDataSubscriptionRequest Request = await this.sensorClient.Subscribe(this.client2.FullJID, FieldType.All,
					new FieldSubscriptionRule[]
					{
						new("Temperature", this.temp, 1, null)
					},
					Duration.Parse("PT1S"), Duration.Parse("PT5S"), false);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Error.Set();
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Done.Set();
					return Task.CompletedTask;
				};

				int Count = 6;
				DateTime Start = DateTime.Now;

				while (Count > 0)
				{
					this.temp += 1;
					await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
						FieldType.Momentary, FieldQoS.AutomaticReadout));

					this.temp -= 1;
					await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
						FieldType.Momentary, FieldQoS.AutomaticReadout));

					switch (WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 100))
					{
						case 0:
							Done.Reset();
							Count--;
							break;

						case 1:
							Assert.Fail("Subscription not performed correctly");
							break;
					}
				}

				TimeSpan Elapsed = DateTime.Now - Start;
				Assert.IsTrue(Elapsed > new TimeSpan(0, 0, 5));
			}
			finally
			{
				await this.DisposeClients();
			}
		}

		[TestMethod]
		public async Task SensorData_Test_07_Unsubscribe()
		{
			await this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);
				IEnumerable<Field> Fields = null;

				SensorDataSubscriptionRequest Request = await this.sensorClient.Subscribe(this.client2.FullJID, FieldType.All,
					Duration.Parse("PT1S"), Duration.Parse("PT5S"), false);
				Request.OnStateChanged += (sender, NewState) =>
				{
					ConsoleOut.WriteLine(NewState.ToString());
					return Task.CompletedTask;
				};
				Request.OnErrorsReceived += (sender, Errors) =>
				{
					Error.Set();
					return Task.CompletedTask;
				};
				Request.OnFieldsReceived += (sender, NewFields) =>
				{
					Fields = NewFields;
					Done.Set();
					return Task.CompletedTask;
				};

				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Subscription not performed correctly");

				Done.Reset();
				await Request.Unsubscribe();

				await this.sensorServer.NewMomentaryValues(new QuantityField(ThingReference.Empty, DateTime.Now, "Temperature", this.temp, 1, "C",
					FieldType.Momentary, FieldQoS.AutomaticReadout));

				Assert.AreEqual(WaitHandle.WaitTimeout, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 10000), "Unsubscription not performed correctly");
			}
			finally
			{
				await this.DisposeClients();
			}
		}
	}
}
