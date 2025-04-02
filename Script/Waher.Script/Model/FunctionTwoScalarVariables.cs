﻿using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Objects;

namespace Waher.Script.Model
{
	/// <summary>
	/// Base class for funcions of two scalar variables.
	/// </summary>
	public abstract class FunctionTwoScalarVariables : FunctionTwoVariables
	{
		/// <summary>
		/// Base class for funcions of one scalar variable.
		/// </summary>
		/// <param name="Argument1">Argument 1.</param>
		/// <param name="Argument2">Argument 2.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public FunctionTwoScalarVariables(ScriptNode Argument1, ScriptNode Argument2, int Start, int Length, Expression Expression)
			: base(Argument1, Argument2, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement Argument1, IElement Argument2, Variables Variables)
		{
			if (Argument1.IsScalar)
			{
				if (Argument2.IsScalar)
				{
					ISet Set1 = Argument1.AssociatedSet;
					ISet Set2 = Argument2.AssociatedSet;

					if (Set1 != Set2)
					{
						if (!Expression.UpgradeField(ref Argument1, ref Set1, ref Argument2, ref Set2))
							return this.EvaluateScalar(Argument1, Argument2, Variables);
					}

					object x = Argument1.AssociatedObjectValue;
					object y = Argument2.AssociatedObjectValue;

					if (x is double xd && y is double yd)
						return this.EvaluateScalar(xd, yd, Variables);

					if (x is Complex xz && y is Complex yz)
						return this.EvaluateScalar(xz, yz, Variables);

					if (x is bool xb && y is bool yb)
						return this.EvaluateScalar(xb, yb, Variables);

					if (x is string xs && y is string ys)
						return this.EvaluateScalar(xs, ys, Variables);

					double arg1, arg2;

					if (x is double xd2)
						arg1 = xd2;
					else
					{
						if (x is IPhysicalQuantity Q1)
							arg1 = Q1.ToPhysicalQuantity().Magnitude;
						else
							return this.EvaluateScalar(Argument1, Argument2, Variables);
					}

					if (y is double yd2)
						arg2 = yd2;
					else
					{
						if (y is IPhysicalQuantity Q2)
							arg2 = Q2.ToPhysicalQuantity().Magnitude;
						else
							return this.EvaluateScalar(Argument1, Argument2, Variables);
					}

					return this.EvaluateScalar(arg1, arg2, Variables);
				}
				else
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement E in Argument2.ChildElements)
						Elements.Add(this.Evaluate(Argument1, E, Variables));

					return Argument2.Encapsulate(Elements, this);
				}
			}
			else
			{
				if (Argument2.IsScalar)
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement E in Argument1.ChildElements)
						Elements.Add(this.Evaluate(E, Argument2, Variables));

