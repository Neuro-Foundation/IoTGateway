﻿using System;
using System.IO;
using System.Numerics;
using SkiaSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Script.Graphs;
using Waher.Script.Graphs3D;

namespace Waher.Script.Test
{
	[TestClass]
	public class Canvas3DTests
	{
		private static void Save(Canvas3D Canvas, string FileName)
		{
			if (!Directory.Exists("Canvas3D"))
				Directory.CreateDirectory("Canvas3D");

			PixelInformation Pixels = Canvas.GetPixels();
			File.WriteAllBytes(Path.Combine("Canvas3D", FileName), Pixels.EncodeAsPng());
		}

		[TestMethod]
		public void Canvas3D_Test_01_Plot()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			int t;

			for (t = 0; t < 1000000; t++)
			{
				double x = t * Math.Sin(t / 10000.0) / 2500.0;
				double y = t * Math.Cos(t / 20000.0) / 2500.0;
				double z = t / 5000.0;
				Vector4 P = new((float)x, (float)y, (float)z, 1);
				Canvas.Plot(P, SKColors.Red);
			}

			Save(Canvas, "01.png");
		}

		[TestMethod]
		public void Canvas3D_Test_02_Line()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			DrawCurve(Canvas);
			Save(Canvas, "02.png");
		}

		private static void DrawCurve(Canvas3D Canvas)
		{
			int t;

			for (t = 0; t < 10000; t++)
			{
				double x = t * Math.Sin(t / 100.0) / 25.0;
				double y = t * Math.Cos(t / 200.0) / 25.0;
				double z = t / 50.0;
				Vector4 P = new((float)x, (float)y, (float)z, 1);

				if (t == 0)
					Canvas.MoveTo(P);
				else
					Canvas.LineTo(P, SKColors.Red);
			}
		}

		[TestMethod]
		public void Canvas3D_Test_03_Oversampling()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 3, SKColors.White);
			DrawCurve(Canvas);
			Save(Canvas, "03.png");
		}

		[TestMethod]
		public void Canvas3D_Test_04_Perspective()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawWireframeCube(Canvas);
			Save(Canvas, "04.png");
		}

		private static void DrawWireframeCube(Canvas3D Canvas)
		{
			Vector4 P0 = new(-500, -500, 1000, 1);
			Vector4 P1 = new(-500, -500, 2000, 1);
			Vector4 P2 = new(500, -500, 2000, 1);
			Vector4 P3 = new(500, -500, 1000, 1);
			Vector4 P4 = new(-500, 500, 1000, 1);
			Vector4 P5 = new(-500, 500, 2000, 1);
			Vector4 P6 = new(500, 500, 2000, 1);
			Vector4 P7 = new(500, 500, 1000, 1);

			Canvas.PolyLine(new Vector4[] { P0, P1, P2, P3, P0 }, SKColors.Red);
			Canvas.PolyLine(new Vector4[] { P4, P5, P6, P7, P4 }, SKColors.Red);
			Canvas.Line(P0, P4, SKColors.Red);
			Canvas.Line(P1, P5, SKColors.Red);
			Canvas.Line(P2, P6, SKColors.Red);
			Canvas.Line(P3, P7, SKColors.Red);
		}

		[TestMethod]
		public void Canvas3D_Test_05_Polygon()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawCube(Canvas);
			Save(Canvas, "05.png");
		}

		private static void DrawCube(Canvas3D Canvas)
		{
			Vector4 P0 = new(-500, -500, 1000, 1);
			Vector4 P1 = new(-500, -500, 2000, 1);
			Vector4 P2 = new(500, -500, 2000, 1);
			Vector4 P3 = new(500, -500, 1000, 1);
			Vector4 P4 = new(-500, 500, 1000, 1);
			Vector4 P5 = new(-500, 500, 2000, 1);
			Vector4 P6 = new(500, 500, 2000, 1);
			Vector4 P7 = new(500, 500, 1000, 1);

			Canvas.Polygon(new Vector4[] { P0, P1, P2, P3 }, new SKColor(255, 0, 0, 128), true);
			Canvas.Polygon(new Vector4[] { P4, P5, P6, P7 }, new SKColor(255, 0, 0, 128), true);
			Canvas.Polygon(new Vector4[] { P1, P2, P6, P5 }, new SKColor(0, 255, 0, 128), true);
			Canvas.Polygon(new Vector4[] { P0, P1, P5, P4 }, new SKColor(0, 0, 255, 128), true);
			Canvas.Polygon(new Vector4[] { P2, P3, P7, P6 }, new SKColor(0, 0, 255, 128), true);
			Canvas.Polygon(new Vector4[] { P0, P3, P7, P4 }, new SKColor(0, 255, 0, 128), true);
		}

		[TestMethod]
		public void Canvas3D_Test_06_ZBuffer()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawPlanes(Canvas);
			Save(Canvas, "06.png");
		}

		private static void DrawPlanes(Canvas3D Canvas)
		{
			Canvas.Polygon(new Vector4[]
			{
				new(-500, 100, 1000, 1),
				new(-500, 100, 2000, 1),
				new(500, 100, 2000, 1),
				new(500, 100, 1000, 1)
			}, SKColors.Red, true);

			Canvas.Polygon(new Vector4[]
			{
				new(100, -500, 1000, 1),
				new(100, -500, 2000, 1),
				new(100, 500, 2000, 1),
				new(100, 500, 1000, 1)
			}, SKColors.Green, true);

			Canvas.Polygon(new Vector4[]
			{
				new(-500, -500, 1500, 1),
				new(500, -500, 1500, 1),
				new(500, 500, 1500, 1),
				new(-500, 500, 1500, 1),
			}, new SKColor(0, 0, 255, 64), true);
		}

		[TestMethod]
		public void Canvas3D_Test_07_Text()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawPlanes(Canvas);
			Canvas.Text("Hello World!", new(-400, 50, 1150, 1), "Tahoma", 150, SKColors.BlueViolet);

			Save(Canvas, "07.png");
		}

		[TestMethod]
		public void Canvas3D_Test_08_PhongShading_NoOversampling()
		{
			I3DShader Shader = GetPhongShader(SKColors.Red);
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawThreePlanes(Canvas, Shader);
			Save(Canvas, "08.png");
		}

		[TestMethod]
		public void Canvas3D_Test_09_PhongShading_Oversampling_2()
		{
			I3DShader Shader = GetPhongShader(SKColors.Red);
			Canvas3D Canvas = new(new Variables(), 1200, 800, 2, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawThreePlanes(Canvas, Shader);
			Save(Canvas, "09.png");
		}

		[TestMethod]
		public void Canvas3D_Test_10_PhongShading_Oversampling_3()
		{
			I3DShader Shader = GetPhongShader(SKColors.Red);
			Canvas3D Canvas = new(new Variables(), 1200, 800, 3, SKColors.White);
			Canvas.Perspective(200, 2000);
			DrawThreePlanes(Canvas, Shader);
			Save(Canvas, "10.png");
		}

		private static I3DShader GetPhongShader(SKColor Color)
		{
			return new PhongShader(
				new PhongMaterial(1, 2, 0, 10),
				new PhongIntensity(64, 64, 64, Color.Alpha),
				new PhongLightSource(
					new PhongIntensity(Color.Red, Color.Green, Color.Blue, Color.Alpha),
					new PhongIntensity(255, 255, 255, 255),
					new Vector3(1000, 1000, 0)));
		}

		private static void DrawThreePlanes(Canvas3D Canvas, I3DShader Shader)
		{
			DrawThreePlanes(Canvas, Shader, -500, -500, 2000);
		}

		private static void DrawThreePlanes(Canvas3D Canvas, I3DShader Shader, float x, float y, float z)
		{
			Canvas.Polygon(new Vector4[]
			{
				new(-500, 500, z, 1),
				new(500, 500, z, 1),
				new(500, -500, z, 1),
				new(-500, -500, z, 1)
			}, Shader, true);

			Canvas.Polygon(new Vector4[]
			{
				new(x, 500, 1000, 1),
				new(x, 500, 2000, 1),
				new(x, -500, 2000, 1),
				new(x, -500, 1000, 1)
			}, Shader, true);

			Canvas.Polygon(new Vector4[]
			{
				new(-500, y, 2000, 1),
				new(500, y, 2000, 1),
				new(500, y, 1000, 1),
				new(-500, y, 1000, 1)
			}, Shader, true);
		}

		[TestMethod]
		public void Canvas3D_Test_11_Rotate_X()
		{
			I3DShader Shader = GetPhongShader(SKColors.Red);
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);

			DrawThreePlanes(Canvas, Shader);

			Shader = GetPhongShader(SKColors.Blue);
			Matrix4x4 Bak = Canvas.Translate(-250, 250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateX(30, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(250, 250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateX(120, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(-250, -250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateX(210, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(250, -250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateX(500, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Save(Canvas, "11.png");
		}

		[TestMethod]
		public void Canvas3D_Test_12_Rotate_Y()
		{
			I3DShader Shader = GetPhongShader(SKColors.Red);
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);

			DrawThreePlanes(Canvas, Shader);

			Shader = GetPhongShader(SKColors.Blue);
			Matrix4x4 Bak = Canvas.Translate(-250, 250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateY(30, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(250, 250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateY(120, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(-250, -250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateY(210, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(250, -250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateY(500, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Save(Canvas, "12.png");
		}

		[TestMethod]
		public void Canvas3D_Test_13_Rotate_Z()
		{
			I3DShader Shader = GetPhongShader(SKColors.Red);
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);

			DrawThreePlanes(Canvas, Shader);

			Shader = GetPhongShader(SKColors.Blue);
			Matrix4x4 Bak = Canvas.Translate(-250, 250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateZ(30, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(250, 250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateZ(120, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(-250, -250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateZ(210, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Canvas.ModelTransformation = Bak;
			Canvas.Translate(250, -250, 0);
			Canvas.Scale(0.25f, new Vector3(0, 0, 1500));
			Canvas.RotateZ(500, new Vector3(0, 0, 1500));
			Canvas.Box(-500, -500, 1000, 500, 500, 2000, Shader);

			Save(Canvas, "13.png");
		}

		[TestMethod]
		public void Canvas3D_Test_14_Ellipsoid()
		{
			Canvas3D Canvas = new(new Variables(), 1200, 800, 1, SKColors.White);
			Canvas.Perspective(200, 2000);
			//Canvas.LookAt(-200, 500, 0, 0, 0, 1500, 0, 1, 0);
			Canvas.RotateX(30, new Vector3(0, 0, 1500));
			Canvas.RotateY(45, new Vector3(0, 0, 1500));
			Canvas.RotateZ(60, new Vector3(0, 0, 1500));

			I3DShader Shader = GetPhongShader(SKColors.Orange);
			Canvas.Ellipsoid(0, 0, 1500, 400, 400, 400, 1000, Shader);

			Shader = GetPhongShader(new SKColor(0, 0, 255, 64));
			DrawThreePlanes(Canvas, Shader, 0, 0, 1500);

			Save(Canvas, "14.png");
		}
	}
}

// TODO: Clip Z
// TODO: Test Light / Phong shading with multiple light sources
// TODO: Specular lighting
// TODO: Proper interpolation of z-coordinate to avoid fish-eyes.
// TODO: Check if quicker to work with Vector3 directly, than with 3 separate coordinate values.
// TODO: C.LookAt(-200, 500, 0, 0, 0, 1500, 0, -1, 0);
