﻿using SkiaSharp;

namespace Waher.Script.Graphs.Functions.Plots
{
	/// <summary>
	/// Plots a two-dimensional vertical-bar chart.
	/// </summary>
	public class VerticalBarsPainter : SingleColorGraphPainter, IPainter2D
	{
		/// <summary>
		/// Draws the graph on a canvas.
		/// </summary>
		/// <param name="Canvas">Canvas to draw on.</param>
		/// <param name="Points">Points to draw.</param>
		/// <param name="Parameters">Graph-specific parameters.</param>
		/// <param name="PrevPoints">Points of previous graph of same type (if available), null (if not available).</param>
		/// <param name="PrevParameters">Parameters of previous graph of same type (if available), null (if not available).</param>
		/// <param name="DrawingArea">Current drawing area.</param>
		public void DrawGraph(SKCanvas Canvas, SKPoint[] Points, object[] Parameters, SKPoint[] PrevPoints, object[] PrevParameters,
			DrawingArea DrawingArea)
		{
			SKPaint Brush = null;
			SKPath Path = null;

			try
			{
				Brush = new SKPaint()
				{
					Color = Graph.ToColor(Parameters[0]),
					Style = SKPaintStyle.Fill
				};
				Path = new SKPath();

				int Count = DrawingArea.XLabelPositions?.Count ?? Points.Length;
				float HalfBarWidth = (DrawingArea.Width - Count) * 0.45f / Count;
				float x0, y0, x1, y1;
				int i, c;

				if (!(PrevPoints is null))
				{
					if ((c = PrevPoints.Length) != Points.Length)
						PrevPoints = null;
					else
					{
						for (i = 0; i < c; i++)
						{
							if (PrevPoints[i].X != Points[i].X)
							{
								PrevPoints = null;
								break;
							}
						}
					}
				}

				int MinX = DrawingArea.OffsetX;
				int MaxX = DrawingArea.OffsetX + DrawingArea.Width;

				i = 0;
				foreach (SKPoint Point in Points)
				{
					x0 = Point.X - HalfBarWidth + 1;
					y0 = Point.Y;
					x1 = Point.X + HalfBarWidth - 1;
					y1 = !(PrevPoints is null) ? PrevPoints[i++].Y : DrawingArea.OrigoY;

					if (x0 < MinX)
						x0 = MinX;

					if (x1 > MaxX)
						x1 = MaxX;

					Canvas.DrawRect(new SKRect(x0, y0, x1, y1), Brush);
				}

				Canvas.DrawPath(Path, Brush);
			}
			finally
			{
				Brush?.Dispose();
				Path?.Dispose();
			}
		}

	}
}
