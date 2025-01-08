﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SkiaSharp;
using Waher.Runtime.Inventory;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Functions.Vectors;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.Sets;
using Waher.Script.Objects.VectorSpaces;
using Waher.Script.Operators.Vectors;

namespace Waher.Script.Graphs
{
	/// <summary>
	/// Handles two-dimensional graphs.
	/// </summary>
	public class Graph2D : Graph
	{
		private LinkedList<IVector> x = new LinkedList<IVector>();
		private LinkedList<IVector> y = new LinkedList<IVector>();
		private readonly LinkedList<object[]> parameters = new LinkedList<object[]>();
		private readonly LinkedList<IPainter2D> painters = new LinkedList<IPainter2D>();
		private IElement minX, maxX;
		private IElement minY, maxY;
		private Type axisTypeX;
		private Type axisTypeY;
		private string title = string.Empty;
		private string labelX = string.Empty;
		private string labelY = string.Empty;
		private bool showXAxis = true;
		private bool showYAxis = true;
		private bool showGrid = true;
		private bool elementwise = false;
		//private readonly bool showZeroX = false;
		//private readonly bool showZeroY = false;

		/// <summary>
		/// Base class for two-dimensional graphs.
		/// </summary>
		public Graph2D()
			: this(new Variables())
		{
		}

		/// <summary>
		/// Base class for two-dimensional graphs.
		/// </summary>
		/// <param name="Variables">Current set of variables, where graph settings might be available.</param>
		public Graph2D(Variables Variables)
			: this(Variables, null, null)
		{
		}

		/// <summary>
		/// Base class for two-dimensional graphs.
		/// </summary>
		/// <param name="Variables">Current set of variables, where graph settings might be available.</param>
		/// <param name="DefaultWidth">Default width.</param>
		/// <param name="DefaultHeight">Default height.</param>
		public Graph2D(Variables Variables, int? DefaultWidth, int? DefaultHeight)
			: base(Variables, DefaultWidth, DefaultHeight)
		{
		}

		/// <summary>
		/// Base class for two-dimensional graphs.
		/// </summary>
		/// <param name="Settings">Graph settings.</param>
		public Graph2D(GraphSettings Settings)
			: base(Settings)
		{
		}

		/// <summary>
		/// Base class for two-dimensional graphs.
		/// </summary>
		/// <param name="Variables">Current set of variables, where graph settings might be available.</param>
		/// <param name="X">X-axis</param>
		/// <param name="Y">Y-axis</param>
		/// <param name="Painter">Painter of graph.</param>
		/// <param name="ShowZeroX">If the y-axis (x=0) should always be shown.</param>
		/// <param name="ShowZeroY">If the x-axis (y=0) should always be shown.</param>
		/// <param name="Node">Node creating the graph.</param>
		/// <param name="Parameters">Graph-specific parameters.</param>
		public Graph2D(Variables Variables, IVector X, IVector Y, IPainter2D Painter, bool ShowZeroX, bool ShowZeroY,
			ScriptNode Node, params object[] Parameters)
			: base(Variables)
		{
			if (X is Interval XI)
				X = new DoubleVector(XI.GetArray());

			if (Y is Interval YI)
				Y = new DoubleVector(YI.GetArray());

			int i, c = X.Dimension;
			bool HasNull = false;
			IElement ex, ey;
			IElement Zero;

			if (c != Y.Dimension)
				throw new ScriptException("X and Y series must be equally large.");

			for (i = 0; i < c; i++)
			{
				ex = X.GetElement(i);
				ey = Y.GetElement(i);

				if (ex.AssociatedObjectValue is null || ey.AssociatedObjectValue is null)
				{
					HasNull = true;
					break;
				}
			}

			//this.showZeroX = ShowZeroX;
			//this.showZeroY = ShowZeroY;

			this.minX = Min.CalcMin(X, Node);
			this.maxX = Max.CalcMax(X, Node);

			if (ShowZeroX && c > 0 && this.minX.AssociatedSet is IAbelianGroup AG)
			{
				Zero = AG.AdditiveIdentity;

				this.minX = Min.CalcMin(new ObjectVector(this.minX, Zero), null);
				this.maxX = Max.CalcMax(new ObjectVector(this.maxX, Zero), null);
			}

			this.minY = Min.CalcMin(Y, Node);
			this.maxY = Max.CalcMax(Y, Node);

			if (ShowZeroY && c > 0 && this.minY.AssociatedSet is IAbelianGroup AG2)
			{
				Zero = AG2.AdditiveIdentity;

				this.minY = Min.CalcMin(new ObjectVector(this.minY, Zero), null);
				this.maxY = Max.CalcMax(new ObjectVector(this.maxY, Zero), null);
			}

			if (HasNull)
			{
				LinkedList<IElement> X2 = new LinkedList<IElement>();
				LinkedList<IElement> Y2 = new LinkedList<IElement>();

				this.axisTypeX = null;
				this.axisTypeY = null;

				for (i = 0; i < c; i++)
				{
					ex = X.GetElement(i);
					ey = Y.GetElement(i);

					if (ex.AssociatedObjectValue is null || ey.AssociatedObjectValue is null)
					{
						if (!(X2.First is null))
						{
							this.AddSegment(X, Y, X2, Y2, Node, Painter, Parameters);
							X2 = new LinkedList<IElement>();
							Y2 = new LinkedList<IElement>();
						}
					}
					else
					{
						X2.AddLast(ex);
						Y2.AddLast(ey);
					}
				}

				if (!(X2.First is null))
					this.AddSegment(X, Y, X2, Y2, Node, Painter, Parameters);
			}
			else
			{
				this.axisTypeX = X.GetType();
				this.axisTypeY = Y.GetType();

				if (c > 0)
				{
					this.x.AddLast(X);
					this.y.AddLast(Y);
					this.painters.AddLast(Painter);
					this.parameters.AddLast(Parameters);
				}
			}
		}

