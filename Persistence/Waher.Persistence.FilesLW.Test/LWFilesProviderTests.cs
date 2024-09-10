﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Console;

#if !LW
using Waher.Persistence.Files.Test.Classes;

namespace Waher.Persistence.Files.Test
#else
using Waher.Persistence.Files;
using Waher.Persistence.FilesLW.Test.Classes;

namespace Waher.Persistence.FilesLW.Test
#endif
{
	[TestClass]
	public class DBFilesProviderTests
	{
		internal const int BlocksInCache = 10000;

		protected FilesProvider provider;
		private ObjectBTreeFile file;

		[TestInitialize]
		public async Task TestInitialize()
		{
			DBFilesBTreeTests.DeleteFiles();

#if LW
			this.provider = await FilesProvider.CreateAsync("Data", DBFilesBTreeTests.CollectionName, 8192, BlocksInCache, 8192, Encoding.UTF8, 10000);
#else
			this.provider = await FilesProvider.CreateAsync("Data", DBFilesBTreeTests.CollectionName, 8192, BlocksInCache, 8192, Encoding.UTF8, 10000, true);
#endif
			this.file = await this.provider.GetFile("Default");
		}

		[TestCleanup]
		public void TestCleanup()
		{
			if (this.provider is not null)
			{
				this.provider.Dispose();
				this.provider = null;
				this.file = null;
			}
		}

		[TestMethod]
		public async Task DBFiles_Provider_01_ByReference()
		{
			ByReference Obj = new()
			{
				Default = DBFilesBTreeTests.CreateDefault(100),
				Simple = DBFilesBTreeTests.CreateSimple(100)
			};

			await this.provider.Insert(Obj);

			ObjectBTreeFile File = await this.provider.GetFile("Default");
			await DBFilesBTreeTests.AssertConsistent(File, this.provider, 3, Obj, true);
			ConsoleOut.WriteLine(await DBFilesBTreeTests.ExportXML(File, "Data\\BTree.xml", false));

			Assert.AreNotEqual(Guid.Empty, Obj.ObjectId);
			Assert.AreNotEqual(Guid.Empty, Obj.Default.ObjectId);
			Assert.AreNotEqual(Guid.Empty, Obj.Simple.ObjectId);

			ByReference Obj2 = await this.provider.TryLoadObject<ByReference>(Obj.ObjectId);
			Assert.IsNotNull(Obj2);

			DBFilesObjectSerializationTests.AssertEqual(Obj2.Default, Obj.Default);
			DBFilesObjectSerializationTests.AssertEqual(Obj2.Simple, Obj.Simple);
		}

		// TODO: Solve deadlocks.
		// TODO: Multi-threaded stress test (with multiple indices).
		// TOOO: Test huge databases with more than uint.MaxValue objects.
		// TODO: Startup: Scan file if not shut down correctly. Rebuild in case file is corrupt
	}
}
