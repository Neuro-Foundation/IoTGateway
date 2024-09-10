﻿using System;
using System.IO;
using System.Security.Cryptography;
using Waher.Runtime.Console;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Waher.Utility.DeleteDB
{
	/// <summary>
	/// Tool that helps you delete an object database created by the 
	/// <see cref="Waher.Persistence.Files"/> or 
	/// <see cref="Waher.Persistence.FilesLW libraries"/>, such as the IoT Gateway database, 
	/// including any cryptographic keys stored in the CSP.
	/// 
	/// Command line switches:
	/// 
	/// -d APP_DATA_FOLDER    Points to the application data folder.
	/// -e                    If encryption is used by the database.
	/// -?                    Help.
	/// </summary>
	class Program
	{
		static int Main(string[] args)
		{
			try
			{
				string ProgramDataFolder = null;
				string s;
				int i = 0;
				int c = args.Length;
				bool Help = false;
				bool Encryption = false;

				while (i < c)
				{
					s = args[i++].ToLower();

					switch (s)
					{
						case "-d":
							if (i >= c)
								throw new Exception("Missing program data folder.");

							if (string.IsNullOrEmpty(ProgramDataFolder))
								ProgramDataFolder = args[i++];
							else
								throw new Exception("Only one program data folder allowed.");
							break;

						case "-?":
							Help = true;
							break;

						case "-e":
							Encryption = true;
							break;

						default:
							throw new Exception("Unrecognized switch: " + s);
					}
				}

				if (Help || c == 0)
				{
					ConsoleOut.WriteLine("Tool that helps you delete an object database created by the");
					ConsoleOut.WriteLine("Waher.Persistence.Files or Waher.Persistence.FilesLW libraries,");
					ConsoleOut.WriteLine("such as the IoT Gateway database, including any cryptographic keys");
					ConsoleOut.WriteLine("stored in the CSP.");
					ConsoleOut.WriteLine();
					ConsoleOut.WriteLine("Command line switches:");
					ConsoleOut.WriteLine();
					ConsoleOut.WriteLine("-d APP_DATA_FOLDER    Points to the application data folder.");
					ConsoleOut.WriteLine("-e                    If encryption is used by the database.");
					ConsoleOut.WriteLine("-?                    Help.");
					return 0;
				}

				if (string.IsNullOrEmpty(ProgramDataFolder))
					throw new Exception("No program data folder set");

				if (!Directory.Exists(ProgramDataFolder))
					throw new Exception("Program data folder does not exist.");

				string[] Files = Directory.GetFiles(ProgramDataFolder, "*.*", SearchOption.AllDirectories);

				ConsoleOut.WriteLine(Files.Length + " file(s) will be deleted by this operation. Do you wish to continue? [y/n]");
				string Input = ConsoleIn.ReadLine();
				int NrDeleted = 0;
				
				if (Input.ToLower().StartsWith("y"))
				{
					foreach (string File in Files)
					{
						ConsoleOut.WriteLine("Deleting file " + File);

						if (Encryption)
						{
							CspParameters CspParameters = new()
							{
								Flags = CspProviderFlags.UseExistingKey | CspProviderFlags.UseMachineKeyStore,
								KeyContainerName = File
							};

							try
							{
								using RSACryptoServiceProvider RSA = new(CspParameters);
								
								RSA.PersistKeyInCsp = false;    // Deletes key.
								RSA.Clear();
							}
							catch (CryptographicException ex)
							{
								ConsoleOut.WriteLine("Unable to delete cryptographic key: " + ex.Message);
								continue;
							}
						}

						try
						{
							System.IO.File.Delete(File);
						}
						catch (Exception ex)
						{
							ConsoleOut.WriteLine("Unable to delete file: " + ex.Message);
							continue;
						}

						NrDeleted++;
					}

					ConsoleOut.WriteLine(NrDeleted + " file(s) deleted.");
				}

				return 0;
			}
			catch (Exception ex)
			{
				ConsoleOut.WriteLine(ex.Message);
				return -1;
			}
		}

	}
}

#pragma warning restore CA1416 // Validate platform compatibility
