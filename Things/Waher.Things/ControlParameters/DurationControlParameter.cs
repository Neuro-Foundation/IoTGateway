﻿using System;
using System.Threading.Tasks;
using System.Xml;
using Waher.Events;
using Waher.Content;

namespace Waher.Things.ControlParameters
{
	/// <summary>
	/// Set handler delegate for duration control parameters.
	/// </summary>
	/// <param name="Node">Node whose parameter is being set.</param>
	/// <param name="Value">Value set.</param>
	public delegate Task DurationSetHandler(IThingReference Node, Duration Value);

	/// <summary>
	/// Get handler delegate for duration control parameters.
	/// </summary>
	/// <param name="Node">Node whose parameter is being retrieved.</param>
	/// <returns>Current value, or null if not available.</returns>
	public delegate Task<Duration> DurationGetHandler(IThingReference Node);

	/// <summary>
	/// Duration control parameter.
	/// </summary>
	public class DurationControlParameter : ControlParameter
	{
		private readonly DurationGetHandler getHandler;
		private readonly DurationSetHandler setHandler;
		private readonly Duration? min, max;

		/// <summary>
		/// Duration control parameter.
		/// </summary>
		/// <param name="Name">Parameter name.</param>
		/// <param name="Page">On which page in the control dialog the parameter should appear.</param>
		/// <param name="Label">Label for parameter.</param>
		/// <param name="Description">Description for parameter.</param>
		/// <param name="GetHandler">This callback method is called when the value of the parameter is needed.</param>
		/// <param name="SetHandler">This callback method is called when the value of the parameter is set.</param>
		public DurationControlParameter(string Name, string Page, string Label, string Description, 
			DurationGetHandler GetHandler, DurationSetHandler SetHandler)
			: base(Name, Page, Label, Description)
		{
			this.getHandler = GetHandler;
			this.setHandler = SetHandler;
			this.min = null;
			this.max = null;
		}

		/// <summary>
		/// Duration control parameter.
		/// </summary>
		/// <param name="Name">Parameter name.</param>
		/// <param name="Page">On which page in the control dialog the parameter should appear.</param>
		/// <param name="Label">Label for parameter.</param>
		/// <param name="Description">Description for parameter.</param>
		/// <param name="Min">Smallest value allowed.</param>
		/// <param name="Max">Largest value allowed.</param>
		/// <param name="GetHandler">This callback method is called when the value of the parameter is needed.</param>
		/// <param name="SetHandler">This callback method is called when the value of the parameter is set.</param>
		public DurationControlParameter(string Name, string Page, string Label, string Description, Duration Min, Duration Max,
			DurationGetHandler GetHandler, DurationSetHandler SetHandler)
			: base(Name, Page, Label, Description)
		{
			this.getHandler = GetHandler;
			this.setHandler = SetHandler;
			this.min = Min;
			this.max = Max;
		}

		/// <summary>
		/// Sets the value of the control parameter.
		/// </summary>
		/// <param name="Node">Node reference, if available.</param>
		/// <param name="Value">Value to set.</param>
		/// <returns>If the parameter could be set (true), or if the value was invalid (false).</returns>
		public async Task<bool> Set(IThingReference Node, Duration Value)
		{
			try
			{
				if ((!(this.min is null) && Value < this.min) || (!(this.max is null) && Value > this.max))
					return false;

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
			if (!Duration.TryParse(StringValue, out Duration Value))
				return false;

			await this.Set(Node, Value);

			return true;
		}

		/// <summary>
		/// Gets the value of the control parameter.
		/// </summary>
		/// <returns>Current value, or null if not available.</returns>
		public async Task<Duration?> Get(IThingReference Node)
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
			Duration? Value = await this.Get(Node);

			if (Value.HasValue)
				return Value.ToString();
			else
				return string.Empty;
		}

		/// <summary>
		/// Exports form validation rules for the parameter.
		/// </summary>
		/// <param name="Output">Output</param>
		/// <param name="Node">Node reference, if available.</param>
		public override Task ExportValidationRules(XmlWriter Output, IThingReference Node)
		{
			Output.WriteStartElement("xdv", "validate", null);
			Output.WriteAttributeString("datatype", "xs:duration");
			Output.WriteEndElement();

			return Task.CompletedTask;
		}
	}
}