					return Argument1.Encapsulate(Elements, this);
				}
				else
				{
					ICollection<IElement> Argument1Children = Argument1.ChildElements;
					ICollection<IElement> Argument2Children = Argument2.ChildElements;

					if (Argument1Children.Count == Argument2Children.Count)
					{
						ChunkedList<IElement> Elements = new ChunkedList<IElement>();
						IEnumerator<IElement> eArgument1 = Argument1Children.GetEnumerator();
						IEnumerator<IElement> eArgument2 = Argument2Children.GetEnumerator();

						try
						{
							while (eArgument1.MoveNext() && eArgument2.MoveNext())
								Elements.Add(this.Evaluate(eArgument1.Current, eArgument2.Current, Variables));
						}
						finally
						{
							eArgument1.Dispose();
							eArgument2.Dispose();
						}

						return Argument1.Encapsulate(Elements, this);
					}
					else
					{
						ChunkedList<IElement> Argument1Result = new ChunkedList<IElement>();

						foreach (IElement Argument1Child in Argument1Children)
						{
							ChunkedList<IElement> Argument2Result = new ChunkedList<IElement>();

							foreach (IElement Argument2Child in Argument2Children)
								Argument2Result.Add(this.Evaluate(Argument1Child, Argument2Child, Variables));

							Argument1Result.Add(Argument2.Encapsulate(Argument2Result, this));
						}

						return Argument1.Encapsulate(Argument1Result, this);
					}
				}
			}
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual IElement EvaluateScalar(IElement Argument1, IElement Argument2, Variables Variables)
		{
			object v1 = Argument1.AssociatedObjectValue;
			object v2 = Argument2.AssociatedObjectValue;

			if (Expression.TryConvert(v1, out string s1) && Expression.TryConvert(v2, out string s2))
				return this.EvaluateScalar(s1, s2, Variables);
			else if (Expression.TryConvert(v1, out double d1) && Expression.TryConvert(v2, out double d2))
				return this.EvaluateScalar(d1, d2, Variables);
			else if (Expression.TryConvert(v1, out bool b1) && Expression.TryConvert(v2, out bool b2))
				return this.EvaluateScalar(b1, b2, Variables);
			else if (Expression.TryConvert(v1, out Complex z1) && Expression.TryConvert(v2, out Complex z2))
				return this.EvaluateScalar(z1, z2, Variables);
			else if (Expression.TryConvert(v1, out Integer i1) && Expression.TryConvert(v2, out Integer i2))
				return this.EvaluateScalar((double)i1.Value, (double)i2.Value, Variables);
			else if (Expression.TryConvert(v1, out RationalNumber q1) && Expression.TryConvert(v2, out RationalNumber q2))
				return this.EvaluateScalar(q1.ToDouble(), q2.ToDouble(), Variables);
			else
				throw new ScriptRuntimeException("Type of scalar not supported.", this);
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual IElement EvaluateScalar(double Argument1, double Argument2, Variables Variables)
		{
			throw new ScriptRuntimeException("Double-valued arguments not supported.", this);
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual IElement EvaluateScalar(Complex Argument1, Complex Argument2, Variables Variables)
		{
			throw new ScriptRuntimeException("Complex-valued arguments not supported.", this);
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual IElement EvaluateScalar(bool Argument1, bool Argument2, Variables Variables)
		{
			throw new ScriptRuntimeException("Boolean-valued arguments not supported.", this);
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual IElement EvaluateScalar(string Argument1, string Argument2, Variables Variables)
		{
			throw new ScriptRuntimeException("String-valued arguments not supported.", this);
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override async Task<IElement> EvaluateAsync(IElement Argument1, IElement Argument2, Variables Variables)
		{
			if (Argument1.IsScalar)
			{
				if (Argument2.IsScalar)
				{
					ISet Set1 = Argument1.AssociatedSet;
					ISet Set2 = Argument2.AssociatedSet;

					if (Set1 != Set2)
					{
						if (!Expression.UpgradeField(ref Argument1, ref Set1, ref Argument2, ref Set2))
							return await this.EvaluateScalarAsync(Argument1, Argument2, Variables);
					}

					object x = Argument1.AssociatedObjectValue;
					object y = Argument2.AssociatedObjectValue;

					if (x is double xd && y is double yd)
						return await this.EvaluateScalarAsync(xd, yd, Variables);

					if (x is Complex xz && y is Complex yz)
						return await this.EvaluateScalarAsync(xz, yz, Variables);

					if (x is bool xb && y is bool yb)
						return await this.EvaluateScalarAsync(xb, yb, Variables);

					if (x is string xs && y is string ys)
						return await this.EvaluateScalarAsync(xs, ys, Variables);

					double arg1, arg2;

					if (x is double xd2)
						arg1 = xd2;
					else
					{
						if (x is IPhysicalQuantity Q1)
							arg1 = Q1.ToPhysicalQuantity().Magnitude;
						else
							return await this.EvaluateScalarAsync(Argument1, Argument2, Variables);
					}

					if (y is double yd2)
						arg2 = yd2;
					else
					{
						if (y is IPhysicalQuantity Q2)
							arg2 = Q2.ToPhysicalQuantity().Magnitude;
						else
							return await this.EvaluateScalarAsync(Argument1, Argument2, Variables);
					}

					return await this.EvaluateScalarAsync(arg1, arg2, Variables);
				}
				else
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement E in Argument2.ChildElements)
						Elements.Add(await this.EvaluateAsync(Argument1, E, Variables));

					return Argument2.Encapsulate(Elements, this);
				}
			}
			else
			{
				if (Argument2.IsScalar)
				{
					ChunkedList<IElement> Elements = new ChunkedList<IElement>();

					foreach (IElement E in Argument1.ChildElements)
						Elements.Add(await this.EvaluateAsync(E, Argument2, Variables));

					return Argument1.Encapsulate(Elements, this);
				}
				else
				{
					ICollection<IElement> Argument1Children = Argument1.ChildElements;
					ICollection<IElement> Argument2Children = Argument2.ChildElements;

					if (Argument1Children.Count == Argument2Children.Count)
					{
						ChunkedList<IElement> Elements = new ChunkedList<IElement>();
						IEnumerator<IElement> eArgument1 = Argument1Children.GetEnumerator();
						IEnumerator<IElement> eArgument2 = Argument2Children.GetEnumerator();

						try
						{
							while (eArgument1.MoveNext() && eArgument2.MoveNext())
								Elements.Add(await this.EvaluateAsync(eArgument1.Current, eArgument2.Current, Variables));
						}
						finally
						{
							eArgument1.Dispose();
							eArgument2.Dispose();
						}

						return Argument1.Encapsulate(Elements, this);
					}
					else
					{
						ChunkedList<IElement> Argument1Result = new ChunkedList<IElement>();

						foreach (IElement Argument1Child in Argument1Children)
						{
							ChunkedList<IElement> Argument2Result = new ChunkedList<IElement>();

							foreach (IElement Argument2Child in Argument2Children)
								Argument2Result.Add(await this.EvaluateAsync(Argument1Child, Argument2Child, Variables));

							Argument1Result.Add(Argument2.Encapsulate(Argument2Result, this));
						}

						return Argument1.Encapsulate(Argument1Result, this);
					}
				}
			}
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual Task<IElement> EvaluateScalarAsync(IElement Argument1, IElement Argument2, Variables Variables)
		{
			object v1 = Argument1.AssociatedObjectValue;
			object v2 = Argument2.AssociatedObjectValue;

			if (Expression.TryConvert(v1, out string s1) && Expression.TryConvert(v2, out string s2))
				return this.EvaluateScalarAsync(s1, s2, Variables);
			else if (Expression.TryConvert(v1, out double d1) && Expression.TryConvert(v2, out double d2))
				return this.EvaluateScalarAsync(d1, d2, Variables);
			else if (Expression.TryConvert(v1, out bool b1) && Expression.TryConvert(v2, out bool b2))
				return this.EvaluateScalarAsync(b1, b2, Variables);
			else if (Expression.TryConvert(v1, out Complex z1) && Expression.TryConvert(v2, out Complex z2))
				return this.EvaluateScalarAsync(z1, z2, Variables);
			else if (Expression.TryConvert(v1, out Integer i1) && Expression.TryConvert(v2, out Integer i2))
				return this.EvaluateScalarAsync((double)i1.Value, (double)i2.Value, Variables);
			else if (Expression.TryConvert(v1, out RationalNumber q1) && Expression.TryConvert(v2, out RationalNumber q2))
				return this.EvaluateScalarAsync(q1.ToDouble(), q2.ToDouble(), Variables);
			else
				throw new ScriptRuntimeException("Type of scalar not supported.", this);
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual Task<IElement> EvaluateScalarAsync(double Argument1, double Argument2, Variables Variables)
		{
			return Task.FromResult(this.EvaluateScalar(Argument1, Argument2, Variables));
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual Task<IElement> EvaluateScalarAsync(Complex Argument1, Complex Argument2, Variables Variables)
		{
			return Task.FromResult(this.EvaluateScalar(Argument1, Argument2, Variables));
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual Task<IElement> EvaluateScalarAsync(bool Argument1, bool Argument2, Variables Variables)
		{
			return Task.FromResult(this.EvaluateScalar(Argument1, Argument2, Variables));
		}

		/// <summary>
		/// Evaluates the function on two scalar arguments.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public virtual Task<IElement> EvaluateScalarAsync(string Argument1, string Argument2, Variables Variables)
		{
			return Task.FromResult(this.EvaluateScalar(Argument1, Argument2, Variables));
		}

	}
}