		private void AddSegment(IVector X, IVector Y, ICollection<IElement> X2, ICollection<IElement> Y2,
			ScriptNode Node, IPainter2D Painter, params object[] Parameters)
		{
			IVector X2V = (IVector)X.Encapsulate(X2, Node);
			IVector Y2V = (IVector)Y.Encapsulate(Y2, Node);

			if (this.axisTypeX is null)
			{
				this.axisTypeX = X2V.GetType();
				this.axisTypeY = Y2V.GetType();
			}
			else if (X2V.GetType() != this.axisTypeX || Y2V.GetType() != this.axisTypeY)
				throw new ScriptException("Incompatible types of series.");

			this.x.AddLast(X2V);
			this.y.AddLast(Y2V);
			this.painters.AddLast(Painter);
			this.parameters.AddLast(Parameters);
		}

		/// <summary>
		/// X-axis series.
		/// </summary>
		public LinkedList<IVector> X => this.x;

		/// <summary>
		/// Y-axis series.
		/// </summary>
		public LinkedList<IVector> Y => this.y;

		/// <summary>
		/// Parameters.
		/// </summary>
		public LinkedList<object[]> Parameters => this.parameters;

		/// <summary>
		/// Smallest X-value.
		/// </summary>
		public IElement MinX => this.minX;

		/// <summary>
		/// Largest X-value.
		/// </summary>
		public IElement MaxX => this.maxX;

		/// <summary>
		/// Smallest Y-value.
		/// </summary>
		public IElement MinY => this.minY;

		/// <summary>
		/// Largest Y-value.
		/// </summary>
		public IElement MaxY => this.maxY;

		/// <summary>
		/// If graph was generated using element-wise addition operations.
		/// </summary>
		public bool Elementwise => this.elementwise;

		/// <summary>
		/// Title for graph.
		/// </summary>
		public string Title
		{
			get => this.title;
			set => this.title = value;
		}

		/// <summary>
		/// Label for x-axis.
		/// </summary>
		public string LabelX
		{
			get => this.labelX;
			set => this.labelX = value;
		}

		/// <summary>
		/// Label for y-axis.
		/// </summary>
		public string LabelY
		{
			get => this.labelY;
			set => this.labelY = value;
		}

		/// <summary>
		/// If the X-axis is to be displayed.
		/// </summary>
		public bool ShowXAxis
		{
			get => this.showXAxis;
			set => this.showXAxis = value;
		}

		/// <summary>
		/// If the Y-axis is to be displayed.
		/// </summary>
		public bool ShowYAxis
		{
			get => this.showYAxis;
			set => this.showYAxis = value;
		}

		/// <summary>
		/// If the grid is to be displayed.
		/// </summary>
		public bool ShowGrid
		{
			get => this.showGrid;
			set => this.showGrid = value;
		}

		/// <summary>
		/// Tries to add an element to the current element, from the left.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override ISemiGroupElement AddLeft(ISemiGroupElement Element)
		{
			return Element.AddRight(this);
		}

		/// <summary>
		/// Tries to add an element to the current element, from the right.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override ISemiGroupElement AddRight(ISemiGroupElement Element)
		{
			return this.AddRight(Element, false);
		}

		/// <summary>
		/// Tries to add an element to the current element, from the left, element-wise.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override ISemiGroupElementWise AddLeftElementWise(ISemiGroupElementWise Element)
		{
			return Element.AddRightElementWise(this);
		}

		/// <summary>
		/// Tries to add an element to the current element, from the right, element-wise.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override ISemiGroupElementWise AddRightElementWise(ISemiGroupElementWise Element)
		{
			return this.AddRight(Element, true) as ISemiGroupElementWise;
		}

