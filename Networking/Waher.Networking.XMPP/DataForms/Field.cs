﻿using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.XMPP.DataForms.DataTypes;
using Waher.Networking.XMPP.DataForms.ValidationMethods;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Events;
using System;

namespace Waher.Networking.XMPP.DataForms
{
	/// <summary>
	/// Base class for form fields
	/// </summary>
	public abstract class Field
	{
		private DataForm form;
		private readonly string var;
		private readonly string label;
		private readonly bool required;
		private string[] valueStrings;
		private KeyValuePair<string, string>[] options;
		private readonly string description;
		private readonly DataType dataType;
		private readonly ValidationMethod validationMethod;
		private string error;
		private int priority = 0;
		private int ordinal = 0;
		private readonly bool postBack;
		private bool readOnly;
		private bool notSame;
		private bool edited = false;
		private bool exclude = false;

		/// <summary>
		/// Base class for form fields
		/// </summary>
		/// <param name="Form">Form containing the field.</param>
		/// <param name="Var">Variable name</param>
		/// <param name="Label">Label</param>
		/// <param name="Required">If the field is required.</param>
		/// <param name="ValueStrings">Values for the field (string representations).</param>
		/// <param name="Options">Options, as (Key=M2H Label, Value=M2M Value) pairs.</param>
		/// <param name="Description">Description</param>
		/// <param name="DataType">Data Type</param>
		/// <param name="ValidationMethod">Validation Method</param>
		/// <param name="Error">Flags the field as having an error.</param>
		/// <param name="PostBack">Flags a field as requiring server post-back after having been edited.</param>
		/// <param name="ReadOnly">Flags a field as being read-only.</param>
		/// <param name="NotSame">Flags a field as having an undefined or uncertain value.</param>
		public Field(DataForm Form, string Var, string Label, bool Required, string[] ValueStrings, KeyValuePair<string, string>[] Options,
			string Description, DataType DataType, ValidationMethod ValidationMethod, string Error, bool PostBack, bool ReadOnly, bool NotSame)
		{
			this.form = Form;
			this.var = Var;
			this.label = Label;
			this.required = Required;
			this.valueStrings = ValueStrings;
			this.options = Options;
			this.description = Description;
			this.dataType = DataType;
			this.validationMethod = ValidationMethod;
			this.error = Error;
			this.postBack = PostBack;
			this.readOnly = ReadOnly;
			this.notSame = NotSame;
		}

		/// <summary>
		/// Data Form containing the field.
		/// </summary>
		public DataForm Form
		{
			get => this.form;
			internal set => this.form = value;
		}

		/// <summary>
		/// Variable name
		/// </summary>
		public string Var => this.var;

		/// <summary>
		/// Label
		/// </summary>
		public string Label => this.label;

		/// <summary>
		/// If the field is required.
		/// </summary>
		public bool Required => this.required;

		/// <summary>
		/// Values for the field (string representations).
		/// </summary>
		public string[] ValueStrings => this.valueStrings;

		/// <summary>
		/// Value as a single string. If field contains multiple values, they will be concatenated into a single string, each one delimited by a CRLF.
		/// </summary>
		public string ValueString => XmppClient.Concat(this.valueStrings);

		/// <summary>
		/// Options, as (Key=M2H Label, Value=M2M Value) pairs.
		/// </summary>
		public KeyValuePair<string, string>[] Options => this.options;

		/// <summary>
		/// Description
		/// </summary>
		public string Description => this.description;

		/// <summary>
		/// Data Type
		/// </summary>
		public DataType DataType => this.dataType;

		/// <summary>
		/// Validation Method
		/// </summary>
		public ValidationMethod ValidationMethod => this.validationMethod;

		/// <summary>
		/// If not null, flags the field as having an error.
		/// </summary>
		public string Error
		{
			get => this.error;
			set => this.error = value;
		}

		/// <summary>
		/// If the field has an error. Any error message is available in the <see cref="Error"/> property.
		/// </summary>
		public bool HasError => !string.IsNullOrEmpty(this.error);

		/// <summary>
		/// Flags the field as requiring server post-back after having been edited.
		/// </summary>
		public bool PostBack => this.postBack;

		/// <summary>
		/// Flags the field as being read-only.
		/// </summary>
		public bool ReadOnly => this.readOnly;

		/// <summary>
		/// Flags the field as having an undefined or uncertain value.
		/// </summary>
		public bool NotSame => this.notSame;

		/// <summary>
		/// If the field has been edited.
		/// </summary>
		public bool Edited => this.edited;

