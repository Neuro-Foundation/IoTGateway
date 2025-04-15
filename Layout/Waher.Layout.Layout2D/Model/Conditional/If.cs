﻿using System;
using System.Threading.Tasks;
using System.Xml;
using Waher.Layout.Layout2D.Exceptions;
using Waher.Layout.Layout2D.Model.Attributes;

namespace Waher.Layout.Layout2D.Model.Conditional
{
	/// <summary>
	/// Conditional layout based on one conditional statement.
	/// </summary>
	public class If : DynamicElement
	{
		private ExpressionAttribute condition;
		private LayoutContainer ifTrue;
		private LayoutContainer ifFalse;
		private bool conditionResult;
		private bool conditionResultEvaluated;

		/// <summary>
		/// Conditional layout based on one conditional statement.
		/// </summary>
		/// <param name="Document">Layout document containing the element.</param>
		/// <param name="Parent">Parent element.</param>
		public If(Layout2DDocument Document, ILayoutElement Parent)
			: base(Document, Parent)
		{
		}

		/// <summary>
		/// Local name of type of element.
		/// </summary>
		public override string LocalName => "If";

		/// <summary>
		/// Dynamic array of children
		/// </summary>
		public override ILayoutElement[] DynamicChildren
		{
			get
			{
				ILayoutElement E = this.conditionResult ? this.ifTrue : this.ifFalse;
				if (E is null)
					return Array.Empty<ILayoutElement>();
				else
					return new ILayoutElement[] { E };
			}
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			this.ifTrue?.Dispose();
			this.ifFalse?.Dispose();
		}

		/// <summary>
		/// Condition
		/// </summary>
		public ExpressionAttribute ConditionAttribute
		{
			get => this.condition;
			set => this.condition = value;
		}

		/// <summary>
		/// Populates the element (including children) with information from its XML definition.
		/// </summary>
		/// <param name="Input">XML definition.</param>
		public override async Task FromXml(XmlElement Input)
		{
			await base.FromXml(Input);

			this.condition = new ExpressionAttribute(Input, "condition", this.Document);

			foreach (XmlNode Node in Input.ChildNodes)
			{
				if (Node is XmlElement E)
				{
					ILayoutElement Child = await this.Document.CreateElement(E, this);
					if (Child is True True)
					{
						if (this.ifTrue is null)
							this.ifTrue = True;
						else
							throw new LayoutSyntaxException("If statement already has a True statement.");
					}
					else if (Child is False False)
					{
						if (this.ifFalse is null)
							this.ifFalse = False;
						else
							throw new LayoutSyntaxException("If statement already has a False statement.");
					}
					else
						throw new LayoutSyntaxException("Unrecognized child element in If statement: " + E.NamespaceURI + "#" + E.LocalName);
				}
			}
		}

		/// <summary>
		/// Exports attributes to XML.
		/// </summary>
		/// <param name="Output">XML output.</param>
		public override void ExportAttributes(XmlWriter Output)
		{
			base.ExportAttributes(Output);

			this.condition?.Export(Output);
		}

		/// <summary>
		/// Exports child elements to XML.
		/// </summary>
		/// <param name="Output">XML output.</param>
		public override void ExportChildren(XmlWriter Output)
		{
			base.ExportChildren(Output);

			this.ifTrue?.ToXml(Output);
			this.ifFalse?.ToXml(Output);
		}

		/// <summary>
		/// Creates a new instance of the layout element.
		/// </summary>
		/// <param name="Document">Document containing the new element.</param>
		/// <param name="Parent">Parent element.</param>
		/// <returns>New instance.</returns>
		public override ILayoutElement Create(Layout2DDocument Document, ILayoutElement Parent)
		{
			return new If(Document, Parent);
		}

		/// <summary>
		/// Copies contents (attributes and children) to the destination element.
		/// </summary>
		/// <param name="Destination">Destination element</param>
		public override void CopyContents(ILayoutElement Destination)
		{
			base.CopyContents(Destination);

			if (Destination is If Dest)
			{
				Dest.condition = this.condition?.CopyIfNotPreset(Destination.Document);
				Dest.ifTrue = this.ifTrue?.Copy(Dest) as LayoutContainer;
				Dest.ifFalse = this.ifFalse?.Copy(Dest) as LayoutContainer;
			}
		}

		/// <summary>
		/// Measures layout entities and defines unassigned properties, related to dimensions.
		/// </summary>
		/// <param name="State">Current drawing state.</param>
		/// <returns>If layout contains relative sizes and dimensions should be recalculated.</returns>
		public override async Task DoMeasureDimensions(DrawingState State)
		{
			await base.DoMeasureDimensions(State);

			if (!this.conditionResultEvaluated)
			{
				object Result = this.condition is null ? null : await this.condition.EvaluateAsync(State.Session);
				if (Result is bool b)
					this.conditionResult = b;
				else
					this.conditionResult = false;

				this.conditionResultEvaluated = true;
			}

			ILayoutElement E = this.conditionResult ? this.ifTrue : this.ifFalse;

			if (!(E is null))
				await E.MeasureDimensions(State);
		}

		/// <summary>
		/// Measures layout entities and defines unassigned properties, related to positions.
		/// </summary>
		/// <param name="State">Current drawing state.</param>
		public override void MeasurePositions(DrawingState State)
		{
			if (this.conditionResult)
				this.ifTrue?.MeasurePositions(State);
			else
				this.ifFalse?.MeasurePositions(State);
		}

		/// <summary>
		/// Draws layout entities.
		/// </summary>
		/// <param name="State">Current drawing state.</param>
		public override async Task Draw(DrawingState State)
		{
			if (this.conditionResult)
			{
				if (this.ifTrue?.IsVisible ?? false)
					await this.ifTrue.Draw(State);
			}
			else
			{
				if (this.ifFalse?.IsVisible ?? false)
					await this.ifFalse.Draw(State);
			}
		}

		/// <summary>
		/// Exports the local attributes of the current element.
		/// </summary>
		/// <param name="Output">XML output.</param>
		public override void ExportStateAttributes(XmlWriter Output)
		{
			base.ExportStateAttributes(Output);

			this.condition?.ExportState(Output);
		}

		/// <summary>
		/// Exports the current state of child nodes of the current element.
		/// </summary>
		/// <param name="Output">XML output.</param>
		public override void ExportStateChildren(XmlWriter Output)
		{
			if (this.conditionResult)
			{
				if (this.ifTrue?.IsVisible ?? false)
					this.ifTrue?.ExportState(Output);
			}
			else
			{
				if (this.ifFalse?.IsVisible ?? false)
					this.ifFalse.ExportState(Output);
			}
		}

	}
}
