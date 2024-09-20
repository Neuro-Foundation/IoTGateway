﻿using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Script;

namespace Waher.Networking.XMPP.Contracts
{
	/// <summary>
	/// Calculation contractual parameter
	/// </summary>
	public class CalcParameter : Parameter
	{
		private object value;

		/// <summary>
		/// Parameter value.
		/// </summary>
		public override object ObjectValue => this.value;

		/// <summary>
		/// String representation of value.
		/// </summary>
		public override string StringValue
		{
			get => this.@value?.ToString() ?? string.Empty;
			set => this.@value = value;
		}

		/// <summary>
		/// Parameter type name, corresponding to the local name of the parameter element in XML.
		/// </summary>
		public override string ParameterType => "calcParameter";

		/// <summary>
		/// Serializes the parameter, in normalized form.
		/// </summary>
		/// <param name="Xml">XML Output</param>
		/// <param name="UsingTemplate">If the XML is for creating a contract using a template.</param>
		public override void Serialize(StringBuilder Xml, bool UsingTemplate)
		{
			if (!UsingTemplate)
			{
				Xml.Append("<calcParameter");

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

				Xml.Append(" name=\"");
				Xml.Append(XML.Encode(this.Name.Normalize(NormalizationForm.FormC)));
				Xml.Append('"');

				if (this.Protection != ProtectionLevel.Normal)
				{
					Xml.Append(" protection=\"");
					Xml.Append(this.Protection.ToString());
					Xml.Append('"');
				}

				if (this.Descriptions is null || this.Descriptions.Length == 0)
					Xml.Append("/>");
				else
				{
					Xml.Append('>');

					foreach (HumanReadableText Description in this.Descriptions)
						Description.Serialize(Xml, "description", null);

					Xml.Append("</calcParameter>");
				}
			}
		}

		/// <summary>
		/// Checks if the parameter value is valid.
		/// </summary>
		/// <param name="Variables">Collection of parameter values.</param>
		/// <param name="Client">Connected contracts client. If offline or null, partial validation in certain cases will be performed.</param>
		/// <returns>If parameter value is valid.</returns>
		public override async Task<bool> IsParameterValid(Variables Variables, ContractsClient Client)
		{
			if (string.IsNullOrEmpty(this.Expression))
			{
				this.ErrorReason = ParameterErrorReason.LacksValue;
				this.ErrorText = null;
				this.value = null;
				return false;
			}
			else
			{
				try
				{
					object Result = await this.Parsed.EvaluateAsync(Variables);
					Variables[this.Name] = Result;

					if (Result is double d)
						this.value = (decimal)d;
					else
						this.value = Result;

					this.ProtectedValue = null;
					this.ErrorReason = null;
					this.ErrorText = null;

					return true;
				}
				catch (Exception ex)
				{
					this.ErrorReason = ParameterErrorReason.Exception;
					this.ErrorText = ex.Message;

					this.value = null;
					this.ProtectedValue = null;
					return false;
				}
			}
		}

		/// <summary>
		/// Populates a variable collection with the value of the parameter.
		/// </summary>
		/// <param name="Variables">Variable collection.</param>
		public override void Populate(Variables Variables)
		{
			// Do nothing.
		}

		/// <summary>
		/// Sets the parameter value.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <exception cref="ArgumentException">If <paramref name="Value"/> is not of the correct type.</exception>
		public override void SetValue(object Value)
		{
			// Ignore
		}

		/// <summary>
		/// Sets the minimum value allowed by the parameter.
		/// </summary>
		/// <param name="Value">Minimum value.</param>
		/// <param name="Inclusive">If the value is included in the range. If null, keeps the original value.</param>
		public override void SetMinValue(object Value, bool? Inclusive)
		{
			// Ignore
		}

		/// <summary>
		/// Sets the maximum value allowed by the parameter.
		/// </summary>
		/// <param name="Value">Maximum value.</param>
		/// <param name="Inclusive">If the value is included in the range. If null, keeps the original value.</param>
		public override void SetMaxValue(object Value, bool? Inclusive)
		{
			// Ignore
		}

	}
}
