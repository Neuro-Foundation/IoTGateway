﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Graphs;
using Waher.Script.Model;
using Waher.Script.Objects.Matrices;

namespace Waher.Script.Fractals.IFS
{
    /// <summary>
    /// Calculates a flame fractal in HSL space. Intensity is mapped along the L-axis.
    /// Gamma correction is done along the SL-axes. The L-axis is multiplicated with the LightFactor.
    /// </summary>
    /// <example>
    /// </example>
    public class FlameFractalHsl : FunctionMultiVariate
    {
		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, ScriptNode DimX, ScriptNode DimY, ScriptNode SuperSampling,
			ScriptNode Gamma, ScriptNode LightFactor, ScriptNode Seed, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel, DimX, DimY, SuperSampling,
				  Gamma, LightFactor, Seed }, 
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar, 
				  ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar, 
				  ArgumentType.Scalar, ArgumentType.Scalar }, Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, ScriptNode DimX, ScriptNode DimY, ScriptNode SuperSampling,
			ScriptNode Gamma, ScriptNode LightFactor, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel, DimX, DimY, SuperSampling,
				  Gamma, LightFactor },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar }, Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, ScriptNode DimX, ScriptNode DimY, ScriptNode SuperSampling,
			ScriptNode Gamma, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel, DimX, DimY, SuperSampling,
				  Gamma },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar }, 
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, ScriptNode DimX, ScriptNode DimY, ScriptNode SuperSampling,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel, DimX, DimY, SuperSampling },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, ScriptNode DimX, ScriptNode DimY, int Start, int Length, 
			Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel, DimX, DimY },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Scalar },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, ScriptNode DimX, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel, DimX },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, ScriptNode Parallel, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview, Parallel },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			ScriptNode Preview, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions, Preview },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector, ArgumentType.Scalar },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public FlameFractalHsl(ScriptNode xc, ScriptNode yc, ScriptNode dr, ScriptNode N, ScriptNode FlameFunctions,
			int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { xc, yc, dr, N, FlameFunctions },
				  new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar,
				  ArgumentType.Scalar, ArgumentType.Vector },
				  Start, Length, Expression)
		{
		}

		/// <summary>
		/// TODO
		/// </summary>
		public override string[] DefaultArgumentNames
		{
			get
			{
				return new string[] 
				{
					"xc", "yc", "dr", "N", "FlameFunctions", "Preview", "Parallel",
					"DimX", "DimY", "SuperSampling", "Gamma", "LightFactor", "Seed"
				};
			}
		}

		/// <summary>
		/// TODO
		/// </summary>
		public override string FunctionName => nameof(FlameFractalHsl);

		/// <summary>
		/// TODO
		/// </summary>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
        {
            double xc, yc;
            double dr;
            long N;
            int dimx, dimy;
            int i, c;
            int Seed;

            i = 0;
            c = Arguments.Length;
            xc = Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            yc = Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            dr = Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            N = (long)Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            if (N <= 0)
                throw new ScriptRuntimeException("N in calls to FlameFractalHsl() must be a positive integer.", this);

            object Obj = Arguments[i].AssociatedObjectValue;
            string FunctionsExpression = this.Arguments[i++].SubExpression;

			if (!(Obj is Array FlameArray))
				throw new ScriptRuntimeException("The fifth parameter to FlameFractalHsl must be an array, containing flame definitions.", this);

			List<FlameFunction> FlameFunctions = new List<FlameFunction>();
            double Weight;
            double Gamma;
            double LightFactor;
            int SuperSampling;
            FlameFunction CurrentFunction = null;

            foreach (object FlameItem in FlameArray)
            {
                if (FlameItem is DoubleMatrix M)
                {
                    CurrentFunction = new FlameFunction(M, this);
                    FlameFunctions.Add(CurrentFunction);
                }
                else if (FlameItem is SKColor || FlameItem is string)
                {
                    if (CurrentFunction is null)
                    {
                        M = new DoubleMatrix(new double[,] { { 1, 0 }, { 0, 1 } });
                        CurrentFunction = new FlameFunction(M, this);
                        FlameFunctions.Add(CurrentFunction);
                    }

					SKColor cl = Graph.ToColor(FlameItem);
					cl.ToHsl(out float H, out float S, out float L);

					CurrentFunction.SetColorHsl(H, S, L);
                    CurrentFunction = null;
				}
				else if (FlameItem is IFlameVariation FlameVariation)
                {
                    if (CurrentFunction is null)
                    {
                        M = new DoubleMatrix(new double[,] { { 1, 0 }, { 0, 1 } });
                        CurrentFunction = new FlameFunction(M, this);
                        FlameFunctions.Add(CurrentFunction);
                    }

                    CurrentFunction.Add(FlameVariation);
                }
                else if (FlameItem is ILambdaExpression LambdaExpression)
                {
                    if (CurrentFunction is null)
                    {
                        M = new DoubleMatrix(new double[,] { { 1, 0 }, { 0, 1 } });
                        CurrentFunction = new FlameFunction(M, this);
                        FlameFunctions.Add(CurrentFunction);
                    }

                    CurrentFunction.Add(LambdaExpression);
                }
                else
                {
                    try
                    {
                        Weight = Expression.ToDouble(FlameItem);

                        if (CurrentFunction is null)
                        {
                            M = new DoubleMatrix(new double[,] { { 1, 0 }, { 0, 1 } });
                            CurrentFunction = new FlameFunction(M, this);
                            FlameFunctions.Add(CurrentFunction);
                        }

                        CurrentFunction.SetWeight(Weight);
                    }
                    catch (Exception)
                    {
                        throw new ScriptRuntimeException("Invalid flame variation definition.", this);
                    }
                }
            }

            if (i < c && Arguments[i].AssociatedObjectValue is bool Preview)
                i++;
            else
                Preview = false;

			if (i < c && Arguments[i].AssociatedObjectValue is bool Parallel)
				i++;
			else
				Parallel = false;

            if (i < c)
                dimx = (int)Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            else
                dimx = 320;

            if (i < c)
                dimy = (int)Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            else
                dimy = 200;

            if (i < c)
                SuperSampling = (int)Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            else
                SuperSampling = 2;

            if (i < c)
                Gamma = Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            else
                Gamma = 2.2;

            if (i < c)
                LightFactor = Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            else
                LightFactor = 1.0;

            if (i < c)
                Seed = (int)Expression.ToDouble(Arguments[i++].AssociatedObjectValue);
            else
            {
                lock (gen)
                {
                    Seed = gen.Next();
                }

				Variables.ConsoleOut?.WriteLine("Seed = " + Seed.ToString(), Variables);
			}

			if (i < c)
            {
                throw new ScriptRuntimeException("Parameter mismatch in call to FlameFractalHsl(xc,yc,dr,N,FlameFunctions[,Preview[,Parallel[,dimx[,dimy[,SuperSampling[,Gamma[,LightFactor[,Seed]]]]]]]]).",
                    this);
            }

            if (dimx <= 0 || dimx > 5000 || dimy <= 0 || dimy > 5000)
                throw new ScriptRuntimeException("Image size must be within 1x1 to 5000x5000", this);

            FlameFunction[] Functions = FlameFunctions.ToArray();

            if (dr <= 0)
            {
                FlameFractalRgba.EstimateSize(out xc, out yc, out dr, Functions, dimx, dimy, Seed, 5000, Variables, this);
				
                Variables.ConsoleOut?.WriteLine("X-center: " + Expression.ToString(xc), Variables);
                Variables.ConsoleOut?.WriteLine("Y-center: " + Expression.ToString(yc), Variables);
                Variables.ConsoleOut?.WriteLine("Width: " + Expression.ToString(dr), Variables);
            }

            return CalcFlame(xc, yc, dr, N, Functions, dimx, dimy, Seed, SuperSampling, Gamma, LightFactor, Preview, Parallel, Variables, this, 
				this.FractalZoomScript, new object[] { dimx, dimy, N, FunctionsExpression, Seed, SuperSampling, Gamma, LightFactor, Preview, Parallel });
        }

        private readonly static Random gen = new Random();

        private string FractalZoomScript(double r, double i, double Size, object State)
        {
            object[] Parameters = (object[])State;
            int DimX = (int)Parameters[0];
            int DimY = (int)Parameters[1];
            long N = (long)Parameters[2];
            string FunctionsExpression = (string)Parameters[3];
            int Seed = (int)Parameters[4];
            int SuperSampling = (int)Parameters[5];
            double Gamma = (double)Parameters[6];
            double LightFactor = (double)Parameters[7];
            bool Preview = (bool)Parameters[8];
            bool Parallel = (bool)Parameters[9];

            StringBuilder sb = new StringBuilder();

            sb.Append("FlameFractalHsl(");
            sb.Append(Expression.ToString(r));
            sb.Append(',');
            sb.Append(Expression.ToString(i));
            sb.Append(',');
            sb.Append(Expression.ToString(Size / 4));
            sb.Append(',');
            sb.Append(N.ToString());
            sb.Append(',');
            sb.Append(FunctionsExpression);
            sb.Append(',');
            sb.Append(Expression.ToString(Preview));
            sb.Append(',');
            sb.Append(Expression.ToString(Parallel));
            sb.Append(',');
            sb.Append(DimX.ToString());
            sb.Append(',');
            sb.Append(DimY.ToString());
            sb.Append(',');
            sb.Append(SuperSampling.ToString());
            sb.Append(',');
            sb.Append(Expression.ToString(Gamma));
            sb.Append(',');
            sb.Append(Expression.ToString(LightFactor));
            sb.Append(',');
            sb.Append(Seed.ToString());
            sb.Append(')');

            return sb.ToString();
        }

		/// <summary>
		/// TODO
		/// </summary>
		public static FractalGraph CalcFlame(double xCenter, double yCenter, double rDelta, long N,
            FlameFunction[] Functions, int Width, int Height, int Seed, int SuperSampling, double Gamma,
            double LightFactor, bool Preview, bool Parallel, Variables Variables, ScriptNode Node,
            FractalZoomScript FractalZoomScript, object State)
        {
            double TotWeight = 0;
            double Weight;
            int i, c = Functions.Length;
            Random Gen = new Random(Seed);

            if (c < 1)
                throw new ScriptRuntimeException("At least one flame function needs to be provided.", Node);

            if (SuperSampling < 1)
                throw new ScriptRuntimeException("SuperSampling must be a postitive integer.", Node);

            if (!Variables.HandlesPreview)
                Preview = false;

			Array.Sort(Functions, (f1, f2) =>
				{
					double d = f2.Weight - f1.Weight;
					if (d < 0)
						return -1;
					else if (d > 0)
						return 1;
					else
						return 0;
				});

            double[] SumWeights = new double[c];
            FlameFunction f;

            for (i = 0; i < c; i++)
            {
                f = Functions[i];
                Weight = f.Weight;
                if (Weight < 0)
                    throw new ScriptRuntimeException("Weights must be non-negative.", Node);

                f.DefinitionDone();
                TotWeight += Weight;
                SumWeights[i] = TotWeight;
            }

            if (TotWeight == 0)
                throw new ScriptRuntimeException("The total weight of all functions must be postitive.", Node);

            for (i = 0; i < c; i++)
                SumWeights[i] /= TotWeight;

            double AspectRatio = ((double)Width) / Height;
            double xMin, xMax, yMin, yMax;

            xMin = xCenter - rDelta / 2;
            xMax = xMin + rDelta;
            yMin = yCenter - rDelta / (2 * AspectRatio);
            yMax = yMin + rDelta / AspectRatio;

            int NrGames = Parallel ? System.Environment.ProcessorCount : 1;

            if (NrGames <= 1)
            {
                FlameState P = new FlameState(Gen, xMin, xMax, yMin, yMax, Width, Height, SuperSampling, N, ColorMode.Hsl, Node);
                Variables v = new Variables();
                Variables.CopyTo(v);

                RunChaosGame(v, Functions, SumWeights, P, Preview, Gamma, LightFactor, Node);

                return new FractalGraph(Variables, P.RenderBitmapHsl(Gamma, LightFactor, false, SKColors.Black), xMin, yMin, xMax, yMax, rDelta,
                    false, Node, FractalZoomScript, State);
            }
            else
            {
                FlameState[] P = new FlameState[NrGames];
                WaitHandle[] Done = new WaitHandle[NrGames];
                Thread[] T = new Thread[NrGames];

                try
                {
                    for (i = 0; i < NrGames; i++)
                    {
                        Done[i] = new ManualResetEvent(false);
                        P[i] = new FlameState(Gen, xMin, xMax, yMin, yMax, Width, Height, SuperSampling,
                            i < NrGames - 1 ? N / NrGames : N - (N / NrGames) * (NrGames - 1),
                            ColorMode.Hsl, Node);

                        Variables v = new Variables();
                        Variables.CopyTo(v);

						T[i] = new Thread(new ParameterizedThreadStart(ChaosGameThread))
						{
							Name = "FlameFractal thread #" + (i + 1).ToString(),
							Priority = ThreadPriority.BelowNormal
						};

						T[i].Start(new object[] { Done[i], i, v, Functions, SumWeights, P[i], 
                            Node, i == 0 && Preview, Gamma, LightFactor });
                    }

                    WaitHandle.WaitAll(Done);

                    for (i = 1; i < NrGames; i++)
                        P[0].Add(P[i]);

                    return new FractalGraph(Variables, P[0].RenderBitmapHsl(Gamma, LightFactor, false, SKColors.Black), xMin, yMin, xMax, yMax, rDelta,
                        false, Node, FractalZoomScript, State);
                }
                catch (ThreadAbortException)
                {
                    for (i = 0; i < NrGames; i++)
                    {
                        try
                        {
                            if (T[i] is null)
                                continue;

                            if (Done[i] is null || !Done[i].WaitOne(0))
                                T[i].Abort();
                        }
                        catch (Exception)
                        {
                            // Ignore
                        }
                    }

                    return null;
                }
                finally
                {
                    for (i = 0; i < NrGames; i++)
                    {
                        try
                        {
                            Done[i]?.Close();
                        }
                        catch (Exception)
                        {
                            // Ignore
                        }
                    }
                }
            }
        }

        private static void ChaosGameThread(object P)
        {
            object[] Parameters = (object[])P;
            ManualResetEvent Done = (ManualResetEvent)Parameters[0];
            //int GameNr = (int)Parameters[1];
            Variables v = (Variables)Parameters[2];
            FlameFunction[] Functions = (FlameFunction[])Parameters[3];
            double[] SumWeights = (double[])Parameters[4];
            FlameState P2 = (FlameState)Parameters[5];
            ScriptNode Node = (ScriptNode)Parameters[6];
            bool Preview = (bool)Parameters[7];
            double Gamma = (double)Parameters[8];
            double LightFactor = (double)Parameters[9];

            try
            {
                RunChaosGame(v, Functions, SumWeights, P2, Preview, Gamma, LightFactor, Node);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (Exception)
            {
                // Ignore.
            }
            finally
            {
                Done.Set();
            }
        }

        private static void RunChaosGame(Variables v, FlameFunction[] Functions,
            double[] SumWeights, FlameState P, bool Preview, double Gamma, double LightFactor,
            ScriptNode Node)
        {
            Random Gen = new Random();
            FlameFunction f;
            double Weight;
            int i, j;
            int c = Functions.Length;

            for (i = 0; i < 20; i++)
            {
                Weight = Gen.NextDouble();
                j = 0;
                while (j < c - 1 && SumWeights[j] <= Weight)
                    j++;

                f = Functions[j];
                if (!f.Operate(P, v))
                {
                    P.N = 0;
                    break;
                }
            }

            if (Preview || v.HandlesStatus)
            {
                DateTime Start = DateTime.Now;
                DateTime NextPreview = Start.AddSeconds(1);
				DateTime PrevPreview = Start;
				DateTime Temp;
				DateTime Temp2;
				TimeSpan TimeLeft;
				int NextPreviewDeciSeconds = 10;
				int NrIterationsPerPreview = 4096;
				int Pos = NrIterationsPerPreview;
				long NrIterations;
				long PrevNrIterations = 0;
				long NrIterationsSinceLast;
				double PercentDone;
				double IterationsPerSeconds;

                do
                {
                    if (Pos-- <= 0)
                    {
                        Pos = NrIterationsPerPreview;
                        Temp = DateTime.Now;
                        if (Temp > NextPreview)
                        {
                            NextPreview = Temp.AddSeconds(NextPreviewDeciSeconds * 0.1);
                            if (NextPreviewDeciSeconds < 50)
                                NextPreviewDeciSeconds++;

							if (Preview)
							{
								v.Preview(Node.Expression, new GraphBitmap(v, P.RenderBitmapHsl(Gamma, LightFactor, true, SKColors.Black)));

								Temp2 = DateTime.Now;

								double d = (Temp2 - Temp).TotalSeconds;
								double d2 = (Temp - PrevPreview).TotalSeconds;

								if (d / d2 > 0.1)
								{
									NrIterationsPerPreview <<= 1;
									if (NrIterationsPerPreview < 0)
										NrIterationsPerPreview = int.MaxValue;
								}
							}

							NrIterations = P.N0 - P.N;
							NrIterationsSinceLast = NrIterations - PrevNrIterations;
							IterationsPerSeconds = NrIterationsSinceLast / (Temp - PrevPreview).TotalSeconds;
							PercentDone = (100 * (1.0 - ((double)P.N) / P.N0));
							TimeLeft = new TimeSpan((long)((Temp - Start).Ticks * 100 / PercentDone));
							v.Status(Node.Expression, P.N.ToString() + " iterations left, " + NrIterations.ToString() + " iterations done, " + IterationsPerSeconds.ToString("F0") + " iterations/s, " + PercentDone.ToString("F1") + "% done, Time Left: " + TimeLeft.ToString() + ".");
							PrevNrIterations = NrIterations;
							PrevPreview = Temp;
						}
                    }

                    Weight = Gen.NextDouble();
                    j = 0;
                    while (j < c - 1 && SumWeights[j] <= Weight)
                        j++;

                    f = Functions[j];
                    if (!f.Operate(P, v))
                        break;
                }
                while (P.IncHistogram());

				v.Status(Node.Expression, string.Empty);
            }
            else
            {
                do
                {
                    Weight = Gen.NextDouble();
                    j = 0;
                    while (j < c - 1 && SumWeights[j] <= Weight)
                        j++;

                    f = Functions[j];
                    if (!f.Operate(P, v))
                        break;
                }
                while (P.IncHistogram());
            }
        }

    }
}