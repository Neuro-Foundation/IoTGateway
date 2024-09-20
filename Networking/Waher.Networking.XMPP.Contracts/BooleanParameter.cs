﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Script;

namespace Waher.Networking.XMPP.Contracts
{
	/// <summary>
	/// Boolean contractual parameter
	/// </summary>
	public class BooleanParameter : Parameter
	{
		private bool? @value;

		/// <summary>
		/// Parameter value
		/// </summary>
		public bool? Value
		{
			get => this.@value;
			set
			{
				this.@value = value;
				this.ProtectedValue = null;
			}
		}

		/// <summary>
		/// String representation of value.
		/// </summary>
		public override string StringValue
		{
			get => this.Value.HasValue ? CommonTypes.Encode(this.Value.Value) : string.Empty;
			set
			{
				if (CommonTypes.TryParse(value, out bool b))
					this.Value = b;
				else
					this.Value = null;
			}
		}

		/// <summary>
		/// Parameter value.
		/// </summary>
		public override object ObjectValue => this.@value;

		/// <summary>
		/// Parameter type name, corresponding to the local name of the parameter element in XML.
		/// </summary>
		public override string ParameterType => "booleanParameter";

		/// <summary>
		/// Serializes the parameter, in normalized form.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="UsingTemplate">If the XML is for creating a contract using a template.</param>
		public override void Serialize(StringBuilder Xml, bool UsingTemplate)
		{
			Xml.Append("<booleanParameter");

			if (!UsingTemplate)
			{
				if (!string.IsNullOrEmpty(this.Expression))
				{
					Xml.Append(" exp=\"");
					Xml.Append(XML.Encode(this.Expression.Normalize(NormalizationForm.FormC)));
					Xml.Append('"');
				}

				if (!string.IsNullOrEmpty(this.Guide))
				{
					Xml.Append(" guide=\"");
					Xml.Append(XML.Encode(this.Guide.Normalize(NormalizationForm.FormC)));
					Xml.Append('"');
				}
			}

			Xml.Append(" name=\"");
			Xml.Append(XML.Encode(this.Name));
			Xml.Append('"');

			if (this.CanSerializeProtectedValue)
			{
				Xml.Append(" protected=\"");
				Xml.Append(Convert.ToBase64String(this.ProtectedValue));
				Xml.Append('"');
			}

			if (!UsingTemplate && this.Protection != ProtectionLevel.Normal)
			{
				Xml.Append(" protection=\"");
				Xml.Append(this.Protection.ToString());
				Xml.Append('"');
			}

			if (this.@value.HasValue && this.CanSerializeValue)
			{
				Xml.Append(" value=\"");
				Xml.Append(CommonTypes.Encode(this.@value.Value));
				Xml.Append('"');
			}

			if (UsingTemplate || this.Descriptions is null || this.Descriptions.Length == 0)
				Xml.Append("/>");
			else
			{
				Xml.Append('>');

				foreach (HumanReadableText Description in this.Descriptions)
					Description.Serialize(Xml, "description", null);

				Xml.Append("</booleanParameter>");
			}
		}

		/// <summary>
		/// Checks if the parameter value is valid.
		/// </summary>
		/// <param name="Variables">Collection of parameter values.</param>
		/// <param name="Client">Connected contracts client. If offline or null, partial validation in certain cases will be performed.</param>
		/// <returns>If parameter value is valid.</returns>
		public override Task<bool> IsParameterValid(Variables Variables, ContractsClient Client)
		{
			if (!this.@value.HasValue)
			{
				this.ErrorReason = ParameterErrorReason.LacksValue;
				this.ErrorText = null;

				return Task.FromResult(false);
			}

			return base.IsParameterValid(Variables, Client);
		}

		/// <summary>
		/// Populates a variable collection with the value of the parameter.
		/// </summary>
		/// <param name="Variables">Variable collection.</param>
		public override void Populate(Variables Variables)
		{
			Variables[this.Name] = this.@value;
		}

		/// <summary>
		/// Sets the parameter value.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <exception cref="ArgumentException">If <paramref name="Value"/> is not of the correct type.</exception>
		public override void SetValue(object Value)
		{
			if (Value is bool b)
				this.Value = b;
			else if (Value is string s && CommonTypes.TryParse(s, out b))
				this.Value = b;
			else
				throw new ArgumentException("Invalid parameter type.", nameof(Value));
		}

		/// <summary>
		/// Sets the minimum value allowed by the parameter.
		/// </summary>
		/// <param name="Value">Minimum value.</param>
		/// <param name="Inclusive">If the value is included in the range. If null, keeps the original value.</param>
		public override void SetMinValue(object Value, bool? Inclusive)
		{
			throw new InvalidOperationException("Minimum value for Boolean parameter types not supported.");
		}

		/// <summary>
		/// Sets the maximum value allowed by the parameter.
		/// </summary>
		/// <param name="Value">Maximum value.</param>
		/// <param name="Inclusive">If the value is included in the range. If null, keeps the original value.</param>
		public override void SetMaxValue(object Value, bool? Inclusive)
		{
			throw new InvalidOperationException("Maximum value for Boolean parameter types not supported.");
		}

		/// <summary>
		/// Imports parameter values from its XML definition.
		/// </summary>
		/// <param name="Xml">XML definition.</param>
		/// <returns>If import was successful.</returns>
		public override Task<bool> Import(XmlElement Xml)
		{
			this.Value = Xml.HasAttribute("value") ? XML.Attribute(Xml, "value", false) : (bool?)null;
			return base.Import(Xml);
		}

	}
}