		/// <summary>
		/// Tries to add an element to the current element, from the right.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <param name="ElementWise">If element-wise addition is to be performed.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		protected ISemiGroupElement AddRight(ISemiGroupElement Element, bool ElementWise)
		{
			if (this.x.First is null)
				return Element;

			if (!(Element is Graph2D G))
				return null;

			if (G.x.First is null)
				return this;

			Graph2D Result = new Graph2D(this.Settings)
			{
				minX = this.minX,
				maxX = this.maxX,
				minY = this.minY,
				maxY = this.maxY,
				axisTypeX = this.axisTypeX,
				axisTypeY = this.axisTypeY,
				title = this.title,
				labelX = this.labelX,
				labelY = this.labelY,
				SameScale = this.SameScale
			};

			foreach (IVector v in this.x)
				Result.x.AddLast(v);

			foreach (IVector v in this.y)
				Result.y.AddLast(v);

			foreach (IPainter2D Painter in this.painters)
				Result.painters.AddLast(Painter);

			foreach (object[] P in this.parameters)
				Result.parameters.AddLast(P);

			if (G.axisTypeX != this.axisTypeX || G.axisTypeY != this.axisTypeY)
				throw new ScriptException("Incompatible types of series.");

			if (ElementWise)
			{
				GetLabels(ref Result.minX, ref Result.maxX, this.x, 2, out LabelType XLabelType);
				GetLabels(ref Result.minY, ref Result.maxY, this.y, 2, out LabelType YLabelType);

				if (XLabelType == LabelType.String && (YLabelType == LabelType.Double || YLabelType == LabelType.PhysicalQuantity))
					ElementwiseAccumulatedAddition(ref Result.x, ref Result.y, G.x, G.y, ref Result.minX, ref Result.maxX, ref Result.minY, ref Result.maxY, G.minX, G.maxX);
				else if (YLabelType == LabelType.String && (XLabelType == LabelType.Double || XLabelType == LabelType.PhysicalQuantity))
					ElementwiseAccumulatedAddition(ref Result.y, ref Result.x, G.y, G.x, ref Result.minY, ref Result.maxY, ref Result.minX, ref Result.maxX, G.minY, G.maxY);
				else
					ElementWise = false;
			}

			if (!ElementWise)
			{
				foreach (IVector v in G.x)
					Result.x.AddLast(v);

				foreach (IVector v in G.y)
					Result.y.AddLast(v);

				Result.minX = Min.CalcMin((IVector)VectorDefinition.Encapsulate(new IElement[] { Result.minX, G.minX }, false, null), null);
				Result.maxX = Max.CalcMax((IVector)VectorDefinition.Encapsulate(new IElement[] { Result.maxX, G.maxX }, false, null), null);
				Result.minY = Min.CalcMin((IVector)VectorDefinition.Encapsulate(new IElement[] { Result.minY, G.minY }, false, null), null);
				Result.maxY = Max.CalcMax((IVector)VectorDefinition.Encapsulate(new IElement[] { Result.maxY, G.maxY }, false, null), null);
			}

			foreach (IPainter2D Painter in G.painters)
				Result.painters.AddLast(Painter);

			foreach (object[] P in G.parameters)
				Result.parameters.AddLast(P);

			Result.showXAxis |= G.showXAxis;
			Result.showYAxis |= G.showYAxis;
			Result.elementwise = ElementWise;

			return Result;
		}

		private static void ElementwiseAccumulatedAddition(ref LinkedList<IVector> DestFixed, ref LinkedList<IVector> DestValues,
			LinkedList<IVector> AddFixed, LinkedList<IVector> AddValues, ref IElement MinFixed, ref IElement MaxFixed,
			ref IElement MinValues, ref IElement MaxValues, IElement AddMinFixed, IElement AddMaxFixed)
		{
			Dictionary<string, IElement> Values = new Dictionary<string, IElement>();
			IEnumerator<IVector> vX = DestFixed.GetEnumerator();
			IEnumerator<IVector> vY = DestValues.GetEnumerator();
			IEnumerator<IElement> eX;
			IEnumerator<IElement> eY;
			IVector X;
			IVector Y;
			LinkedList<IElement> Y2;

			while (vX.MoveNext() && vY.MoveNext())
			{
				X = vX.Current;
				Y = vY.Current;

				eX = X.ChildElements.GetEnumerator();
				eY = Y.ChildElements.GetEnumerator();

				while (eX.MoveNext() && eY.MoveNext())
					Values[eX.Current.AssociatedObjectValue?.ToString() ?? string.Empty] = eY.Current;
			}

			vX = AddFixed.GetEnumerator();
			vY = AddValues.GetEnumerator();

			while (vX.MoveNext() && vY.MoveNext())
			{
				X = vX.Current;
				Y = vY.Current;
				Y2 = new LinkedList<IElement>();

				eX = X.ChildElements.GetEnumerator();
				eY = Y.ChildElements.GetEnumerator();

				while (eX.MoveNext() && eY.MoveNext())
				{
					if (Values.TryGetValue(eX.Current.AssociatedObjectValue?.ToString() ?? string.Empty, out IElement Value))
						Y2.AddLast(Operators.Arithmetics.Add.EvaluateAddition(Value, eY.Current, null));
					else
						Y2.AddLast(eY.Current);
				}

				DestFixed.AddLast(X);
				DestValues.AddLast(Y = (IVector)Y.Encapsulate(Y2, null));

				MinValues = Min.CalcMin((IVector)VectorDefinition.Encapsulate(new IElement[] { MinValues, Min.CalcMin(Y, null) }, false, null), null);
				MaxValues = Max.CalcMax((IVector)VectorDefinition.Encapsulate(new IElement[] { MaxValues, Max.CalcMax(Y, null) }, false, null), null);
			}

			MinFixed = Min.CalcMin((IVector)VectorDefinition.Encapsulate(new IElement[] { MinFixed, AddMinFixed }, false, null), null);
			MaxFixed = Max.CalcMax((IVector)VectorDefinition.Encapsulate(new IElement[] { MaxFixed, AddMaxFixed }, false, null), null);

			IVector Labels = GetLabels(ref MinFixed, ref MaxFixed, DestFixed, 2, out LabelType LabelType);
			string[] Strings = LabelStrings(Labels, LabelType);
			LinkedList<IVector> NormalizedX = new LinkedList<IVector>();
			LinkedList<IVector> NormalizedY = new LinkedList<IVector>();
			Dictionary<string, IElement> Sorted = new Dictionary<string, IElement>();
			IElement Zero = (MinValues.AssociatedSet as Group)?.AdditiveIdentity ?? DoubleNumber.ZeroElement;

			vX = DestFixed.GetEnumerator();
			vY = DestValues.GetEnumerator();

			while (vX.MoveNext() && vY.MoveNext())
			{
				X = vX.Current;
				Y = vY.Current;
				Y2 = new LinkedList<IElement>();

				eX = X.ChildElements.GetEnumerator();
				eY = Y.ChildElements.GetEnumerator();

				while (eX.MoveNext() && eY.MoveNext())
					Sorted[eX.Current.AssociatedObjectValue?.ToString() ?? string.Empty] = eY.Current;

				foreach (string s in Strings)
				{
					if (Sorted.TryGetValue(s, out IElement E))
						Y2.AddLast(E);
					else
						Y2.AddLast(Zero);
				}

				NormalizedX.AddLast(Labels);
				NormalizedY.AddLast((IVector)Y.Encapsulate(Y2, null));
			}

			DestFixed = NormalizedX;
			DestValues = NormalizedY;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (!(obj is Graph2D G))
				return false;

			return (
				this.minX.Equals(G.minX) &&
				this.maxX.Equals(G.maxX) &&
				this.minY.Equals(G.minY) &&
				this.maxY.Equals(G.maxY) &&
				this.axisTypeX.Equals(G.axisTypeX) &&
				this.axisTypeY.Equals(G.axisTypeY) &&
				this.title.Equals(G.title) &&
				this.labelX.Equals(G.labelX) &&
				this.labelY.Equals(G.labelY) &&
				this.showXAxis.Equals(G.showXAxis) &&
				this.showYAxis.Equals(G.showYAxis) &&
				this.showGrid.Equals(G.showGrid) &&
				this.Equals(this.x.GetEnumerator(), G.x.GetEnumerator()) &&
				this.Equals(this.y.GetEnumerator(), G.y.GetEnumerator()) &&
				this.Equals(this.parameters.GetEnumerator(), G.parameters.GetEnumerator()) &&
				this.Equals(this.painters.GetEnumerator(), G.painters.GetEnumerator()));
		}

