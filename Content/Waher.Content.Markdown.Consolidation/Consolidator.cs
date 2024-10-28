﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SkiaSharp;
using Waher.Events;
using Waher.Runtime.Threading;
using Waher.Script;
using Waher.Script.Graphs;

namespace Waher.Content.Markdown.Consolidation
{
	/// <summary>
	/// Consolidates Markdown from multiple sources, sharing the same thread.
	/// </summary>
	public class Consolidator : IConsolidator
	{
		private readonly string threadId;
		private readonly SortedDictionary<string, SourceState> sources = new SortedDictionary<string, SourceState>();
		private readonly MultiReadSingleWriteObject synchObject = new MultiReadSingleWriteObject();
		private readonly int maxPaletteSize;
		private Dictionary<string, KeyValuePair<SKColor, int>> legend = null;
		private DocumentType type = DocumentType.Empty;
		private SKColor[] palette = null;
		private object tag = null;
		private int nrTop = 0;
		private int nrBottom = 0;
		private bool filterSources = false;

		/// <summary>
		/// Consolidates Markdown from multiple sources, sharing the same thread.
		/// </summary>
		/// <param name="ThreadId">Thread ID</param>
		/// <param name="MaxPaletteSize">Maximum PaletteSize</param>
		public Consolidator(string ThreadId, int MaxPaletteSize)
		{
			this.threadId = ThreadId;
			this.maxPaletteSize = MaxPaletteSize;
		}

		/// <summary>
		/// Thread ID
		/// </summary>
		public string ThreadId => this.threadId;

		/// <summary>
		/// Consolidated sources.
		/// </summary>
		public async Task<string[]> GetSources()
		{
			await this.synchObject.BeginRead();
			try
			{
				string[] Result = new string[this.sources.Count];
				this.sources.Keys.CopyTo(Result, 0);
				return Result;
			}
			finally
			{
				await this.synchObject.EndRead();
			}
		}

		/// <summary>
		/// Number of sources that have reported content.
		/// </summary>
		public async Task<int> GetNrReportedSources()
		{
			await this.synchObject.BeginRead();
			try
			{
				int Result = 0;

				foreach (SourceState State in this.sources.Values)
				{
					if (!State.IsDefault)
						Result++;
				}

				return Result;
			}
			finally
			{
				await this.synchObject.EndRead();
			}
		}

		/// <summary>
		/// If input should be restricted to a defined set of sources.
		/// </summary>
		public bool FilterSources
		{
			get => this.filterSources;
			set => this.filterSources = value;
		}

		/// <summary>
		/// External tag object that can be tagged to the object by its owner.
		/// </summary>
		public object Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		/// <summary>
		/// Adds incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Markdown">Markdown document.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> Add(string Source, MarkdownDocument Markdown)
		{
			return this.Add(Source, Markdown, string.Empty);
		}

		/// <summary>
		/// Adds incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Markdown">Markdown document.</param>
		/// <param name="Id">Optional ID of document.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> Add(string Source, MarkdownDocument Markdown, string Id)
		{
			return this.Add(Source, Markdown, Id, false, false);
		}

		/// <summary>
		/// Adds incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Text">Text input.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> Add(string Source, string Text)
		{
			return this.Add(Source, Text, string.Empty);
		}

		/// <summary>
		/// Adds incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Text">Text input.</param>
		/// <param name="Id">Optional ID of document.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> Add(string Source, string Text, string Id)
		{
			return this.Add(Source, Text, Id, false, false);
		}

		/// <summary>
		/// Updates incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Markdown">Markdown document.</param>
		/// <param name="Id">Optional ID of document.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> Update(string Source, MarkdownDocument Markdown, string Id)
		{
			return this.Add(Source, Markdown, Id, true, false);
		}

		/// <summary>
		/// Updates incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Text">Text input.</param>
		/// <param name="Id">Optional ID of document.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> Update(string Source, string Text, string Id)
		{
			return this.Add(Source, Text, Id, true, false);
		}

		/// <summary>
		/// Adds incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Text">Text input.</param>
		/// <param name="Id">Optional ID of document.</param>
		/// <param name="Update">If a document should be updated.</param>
		/// <param name="IsDefault">If the content is default content (true), or reported content (false).</param>
		/// <returns>If the source is new.</returns>
		private async Task<bool> Add(string Source, string Text, string Id, bool Update, bool IsDefault)
		{
			MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Text);
			return await this.Add(Source, Doc, Id, Update, IsDefault);
		}

