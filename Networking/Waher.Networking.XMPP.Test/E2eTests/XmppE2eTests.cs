﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.P2P.SymmetricCiphers;

namespace Waher.Networking.XMPP.Test.E2eTests
{
	public abstract class XmppE2eTests : E2eTests
	{
		[TestMethod]
		public void Test_01_Message_AES()
		{
			this.Test_Message(
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) });
		}

		[TestMethod]
		public void Test_02_Message_ChaCha20()
		{
			this.Test_Message(
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) });
		}

		[TestMethod]
		public void Test_03_Message_AEAD_ChaCha20_Poly1305()
		{
			this.Test_Message(
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) });
		}

		private void Test_Message(IE2eEndpoint[] Endpoints1, IE2eEndpoint[] Endpoints2)
		{
			this.endpoints1 = Endpoints1;
			this.endpoints2 = Endpoints2;

			this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.client2.OnNormalMessage += (sender, e) =>
				{
					if (e.UsesE2eEncryption && e.Body == "Test message" && e.Subject == "Subject" && e.Id == "1")
						Done.Set();
					else
						Error.Set();

					return Task.CompletedTask;
				};

				this.endpointSecurity1.SendMessage(this.client1, E2ETransmission.AssertE2E,
					QoSLevel.Unacknowledged, MessageType.Normal, "1", this.client2.FullJID,
					"<test/>", "Test message", "Subject", "en", string.Empty, string.Empty,
					null, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 2000));
			}
			finally
			{
				this.endpointSecurity1?.Dispose();
				this.endpointSecurity2?.Dispose();

				this.DisposeClients();
			}
		}

		[TestMethod]
		public void Test_04_IQ_Get_AES()
		{
			this.Test_IQ_Get(
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) });
		}

		[TestMethod]
		public void Test_05_IQ_Get_ChaCha20()
		{
			this.Test_IQ_Get(
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) });
		}

		[TestMethod]
		public void Test_06_IQ_Get_AEAD_ChaCha20_Poly1305()
		{
			this.Test_IQ_Get(
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) });
		}

		private void Test_IQ_Get(IE2eEndpoint[] Endpoints1, IE2eEndpoint[] Endpoints2)
		{
			this.Test_IQ_Get(Endpoints1, Endpoints2, "Hello", "Hello", true);
		}

		private void Test_IQ_Get(IE2eEndpoint[] Endpoints1, IE2eEndpoint[] Endpoints2,
			string Send, string Check, bool ExpectOk)
		{
			this.endpoints1 = Endpoints1;
			this.endpoints2 = Endpoints2;

			this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.client2.RegisterIqGetHandler("test", "testns", (sender, e) =>
				{
					if (e.UsesE2eEncryption &&
						e.E2eEncryption is not null &&
						!string.IsNullOrEmpty(e.E2eReference) &&
						e.E2eSymmetricCipher is not null &&
						e.Query.InnerText == Check)
					{
						e.IqResult("<test xmlns='testns'>World</test>");
					}
					else
						throw new StanzaErrors.BadRequestException("Bad request", e.IQ);

					return Task.CompletedTask;
				}, true);

				this.endpointSecurity1.SendIqGet(this.client1, E2ETransmission.AssertE2E,
					this.client2.FullJID, "<test xmlns='testns'>" + Send + "</test>", (sender, e) =>
					{
						if (e.UsesE2eEncryption &&
							e.E2eEncryption is not null &&
							!string.IsNullOrEmpty(e.E2eReference) &&
							e.E2eSymmetricCipher is not null &&
							e.Ok == ExpectOk &&
							(!ExpectOk || (e.FirstElement is not null &&
							e.FirstElement.LocalName == "test" &&
							e.FirstElement.NamespaceURI == "testns" &&
							e.FirstElement.InnerText == "World")))
						{
							Done.Set();
						}
						else
							Error.Set();

						return Task.CompletedTask;
					}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 2000));
			}
			finally
			{
				this.endpointSecurity1?.Dispose();
				this.endpointSecurity2?.Dispose();

				this.DisposeClients();
			}
		}

		[TestMethod]
		public void Test_07_IQ_Set_AES()
		{
			this.Test_IQ_Set(
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) });
		}

		[TestMethod]
		public void Test_08_IQ_Set_ChaCha20()
		{
			this.Test_IQ_Set(
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) });
		}

		[TestMethod]
		public void Test_09_IQ_Set_AEAD_ChaCha20_Poly1305()
		{
			this.Test_IQ_Set(
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) });
		}

		private void Test_IQ_Set(IE2eEndpoint[] Endpoints1, IE2eEndpoint[] Endpoints2)
		{
			this.endpoints1 = Endpoints1;
			this.endpoints2 = Endpoints2;

			this.ConnectClients();
			try
			{
				ManualResetEvent Done = new(false);
				ManualResetEvent Error = new(false);

				this.client2.RegisterIqSetHandler("test", "testns", (sender, e) =>
				{
					if (e.UsesE2eEncryption &&
						e.E2eEncryption is not null &&
						!string.IsNullOrEmpty(e.E2eReference) &&
						e.E2eSymmetricCipher is not null &&
						e.Query.InnerText == "Hello")
					{
						e.IqResult("<test xmlns='testns'>World</test>");
					}
					else
						throw new StanzaErrors.BadRequestException("Bad request", e.IQ);

					return Task.CompletedTask;
				}, true);

				this.endpointSecurity1.SendIqSet(this.client1, E2ETransmission.AssertE2E,
					this.client2.FullJID, "<test xmlns='testns'>Hello</test>", (sender, e) =>
					{
						if (e.E2eEncryption is not null &&
							!string.IsNullOrEmpty(e.E2eReference) &&
							e.E2eSymmetricCipher is not null &&
							e.Ok &&
							e.FirstElement is not null &&
							e.FirstElement.LocalName == "test" &&
							e.FirstElement.NamespaceURI == "testns" &&
							e.FirstElement.InnerText == "World")
						{
							Done.Set();
						}
						else
							Error.Set();

						return Task.CompletedTask;
					}, null);

				Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 2000));
			}
			finally
			{
				this.endpointSecurity1?.Dispose();
				this.endpointSecurity2?.Dispose();

				this.DisposeClients();
			}
		}

		[TestMethod]
		public void Test_10_IQ_Error_AES()
		{
			this.Test_IQ_Error(
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new Aes256()) });
		}

		[TestMethod]
		public void Test_11_IQ_Error_ChaCha20()
		{
			this.Test_IQ_Error(
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new ChaCha20()) });
		}

		[TestMethod]
		public void Test_12_IQ_Error_AEAD_ChaCha20_Poly1305()
		{
			this.Test_IQ_Error(
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) },
				new IE2eEndpoint[] { this.GenerateEndpoint(new AeadChaCha20Poly1305()) });
		}

		private void Test_IQ_Error(IE2eEndpoint[] Endpoints1, IE2eEndpoint[] Endpoints2)
		{
			this.Test_IQ_Get(Endpoints1, Endpoints2, "Hello", "Bye", false);
		}

		[TestMethod]
		public void Test_13_Binary_AES()
		{
			Test_Binary(this.GenerateEndpoint(new Aes256()),
				this.GenerateEndpoint(new Aes256()));
		}

		[TestMethod]
		public void Test_14_Binary_ChaCha20()
		{
			Test_Binary(this.GenerateEndpoint(new ChaCha20()),
				this.GenerateEndpoint(new ChaCha20()));
		}

		[TestMethod]
		public void Test_15_Binary_AEAD_ChaCha20_Poly1305()
		{
			Test_Binary(this.GenerateEndpoint(new AeadChaCha20Poly1305()),
				this.GenerateEndpoint(new AeadChaCha20Poly1305()));
		}

		private static void Test_Binary(IE2eEndpoint Endpoint1, IE2eEndpoint Endpoint2)
		{
			byte[] Data = new byte[1024];
			using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
			{
				Rnd.GetBytes(Data);
			}

			byte[] Encrypted = Endpoint1.DefaultSymmetricCipher.Encrypt(
				"ID", "Type", "From", "To", 1, Data, Endpoint1, Endpoint2);
			byte[] Decrypted = Endpoint2.DefaultSymmetricCipher.Decrypt(
				"ID", "Type", "From", "To", Encrypted, Endpoint1, Endpoint2);

			Assert.IsNotNull(Decrypted, "Decryption failed.");

			int i, c = Data.Length;
			Assert.AreEqual(c, Decrypted.Length, "Length mismatch.");

			for (i = 0; i < c; i++)
				Assert.AreEqual(Data[i], Decrypted[i], "Encryption/Decryption failed.");
		}

		[TestMethod]
		public Task Test_16_Stream_AES()
		{
			return Test_Stream(this.GenerateEndpoint(new Aes256()),
				this.GenerateEndpoint(new Aes256()));
		}

		[TestMethod]
		public Task Test_17_Stream_ChaCha20()
		{
			return Test_Stream(this.GenerateEndpoint(new ChaCha20()),
				this.GenerateEndpoint(new ChaCha20()));
		}

		[TestMethod]
		public Task Test_18_Stream_AEAD_ChaCha20_Poly1305()
		{
			return Test_Stream(this.GenerateEndpoint(new AeadChaCha20Poly1305()),
				this.GenerateEndpoint(new AeadChaCha20Poly1305()));
		}

		private static async Task Test_Stream(IE2eEndpoint Endpoint1, IE2eEndpoint Endpoint2)
		{
			MemoryStream Data = new();
			byte[] Temp = new byte[1024];
			byte[] Temp2 = new byte[1024];
			int i;

			using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
			{
				for (i = 0; i < 1024; i++)
				{
					Rnd.GetBytes(Temp);
					Data.Write(Temp, 0, Temp.Length);
				}
			}

			MemoryStream Encrypted = new();

			Data.Position = 0;
			await Endpoint1.DefaultSymmetricCipher.Encrypt(
				"ID", "Type", "From", "To", 1, Data, Encrypted, Endpoint1, Endpoint2);

			Encrypted.Position = 0;
			Stream Decrypted = await Endpoint2.DefaultSymmetricCipher.Decrypt(
				"ID", "Type", "From", "To", Encrypted, Endpoint1, Endpoint2);

			Assert.IsNotNull(Decrypted, "Decryption failed.");

			long c = Data.Length;
			Assert.AreEqual(c, Decrypted.Length, "Length mismatch.");

			Decrypted.Position = 0;
			Data.Position = 0;

			while (true)
			{
				i = await Data.ReadAsync(Temp, 0, Temp.Length);
				Assert.AreEqual(i, await Decrypted.ReadAsync(Temp2, 0, Temp2.Length));

				if (i <= 0)
					break;

				while (i > 0)
				{
					i--;
					Assert.AreEqual(Temp[i], Temp2[i], "Encryption/Decryption failed.");
				}
			}
		}

	}
}