		private bool Equals(IEnumerator e1, IEnumerator e2)
		{
			bool b1 = e1.MoveNext();
			bool b2 = e2.MoveNext();

			while (b1 && b2)
			{
				if (!e1.Current.Equals(e2.Current))
					return false;

				b1 = e1.MoveNext();
				b2 = e2.MoveNext();
			}

			return !(b1 || b2);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = this.minX.GetHashCode();
			Result ^= Result << 5 ^ this.maxX.GetHashCode();
			Result ^= Result << 5 ^ this.minY.GetHashCode();
			Result ^= Result << 5 ^ this.maxY.GetHashCode();
			Result ^= Result << 5 ^ this.axisTypeX.GetHashCode();
			Result ^= Result << 5 ^ this.axisTypeY.GetHashCode();
			Result ^= Result << 5 ^ this.title.GetHashCode();
			Result ^= Result << 5 ^ this.labelX.GetHashCode();
			Result ^= Result << 5 ^ this.labelY.GetHashCode();
			Result ^= Result << 5 ^ this.showXAxis.GetHashCode();
			Result ^= Result << 5 ^ this.showYAxis.GetHashCode();
			Result ^= Result << 5 ^ this.showGrid.GetHashCode();

			foreach (IElement E in this.x)
				Result ^= Result << 5 ^ E.GetHashCode();

			foreach (IElement E in this.y)
				Result ^= Result << 5 ^ E.GetHashCode();

			foreach (object Obj in this.parameters)
				Result ^= Result << 5 ^ Obj.GetHashCode();

			foreach (IPainter2D Painter in this.painters)
				Result ^= Result << 5 ^ Painter.GetHashCode();

			return Result;
		}

