﻿using System;
using System.Security.Cryptography;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Cryptography.Functions.Encryption
{
	/// <summary>
	/// AES Decryption
	/// </summary>
	public class Aes256Decrypt : FunctionMultiVariate
	{
		/// <summary>
		/// AES Decryption
		/// </summary>
		/// <param name="Content">Content to be decrypted.</param>
		/// <param name="Key">Key to use for decryption.</param>
		/// <param name="IV">Initiation Vector to use for decryption.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression.</param>
		public Aes256Decrypt(ScriptNode Content, ScriptNode Key, ScriptNode IV, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Content, Key, IV }, argumentTypes3Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// AES Decryption
		/// </summary>
		/// <param name="Content">Content to be decrypted.</param>
		/// <param name="Key">Key to use for decryption.</param>
		/// <param name="IV">Initiation Vector to use for decryption.</param>
		/// <param name="CipherMode">Cipher Mode</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression.</param>
		public Aes256Decrypt(ScriptNode Content, ScriptNode Key, ScriptNode IV, ScriptNode CipherMode, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Content, Key, IV, CipherMode }, argumentTypes4Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// AES Decryption
		/// </summary>
		/// <param name="Content">Content to be decrypted.</param>
		/// <param name="Key">Key to use for decryption.</param>
		/// <param name="IV">Initiation Vector to use for decryption.</param>
		/// <param name="CipherMode">Cipher Mode</param>
		/// <param name="PaddingMode">Padding mode</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression.</param>
		public Aes256Decrypt(ScriptNode Content, ScriptNode Key, ScriptNode IV, ScriptNode CipherMode, ScriptNode PaddingMode, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { Content, Key, IV, CipherMode, PaddingMode }, argumentTypes5Normal, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(Aes256Decrypt);

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Content", "Key", "IV", "CipherMode", "PaddingMode" };

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			byte[] Data = Arguments[0].AssociatedObjectValue as byte[];
			if (Data is null)
				throw new ScriptRuntimeException("Data to decrypt must be binary (i.e. an array of bytes).", this);

			byte[] Key = Arguments[1].AssociatedObjectValue as byte[];
			if (Key is null)
				throw new ScriptRuntimeException("Key to use for decryption must be binary (i.e. an array of bytes).", this);

			byte[] IV = Arguments[2].AssociatedObjectValue as byte[];
			if (IV is null)
				throw new ScriptRuntimeException("Initiation Vector to use for decryption must be binary (i.e. an array of bytes).", this);

			int c = Arguments.Length;
			CipherMode CipherMode = c <= 3 ? CipherMode.CBC : this.ToEnum<CipherMode>(Arguments[3]);
			PaddingMode PaddingMode = c <= 4 ? PaddingMode.PKCS7 : this.ToEnum<PaddingMode>(Arguments[4]);

			return new ObjectValue(Decrypt(Data, Key, IV, CipherMode, PaddingMode));
		}

		/// <summary>
		/// Performs AES decryption of data.
		/// </summary>
		/// <param name="Data">Binary Data</param>
		/// <param name="Key">Symmetric Key</param>
		/// <param name="IV">Initialization Vector</param>
		/// <param name="CipherMode">Cipher Mode</param>
		/// <param name="PaddingMode">Padding Mode</param>
		/// <returns>Encrypted data.</returns>
		public static byte[] Decrypt(byte[] Data, byte[] Key, byte[] IV, CipherMode CipherMode = CipherMode.CBC, PaddingMode PaddingMode = PaddingMode.PKCS7)
		{
			if (Data is null)
				throw new ArgumentNullException(nameof(Data));

			if (Key is null)
				throw new ArgumentNullException(nameof(Key));

			if (IV is null)
				throw new ArgumentNullException(nameof(IV));

			using (Aes Aes = Aes.Create())
			{
				Aes.BlockSize = 128;
				Aes.KeySize = 256;
				Aes.Mode = CipherMode;
				Aes.Padding = PaddingMode;

				using (ICryptoTransform Transform = Aes.CreateDecryptor(Key, IV))
				{
					return Transform.TransformFinalBlock(Data, 0, Data.Length);
				}
			}
		}
	}
}