		/// <summary>
		/// Adds default markdown to present, until a proper response is returned.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Text">Text input.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> AddDefault(string Source, string Text)
		{
			return this.Add(Source, Text, string.Empty, false, true);
		}

		/// <summary>
		/// Adds default markdown to present, until a proper response is returned.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Markdown">Markdown document.</param>
		/// <returns>If the source is new.</returns>
		public Task<bool> AddDefault(string Source, MarkdownDocument Markdown)
		{
			return this.Add(Source, Markdown, string.Empty, false, true);
		}

		/// <summary>
		/// Adds incoming markdown information.
		/// </summary>
		/// <param name="Source">Source of information.</param>
		/// <param name="Markdown">Markdown document.</param>
		/// <param name="Id">Optional ID of document.</param>
		/// <param name="Update">If a document should be updated.</param>
		/// <param name="IsDefault">If the content is default content (true), or reported content (false).</param>
		/// <returns>If the source is new.</returns>
		private async Task<bool> Add(string Source, MarkdownDocument Markdown, string Id, bool Update, bool IsDefault)
		{
			DocumentType Type;
			bool Result;

			await this.synchObject.BeginWrite();
			try
			{
				if ((Result = !this.sources.TryGetValue(Source, out SourceState State)) || State.IsDefault)
				{
					if (Result && this.filterSources)
						return false;

					State = new SourceState(Source, IsDefault);
					this.sources[Source] = State;
				}

				if (Markdown is null)
					return false;

				if (Update)
					Type = await State.Update(Markdown, Id);
				else
					Type = await State.Add(Markdown, Id);

				if ((int)(this.type & Type) != 0)
					this.type = (DocumentType)Math.Max((int)this.type, (int)Type);
				else
					this.type = DocumentType.Complex;

				this.nrTop = 0;
				this.nrBottom = 0;

				switch (this.type)
				{
					case DocumentType.SingleCode:
						int i, c, d, d0 = 0;
						bool First = true;
						string[] Rows0 = null;
						string[] Rows;

						foreach (SourceState Info in this.sources.Values)
						{
							Rows = Info.FirstDocument.Rows;
							d = Rows.Length;

							if (First)
							{
								First = false;
								this.nrTop = this.nrBottom = d;
								Rows0 = Rows;
								d0 = d;
							}
							else
							{
								c = Math.Min(this.nrTop, d);

								for (i = 0; i < c; i++)
								{
									if (Rows[i] != Rows0[i])
										break;
								}

								this.nrTop = i;

								c = Math.Min(this.nrBottom, d);

								for (i = 0; i < c; i++)
								{
									if (Rows[d - i - 1] != Rows0[d0 - i - 1])
										break;
								}

								this.nrBottom = i;
							}
						}

						if (this.nrTop < 1 || this.nrBottom <= 1)
							this.type = DocumentType.Complex;
						break;

					case DocumentType.SingleXml:
						if (this.sources.Count >= 2)
							this.type = DocumentType.Complex;
						break;
				}
			}
			finally
			{
				await this.synchObject.EndWrite();
			}

			if (Update)
				await this.Raise(this.Updated, Source);
			else
				await this.Raise(this.Added, Source);

			return Result;
		}

