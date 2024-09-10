﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using Waher.Runtime.Console;
using Waher.Script.Graphs;
using Waher.Script.Xml;

namespace Waher.Script.Test
{
	[TestClass]
	public class ScriptGraphTests
	{
		private async Task Test(string Script, string FileName)
		{
			Variables v = new();

			Expression Exp = new(Script);
			Graph Result = await Exp.EvaluateAsync(v) as Graph;

			if (Result is null)
				Assert.Fail("Expected graph.");

			GraphSettings Settings = new();
			PixelInformation Pixels = Result.CreatePixels(Settings);
			
			this.Save(Pixels, FileName);

			ScriptParsingTests.AssertParentNodesAndSubsexpressions(Exp);

			ConsoleOut.WriteLine();
			Exp.ToXml(ConsoleOut.Writer);
			ConsoleOut.WriteLine();
		}

		private void Save(PixelInformation Pixels, string FileName)
		{
			if (!Directory.Exists("Graphs"))
				Directory.CreateDirectory("Graphs");

			File.WriteAllBytes(Path.Combine("Graphs", FileName), Pixels.EncodeAsPng());
		}

		[TestMethod]
		public async Task Graph2D_Test_01_Plot2dCurve()
		{
			await this.Test("x:=-10..10|0.1;y:=sin(x);plot2dcurve(x,y)", "2D_01_1.png");
			await this.Test("x:=-10..10|0.1;y:=sin(x);plot2dcurve(x,y,'Blue')", "2D_01_2.png");
			await this.Test("x:=-10..10|0.1;y:=sin(x);plot2dcurve(x,y,'Blue',5)", "2D_01_3.png");
		}

		[TestMethod]
		public async Task Graph2D_Test_02_Plot2dLine()
		{
			await this.Test("x:=-10..10|0.1;y:=sin(x);plot2dline(x,y)", "2D_02_1.png");
			await this.Test("x:=-10..10|0.1;y:=sin(x);plot2dline(x,y,'Blue')", "2D_02_2.png");
			await this.Test("x:=-10..10|0.1;y:=sin(x);plot2dline(x,y,'Blue',5)", "2D_02_3.png");
		}

		[TestMethod]
		public async Task Graph2D_Test_03_DateTimeAxis()
		{
			await this.Test("x:=0..59;t:= Now.AddSeconds(x);y:= sin(x);plot2dcurve(t,y)", "2D_03_1.png");
			await this.Test("x:=0..59;t:= Now.AddMinutes(x);y:= sin(x);plot2dcurve(t,y)", "2D_03_2.png");
			await this.Test("x:=0..59;t:= Now.AddHours(x);y:= sin(x);plot2dcurve(t,y)", "2D_03_3.png");
			await this.Test("x:=0..59;t:= Now.AddDays(x);y:= sin(x);plot2dcurve(t,y)", "2D_03_4.png");
			await this.Test("x:=0..59;t:= Now.AddDays(x*7);y:= sin(x);plot2dcurve(t,y)", "2D_03_5.png");
			await this.Test("x:=0..59;t:= Now.AddMonths(x);y:= sin(x);plot2dcurve(t,y)", "2D_03_6.png");
			await this.Test("x:=0..59;t:= Now.AddYears(x);y:= sin(x);plot2dcurve(t,y)", "2D_03_7.png");
		}

		[TestMethod]
		public async Task Graph2D_Test_04_PhysicalQuantities()
		{
			await this.Test("x:=-10..10|0.1;t:=DateTime(2016,3,11).AddHours(x);y:=sin(x) C;plot2dcurve(t,y)", "2D_04.png");
		}

