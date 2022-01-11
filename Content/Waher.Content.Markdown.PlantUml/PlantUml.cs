﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SkiaSharp;
using Waher.Content.Markdown.Model;
using Waher.Content.SystemFiles;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Runtime.Inventory;
using Waher.Runtime.Timing;
using Waher.Script.Graphs;
using Waher.Security;

namespace Waher.Content.Markdown.PlantUml
{
	/// <summary>
	/// Class managing PlantUML integration into Markdown documents.
	/// </summary>
	public class PlantUml : IImageCodeContent
	{
		private static readonly Random rnd = new Random();
		private static Scheduler scheduler = null;
		private static string jarPath = null;
		private static string javaPath = null;
		private static string plantUmlFolder = null;
		private static string contentRootFolder = null;
		private static string defaultBgColor = null;
		private static string defaultFgColor = null;

		/// <summary>
		/// Class managing PlantUML integration into Markdown documents.
		/// </summary>
		public PlantUml()
		{
		}

		/// <summary>
		/// Initializes the PlantUML-Markdown integration.
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

				SearchForInstallationFolder(out string JarPath, out string JavaPath);

				if (string.IsNullOrEmpty(JarPath))
					Log.Warning("PlantUML not found. PlantUML support will not be available in Markdown.");
				else if (string.IsNullOrEmpty(JavaPath))
					Log.Warning("Java not found. PlantUML support will not be available in Markdown.");
				else
				{
					SetPath(JarPath, JavaPath);

					Log.Informational("PlantUML found. Integration with Markdown added.",
						new KeyValuePair<string, object>("Path", jarPath),
						new KeyValuePair<string, object>("Java", javaPath));
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Sets the full path of PlantUML.
		/// </summary>
		/// <param name="JarPath">Full path of plantuml.jar</param>
		/// <param name="JavaPath">Full path of java.exe</param>
		/// <exception cref="Exception">If trying to set the path to a different path than the one set previously.
		/// The path can only be set once, for security reasons.</exception>
		public static void SetPath(string JarPath, string JavaPath)
		{
			if (!string.IsNullOrEmpty(jarPath) && JarPath != jarPath)
				throw new Exception("PlantUML an Java paths have already been set.");

			jarPath = JarPath;
			javaPath = JavaPath;
			plantUmlFolder = Path.Combine(contentRootFolder, "PlantUML");

			if (!Directory.Exists(plantUmlFolder))
				Directory.CreateDirectory(plantUmlFolder);

			DeleteOldFiles(null);
		}

		private static void DeleteOldFiles(object P)
		{
			DeleteOldFiles(DateTime.Now.AddDays(-7));
		}

		/// <summary>
		/// Deletes generated files older than <paramref name="Limit"/>.
		/// </summary>
		/// <param name="Limit">Age limit.</param>
		public static void DeleteOldFiles(DateTime Limit)
		{
			int Count = 0;

			foreach (string FileName in Directory.GetFiles(plantUmlFolder, "*.*"))
			{
				if (File.GetLastAccessTime(FileName) < Limit)
				{
					try
					{
						File.Delete(FileName);
						Count++;
					}
					catch (Exception ex)
					{
						Log.Error("Unable to delete old file: " + ex.Message, FileName);
					}
				}
			}

			if (Count > 0)
				Log.Informational(Count.ToString() + " old file(s) deleted.", plantUmlFolder);

			lock (rnd)
			{
				scheduler.Add(DateTime.Now.AddDays(rnd.NextDouble() * 2), DeleteOldFiles, null);
			}
		}

		/// <summary>
		/// Searches for the installation folder on the local machine.
		/// </summary>
		/// <param name="JarPath">Path to PlantUML Jar file.</param>
		/// <param name="JavaPath">Path to Java VM.</param>
		public static void SearchForInstallationFolder(out string JarPath, out string JavaPath)
		{
			string[] Folders = FileSystem.GetFolders(new Environment.SpecialFolder[]
				{
					Environment.SpecialFolder.ProgramFiles,
					Environment.SpecialFolder.ProgramFilesX86
				});

			JarPath = FileSystem.FindLatestFile(Folders, "plantuml.jar", 1);
			JavaPath = FileSystem.FindLatestFile(Folders, "java.exe", 3);
		}

		/// <summary>
		/// Checks how well the handler supports multimedia content of a given type.
		/// </summary>
		/// <param name="Language">Language.</param>
		/// <returns>How well the handler supports the content.</returns>
		public Grade Supports(string Language)
		{
			if (!string.IsNullOrEmpty(jarPath) && !string.IsNullOrEmpty(javaPath))
			{
				int i = Language.IndexOf(':');
				if (i > 0)
					Language = Language.Substring(0, i).TrimEnd();

				switch (Language.ToLower())
				{
					case "uml": return Grade.Excellent;
					case "plantuml": return Grade.Perfect;
				}
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
		/// If HTML is handled.
		/// </summary>
		public bool HandlesHTML => true;

		/// <summary>
		/// If Plain Text is handled.
		/// </summary>
		public bool HandlesPlainText => true;

		/// <summary>
		/// If XAML is handled.
		/// </summary>
		public bool HandlesXAML => true;

		/// <summary>
		/// Generates HTML for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateHTML(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await this.GetFileName(Language, Rows, ResultType.Svg);
			if (Info is null)
				return false;

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

			Output.Append("\" class=\"aloneUnsized\"/>");

			if (!string.IsNullOrEmpty(Info.Title))
			{
				Output.Append("<figcaption>");
				Output.Append(XML.HtmlValueEncode(Info.Title));
				Output.Append("</figcaption>");
			}

			Output.AppendLine("</figure>");

			return true;
		}

		private enum ResultType
		{
			Svg,
			Png
		}

		private class GraphInfo
		{
			public string FileName;
			public string Title;
		}

		private async Task<GraphInfo> GetFileName(string Language, string[] Rows, ResultType Type)
		{
			GraphInfo Result = new GraphInfo();
			StringBuilder sb = new StringBuilder();

			foreach (string Row in Rows)
				sb.AppendLine(Row);

			string Graph = sb.ToString();
			int i = Language.IndexOf(':');

			if (i > 0)
			{
				Result.Title = Language.Substring(i + 1).Trim();
				Language = Language.Substring(0, i).TrimEnd();
			}
			else
				Result.Title = string.Empty;

			sb.Append(Language);

			Result.FileName = Hashes.ComputeSHA256HashString(Encoding.UTF8.GetBytes(sb.ToString()));
			string PlantUmlFolder = Path.Combine(contentRootFolder, "PlantUML");
			string ResultFileName = Path.Combine(PlantUmlFolder, Result.FileName);

			switch (Type)
			{
				case ResultType.Svg:
				default:
					Result.FileName = ResultFileName + ".svg";
					break;

				case ResultType.Png:
					Result.FileName = ResultFileName + ".png";
					break;
			}

			if (!File.Exists(Result.FileName))
			{
				string TxtFileName = ResultFileName + ".txt";
				await Resources.WriteAllTextAsync(TxtFileName, Graph, Encoding.UTF8);

				StringBuilder Arguments = new StringBuilder();
				Arguments.Append("-jar \"");
				Arguments.Append(jarPath);
				Arguments.Append("\" -charset UTF-8 -t");
				Arguments.Append(Type.ToString().ToLower());

				//if (!string.IsNullOrEmpty(defaultBgColor))
				//{
				//	Arguments.Append(" -SbackgroundColor=");
				//	Arguments.Append(defaultBgColor);
				//}
				//
				//if (!string.IsNullOrEmpty(defaultFgColor))
				//{
				//	Arguments.Append(" -SborderColor=");
				//	Arguments.Append(defaultFgColor);
				//	Arguments.Append(" -SarrowColor=");
				//	Arguments.Append(defaultFgColor);
				//	Arguments.Append(" -SarrowFontColor=");
				//	Arguments.Append(defaultFgColor);
				//	Arguments.Append(" -SlabelFontColor=");
				//	Arguments.Append(defaultFgColor);
				//	Arguments.Append(" -SlegendFontColor=");
				//	Arguments.Append(defaultFgColor);
				//	Arguments.Append(" -StitleFontColor=");
				//	Arguments.Append(defaultFgColor);
				//	Arguments.Append(" -StimingFontColor=");
				//	Arguments.Append(defaultFgColor);
				//}

				Arguments.Append(" -quiet \"");
				Arguments.Append(TxtFileName);
				Arguments.Append("\" \"");
				Arguments.Append(Result.FileName);
				Arguments.Append("\"");

				ProcessStartInfo ProcessInformation = new ProcessStartInfo()
				{
					FileName = javaPath,
					Arguments = Arguments.ToString(),
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					WorkingDirectory = PlantUmlFolder,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				Process P = new Process();
				TaskCompletionSource<GraphInfo> ResultSource = new TaskCompletionSource<GraphInfo>();

				P.ErrorDataReceived += (sender, e) =>
				{
					Log.Error("Unable to generate graph: " + e.Data);
					ResultSource.TrySetResult(null);
				};

				P.Exited += (sender, e) =>
				{
					if (P.ExitCode != 0)
					{
						Log.Error("Unable to generate graph. Exit code: " + P.ExitCode.ToString());
						ResultSource.TrySetResult(null);
					}
					else
						ResultSource.TrySetResult(Result);
				};

				Task _ = Task.Delay(10000).ContinueWith(Prev => ResultSource.TrySetException(new TimeoutException("PlantUml process did not terminate properly.")));

				P.StartInfo = ProcessInformation;
				P.EnableRaisingEvents = true;
				P.Start();

				return await ResultSource.Task;
			}

			return Result;
		}

		/// <summary>
		/// Generates Plain Text for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GeneratePlainText(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await this.GetFileName(Language, Rows, ResultType.Svg);
			if (Info is null)
				return false;

			Output.AppendLine(Info.Title);

			return true;
		}

		/// <summary>
		/// Generates WPF XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="TextAlignment">Alignment of text in element.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await this.GetFileName(Language, Rows, ResultType.Png);
			if (Info is null)
				return false;

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Info.FileName);
			Output.WriteAttributeString("Stretch", "None");

			if (!string.IsNullOrEmpty(Info.Title))
				Output.WriteAttributeString("ToolTip", Info.Title);

			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="State">Xamarin Forms XAML Rendering State.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language used.</param>
		/// <param name="Indent">Additional indenting.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If content was rendered. If returning false, the default rendering of the code block will be performed.</returns>
		public async Task<bool> GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			GraphInfo Info = await this.GetFileName(Language, Rows, ResultType.Png);
			if (Info is null)
				return false;

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Info.FileName);
			Output.WriteEndElement();

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
			GraphInfo Info = await this.GetFileName(Language, Rows, ResultType.Png);
			if (Info is null)
				return null;

			byte[] Data = await Resources.ReadAllBytesAsync(Info.FileName);

			using (SKBitmap Bitmap = SKBitmap.Decode(Data))
			{
				return new PixelInformationPng(Data, Bitmap.Width, Bitmap.Height);
			}
		}

		/// <summary>
		/// Default Background color
		/// </summary>
		public static string DefaultBgColor
		{
			get => defaultBgColor;
			set => defaultBgColor = value;
		}

		/// <summary>
		/// Default Foreground color
		/// </summary>
		public static string DefaultFgColor
		{
			get => defaultFgColor;
			set => defaultFgColor = value;
		}
	}
}