		/// <summary>
		/// Creates a bitmap of the graph.
		/// </summary>
		/// <param name="Settings">Graph settings.</param>
		/// <param name="States">State object(s) that contain graph-specific information about its inner states.
		/// These can be used in calls back to the graph object to make actions on the generated graph.</param>
		/// <returns>Bitmap</returns>
		public override PixelInformation CreatePixels(GraphSettings Settings, out object[] States)
		{
			using (SKSurface Surface = SKSurface.Create(new SKImageInfo(Settings.Width, Settings.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul)))
			{
				SKCanvas Canvas = Surface.Canvas;

				States = new object[0];

				Canvas.Clear(Settings.BackgroundColor);

				int x1, y1, x2, y2, x3, y3, w, h;

				x1 = Settings.MarginLeft;
				x2 = Settings.Width - Settings.MarginRight;
				y1 = Settings.MarginTop;
				y2 = Settings.Height - Settings.MarginBottom;

				if (!string.IsNullOrEmpty(this.labelY))
					x1 += (int)(Settings.LabelFontSize * 2 + 0.5);

				if (!string.IsNullOrEmpty(this.labelX))
					y2 -= (int)(Settings.LabelFontSize * 2 + 0.5);

				if (!string.IsNullOrEmpty(this.title))
					y1 += (int)(Settings.LabelFontSize * 2 + 0.5);

				IVector YLabels = GetLabels(ref this.minY, ref this.maxY, this.y, Settings.ApproxNrLabelsY, out LabelType YLabelType);
				string[] YLabelStrings = LabelStrings(YLabels, YLabelType);
				SKPaint Font = new SKPaint()
				{
					FilterQuality = SKFilterQuality.High,
					HintingLevel = SKPaintHinting.Full,
					SubpixelText = true,
					IsAntialias = true,
					Style = SKPaintStyle.Fill,
					Color = Settings.AxisColor,
					Typeface = SKTypeface.FromFamilyName(Settings.FontName, SKFontStyle.Normal),
					TextSize = (float)Settings.LabelFontSize
				};
				SKRect Bounds = new SKRect();
				float Size;
				double MaxSize = 0;

				if (this.showYAxis)
				{
					foreach (IElement Label in YLabels.ChildElements)
					{
						Font.MeasureText(LabelString(Label, YLabelType), ref Bounds);
						Size = Bounds.Width;
						if (Size > MaxSize)
							MaxSize = Size;
					}
				}

				x3 = (int)Math.Ceiling(x1 + MaxSize) + Settings.MarginLabel;

				IVector XLabels = GetLabels(ref this.minX, ref this.maxX, this.x, Settings.ApproxNrLabelsX, out LabelType XLabelType);
				string[] XLabelStrings = LabelStrings(XLabels, XLabelType);
				MaxSize = 0;

				if (this.showXAxis)
				{
					foreach (IElement Label in XLabels.ChildElements)
					{
						Font.MeasureText(LabelString(Label, XLabelType), ref Bounds);
						Size = Bounds.Height;
						if (Size > MaxSize)
							MaxSize = Size;
					}
				}

				y3 = (int)Math.Floor(y2 - MaxSize) - Settings.MarginLabel;
				w = x2 - x3;
				h = y3 - y1;

				SKPaint AxisBrush = new SKPaint()
				{
					FilterQuality = SKFilterQuality.High,
					IsAntialias = true,
					Style = SKPaintStyle.Fill,
					Color = Settings.AxisColor
				};
				SKPaint GridBrush = new SKPaint()
				{
					FilterQuality = SKFilterQuality.High,
					IsAntialias = true,
					Style = SKPaintStyle.Fill,
					Color = Settings.GridColor
				};
				SKPaint AxisPen = new SKPaint()
				{
					FilterQuality = SKFilterQuality.High,
					IsAntialias = true,
					Style = SKPaintStyle.Stroke,
					Color = Settings.AxisColor,
					StrokeWidth = Settings.AxisWidth
				};
				SKPaint GridPen = new SKPaint()
				{
					FilterQuality = SKFilterQuality.High,
					IsAntialias = true,
					Style = SKPaintStyle.Stroke,
					Color = Settings.GridColor,
					StrokeWidth = Settings.GridWidth
				};

				if (this.SameScale &&
					this.minX.AssociatedObjectValue is double MinX &&
					this.maxX.AssociatedObjectValue is double MaxX &&
					this.minY.AssociatedObjectValue is double MinY &&
					this.maxY.AssociatedObjectValue is double MaxY)
				{
					double DX = MaxX - MinX;
					double DY = MaxY - MinY;
					double SX = w / (DX == 0 ? 1 : DX);
					double SY = h / (DY == 0 ? 1 : DY);

					if (SX < SY)
					{
						int h2 = (int)(h * SX / SY + 0.5);
						y3 -= (h - h2) / 2;
						h = h2;
					}
					else if (SY < SX)
					{
						int w2 = (int)(w * SY / SX + 0.5);
						x3 += (w - w2) / 2;
						w = w2;
					}
				}

				double OrigoX;
				double OrigoY;

				if (this.minX.AssociatedSet is IAbelianGroup AgX)
					OrigoX = Scale(new ObjectVector(AgX.AdditiveIdentity), this.minX, this.maxX, x3, w, null)[0];
				else
					OrigoX = 0;

				if (this.minY.AssociatedSet is IAbelianGroup AgY)
					OrigoY = Scale(new ObjectVector(AgY.AdditiveIdentity), this.minY, this.maxY, y3, -h, null)[0];
				else
					OrigoY = 0;

				DrawingArea DrawingArea = new DrawingArea(this.minX, this.maxX, this.minY, this.maxY, x3, y3, w, -h, (float)OrigoX, (float)OrigoY, this.elementwise);
				double[] LabelYY = DrawingArea.ScaleY(YLabels);
				Dictionary<string, double> YLabelPositions = YLabelType == LabelType.String ? new Dictionary<string, double>() : null;
				int i = 0;
				float f;
				double d;
				string s;

				foreach (IElement Label in YLabels.ChildElements)
				{
					s = YLabelStrings[i];
					Font.MeasureText(s, ref Bounds);
					d = LabelYY[i++];
					f = (float)d;

					if (!(YLabelPositions is null))
						YLabelPositions[s] = d;

					if (this.showGrid)
					{
						if (Label.AssociatedObjectValue is double Lbl && Lbl == 0)
							Canvas.DrawLine(x3, f, x2, f, AxisPen);
						else
							Canvas.DrawLine(x3, f, x2, f, GridPen);
					}

					if (this.showYAxis)
					{
						f += Bounds.Height * 0.5f;
						Canvas.DrawText(s, x3 - Bounds.Width - Settings.MarginLabel, f, Font);
					}
				}

				double[] LabelXX = DrawingArea.ScaleX(XLabels);
				Dictionary<string, double> XLabelPositions = XLabelType == LabelType.String ? new Dictionary<string, double>() : null;
				i = 0;

				foreach (IElement Label in XLabels.ChildElements)
				{
					s = XLabelStrings[i];
					Font.MeasureText(s, ref Bounds);
					d = LabelXX[i++];
					f = (float)d;

					if (!(XLabelPositions is null))
						XLabelPositions[s] = d;

					if (this.showGrid)
					{
						if (Label.AssociatedObjectValue is double DLbl && DLbl == 0)
							Canvas.DrawLine(f, y1, f, y3, AxisPen);
						else
							Canvas.DrawLine(f, y1, f, y3, GridPen);
					}

					if (this.showXAxis)
					{
						Size = Bounds.Width;
						f -= Size * 0.5f;
						if (f < x3)
							f = x3;
						else if (f + Size > x3 + w)
							f = x3 + w - Size;

						Canvas.DrawText(s, f, y3 + Settings.MarginLabel + (float)Settings.LabelFontSize, Font);
					}
				}

				DrawingArea.XLabelPositions = XLabelPositions;
				DrawingArea.YLabelPositions = YLabelPositions;

				Font.Dispose();
				Font = null;

				Font = new SKPaint()
				{
					FilterQuality = SKFilterQuality.High,
					HintingLevel = SKPaintHinting.Full,
					SubpixelText = true,
					IsAntialias = true,
					Style = SKPaintStyle.Fill,
					Color = Settings.AxisColor,
					Typeface = SKTypeface.FromFamilyName(Settings.FontName, SKFontStyle.Bold),
					TextSize = (float)(Settings.LabelFontSize * 1.5)
				};

				if (!string.IsNullOrEmpty(this.title))
				{
					Font.MeasureText(this.title, ref Bounds);
					Size = Bounds.Width;

					f = x3 + (x2 - x3 - Size) * 0.5f;

					if (f < x3)
						f = x3;
					else if (f + Size > x3 + w)
						f = x3 + w - Size;

					Canvas.DrawText(this.title, f, (float)(Settings.MarginTop + 0.1 * Settings.LabelFontSize - Bounds.Top), Font);
				}

				if (!string.IsNullOrEmpty(this.labelX))
				{
					Font.MeasureText(this.labelX, ref Bounds);
					Size = Bounds.Width;

					f = x3 + (x2 - x3 - Size) * 0.5f;

					if (f < x3)
						f = x3;
					else if (f + Size > x3 + w)
						f = x3 + w - Size;

					Canvas.DrawText(this.labelX, f, (float)(Settings.Height - Settings.MarginBottom - 0.1 * Settings.LabelFontSize - Bounds.Height - Bounds.Top), Font);
				}

				if (!string.IsNullOrEmpty(this.labelY))
				{
					Font.MeasureText(this.labelY, ref Bounds);
					Size = Bounds.Width;

					f = y3 - (y3 - y1 - Size) * 0.5f;

					if (f - Size < y1)
						f = y1 + Size;
					else if (f > y3 + h)
						f = y3 + h;

					Canvas.Translate((float)(Settings.MarginLeft + 0.1 * Settings.LabelFontSize - Bounds.Top), f);
					Canvas.RotateDegrees(-90);
					Canvas.DrawText(this.labelY, 0, 0, Font);
					Canvas.ResetMatrix();
				}

				IEnumerator<IVector> ex = this.x.GetEnumerator();
				IEnumerator<IVector> ey = this.y.GetEnumerator();
				IEnumerator<object[]> eParameters = this.parameters.GetEnumerator();
				IEnumerator<IPainter2D> ePainters = this.painters.GetEnumerator();
				SKPoint[] Points;
				SKPoint[] PrevPoints = null;
				object[] PrevParameters = null;
				IPainter2D PrevPainter = null;

				while (ex.MoveNext() && ey.MoveNext() && eParameters.MoveNext() && ePainters.MoveNext())
				{
					Points = DrawingArea.Scale(ex.Current, ey.Current);

					if (!(PrevPainter is null) && ePainters.Current.GetType() == PrevPainter.GetType())
						ePainters.Current.DrawGraph(Canvas, Points, eParameters.Current, PrevPoints, PrevParameters, DrawingArea);
					else
						ePainters.Current.DrawGraph(Canvas, Points, eParameters.Current, null, null, DrawingArea);

					PrevPoints = Points;
					PrevParameters = eParameters.Current;
					PrevPainter = ePainters.Current;
				}

				using (SKImage Result = Surface.Snapshot())
				{
					Font?.Dispose();

					AxisBrush.Dispose();
					GridBrush.Dispose();
					GridPen.Dispose();
					AxisPen.Dispose();

					States = new object[] { DrawingArea };

					return PixelInformation.FromImage(Result);
				}
			}
		}