		[TestMethod]
		public async Task Graph2D_Test_05_Plot2dHLine()
		{
			await this.Test("x:=-10..10;y:=sin(x);plot2dhline(x,y,0)", "2D_05_1.png");
			await this.Test("x:=-10..10;y:=sin(x);plot2dhline(x,y,1,'Blue')", "2D_05_2.png");
			await this.Test("x:=-10..10;y:=sin(x);plot2dhline(x,y,-1,'Blue',5)", "2D_05_3.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_01_LineMesh()
		{
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);samescale(linemesh3d(x,y,z))", "3D_01_1.png");
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);samescale(linemesh3d(x,y,z,'Blue'))", "3D_01_2.png");
			await this.Test("theta:=Columns((0..360|5)°);phi:=Rows((0..360|5)°);R0:=5;R1:=20;x:=(R1+R0*cos(theta)).*cos(phi);y:=R0*sin(theta);z:=(R1+R0*cos(theta)).*sin(phi);samescale(linemesh3d(x,y,z))", "3D_01_3.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_02_PolygonMesh()
		{
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);samescale(polygonmesh3d(x,y,z))", "3D_02_1.png");
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);samescale(polygonmesh3d(x,y,z,'Blue'))", "3D_02_2.png");
			await this.Test("theta:=Columns((0..360|5)°);phi:=Rows((0..360|5)°);R0:=5;R1:=20;x:=(R1+R0*cos(theta)).*cos(phi);y:=R0*sin(theta);z:=(R1+R0*cos(theta)).*sin(phi);samescale(polygonmesh3d(x,y,z))", "3D_02_3.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_03_Surface()
		{
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);samescale(surface3d(x,y,z))", "3D_03_1.png");
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);samescale(surface3d(x,y,z,'Blue'))", "3D_03_2.png");
			await this.Test("theta:=Columns((0..360|5)°);phi:=Rows((0..360|5)°);R0:=5;R1:=20;x:=(R1+R0*cos(theta)).*cos(phi);y:=R0*sin(theta);z:=(R1+R0*cos(theta)).*sin(phi);samescale(surface3d(x,y,z))", "3D_03_3.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_04_Labels()
		{
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);G:=samescale(surface3d(x,y,z));G.Title:='Title';G.LabelX:='X-axis';G.LabelY:='Y-axis';G.LabelZ:='Z-axis';G", "3D_04.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_05_Angle()
		{
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);G:=samescale(surface3d(x,y,z));G.Title:='Title';G.LabelX:='X-axis';G.LabelY:='Y-axis';G.LabelZ:='Z-axis';G.Angle:=60;G", "3D_05.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_06_Inclination()
		{
			await this.Test("x:=Columns(-10..10|0.1);z:=Rows(-10..10|0.1);r:=sqrt(x.^2+z.^2);y:=10*cos(r*2).*exp(-r/3);G:=samescale(surface3d(x,y,z));G.Title:='Title';G.LabelX:='X-axis';G.LabelY:='Y-axis';G.LabelZ:='Z-axis';G.Inclination:=60;G", "3D_06.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_07_Add()
		{
			await this.Test("Thorus(R0,R1,Color,dx,dy,dz):=(theta:=Columns((0..360|5)°);phi:=Rows((0..360|5)°);x:=(R1+R0*cos(theta)).*cos(phi)+dx;y:=R0*sin(theta)+dy;z:=(R1+R0*cos(theta)).*sin(phi)+dz;samescale(surface3d(x,y,z,Color)));Thorus(5,20,'Red',0,0,0)+Thorus(3,15,'Blue',0,10,0)+Thorus(2,10,'Green',0,17,0)+Thorus(1,7,'Yellow',0,20,0)", "3D_07.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_08_Planes()
		{
			await this.Test("theta:=Columns((0..360|5)°);phi:=Rows((0..360|5)°);R:=10;x:=R*cos(theta).*cos(phi);y:=R*sin(theta);z:=R*cos(theta).*sin(phi);samescale(linemesh3d(x+10,y+10,z+10)+polygonmesh3d(x+30,y+10,z+10)+surface3d(x+20,y+30,z+10))", "3D_08.png");
		}

		[TestMethod]
		public async Task Graph3D_Test_09_VerticalBars()
		{
			await this.Test("[LabelsX,LabelsZ,Y]:=Histogram2D([Normal(0,1,100000),Normal(0,1,100000)],-5,5,50,-5,5,50);VerticalBars3D(Columns(LabelsX),Y,Rows(LabelsZ))", "3D_09_1.png");
			await this.Test("[LabelsX,LabelsZ,Y]:=Histogram2D([Normal(0,1,100000),Normal(0,1,100000)],-5,5,50,-5,5,50);VerticalBars3D(Columns(LabelsX),Y,Rows(LabelsZ),'Blue')", "3D_09_2.png");
		}


	}
}