		/// <summary>
		/// Can be used to sort fields. Not serialized to or from XML.
		/// </summary>
		public int Priority
		{
			get => this.priority;
			set => this.priority = value;
		}

		/// <summary>
		/// Can be used to sort fields having the same priority. Not serialized to or from XML.
		/// </summary>
		public int Ordinal
		{
			get => this.ordinal;
			set => this.ordinal = value;
		}

		/// <summary>
		/// Validates field input. The <see cref="Field.Error"/> property will reflect any errors found.
		/// </summary>
		/// <param name="Value">Field Value(s)</param>
		/// <returns>Parsed value(s).</returns>
		public virtual object[] Validate(params string[] Value)
		{
			object[] Result = null;

			this.error = string.Empty;

			if ((Value is null ||
				Value.Length == 0 ||
				(Value.Length == 1 && string.IsNullOrEmpty(Value[0]))) && !(this.validationMethod is ListRangeValidation))
			{
				if (this.required)
					this.error = "Required field.";

				Result = Array.Empty<object>();
			}
			else if (!(Value is null))
			{
				if (this.dataType is null)
					Result = Value;
				else
				{
					List<object> Parsed = new List<object>();

					foreach (string s in Value)
					{
						object Obj = this.dataType.Parse(s);
						if (Obj is null)
							this.error = "Invalid input.";
						else
							Parsed.Add(Obj);
					}

					Result = Parsed.ToArray();

					this.validationMethod?.Validate(this, this.dataType, Result, Value);
				}
			}

			return Result;
		}

		/// <summary>
		/// Sets the value of the field.
		/// 
		/// The value is validated.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>Parsed value.</returns>
		public async Task<object> SetValue(string Value)
		{
			object[] Result = await this.SetValue(new string[] { Value });
			if (Result is null || Result.Length == 0)
				return null;
			else
				return Result[0];
		}

		/// <summary>
		/// Sets the value, or values, of the field.
		/// 
		/// The values are validated.
		/// </summary>
		/// <param name="Value">Value(s).</param>
		/// <returns>Parsed value(s).</returns>
		public async Task<object[]> SetValue(params string[] Value)
		{
			object[] Result = this.Validate(Value);

			this.edited = true;
			this.valueStrings = Value;
			this.notSame = false;

			if (this.postBack && string.IsNullOrEmpty(this.error))
			{
				StringBuilder Xml = new StringBuilder();

				Xml.Append("<submit xmlns='");
				Xml.Append(XmppClient.NamespaceDynamicForms);
				Xml.Append("'>");
				this.form.ExportXml(Xml, "submit", true, false);
				Xml.Append("</submit>");

				await this.form.Client.SendIqSet(this.form.From, Xml.ToString(), this.FormUpdated, null);
			}

			return Result;
		}

		private async Task FormUpdated(object Sender, IqResultEventArgs e)
		{
			if (e.Ok && Sender is XmppClient Client && !(Client is null))
			{
				foreach (XmlNode N in e.Response)
				{
					if (N.LocalName == "x")
					{
						DataForm UpdatedForm = new DataForm(Client, (XmlElement)N, null, null, e.From, e.To);
						await this.form.Join(UpdatedForm);
					}
				}
			}
		}

		internal bool Serialize(StringBuilder Output, bool ValuesOnly, bool IncludeLabels)
		{
			if ((this.notSame || this.readOnly) && ValuesOnly)
				return false;

			string TypeName = this.TypeName;

			if (TypeName == "fixed" && ValuesOnly)
				return true;

			Output.Append("<field");

			if (!string.IsNullOrEmpty(this.var))
			{
				Output.Append(" var='");
				Output.Append(XML.Encode(this.var));
				Output.Append('\'');
			}

			if (IncludeLabels && !string.IsNullOrEmpty(this.label))
			{
				Output.Append(" label='");
				Output.Append(XML.Encode(this.label));
				Output.Append('\'');
			}

			if (!ValuesOnly)
			{
				Output.Append(" type='");
				Output.Append(this.TypeName);
				Output.Append('\'');
			}

			Output.Append('>');

			if (!ValuesOnly)
			{
				if (!string.IsNullOrEmpty(this.description))
				{
					Output.Append("<desc>");
					Output.Append(XML.Encode(this.description));
					Output.Append("</desc>");
				}

				if (this.required)
					Output.Append("<required/>");

				if (!(this.dataType is null))
				{
					Output.Append("<xdv:validate datatype='");
					Output.Append(XML.Encode(this.dataType.TypeName));
					Output.Append("'>");

					this.validationMethod?.Serialize(Output);

					Output.Append("</xdv:validate>");
				}

				if (this.notSame)
					Output.Append("<xdd:notSame/>");

				if (this.readOnly)
					Output.Append("<xdd:readOnly/>");

				if (!string.IsNullOrEmpty(this.error))
				{
					Output.Append("<xdd:error>");
					Output.Append(XML.Encode(this.error));
					Output.Append("</xdd:error>");
				}
			}

			if (!(this.valueStrings is null))
			{
				foreach (string Value in this.valueStrings)
				{
					Output.Append("<value>");
					Output.Append(XML.Encode(Value));
					Output.Append("</value>");
				}
			}
			else if (ValuesOnly)
				Output.Append("<value/>");

			if (!ValuesOnly)
			{
				if (!(this.options is null))
				{
					foreach (KeyValuePair<string, string> P in this.options)
					{
						Output.Append("<option label='");
						Output.Append(XML.Encode(P.Key));
						Output.Append("'><value>");
						Output.Append(XML.Encode(P.Value));
						Output.Append("</value></option>");
					}
				}
			}

			this.AnnotateField(Output, ValuesOnly, IncludeLabels);

			Output.Append("</field>");

			return true;
		}