		/// <summary>
		/// Gets script corresponding to a point in a generated bitmap representation of the graph.
		/// </summary>
		/// <param name="X">X-Coordinate.</param>
		/// <param name="Y">Y-Coordinate.</param>
		/// <param name="States">State objects for the generated bitmap.</param>
		/// <returns>Script.</returns>
		public override string GetBitmapClickScript(double X, double Y, object[] States)
		{
			DrawingArea DrawingArea = (DrawingArea)States[0];

			IElement X2 = DrawingArea.DescaleX(X);
			IElement Y2 = DrawingArea.DescaleY(Y);

			return "[" + X2.ToString() + "," + Y2.ToString() + "]";
		}

		/// <summary>
		/// Exports graph specifics to XML.
		/// </summary>
		/// <param name="Output">XML output.</param>
		public override void ExportGraph(XmlWriter Output)
		{
			Output.WriteStartElement("Graph2D");
			Output.WriteAttributeString("title", this.title);
			Output.WriteAttributeString("labelX", this.labelX);
			Output.WriteAttributeString("labelY", this.labelY);
			Output.WriteAttributeString("axisTypeX", this.axisTypeX?.FullName);
			Output.WriteAttributeString("axisTypeY", this.axisTypeY?.FullName);
			Output.WriteAttributeString("minX", ReducedXmlString(this.minX));
			Output.WriteAttributeString("maxX", ReducedXmlString(this.maxX));
			Output.WriteAttributeString("minY", ReducedXmlString(this.minY));
			Output.WriteAttributeString("maxY", ReducedXmlString(this.maxY));
			Output.WriteAttributeString("showXAxis", this.showXAxis ? "true" : "false");
			Output.WriteAttributeString("showYAxis", this.showYAxis ? "true" : "false");
			Output.WriteAttributeString("showGrid", this.showGrid ? "true" : "false");

			Dictionary<string, string> Series = new Dictionary<string, string>();
			string Label;
			string s;
			int i = 1;

			foreach (IVector v in this.x)
			{
				s = ReducedXmlString(v);
				if (Series.TryGetValue(s, out Label))
					Output.WriteElementString("X", Label);
				else
				{
					Label = "X" + (i++).ToString();
					Series[s] = Label;
					Output.WriteElementString("X", Label + ":=" + s);
				}
			}

			i = 1;

			foreach (IVector v in this.y)
			{
				s = ReducedXmlString(v);
				if (Series.TryGetValue(s, out Label))
					Output.WriteElementString("Y", Label);
				else
				{
					Label = "Y" + (i++).ToString();
					Series[s] = Label;
					Output.WriteElementString("Y", Label + ":=" + s);
				}
			}

			i = 1;

			foreach (object[] v in this.parameters)
			{
				s = Expression.ToString(new ObjectVector(v));
				if (Series.TryGetValue(s, out Label))
					Output.WriteElementString("Parameters", Label);
				else
				{
					Label = "P" + (i++).ToString();
					Series[s] = Label;
					Output.WriteElementString("Parameters", Label + ":=" + s);
				}
			}

			foreach (IPainter2D Painter in this.painters)
				Output.WriteElementString("Painter", Painter.GetType().FullName);

			Output.WriteEndElement();
		}

