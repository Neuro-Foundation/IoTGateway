﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Waher.Events;
using Waher.Content;

namespace Waher.Things.ControlParameters
{
	/// <summary>
	/// Set handler delegate for color control parameters.
	/// </summary>
	/// <param name="Node">Node whose parameter is being set.</param>
	/// <param name="Value">Value set.</param>
	public delegate Task ColorSetHandler(IThingReference Node, ColorReference Value);

	/// <summary>
	/// Get handler delegate for color control parameters.
	/// </summary>
	/// <param name="Node">Node whose parameter is being retrieved.</param>
	/// <returns>Current value, or null if not available.</returns>
	public delegate Task<ColorReference> ColorGetHandler(IThingReference Node);

	/// <summary>
	/// Color control parameter.
	/// </summary>
	public class ColorControlParameter : ControlParameter
	{
		private readonly ColorGetHandler getHandler;
		private readonly ColorSetHandler setHandler;

		/// <summary>
		/// Color control parameter.
		/// </summary>
		/// <param name="Name">Parameter name.</param>
		/// <param name="Page">On which page in the control dialog the parameter should appear.</param>
		/// <param name="Label">Label for parameter.</param>
		/// <param name="Description">Description for parameter.</param>
		/// <param name="GetHandler">This callback method is called when the value of the parameter is needed.</param>
		/// <param name="SetHandler">This callback method is called when the value of the parameter is set.</param>
		public ColorControlParameter(string Name, string Page, string Label, string Description, ColorGetHandler GetHandler, ColorSetHandler SetHandler)
			: base(Name, Page, Label, Description)
		{
			this.getHandler = GetHandler;
			this.setHandler = SetHandler;
		}

		/// <summary>
		/// Sets the value of the control parameter.
		/// </summary>
		/// <param name="Node">Node reference, if available.</param>
		/// <param name="Value">Value to set.</param>
		/// <returns>If the parameter could be set (true), or if the value was invalid (false).</returns>
		public async Task<bool> Set(IThingReference Node, ColorReference Value)
		{
			try
			{
				await this.setHandler(Node, Value);
				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// Sets the value of the control parameter.
		/// </summary>
		/// <param name="Node">Node reference, if available.</param>
		/// <param name="StringValue">String representation of value to set.</param>
		/// <returns>If the parameter could be set (true), or if the value could not be parsed or its value was invalid (false).</returns>
		public override async Task<bool> SetStringValue(IThingReference Node, string StringValue)
		{
			byte R, G, B;

			if (StringValue.Length == 6)
			{
				if (byte.TryParse(StringValue.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out R) &&
					byte.TryParse(StringValue.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out G) &&
					byte.TryParse(StringValue.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out B))
				{
					await this.Set(Node, new ColorReference(R, G, B));
				}
				else
					return false;
			}
			else if (StringValue.Length == 8)
			{
				if (byte.TryParse(StringValue.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out R) &&
					byte.TryParse(StringValue.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out G) &&
					byte.TryParse(StringValue.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out B) &&
					byte.TryParse(StringValue.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte A))
				{
					await this.Set(Node, new ColorReference(R, G, B, A));
				}
				else
					return false;
			}
			else
				return false;

			return true;
		}

		/// <summary>
		/// Gets the value of the control parameter.
		/// </summary>
		/// <returns>Current value, or null if not available.</returns>
		public async Task<ColorReference> Get(IThingReference Node)
		{
			try
			{
				return await this.getHandler(Node);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return null;
			}
		}

		/// <summary>
		/// Gets the string value of the control parameter.
		/// </summary>
		/// <param name="Node">Node reference, if available.</param>
		/// <returns>String representation of the value.</returns>
		public override async Task<string> GetStringValue(IThingReference Node)
		{
			ColorReference Value = await this.Get(Node);

			if (!(Value is null))
				return Value.ToString();
			else
				return string.Empty;
		}

		/// <summary>
		/// Exports form validation rules for the parameter.
		/// </summary>
		/// <param name="Output">Output</param>
		/// <param name="Node">Node reference, if available.</param>
		public override async Task ExportValidationRules(XmlWriter Output, IThingReference Node)
		{
			ColorReference Value = await this.Get(Node);

			Output.WriteStartElement("xdv", "validate", null);
			Output.WriteAttributeString("xmlns", "xdc", null, "urn:xmpp:xdata:color");

			if (Value is null || !Value.HasAlpha)
			{
				Output.WriteAttributeString("datatype", "xdc:color");
				Output.WriteElementString("xdv", "regex", null, "^[0-9a-fA-F]{6}$");
			}
			else
			{
				Output.WriteAttributeString("datatype", "xdc:colorAlpha");
				Output.WriteElementString("xdv", "regex", null, "^[0-9a-fA-F]{8}$");
			}

			Output.WriteEndElement();
		}

	}
}
