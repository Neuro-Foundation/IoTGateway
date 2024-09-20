﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SkiaSharp;
using Waher.Content.Images;
using Waher.Content.Markdown.Contracts;
using Waher.Content.Markdown.Latex;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.CodeContent;
using Waher.Content.Markdown.Rendering;
using Waher.Content.Markdown.Wpf;
using Waher.Content.Markdown.Xamarin;
using Waher.Content.SystemFiles;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Runtime.Inventory;
using Waher.Runtime.Timing;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Security;

namespace Waher.Content.Markdown.GraphViz
{
	internal enum ResultType
	{
		Svg,
		Png
	}

	internal class GraphInfo
	{
		public string FileName;
		public string Title;
		public string MapFileName;
		public string Hash;
	}

	/// <summary>
	/// Class managing GraphViz integration into Markdown documents.
	/// </summary>
	public class GraphViz : IImageCodeContent, ICodeContentMarkdownRenderer, ICodeContentHtmlRenderer, ICodeContentTextRenderer,
		ICodeContentContractsRenderer, ICodeContentLatexRenderer, ICodeContentWpfXamlRenderer, ICodeContentXamarinFormsXamlRenderer
	{
		private static readonly Random rnd = new Random();
		private static Scheduler scheduler = null;
		private static string installationFolder = null;
		private static string binFolder = null;
		private static string graphVizFolder = null;
		private static string contentRootFolder = null;
		private static bool supportsDot = false;
		private static bool supportsNeato = false;
		private static bool supportsFdp = false;
		private static bool supportsSfdp = false;
		private static bool supportsTwopi = false;
		private static bool supportsCirco = false;
		private static IMarkdownAsynchronousOutput asyncHtmlOutput = null;

		/// <summary>
		/// Class managing GraphViz integration into Markdown documents.
		/// </summary>
		public GraphViz()
		{
		}

		/// <summary>
		/// Initializes the GraphViz-Markdown integration.
		/// </summary>
		/// <param name="ContentRootFolder">Content root folder. If hosting markdown under a web server, this would correspond
		/// to the roof folder for the web content.</param>
		public static void Init(string ContentRootFolder)
		{
			try
			{
				contentRootFolder = ContentRootFolder;

				if (scheduler is null)
				{
					if (Types.TryGetModuleParameter("Scheduler", out object Obj) && Obj is Scheduler Scheduler)
						scheduler = Scheduler;
					else
					{
						scheduler = new Scheduler();

						Log.Terminating += (sender, e) =>
						{
							scheduler?.Dispose();
							scheduler = null;
						};
					}
				}

				string Folder = SearchForInstallationFolder();

				if (string.IsNullOrEmpty(Folder))
					Log.Warning("GraphViz not found. GraphViz support will not be available in Markdown.");
				else
				{
					SetInstallationFolder(Folder);

					Log.Informational("GraphViz found. Integration with Markdown added.",
						new KeyValuePair<string, object>("Installation Folder", installationFolder),
						new KeyValuePair<string, object>("Binary Folder", binFolder),
						new KeyValuePair<string, object>("dot", supportsDot),
						new KeyValuePair<string, object>("neato", supportsNeato),
						new KeyValuePair<string, object>("fdp", supportsFdp),
						new KeyValuePair<string, object>("sfdp", supportsSfdp),
						new KeyValuePair<string, object>("twopi", supportsTwopi),
						new KeyValuePair<string, object>("circo", supportsCirco));

					asyncHtmlOutput = Types.FindBest<IMarkdownAsynchronousOutput, MarkdownOutputType>(MarkdownOutputType.Html);
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}
		}

		/// <summary>
		/// Sets the installation folder of GraphViz.
		/// </summary>
		/// <param name="Folder">Installation folder.</param>
		/// <exception cref="Exception">If trying to set the installation folder to a different folder than the one set previously.
		/// The folder can only be set once, for security reasons.</exception>
		public static void SetInstallationFolder(string Folder)
		{
			if (!string.IsNullOrEmpty(installationFolder) && Folder != installationFolder)
				throw new Exception("GraphViz installation folder has already been set.");

			string Suffix = FileSystem.ExecutableExtension;

			installationFolder = Folder;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.Win32NT:
				case PlatformID.WinCE:
				default:
					binFolder = Path.Combine(installationFolder, "bin");
					break;

				case PlatformID.Unix:
				case PlatformID.MacOSX:
					binFolder = installationFolder;
					break;
			}

			supportsDot = File.Exists(Path.Combine(binFolder, "dot" + Suffix));
			supportsNeato = File.Exists(Path.Combine(binFolder, "neato" + Suffix));
			supportsFdp = File.Exists(Path.Combine(binFolder, "fdp" + Suffix));
			supportsSfdp = File.Exists(Path.Combine(binFolder, "sfdp" + Suffix));
			supportsTwopi = File.Exists(Path.Combine(binFolder, "twopi" + Suffix));
			supportsCirco = File.Exists(Path.Combine(binFolder, "circo" + Suffix));

			graphVizFolder = Path.Combine(contentRootFolder, "GraphViz");

			if (!Directory.Exists(graphVizFolder))
				Directory.CreateDirectory(graphVizFolder);

			DeleteOldFiles(TimeSpan.FromDays(7));
		}

		private static void DeleteOldFiles(object P)
		{
			if (P is TimeSpan MaxAge)
				DeleteOldFiles(MaxAge, true);
		}

		/// <summary>
		/// Deletes generated files older than <paramref name="MaxAge"/>.
		/// </summary>
		/// <param name="MaxAge">Age limit.</param>
		/// <param name="Reschedule">If rescheduling should be done.</param>
		public static void DeleteOldFiles(TimeSpan MaxAge, bool Reschedule)
		{
			if (string.IsNullOrEmpty(graphVizFolder))
				return;

			DateTime Limit = DateTime.Now - MaxAge;
			int Count = 0;

			DirectoryInfo GraphVizFolder = new DirectoryInfo(graphVizFolder);
			FileInfo[] Files = GraphVizFolder.GetFiles("*.*");

			foreach (FileInfo FileInfo in Files)
			{
				if (FileInfo.LastAccessTime < Limit)
				{
					try
					{
						File.Delete(FileInfo.FullName);
						Count++;
					}
					catch (Exception ex)
					{
						Log.Error("Unable to delete old file: " + ex.Message, FileInfo.FullName);
					}
				}
			}

			if (Count > 0)
				Log.Informational(Count.ToString() + " old file(s) deleted.", graphVizFolder);

			if (Reschedule)
			{
				lock (rnd)
				{
					scheduler.Add(DateTime.Now.AddDays(rnd.NextDouble() * 2), DeleteOldFiles, MaxAge);
				}
			}
		}

		/// <summary>
		/// Searches for the installation folder on the local machine.
		/// </summary>
		/// <returns>Installation folder, if found, null otherwise.</returns>
		public static string SearchForInstallationFolder()
		{
			string InstallationFolder;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.Win32NT:
				case PlatformID.WinCE:
				default:
					InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.ProgramFilesX86);
					if (string.IsNullOrEmpty(InstallationFolder))
					{
						InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.ProgramFiles);
						if (string.IsNullOrEmpty(InstallationFolder))
						{
							InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.Programs);
							if (string.IsNullOrEmpty(InstallationFolder))
							{
								InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.CommonProgramFilesX86);
								if (string.IsNullOrEmpty(InstallationFolder))
								{
									InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.CommonProgramFiles);
									if (string.IsNullOrEmpty(InstallationFolder))
										InstallationFolder = SearchForInstallationFolder(Environment.SpecialFolder.CommonPrograms);
								}
							}
						}
					}
					break;

				case PlatformID.Unix:
				case PlatformID.MacOSX:
					InstallationFolder = "/opt/local/bin";
					if (!Directory.Exists(InstallationFolder))
						InstallationFolder = null;
					break;
			}

			return InstallationFolder;
		}

		private static string SearchForInstallationFolder(Environment.SpecialFolder SpecialFolder)
		{
			string Folder;

			try
			{
				Folder = Environment.GetFolderPath(SpecialFolder);
			}
			catch (Exception)
			{
				return null; // Folder not defined for the operating system.
			}

			string Result = SearchForInstallationFolder(Folder);

			if (string.IsNullOrEmpty(Result) && Types.TryGetModuleParameter("Runtime", out object Obj) && Obj is string RuntimeFolder)
				Result = SearchForInstallationFolder(Path.Combine(RuntimeFolder, SpecialFolder.ToString()));

			return Result;
		}

		private static string SearchForInstallationFolder(string Folder)
		{
			if (string.IsNullOrEmpty(Folder))
				return null;

			if (!Directory.Exists(Folder))
				return null;

			string FolderName;
			string BestFolder = null;
			double BestVersion = 0;
			string[] SubFolders;

			try
			{
				SubFolders = Directory.GetDirectories(Folder);
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return null;
			}

			foreach (string SubFolder in SubFolders)
			{
				FolderName = Path.GetFileName(SubFolder);
				if (!FolderName.StartsWith("Graphviz", StringComparison.CurrentCultureIgnoreCase))
					continue;

				if (!CommonTypes.TryParse(FolderName.Substring(8), out double Version))
					Version = 1.0;

				if (BestFolder is null || Version > BestVersion)
				{
					BestFolder = SubFolder;
					BestVersion = Version;
				}
			}

			return BestFolder;
		}

		/// <summary>
		/// Checks how well the handler supports multimedia content of a given type.
		/// </summary>
		/// <param name="Language">Language.</param>
		/// <returns>How well the handler supports the content.</returns>
		public Grade Supports(string Language)
		{
			int i = Language.IndexOf(':');
			if (i > 0)
				Language = Language.Substring(0, i).TrimEnd();

			switch (Language.ToLower())
			{
				case "dot":
					if (supportsDot)
						return Grade.Excellent;
					break;

				case "neato":
					if (supportsNeato)
						return Grade.Excellent;
					break;

				case "fdp":
					if (supportsFdp)
						return Grade.Excellent;
					break;

				case "sfdp":
					if (supportsSfdp)
						return Grade.Excellent;
					break;

				case "twopi":
					if (supportsTwopi)
						return Grade.Excellent;
					break;

				case "circo":
					if (supportsCirco)
						return Grade.Excellent;
					break;
			}

			return Grade.NotAtAll;
		}

		/// <summary>
		/// Is called on the object when an instance of the element has been created in a document.
		/// </summary>
		/// <param name="Document">Document containing the instance.</param>
		public void Register(MarkdownDocument Document)
		{
			// Do nothing.
		}

		/// <summary>
		/// Generates HTML for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderHtml(HtmlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Svg, asyncHtmlOutput is null, Document.Settings?.Variables);
			if (!(Info is null))
			{
				await this.GenerateHTML(Renderer.Output, Info);
				return true;
			}

			string Title;
			int i = Language.IndexOf(':');
			if (i > 0)
				Title = Language.Substring(i + 1).Trim();
			else
				Title = null;

			string Id = await asyncHtmlOutput.GenerateStub(MarkdownOutputType.Html, Renderer.Output, Title, Document);

			Document.QueueAsyncTask(this.ExecuteGraphViz, new AsyncState()
			{
				Id = Id,
				Language = Language,
				Rows = Rows,
				Document = Document
			});

			return true;
		}

		private class AsyncState
		{
			public string Id;
			public string Language;
			public string[] Rows;
			public MarkdownDocument Document;
		}

		private async Task ExecuteGraphViz(object State)
		{
			AsyncState AsyncState = (AsyncState)State;
			StringBuilder Output = new StringBuilder();

			try
			{
				GraphInfo Info = await GetFileName(AsyncState.Language, AsyncState.Rows, ResultType.Svg, true,
					AsyncState.Document.Settings?.Variables);

				if (!(Info is null))
					await this.GenerateHTML(Output, Info);
			}
			catch (Exception ex)
			{
				using (HtmlRenderer Renderer = new HtmlRenderer(Output, new HtmlSettings()
				{
					XmlEntitiesOnly = true
				}))
				{
					await Renderer.RenderObject(ex, true, new Variables());
				}
			}

			await asyncHtmlOutput.ReportResult(MarkdownOutputType.Html, AsyncState.Id, Output.ToString());
		}

		private async Task GenerateHTML(StringBuilder Output, GraphInfo Info)
		{
			Info.FileName = Info.FileName.Substring(contentRootFolder.Length).Replace(Path.DirectorySeparatorChar, '/');
			if (!Info.FileName.StartsWith("/"))
				Info.FileName = "/" + Info.FileName;

			Output.Append("<figure>");
			Output.Append("<img src=\"");
			Output.Append(XML.HtmlAttributeEncode(Info.FileName));

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("\" alt=\"");
				Output.Append(XML.HtmlAttributeEncode(Info.Title));

				Output.Append("\" title=\"");
				Output.Append(XML.HtmlAttributeEncode(Info.Title));
			}
			else
				Output.Append("\" alt=\"GraphViz graph");

			if (!string.IsNullOrEmpty(Info.MapFileName))
			{
				Output.Append("\" usemap=\"#Map");
				Output.Append(Info.Hash);
			}

			Output.Append("\" class=\"aloneUnsized\"/>");

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("<figcaption>");
				Output.Append(XML.HtmlValueEncode(Info.Title));
				Output.Append("</figcaption>");
			}

			Output.AppendLine("</figure>");

			if (!string.IsNullOrEmpty(Info.MapFileName))
			{
				Output.Append("<map id=\"Map");
				Output.Append(Info.Hash);
				Output.Append("\" name=\"Map");
				Output.Append(Info.Hash);
				Output.AppendLine("\">");

				string Map = await Resources.ReadAllTextAsync(Info.MapFileName);
				string[] MapRows = Map.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);
				int i, c;

				for (i = 1, c = MapRows.Length - 1; i < c; i++)
					Output.AppendLine(MapRows[i]);

				Output.AppendLine("</map>");
			}
		}

		internal static Task<GraphInfo> GetFileName(string Language, string[] Rows, ResultType Type, bool GenerateIfNotExists, Variables Variables)
		{
			return GetFileName(Language, MarkdownDocument.AppendRows(Rows), Type, GenerateIfNotExists, Variables);
		}

		internal static async Task<GraphInfo> GetFileName(string Language, string GraphText, ResultType Type, bool GenerateIfNotExists, Variables Variables)
		{
			GraphInfo Result = new GraphInfo();
			int i = Language.IndexOf(':');

			if (i > 0)
			{
				Result.Title = Language.Substring(i + 1).Trim();
				Language = Language.Substring(0, i).TrimEnd();
			}
			else
				Result.Title = string.Empty;

			string GraphBgColor = GetColor(Graph.GraphBgColorVariableName, Variables);
			string GraphFgColor = GetColor(Graph.GraphFgColorVariableName, Variables);

			Result.Hash = Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(GraphText + Language + GraphBgColor + GraphFgColor));

			string GraphVizFolder = Path.Combine(contentRootFolder, "GraphViz");
			string FileName = Path.Combine(GraphVizFolder, Result.Hash);

			switch (Type)
			{
				case ResultType.Svg:
				default:
					Result.FileName = FileName + "." + ImageCodec.FileExtensionSvg;
					break;

				case ResultType.Png:
					Result.FileName = FileName + "." + ImageCodec.FileExtensionPng;
					break;
			}

			Result.MapFileName = FileName + ".map";

			if (File.Exists(Result.FileName))
			{
				if (!File.Exists(Result.MapFileName))
					Result.MapFileName = null;

				return Result;
			}

			if (!GenerateIfNotExists)
				return null;

			string TxtFileName = FileName + ".txt";
			await Resources.WriteAllTextAsync(TxtFileName, GraphText, Encoding.Default);

			StringBuilder Arguments = new StringBuilder();

			Arguments.Append("-Tcmapx -o\"");
			Arguments.Append(Result.MapFileName);
			Arguments.Append("\" -T");
			Arguments.Append(Type.ToString().ToLower());

			if (!string.IsNullOrEmpty(GraphBgColor))
			{
				Arguments.Append(" -Gbgcolor=\"");
				Arguments.Append(GraphBgColor);
				Arguments.Append('"');
			}

			if (!string.IsNullOrEmpty(GraphFgColor))
			{
				Arguments.Append(" -Gcolor=\"");
				Arguments.Append(GraphFgColor);
				//Arguments.Append("\" -Nfillcolor=\"");
				//Arguments.Append(defaultFgColor);
				Arguments.Append("\" -Nfontcolor=\"");
				Arguments.Append(GraphFgColor);
				Arguments.Append("\" -Nlabelfontcolor=\"");
				Arguments.Append(GraphFgColor);
				Arguments.Append("\" -Npencolor=\"");
				Arguments.Append(GraphFgColor);
				Arguments.Append("\" -Efontcolor=\"");
				Arguments.Append(GraphFgColor);
				Arguments.Append("\" -Elabelfontcolor=\"");
				Arguments.Append(GraphFgColor);
				Arguments.Append("\" -Epencolor=\"");
				Arguments.Append(GraphFgColor);
				Arguments.Append("\"");
			}

			Arguments.Append(" -q -o\"");
			Arguments.Append(Result.FileName);
			Arguments.Append("\" \"");
			Arguments.Append(TxtFileName + "\"");

			ProcessStartInfo ProcessInformation = new ProcessStartInfo()
			{
				FileName = Path.Combine(binFolder, Language.ToLower() + FileSystem.ExecutableExtension),
				Arguments = Arguments.ToString(),
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = false,
				WorkingDirectory = GraphVizFolder,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			Process P = new Process();
			TaskCompletionSource<GraphInfo> ResultSource = new TaskCompletionSource<GraphInfo>();

			P.ErrorDataReceived += (sender, e) =>
			{
				ResultSource.TrySetException(new Exception("Unable to generate graph:\r\n\r\n" + e.Data));
			};

			P.Exited += async (sender, e) =>
			{
				try
				{
					if (P.ExitCode != 0)
					{
						string ErrorText = await P.StandardError.ReadToEndAsync();
						ResultSource.TrySetException(new Exception("Unable to generate graph. Exit code: " + P.ExitCode.ToString() + "\r\n\r\n" + ErrorText));
					}
					else
					{
						string Map = await Resources.ReadAllTextAsync(Result.MapFileName);
						string[] MapRows = Map.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);
						if (MapRows.Length <= 2)
						{
							File.Delete(Result.MapFileName);
							Result.MapFileName = null;
						}

						ResultSource.TrySetResult(Result);
					}
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			};

			Task _ = Task.Delay(10000).ContinueWith(Prev => ResultSource.TrySetException(new TimeoutException("GraphViz process did not terminate properly.")));

			P.StartInfo = ProcessInformation;
			P.EnableRaisingEvents = true;
			P.Start();

			return await ResultSource.Task;
		}

		private static string GetColor(string VariableName, Variables Variables)
		{
			if (Variables is null)
				return null;

			if (!Variables.TryGetVariable(VariableName, out Variable v))
				return null;

			if (v.ValueObject is SKColor Color)
				return Graph.ToRGBAStyle(Color);
			else if (v.ValueObject is string s)
				return s;
			else
				return null;
		}

		/// <summary>
		/// Generates plain text for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderText(TextRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Svg, true, Document.Settings?.Variables);
			if (Info is null)
				return false;

			Renderer.Output.AppendLine(Info.Title);

			return true;
		}

		/// <summary>
		/// Generates Markdown for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderMarkdown(MarkdownRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Png, true, Document.Settings?.Variables);
			if (Info is null)
				return false;

			return await ImageContent.GenerateMarkdownFromFile(Renderer.Output, Info.FileName, Info.Title);
		}

		/// <summary>
		/// Generates WPF XAML for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderWpfXaml(WpfXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Png, true, Document.Settings?.Variables);
			if (Info is null)
				return false;

			XmlWriter Output = Renderer.XmlOutput;

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Info.FileName);
			Output.WriteAttributeString("Stretch", "None");

			if (!string.IsNullOrEmpty(Info.Title))
				Output.WriteAttributeString("ToolTip", Info.Title);

			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Png, true, Document.Settings?.Variables);
			if (Info is null)
				return false;

			XmlWriter Output = Renderer.XmlOutput;

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Info.FileName);
			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates LaTeX for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderLatex(LatexRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Png, true, Document.Settings?.Variables);
			StringBuilder Output = Renderer.Output;

			Output.AppendLine("\\begin{figure}[h]");
			Output.AppendLine("\\centering");

			Output.Append("\\fbox{\\includegraphics{");
			Output.Append(Info.FileName.Replace('\\', '/'));
			Output.AppendLine("}}");

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("\\caption{");
				Output.Append(LatexRenderer.EscapeLaTeX(Info.Title));
				Output.AppendLine("}");
			}

			Output.AppendLine("\\end{figure}");
			Output.AppendLine();

			return true;
		}

		/// <summary>
		/// Generates an image of the contents.
		/// </summary>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>Image, if successful, null otherwise.</returns>
		public async Task<PixelInformation> GenerateImage(string[] Rows, string Language, MarkdownDocument Document)
		{
			GraphInfo Info = await GetFileName(Language, Rows, ResultType.Png, true, Document.Settings?.Variables);
			if (Info is null)
				return null;

			byte[] Data = await Resources.ReadAllBytesAsync(Info.FileName);

			using (SKBitmap Bitmap = SKBitmap.Decode(Data))
			{
				return new PixelInformationPng(Data, Bitmap.Width, Bitmap.Height);
			}
		}

		/// <summary>
		/// Generates smart contract XML for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public async Task<bool> RenderContractXml(ContractsRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			try
			{
				GraphInfo Info = await GetFileName(Language, Rows, ResultType.Png, true, Document.Settings?.Variables);
				if (Info is null)
					return false;

				byte[] Data = await Resources.ReadAllBytesAsync(Info.FileName);
				string ContentType = ImageCodec.ContentTypePng;

				if (!(await InternetContent.DecodeAsync(ContentType, Data, null) is SKImage Image))
					return false;

				XmlWriter Output = Renderer.XmlOutput;
				int Width = Image.Width;
				int Height = Image.Height;

				Output.WriteStartElement("imageStandalone");

				Output.WriteAttributeString("contentType", ContentType);
				Output.WriteAttributeString("width", Width.ToString());
				Output.WriteAttributeString("height", Height.ToString());

				Output.WriteStartElement("binary");
				Output.WriteValue(Convert.ToBase64String(Data));
				Output.WriteEndElement();

				Output.WriteStartElement("caption");
				if (string.IsNullOrEmpty(Info.Title))
					Output.WriteElementString("text", "Graph");
				else
					Output.WriteElementString("text", Info.Title);

				Output.WriteEndElement();
				Output.WriteEndElement();

				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}
	}
}
