﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Waher.Runtime.Console;
using Waher.Script.Statistics;
using Waher.Script.Statistics.Functions;

namespace Waher.Script.Test
{
	[TestClass]
	public class StatMathTests
	{
		[TestMethod]
		public void Test_01_Γ()
		{
			// https://dlmf.nist.gov/5.4

			Assert.IsTrue(Math.Abs(StatMath.Γ(1) - 1) < 1e-10);
			Assert.IsTrue(Math.Abs(StatMath.Γ(1.0 / 2) - 1.77245385090551602729) < 1e-10);
			Assert.IsTrue(Math.Abs(StatMath.Γ(1.0 / 3) - 2.67893853470774763365) < 1e-10);
			Assert.IsTrue(Math.Abs(StatMath.Γ(2.0 / 3) - 1.35411793942640041694) < 1e-10);
			Assert.IsTrue(Math.Abs(StatMath.Γ(1.0 / 4) - 3.62560990822190831193) < 1e-10);
			Assert.IsTrue(Math.Abs(StatMath.Γ(3.0 / 4) - 1.22541670246517764512) < 1e-10);
		}

		[TestMethod]
		public void Test_02()
		{
			double a = 2.5;
			double x = 3;
			int N, i;
			double n, d, q, q0 = 0;

			ConsoleOut.WriteLine(DateTime.Now.ToString());
			ConsoleOut.WriteLine("Γ(" + a + "," + x + "):");

			for (N = 1; N < 100; N++)
			{
				q = 0;
				for (i = N; i > 0; i--)
				{
					d = q + 1 + 2 * i + x - a;
					n = i * (a - i);
					q = n / d;
				}

				n = Math.Pow(x, a) * Math.Exp(-x);
				d = 1 + x - a + q;
				q = n / d;

				ConsoleOut.WriteLine(N + ", " + q + ", " + (q - q0));
				q0 = q;
			}
		}

		/* Ref: https://dlmf.nist.gov/8.3
		 * 
		 * x:=0..8|0.01;
		 * plot2dcurve(x,lgamma(0.25,x),"Green")+
		 * plot2dcurve(x,lgamma(0.5,x),"Red")+
		 * plot2dcurve(x,lgamma(0.75,x),"Blue")+
		 * plot2dcurve(x,lgamma(1,x),"Orange")
		 * 
		 * x:=0..8|0.01;
		 * plot2dcurve(x,lgamma(1,x),"Green")+
		 * plot2dcurve(x,lgamma(2,x),"Red")+
		 * plot2dcurve(x,lgamma(2.5,x),"Blue")+
		 * plot2dcurve(x,lgamma(3,x),"Orange")
		 * 
		 * x:=0..8|0.01;
		 * plot2dcurve(x,ugamma(0.25,x),"Green")+
		 * plot2dcurve(x,ugamma(1,x),"Red")+
		 * plot2dcurve(x,ugamma(2,x),"Blue")+
		 * plot2dcurve(x,ugamma(2.5,x),"Orange")+
		 * plot2dcurve(x,ugamma(3,x),"Purple")
		 */

		[TestMethod]
		public void Test_03_Labels()
		{
			Assert.AreEqual("0", Histogram.TrimLabel("0.00000000000000000001"));
			Assert.AreEqual("0.1", Histogram.TrimLabel("0.10000000000000000001"));
			Assert.AreEqual("0.01", Histogram.TrimLabel("0.01000000000000000001"));
			Assert.AreEqual("0.001", Histogram.TrimLabel("0.00100000000000000001"));
			Assert.AreEqual("0.0001", Histogram.TrimLabel("0.00010000000000000001"));

			Assert.AreEqual("-0", Histogram.TrimLabel("-0.00000000000000000001"));
			Assert.AreEqual("-0.1", Histogram.TrimLabel("-0.10000000000000000001"));
			Assert.AreEqual("-0.01", Histogram.TrimLabel("-0.01000000000000000001"));
			Assert.AreEqual("-0.001", Histogram.TrimLabel("-0.00100000000000000001"));
			Assert.AreEqual("-0.0001", Histogram.TrimLabel("-0.00010000000000000001"));

			Assert.AreEqual("40", Histogram.TrimLabel("39.99999999999999999999"));
			Assert.AreEqual("10", Histogram.TrimLabel("9.99999999999999999999"));
			Assert.AreEqual("1", Histogram.TrimLabel("0.99999999999999999999"));
			Assert.AreEqual("0.1", Histogram.TrimLabel("0.09999999999999999999"));
			Assert.AreEqual("0.01", Histogram.TrimLabel("0.00999999999999999999"));
			Assert.AreEqual("0.001", Histogram.TrimLabel("0.000999999999999999999"));
			Assert.AreEqual("0.0001", Histogram.TrimLabel("0.000099999999999999999"));

			Assert.AreEqual("-40", Histogram.TrimLabel("-39.99999999999999999999"));
			Assert.AreEqual("-10", Histogram.TrimLabel("-9.99999999999999999999"));
			Assert.AreEqual("-1", Histogram.TrimLabel("-0.99999999999999999999"));
			Assert.AreEqual("-0.1", Histogram.TrimLabel("-0.09999999999999999999"));
			Assert.AreEqual("-0.01", Histogram.TrimLabel("-0.00999999999999999999"));
			Assert.AreEqual("-0.001", Histogram.TrimLabel("-0.000999999999999999999"));
			Assert.AreEqual("-0.0001", Histogram.TrimLabel("-0.000099999999999999999"));
		}

	}
}