		/// <summary>
		/// Allows fields to add additional information to the XML serialization of the field.
		/// </summary>
		/// <param name="Output">XML output.</param>
		/// <param name="ValuesOnly">If only values are to be output.</param>
		/// <param name="IncludeLabels">If labels are to be included.</param>
		public virtual void AnnotateField(StringBuilder Output, bool ValuesOnly, bool IncludeLabels)
		{
			// Do nothing by default.
		}

		/// <summary>
		/// XMPP Field Type Name.
		/// </summary>
		public abstract string TypeName
		{
			get;
		}

		/// <summary>
		/// If the the field is marked as excluded.
		/// </summary>
		public bool Exclude
		{
			get => this.exclude;
			set => this.exclude = value;
		}

		/// <summary>
		/// Merges the field with a secondary field, if possible.
		/// </summary>
		/// <param name="SecondaryField">Secondary field to merge with.</param>
		/// <returns>If merger was possible.</returns>
		public bool Merge(Field SecondaryField)
		{
			if (this.var != SecondaryField.var ||
				this.label != SecondaryField.label ||
				this.description != SecondaryField.description ||
				this.postBack != SecondaryField.postBack ||
				this.description != SecondaryField.description ||
				this.required != SecondaryField.required ||
				(this.dataType is null) ^ (SecondaryField.dataType is null) ||
				(this.validationMethod is null) ^ (SecondaryField.validationMethod is null) ||
				(this.options is null) ^ (SecondaryField.options is null))
			{
				return false;
			}

			if (!(this.dataType is null) && !this.dataType.Equals(SecondaryField.dataType))
				return false;

			if (!(this.validationMethod is null) && !this.validationMethod.Equals(SecondaryField.validationMethod))
				return false;

			this.readOnly |= SecondaryField.readOnly;
			this.notSame |= SecondaryField.notSame;

			int i, c;

			if (!(this.options is null))
			{
				KeyValuePair<string, string> O1;
				KeyValuePair<string, string> O2;

				c = this.options.Length;
				bool OptionsDifferent = (c == SecondaryField.options.Length);

				if (!OptionsDifferent)
				{
					for (i = 0; i < c; i++)
					{
						O1 = this.options[i];
						O2 = SecondaryField.options[i];

						if (O1.Key != O2.Key || O1.Value != O2.Value)
						{
							OptionsDifferent = true;
							break;
						}
					}
				}

				if (OptionsDifferent)
				{
					List<KeyValuePair<string, string>> NewOptions = null;

					for (i = 0; i < c; i++)
					{
						O1 = this.options[i];
						O2 = SecondaryField.options[i];

						if (O1.Key == O2.Key && O1.Value == O2.Value)
						{
							if (NewOptions is null)
								NewOptions = new List<KeyValuePair<string, string>>();

							NewOptions.Add(O1);
						}

						if (NewOptions is null)
							return false;

						this.options = NewOptions.ToArray();
					}
				}
			}

			if (!this.notSame)
			{
				if ((c = this.valueStrings.Length) != SecondaryField.valueStrings.Length)
					this.notSame = true;
				else
				{
					for (i = 0; i < c; i++)
					{
						if (this.valueStrings[i] != SecondaryField.valueStrings[i])
						{
							this.notSame = true;
							break;
						}
					}
				}
			}

			return true;
		}


		/*
		private string[] valueStrings;
		 */
	}
}
