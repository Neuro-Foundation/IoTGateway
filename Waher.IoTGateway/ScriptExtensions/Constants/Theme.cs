﻿using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using Waher.Content;
using Waher.Content.Markdown.GraphViz;
using Waher.Content.Markdown.Layout2D;
using Waher.Content.Markdown.PlantUml;
using Waher.IoTGateway.ScriptExtensions.Functions;
using Waher.IoTGateway.Setup;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.IoTGateway.ScriptExtensions.Constants
{
	/// <summary>
	/// Theme constant.
	/// </summary>
	public class Theme : IConstant
	{
		private static readonly Dictionary<string, ThemeDefinition> definitionsPerDomain = new Dictionary<string, ThemeDefinition>(StringComparer.InvariantCultureIgnoreCase);
		private static ThemeDefinition currentDefinition = null;

		/// <summary>
		/// Theme constant.
		/// </summary>
		public Theme()
		{
		}

		/// <summary>
		/// Name of the constant
		/// </summary>
		public string ConstantName => nameof(Theme);

		/// <	summary>
		/// Optional aliases. If there are no aliases for the constant, null is returned.
		/// </summary>
		public string[] Aliases => null;

		/// <summary>
		/// Gets the constant value element.
		/// </summary>
		/// <param name="Variables">Current set of variables.</param>
		public IElement GetValueElement(Variables Variables)
		{
			ThemeDefinition Def = currentDefinition;

			if (Variables.TryGetVariable("Request", out Variable v) && v.ValueObject is IHostReference HostRef)
			{
				string Host = GetDomainSetting.IsAlternativeDomain(HostRef.Host);
				if (!string.IsNullOrEmpty(Host))
					Def = GetTheme(Host);
			}

			if (!(Variables is null))
			{
				Variables["GraphBgColor"] = Def.GraphBgColor;
				Variables["GraphFgColor"] = Def.GraphFgColor;
			}

			return new ObjectValue(Def);
		}

		/// <summary>
		/// Current theme.
		/// </summary>
		public static ThemeDefinition CurrentTheme
		{
			get => currentDefinition;
			internal set
			{
				bool DeleteCahces = !(currentDefinition is null) && currentDefinition.Id != value.Id;

				currentDefinition = value;

				string Color = ColorToString(currentDefinition.GraphBgColor);
				GraphViz.DefaultBgColor = Color;
				PlantUml.DefaultBgColor = Color;

				Color = ColorToString(currentDefinition.GraphFgColor);
				GraphViz.DefaultFgColor = Color;
				PlantUml.DefaultFgColor = Color;

				if (DeleteCahces)
				{
					GraphViz.DeleteOldFiles(TimeSpan.Zero, false);
					PlantUml.DeleteOldFiles(TimeSpan.Zero, false);
					XmlLayout.DeleteOldFiles(TimeSpan.Zero, false);
				}
			}
		}

		/// <summary>
		/// Gets the theme for a given domain.
		/// </summary>
		/// <param name="Domain">Domain</param>
		/// <returns>Theme definition.</returns>
		public static ThemeDefinition GetTheme(string Domain)
		{
			lock (definitionsPerDomain)
			{
				if (definitionsPerDomain.TryGetValue(Domain, out ThemeDefinition Result))
					return Result;
			}

			return CurrentTheme;
		}

		/// <summary>
		/// Sets the theme for a given domain.
		/// </summary>
		/// <param name="Domain">Domain</param>
		/// <param name="Theme">Theme</param>
		internal static void SetTheme(string Domain, ThemeDefinition Theme)
		{
			lock (definitionsPerDomain)
			{
				definitionsPerDomain[Domain] = Theme;
			}
		}

		private static string ColorToString(SKColor Color)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("#");
			sb.Append(Color.Red.ToString("X2"));
			sb.Append(Color.Green.ToString("X2"));
			sb.Append(Color.Blue.ToString("X2"));

			if (Color.Alpha != 255)
				sb.Append(Color.Alpha.ToString("X2"));

			return sb.ToString();
		}

	}
}