		private async Task Raise(SourceEventHandler Handler, string Source)
		{
			if (!(Handler is null))
			{
				try
				{
					await Handler(this, new SourceEventArgs(Source));
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when content from a source has been added.
		/// </summary>
		public event SourceEventHandler Added = null;

		/// <summary>
		/// Event raised when content from a source has been updated.
		/// </summary>
		public event SourceEventHandler Updated = null;

		/// <summary>
		/// Generates consolidated markdown from all sources.
		/// </summary>
		/// <returns>Consolidated markdown.</returns>
		[Obsolete("Use GenerateMarkdownAsync() instead.")]
		public string GenerateMarkdown()
		{
			return this.GenerateMarkdownAsync().Result;
		}

		/// <summary>
		/// Generates consolidated markdown from all sources.
		/// </summary>
		/// <returns>Consolidated markdown.</returns>
		public async Task<string> GenerateMarkdownAsync()
		{
			StringBuilder Markdown = new StringBuilder();

			await this.synchObject.BeginRead();
			try
			{
				switch (this.type)
				{
					case DocumentType.Empty:
						return string.Empty;

					case DocumentType.SingleNumber:
					case DocumentType.SingleLine:

						Markdown.AppendLine("| Nr | Source | Response |");

						if (this.type == DocumentType.SingleNumber)
							Markdown.AppendLine("|---:|:-------|-------:|");
						else
							Markdown.AppendLine("|---:|:-------|:-------|");

						int Nr = 0;

						foreach (KeyValuePair<string, SourceState> P in this.sources)
						{
							Markdown.Append("| ");
							Markdown.Append((++Nr).ToString());
							Markdown.Append(" | `");
							Markdown.Append(P.Key);
							Markdown.Append("` | ");
							Markdown.Append((await P.Value.GetFirstText()).Trim());
							Markdown.AppendLine(" |");
						}
						break;

					case DocumentType.SingleParagraph:

						Markdown.AppendLine("| Nr | Source | Response |");
						Markdown.AppendLine("|---:|:-------|:-------|");

						Nr = 0;

						foreach (KeyValuePair<string, SourceState> P in this.sources)
						{
							Markdown.Append("| ");
							Markdown.Append((++Nr).ToString());
							Markdown.Append(" | `");
							Markdown.Append(P.Key);
							Markdown.Append("` | ");

							foreach (string Row in P.Value.FirstDocument.Rows)
							{
								Markdown.Append(Row);
								Markdown.Append("<br/>");
							}

							Markdown.AppendLine("|");
						}
						break;

					case DocumentType.SingleCode:
					case DocumentType.SingleXml:

						List<string> ConsolidatedRows = new List<string>();
						int j = 0;
						int d = this.sources.Count;

						foreach (KeyValuePair<string, SourceState> P in this.sources)
						{
							string[] Rows = P.Value.FirstDocument.Rows;
							int i = 0;
							int c = Rows.Length;

							j++;
							if (j > 1)
								i += this.nrTop;

							if (j < d)
								c -= this.nrBottom;

							for (; i < c; i++)
								ConsolidatedRows.Add(Rows[i]);
						}

						if (ConsolidatedRows[0].StartsWith("```dot", StringComparison.OrdinalIgnoreCase))
							OptimizeDotEdges(ConsolidatedRows);

						foreach (string Row in ConsolidatedRows)
							Markdown.AppendLine(Row);
						break;

					case DocumentType.SingleTable:

						ConsolidatedTable Table = null;

						try
						{
							foreach (KeyValuePair<string, SourceState> P in this.sources)
							{
								foreach (DocumentInformation Doc in P.Value.Documents)
								{
									if (!(Doc?.Table is null))
									{
										if (Table is null)
											Table = await ConsolidatedTable.CreateAsync(P.Key, Doc.Table);
										else
											await Table.Add(P.Key, Doc.Table);
									}
								}
							}

							Table?.Export(Markdown);
						}
						catch (Exception ex)
						{
							Log.Exception(ex);
							this.GenerateComplexLocked(Markdown);
						}
						break;

					case DocumentType.SingleGraph:
						Graph G = null;

						try
						{
							int i;

							if (this.legend is null)
								this.legend = new Dictionary<string, KeyValuePair<SKColor, int>>();

							if (this.palette is null)
								this.palette = CreatePalette(this.maxPaletteSize);

							foreach (KeyValuePair<string, SourceState> P in this.sources)
							{
								foreach (DocumentInformation Doc in P.Value.Documents)
								{
									if (!(Doc?.Graph is null))
									{
										i = this.legend.Count % this.maxPaletteSize;
										if (Doc.Graph.TrySetDefaultColor(this.palette[i]))
											this.legend[P.Key] = new KeyValuePair<SKColor, int>(this.palette[i], i);

										if (G is null)
											G = Doc.Graph;
										else
											G = (Graph)G.AddRightElementWise(Doc.Graph);
									}
								}
							}

							Markdown.AppendLine("```Graph");
							G.ToXml(Markdown);
							Markdown.AppendLine();
							Markdown.AppendLine("```");

							if (this.legend.Count > 0)
							{
								string[] Labels = new string[this.legend.Count];
								SKColor[] Colors = new SKColor[this.legend.Count];
								bool First = true;

								Markdown.AppendLine();
								Markdown.Append("{{Legend([");

								foreach (KeyValuePair<string, KeyValuePair<SKColor, int>> P in this.legend)
								{
									if (First)
										First = false;
									else
										Markdown.Append(',');

									Markdown.Append(Expression.ToString(P.Key));
								}

								Markdown.Append("],[");
								First = true;

								foreach (KeyValuePair<string, KeyValuePair<SKColor, int>> P in this.legend)
								{
									if (First)
										First = false;
									else
										Markdown.Append(',');

									Markdown.Append(Graph.ToRGBAStyle(P.Value.Key));
								}

								Markdown.AppendLine("],4)}}");
							}

							Markdown.AppendLine();
						}
						catch (Exception ex)
						{
							Log.Exception(ex);
							this.GenerateComplexLocked(Markdown);
						}
						break;

					case DocumentType.Complex:
					default:
						this.GenerateComplexLocked(Markdown);
						break;
				}
			}
			finally
			{
				await this.synchObject.EndRead();
			}

			return Markdown.ToString();
		}

		private static readonly Regex dotEdge = new Regex("^(?'A'(\"[^\"]*\"|'[^']*'|[^\\s]*))\\s*->\\s*(?'B'(\"[^\"]*\"|'[^']*'|[^\\s]*))\\s*([\\[](?'Note'[^\\]]*)[\\]]\\s*)?;\\s*$", RegexOptions.Singleline | RegexOptions.Compiled);

		private static void OptimizeDotEdges(List<string> Rows)
		{
			Match[] Matches;
			Match M;
			string Row, Row2, A, B, Note;
			int i, j, c = Rows.Count;
			bool Changed = false;

			Matches = new Match[c];

			for (i = 0; i < c; i++)
			{
				Row = Rows[i];
				M = Matches[i];
				if (M is null)
				{
					M = dotEdge.Match(Row);
					Matches[i] = M;
				}

				if (!M.Success || M.Index > 0 || M.Length < Row.Length)
					continue;

				A = M.Groups["A"].Value;
				B = M.Groups["B"].Value;
				Note = M.Groups["Note"].Value;

				for (j = i + 1; j < c; j++)
				{
					Row2 = Rows[j];
					M = Matches[j];
					if (M is null)
					{
						M = dotEdge.Match(Row2);
						Matches[j] = M;
					}

					if (M.Success &&
						M.Index == 0 &&
						M.Length == Row.Length &&
						M.Groups["A"].Value == B &&
						M.Groups["B"].Value == A &&
						M.Groups["Note"].Value == Note)
					{
						StringBuilder sb = new StringBuilder();

						sb.Append(A);
						sb.Append(" -> ");
						sb.Append(B);
						sb.Append(" [");
						sb.Append(Note);

						if (!string.IsNullOrEmpty(Note))
							sb.Append(", ");

						sb.Append("dir=both];");

						Rows[i] = sb.ToString();
						Matches[i] = null;
						Rows[j] = string.Empty;
						Matches[j] = null;
						Changed = true;
						break;
					}
				}
			}

			if (Changed)
			{
				for (i = 0; i < c;)
				{
					if (string.IsNullOrEmpty(Rows[i]))
					{
						Rows.RemoveAt(i);
						c--;
					}
					else
						i++;
				}
			}
		}

		/// <summary>
		/// Creates a palette for graphs.
		/// </summary>
		/// <param name="N">Number of colors in palette.</param>
		/// <returns>Palette</returns>
		public static SKColor[] CreatePalette(int N)
		{
			SKColor[] Result = new SKColor[N];
			double d = 360.0 / Math.Max(N, 12);
			int i;

			for (i = 0; i < N; i++)
				Result[i] = SKColor.FromHsl((float)(d * i), 100, 75);

			return Result;
		}

		private void GenerateComplexLocked(StringBuilder Markdown)
		{
			foreach (KeyValuePair<string, SourceState> P in this.sources)
			{
				Markdown.Append('`');
				Markdown.Append(P.Key);
				Markdown.AppendLine("`");
				Markdown.AppendLine();

				DocumentInformation[] Info = P.Value.Documents;

				if (Info.Length == 0)
					Markdown.AppendLine(":\t");
				else
				{
					bool First = true;

					foreach (DocumentInformation Doc in P.Value.Documents)
					{
						if (First)
							First = false;
						else
							Markdown.AppendLine(":\t");

						if (!(Doc?.Rows is null))
						{
							foreach (string Row in Doc.Rows)
							{
								Markdown.Append(":\t");
								Markdown.AppendLine(Row);
							}
						}
					}
				}

				Markdown.AppendLine();
			}
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			EventHandler h = this.Disposed;
			if (!(h is null))
			{
				try
				{
					h(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when consolidator has been disposed.
		/// </summary>
		public event EventHandler Disposed = null;
	}
}
