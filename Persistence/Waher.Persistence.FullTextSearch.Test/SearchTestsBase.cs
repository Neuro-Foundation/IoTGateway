using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Persistence.FullTextSearch.Keywords;
using Waher.Persistence.FullTextSearch.Test.Classes;
using Waher.Runtime.Console;
using Waher.Runtime.Inventory;

namespace Waher.Persistence.FullTextSearch.Test
{
	public abstract class SearchTestsBase<InstanceType, AccessType>
		where InstanceType : class
		where AccessType : class, ITestClassAccess
	{
		private readonly string indexCollection;

		public SearchTestsBase(string IndexCollection)
		{
			this.indexCollection = IndexCollection;
		}

		public static async Task Initialize(string CollectioName, string IndexCollection)
		{
			await Clear(CollectioName, IndexCollection);
			await CreateDataset(IndexCollection);
		}

		private static async Task Clear(string CollectioName, string IndexCollection)
		{
			await Database.Clear(CollectioName);
			await Database.Clear("FullTextSearchObjects");

			await (await Database.GetDictionary(IndexCollection)).ClearAsync();
		}

		public static async Task CreateDataset(string IndexCollection)
		{
			TaskCompletionSource<bool> Done = new();
			int i, c = TokenReferences.MaxReferences * 5;
			int NrIndexed = 0;

			Task OnIndexed(object Sender, ObjectReferenceEventArgs e)
			{
				NrIndexed++;
				if (NrIndexed == c)
					Done.TrySetResult(true);

				return Task.CompletedTask;
			};

			Task _ = Task.Delay(10000).ContinueWith((_) => Done.TrySetResult(false));

			Search.ObjectAddedToIndex += OnIndexed;
			try
			{
				for (i = 0; i < c; i++)
				{
					switch (i % 5)
					{
						case 0:
							await IndexationTestsBase<InstanceType, AccessType>.CreateInstance(
								"Hello World number " + i.ToString() + ". This document is also a document that contains multiple references to the word 'word' and the word document.",
								"Kilroy was here.",
								"Clowns are fun.",
								"Testing indexation.");
							break;

						case 1:
							await IndexationTestsBase<InstanceType, AccessType>.CreateInstance(
								"Hello World number " + i.ToString(),
								"Fitzroy was here.",
								"Clowns are scary.",
								"Testing indexation.");
							break;

						case 2:
							await IndexationTestsBase<InstanceType, AccessType>.CreateInstance(
								"Hello World number " + i.ToString(),
								"Kilroy is a Clown.",
								"Clowns are fun.",
								"Testing indexation.");
							break;

						case 3:
							await IndexationTestsBase<InstanceType, AccessType>.CreateInstance(
								"Hello World number " + i.ToString(),
								"Fitzroy is not a Clown.",
								"Clowns are scary.",
								"Testing indexation.");
							break;

						case 4:
							await IndexationTestsBase<InstanceType, AccessType>.CreateInstance(
								"Hello World number " + i.ToString(),
								"Testing accents with Pel�.",
								"Clowns is the plural form of Clown.",
								"Testing indexation.");
							break;
					}
				}

				Assert.IsTrue(await Done.Task);
			}
			finally
			{
				Search.ObjectAddedToIndex -= OnIndexed;
			}

			IPersistentDictionary Index = await Database.GetDictionary(IndexCollection);

			foreach (string Key in Index.Keys)
				ConsoleOut.WriteLine(Key);
		}

		[TestMethod]
		public async Task Test_01_PlainSearch_1()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kilroy", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_02_PlainSearch_2()
		{
			InstanceType[] SearchResult = await this.DoSearch("Hello Clown Kilroy", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(500, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_03_Required()
		{
			InstanceType[] SearchResult = await this.DoSearch("Hello Clown +Kilroy", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_04_Prohibited()
		{
			InstanceType[] SearchResult = await this.DoSearch("Hello Clown -Fitzroy", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(300, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_05_Wildcard_1()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kil* -Clown", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_06_Wildcard_2()
		{
			InstanceType[] SearchResult = await this.DoSearch("*roy -Clown", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_07_Regex_1()
		{
			InstanceType[] SearchResult = await this.DoSearch("/Kil.*/ -Clown", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_08_Regex_2()
		{
			InstanceType[] SearchResult = await this.DoSearch("/.*roy/ -Clown", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_09_Accents()
		{
			InstanceType[] SearchResult = await this.DoSearch("Pele", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_10_PlainSearch_1_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kilroy", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_11_PlainSearch_2_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("Hello Clown Kilroy", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(500, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_12_Required_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("Hello Clown +Kilroy", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_13_Prohibited_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("Hello Clown -Fitzroy", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(300, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_14_Wildcard_1_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kil* -Clown", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_15_Wildcard_2_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("*roy -Clown", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_16_Regex_1_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("/Kil.*/ -Clown", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_17_Regex_2_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("/.*roy/ -Clown", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_18_Accents_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("Pele", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_19_AsPrefixes()
		{
			InstanceType[] SearchResult = await this.DoSearch("TEST", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_20_Sequence1()
		{
			InstanceType[] SearchResult = await this.DoSearch("'Kilroy was here'", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_21_Sequence2()
		{
			InstanceType[] SearchResult = await this.DoSearch("'Kilroy here was'", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(0, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_22_Sequence3()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kilroy 'was here'", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(300, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_23_Sequence4()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kilroy +'was here'", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_24_Sequence5()
		{
			InstanceType[] SearchResult = await this.DoSearch("+Kilroy +'was here'", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_25_Sequence6()
		{
			InstanceType[] SearchResult = await this.DoSearch("'Kilroy was' here", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(200, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_26_Sequence7()
		{
			InstanceType[] SearchResult = await this.DoSearch("+'Kilroy was' here", true);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_27_Regex_Range()
		{
			InstanceType[] SearchResult = await this.DoSearch("'number /[1-5]\\d/'", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(50, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_28_Regex_RecurringWords()
		{
			InstanceType[] SearchResult = await this.DoSearch("'word document'", false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(100, SearchResult.Length);
		}

		[TestMethod]
		public async Task Test_29_By_Relevance()
		{
			InstanceType[] SearchResult = await this.DoSearch("Fitzroy word document number /5\\d/",
				FullTextSearchOrder.Relevance, false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(500, SearchResult.Length);

			InstanceType? Last = null;
			Dictionary<string, uint>? LastCounts = null;
			AccessType Access = Types.Instantiate<AccessType>(false);

			foreach (InstanceType Current in SearchResult)
			{
				Dictionary<string, uint> CurrentCounts = await CountTokens(Current, "fitzroy", "word", "document", "number", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59");

				if (Last is not null && LastCounts is not null)
				{
					uint LastIndex = LastCounts[" NrTimes"];
					uint CurrentIndex = CurrentCounts[" NrTimes"];

					if (LastIndex < CurrentIndex)
						Assert.Fail("Relevance order failure on number of times tokens were found.");
					else if (LastIndex == CurrentIndex)
					{
						LastIndex = LastCounts[" NrTokens"];
						CurrentIndex = CurrentCounts[" NrTokens"];

						if (LastIndex < CurrentIndex)
							Assert.Fail("Relevance order failure on number of tokens found.");
						else if (LastIndex == CurrentIndex)
						{
							DateTime LastTP = Access.GetCreated(Last);
							DateTime CurrentTP = Access.GetCreated(Current);

							if (LastTP < CurrentTP)
								Assert.Fail("Relevance order failure on creation timestamp.");
						}
					}
				}

				Last = Current;
				LastCounts = CurrentCounts;
			}
		}

		[TestMethod]
		public async Task Test_30_By_Occurrences()
		{
			InstanceType[] SearchResult = await this.DoSearch("Fitzroy word document number /5\\d/",
				FullTextSearchOrder.Occurrences, false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(500, SearchResult.Length);

			InstanceType? Last = null;
			Dictionary<string, uint>? LastCounts = null;
			AccessType Access = Types.Instantiate<AccessType>(false);

			foreach (InstanceType Current in SearchResult)
			{
				Dictionary<string, uint> CurrentCounts = await CountTokens(Current, "fitzroy", "word", "document", "number", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59");

				if (Last is not null && LastCounts is not null)
				{
					uint LastIndex = LastCounts[" NrTokens"];
					uint CurrentIndex = CurrentCounts[" NrTokens"];

					if (LastIndex < CurrentIndex)
						Assert.Fail("Occurrences order failure on number of tokens found.");
					else if (LastIndex == CurrentIndex)
					{
						LastIndex = LastCounts[" NrTimes"];
						CurrentIndex = CurrentCounts[" NrTimes"];

						if (LastIndex < CurrentIndex)
							Assert.Fail("Occurrences order failure on number of times tokens were found.");
						else if (LastIndex == CurrentIndex)
						{
							DateTime LastTP = Access.GetCreated(Last);
							DateTime CurrentTP = Access.GetCreated(Current);

							if (LastTP < CurrentTP)
								Assert.Fail("Occurences order failure on creation timestamp.");
						}
					}
				}

				Last = Current;
				LastCounts = CurrentCounts;
			}
		}

		[TestMethod]
		public async Task Test_31_By_NewToOld()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kilroy word document number /5\\d/",
				FullTextSearchOrder.Newest, false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(500, SearchResult.Length);

			InstanceType? Last = null;
			AccessType Access = Types.Instantiate<AccessType>(false);

			foreach (InstanceType Current in SearchResult)
			{
				Dictionary<string, uint> CurrentCounts = await CountTokens(Current, "fitzroy", "word", "document", "number", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59");

				if (Last is not null)
				{
					DateTime LastTP = Access.GetCreated(Last);
					DateTime CurrentTP = Access.GetCreated(Current);

					if (LastTP < CurrentTP)
						Assert.Fail("NewToOld order failure on creation timestamp.");
				}

				Last = Current;
			}
		}

		[TestMethod]
		public async Task Test_32_By_NewToOld()
		{
			InstanceType[] SearchResult = await this.DoSearch("Kilroy word document number /5\\d/",
				FullTextSearchOrder.Oldest, false);

			Assert.IsNotNull(SearchResult);
			Assert.AreEqual(500, SearchResult.Length);

			InstanceType? Last = null;
			AccessType Access = Types.Instantiate<AccessType>(false);

			foreach (InstanceType Current in SearchResult)
			{
				Dictionary<string, uint> CurrentCounts = await CountTokens(Current, "fitzroy", "word", "document", "number", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59");

				if (Last is not null)
				{
					DateTime LastTP = Access.GetCreated(Last);
					DateTime CurrentTP = Access.GetCreated(Current);

					if (LastTP > CurrentTP)
						Assert.Fail("NewToOld order failure on creation timestamp.");
				}

				Last = Current;
			}
		}

		private static async Task<Dictionary<string, uint>> CountTokens(InstanceType Object, params string[] Tokens)
		{
			Dictionary<string, uint> Counts = new();
			uint NrTokens = 0;
			uint NrTimes = 0;

			foreach (string s in Tokens)
				Counts[s] = 0;

			TokenCount[] TokenCounts = await Search.Tokenize(Object);

			foreach (TokenCount TokenCount in TokenCounts)
			{
				if (Counts.TryGetValue(TokenCount.Token, out uint Nr))
				{
					uint c = (uint)TokenCount.DocIndex.Length;
					Counts[TokenCount.Token] = Nr + c;
					NrTokens++;
					NrTimes += c;
				}
			}

			Counts[" NrTokens"] = NrTokens;
			Counts[" NrTimes"] = NrTimes;

			return Counts;
		}

		private Task<InstanceType[]> DoSearch(string Query, bool TreatKeywordsAsPrefixes)
		{
			return this.DoSearch(Query, FullTextSearchOrder.Relevance, TreatKeywordsAsPrefixes);
		}

		private async Task<InstanceType[]> DoSearch(string Query, FullTextSearchOrder Order, bool TreatKeywordsAsPrefixes)
		{
			Keyword[] Keywords = Search.ParseKeywords(Query, TreatKeywordsAsPrefixes);
			InstanceType[] SearchResult = await Search.FullTextSearch<InstanceType>(this.indexCollection, 0, int.MaxValue,
				Order, PaginationStrategy.PaginateOverObjectsNullIfIncompatible, Keywords);
			List<InstanceType> Paginated = new();
			int Offset = 0;
			int MaxCount = 25;

			while (true)
			{
				InstanceType[] Page = await Search.FullTextSearch<InstanceType>(this.indexCollection, Offset, MaxCount,
					Order, PaginationStrategy.PaginateOverObjectsNullIfIncompatible, Keywords);

				Paginated.AddRange(Page);
				Offset += Page.Length;

				if (Page.Length < MaxCount)
					break;
			}

			Assert.AreEqual(SearchResult.Length, Paginated.Count);

			int i, c = SearchResult.Length;

			for (i = 0; i < c; i++)
				Assert.AreEqual(SearchResult[i], Paginated[i]);

			return SearchResult;
		}
	}
}