﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Networking.XMPP.DataForms.DataTypes;
using Waher.Networking.XMPP.DataForms.ValidationMethods;

namespace Waher.Networking.XMPP.DataForms.FieldTypes
{
	/// <summary>
	/// Media form field.
	/// </summary>
	public class MediaField : Field
	{
		private readonly Media media;

		/// <summary>
		/// Media form field.
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
		/// <param name="Media">Media element.</param>
		/// <param name="Error">Flags the field as having an error.</param>
		/// <param name="PostBack">Flags a field as requiring server post-back after having been edited.</param>
		/// <param name="ReadOnly">Flags a field as being read-only.</param>
		/// <param name="NotSame">Flags a field as having an undefined or uncertain value.</param>
		public MediaField(DataForm Form, string Var, string Label, bool Required, string[] ValueStrings, KeyValuePair<string, string>[] Options, string Description,
			DataType DataType, ValidationMethod ValidationMethod, Media Media, string Error, bool PostBack, bool ReadOnly, bool NotSame)
			: base(Form, Var, Label, Required, ValueStrings, Options, Description, DataType, ValidationMethod, Error, PostBack, ReadOnly, NotSame)
		{
			this.media = Media;
		}

		/// <summary>
		/// Media form field.
		/// </summary>
		/// <param name="Var">Variable name</param>
		/// <param name="Media">Media element.</param>
		public MediaField(string Var, Media Media)
			: base(null, Var, string.Empty, false, Array.Empty<string>(), null, string.Empty, null, null, string.Empty, false, true, false)
		{
			this.media = Media;
		}

		/// <summary>
		/// Media form field.
		/// </summary>
		/// <param name="Var">Variable name</param>
		/// <param name="Label">Label</param>
		/// <param name="Media">Media element.</param>
		public MediaField(string Var, string Label, Media Media)
			: base(null, Var, Label, false, Array.Empty<string>(), null, string.Empty, null, null, string.Empty, false, true, false)
		{
			this.media = Media;
		}

		/// <inheritdoc/>
		public override string TypeName => "fixed";

		/// <summary>
		/// Media content to display in the field.
		/// </summary>
		public Media Media => this.media;

		/// <summary>
		/// Allows fields to add additional information to the XML serialization of the field.
		/// </summary>
		/// <param name="Output">XML output.</param>
		/// <param name="ValuesOnly">If only values are to be output.</param>
		/// <param name="IncludeLabels">If labels are to be included.</param>
		public override void AnnotateField(StringBuilder Output, bool ValuesOnly, bool IncludeLabels)
		{
			base.AnnotateField(Output, ValuesOnly, IncludeLabels);

			this.media?.AnnotateField(Output, ValuesOnly, IncludeLabels);
		}
	}
}