		/// <summary>
		/// Generates an XML value string of an element, possible with reduced resolution, to avoid unnecessary digits when
		/// repersenting graphs remotely.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>String representation.</returns>
		public static string ReducedXmlString(double Value)
		{
			return ((float)Value).ToString().Replace(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
		}

		/// <summary>
		/// Generates an XML value string of an element, possible with reduced resolution, to avoid unnecessary digits when
		/// repersenting graphs remotely.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>String representation.</returns>
		public static string ReducedXmlString(DoubleVector Value)
		{
			StringBuilder sb = null;

			foreach (double d in Value.Values)
			{
				if (sb is null)
					sb = new StringBuilder("[");
				else
					sb.Append(',');

				sb.Append(((float)d).ToString().Replace(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, "."));
			}

			if (sb is null)
				return "[]";
			else
			{
				sb.Append(']');
				return sb.ToString();
			}
		}

		/// <summary>
		/// Generates an XML value string of an element, possible with reduced resolution, to avoid unnecessary digits when
		/// repersenting graphs remotely.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>String representation.</returns>
		public static string ReducedXmlString(DateTimeVector Value)
		{
			StringBuilder sb = null;

			foreach (DateTime d in Value.Values)
			{
				if (sb is null)
					sb = new StringBuilder("DateTime([");
				else
					sb.Append(',');

				int Year = d.Year;
				int Month = d.Month;
				int Day = d.Day;
				int Hour = d.Hour;
				int Minute = d.Minute;
				int Second = d.Second;
				int MSecond = d.Millisecond;
				int Mask = 0;

				if (Month != 0)
					Mask |= 1;

				if (Day != 0)
					Mask |= 2;

				if (Hour != 0)
					Mask |= 4;

				if (Minute != 0)
					Mask |= 8;

				if (Second != 0)
					Mask |= 16;

				if (MSecond != 0)
					Mask |= 32;

				sb.Append('"');
				sb.Append(Year.ToString());

				if (Mask > 0)
				{
					Mask >>= 1;
					sb.Append('-');
					sb.Append(Month.ToString());

					if (Mask > 0)
					{
						Mask >>= 1;
						sb.Append('-');
						sb.Append(Day.ToString());

						if (Mask > 0)
						{
							Mask >>= 1;
							sb.Append('T');
							sb.Append(Hour.ToString());

							if (Mask > 0)
							{
								Mask >>= 1;
								sb.Append(':');
								sb.Append(Minute.ToString());

								if (Mask > 0)
								{
									Mask >>= 1;
									sb.Append(':');
									sb.Append(Second.ToString());

									if (Mask > 0)
									{
										Mask >>= 1;
										sb.Append('.');
										sb.Append(MSecond.ToString());
									}
								}
							}
						}
					}
				}

				if (d.Kind == DateTimeKind.Utc)
					sb.Append('z');

				sb.Append('"');
			}

			if (sb is null)
				return "[]";
			else
			{
				sb.Append("])");
				return sb.ToString();
			}
		}

		/// <summary>
		/// Generates an XML value string of an element, possible with reduced resolution, to avoid unnecessary digits when
		/// repersenting graphs remotely.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>String representation.</returns>
		public static string ReducedXmlStringTimeSpans(ObjectVector Value)
		{
			StringBuilder sb = null;

			foreach (IElement E in Value.Values)
			{
				if (!(E.AssociatedObjectValue is TimeSpan TS))
					continue;

				if (sb is null)
					sb = new StringBuilder("TimeSpan([");
				else
					sb.Append(',');

				int Days = TS.Days;
				int Hours = TS.Hours;
				int Minutes = TS.Minutes;
				int Seconds = TS.Seconds;
				int MSeconds = TS.Milliseconds;
				int Mask = 0;

				if (Hours != 0)
					Mask |= 1;

				if (Minutes != 0)
					Mask |= 2;

				if (Seconds != 0)
					Mask |= 4;

				if (MSeconds != 0)
					Mask |= 5;

				sb.Append('"');

				if (Days != 0 || Mask == 0)
					sb.Append(Days.ToString());

				if (Mask > 0)
				{
					Mask >>= 1;

					if (Days != 0)
						sb.Append('.');

					sb.Append(Hours.ToString());

					if (Mask > 0)
					{
						Mask >>= 1;
						sb.Append(':');
						sb.Append(Minutes.ToString());

						if (Mask > 0)
						{
							Mask >>= 1;
							sb.Append(':');
							sb.Append(Seconds.ToString());

							if (Mask > 0)
							{
								Mask >>= 1;
								sb.Append('.');
								sb.Append(MSeconds.ToString());
							}
						}
					}
				}

				sb.Append('"');
			}

			if (sb is null)
				return "[]";
			else
			{
				sb.Append("])");
				return sb.ToString();
			}
		}

		/// <summary>
		/// Generates an XML value string of an element, possible with reduced resolution, to avoid unnecessary digits when
		/// repersenting graphs remotely.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>String representation.</returns>
		public static string ReducedXmlString(IElement Value)
		{
			if (Value.AssociatedObjectValue is double N)
				return ReducedXmlString(N);
			else if (Value is DoubleVector v)
				return ReducedXmlString(v);
			else if (Value is DateTimeVector DT)
				return ReducedXmlString(DT);
			else if (Value is ObjectVector ov && IsTimeSpanVector(ov))
				return ReducedXmlStringTimeSpans(ov);
			else
				return Expression.ToString(Value);
		}

		private static bool IsTimeSpanVector(ObjectVector v)
		{
			foreach (IElement E in v.VectorElements)
			{
				if (!(E.AssociatedObjectValue is TimeSpan))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Imports graph specifics from XML.
		/// </summary>
		/// <param name="Xml">XML input.</param>
		public override async Task ImportGraphAsync(XmlElement Xml)
		{
			Variables Variables = new Variables();

			foreach (XmlAttribute Attr in Xml.Attributes)
			{
				switch (Attr.Name)
				{
					case "title":
						this.title = Attr.Value;
						break;

					case "labelX":
						this.labelX = Attr.Value;
						break;

					case "labelY":
						this.labelY = Attr.Value;
						break;

					case "axisTypeX":
						this.axisTypeX = Types.GetType(Attr.Value);
						break;

					case "axisTypeY":
						this.axisTypeY = Types.GetType(Attr.Value);
						break;

					case "minX":
						this.minX = await ParseAsync(Attr.Value, Variables);
						break;

					case "maxX":
						this.maxX = await ParseAsync(Attr.Value, Variables);
						break;

					case "minY":
						this.minY = await ParseAsync(Attr.Value, Variables);
						break;

					case "maxY":
						this.maxY = await ParseAsync(Attr.Value, Variables);
						break;

					case "showXAxis":
						this.showXAxis = Attr.Value == "true";
						break;

					case "showYAxis":
						this.showYAxis = Attr.Value == "true";
						break;

					case "showGrid":
						this.showGrid = Attr.Value == "true";
						break;
				}
			}

			foreach (XmlNode N in Xml.ChildNodes)
			{
				if (N is XmlElement E)
				{
					switch (E.LocalName)
					{
						case "X":
							this.x.AddLast((IVector)await ParseAsync(E.InnerText, Variables));
							break;

						case "Y":
							this.y.AddLast((IVector)await ParseAsync(E.InnerText, Variables));
							break;

						case "Parameters":
							IVector v = (IVector)await ParseAsync(E.InnerText, Variables);
							this.parameters.AddLast(this.ToObjectArray(v));
							break;

						case "Painter":
							this.painters.AddLast((IPainter2D)Types.Instantiate(Types.GetType(E.InnerText)));
							break;
					}
				}
			}
		}

		/// <summary>
		/// If graph uses default color
		/// </summary>
		public override bool UsesDefaultColor
		{
			get
			{
				IEnumerator<object[]> eParameters = this.parameters.GetEnumerator();
				IEnumerator<IPainter2D> ePainter = this.painters.GetEnumerator();

				while (eParameters.MoveNext() && ePainter.MoveNext())
				{
					if (!ePainter.Current.UsesDefaultColor(eParameters.Current))
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Tries to set the default color.
		/// </summary>
		/// <param name="Color">Default color.</param>
		/// <returns>If possible to set.</returns>
		public override bool TrySetDefaultColor(SKColor Color)
		{
			if (!this.UsesDefaultColor)
				return false;

			IEnumerator<object[]> eParameters = this.parameters.GetEnumerator();
			IEnumerator<IPainter2D> ePainter = this.painters.GetEnumerator();
			bool Result = true;

			while (eParameters.MoveNext() && ePainter.MoveNext())
			{
				if (!ePainter.Current.TrySetDefaultColor(Color, eParameters.Current))
					Result = false;
			}

			return Result;
		}

	}
}
