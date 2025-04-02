﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators.Comparisons
{
	/// <summary>
	/// Delegate for expression transforms.
	/// </summary>
	/// <param name="Expression">Like expression</param>
	/// <returns>Transformed expression</returns>
	public delegate string ExpressionTransform(string Expression);

	/// <summary>
	/// Like
	/// </summary>
	public class Like : BinaryScalarOperator
	{
		/// <summary>
		/// Like
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Like(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Event raised before performing comparison. Can be used to transform expression.
		/// </summary>
		public event ExpressionTransform TransformExpression;

		/// <summary>
		/// Evaluates the operator on scalar operands.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override IElement EvaluateScalar(IElement Left, IElement Right, Variables Variables)
		{
			if (!(Left.AssociatedObjectValue is string sl))
				throw new ScriptRuntimeException("String values expected.", this);

			if (!(Right.AssociatedObjectValue is string sr))
				throw new ScriptRuntimeException("String values expected.", this);

			Match M;

			ExpressionTransform h = this.TransformExpression;
			if (!(h is null))
				sr = h(sr);

			M = this.Matches(sl, sr, out string[] GroupNames);

			if (M.Success)
			{
				if (!(GroupNames is null))
				{
					foreach (string GroupName in GroupNames)
					{
						Group G = M.Groups[GroupName];
						if (G.Success)
						{
							string Value = G.Value;

							if (Expression.TryParse(Value, out double d))
								Variables[GroupName] = d;
							else
								Variables[GroupName] = Value;

							Variables[GroupName + "_STR"] = Value;
							Variables[GroupName + "_POS"] = G.Index;
							Variables[GroupName + "_LEN"] = G.Length;
						}
					}
				}

				if (this.partialMatch || (M.Index == 0 && M.Length == sl.Length))
					return BooleanValue.True;
			}

			return BooleanValue.False;
		}

		/// <summary>
		/// Checks a value against a regular expression.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <param name="Expression">Regular expression</param>
		/// <param name="GroupNames">Group name values resulting from the matching.</param>
		/// <returns>If there was a match or not.</returns>
		protected Match Matches(string Value, string Expression, out string[] GroupNames)
		{
			lock (this.synchObject)
			{
				if (this.lastExpression is null || Expression != this.lastExpression)
				{
					this.lastExpression = Expression;
					this.regex = new Regex(Expression, this.options);

					ChunkedList<string> Names = null;

					foreach (string s in this.regex.GetGroupNames())
					{
						if (!int.TryParse(s, out int _))
						{
							if (Names is null)
								Names = new ChunkedList<string>();

							Names.Add(s);
						}
					}

					if (Names is null)
						this.groupNames = null;
					else
						this.groupNames = Names.ToArray();
				}

				GroupNames = this.groupNames;

				return this.regex.Match(Value);
			}
		}

		private Regex regex = null;
		private string[] groupNames = null;
		private string lastExpression = null;
		private RegexOptions options = RegexOptions.Singleline;
		private bool partialMatch = false;
		private readonly object synchObject = new object();

		/// <summary>
		/// Options for regular expression. Default is <see cref="RegexOptions.Singleline"/>.
		/// </summary>
		public RegexOptions Options
		{
			get => this.options;
			set
			{
				lock (this.synchObject)
				{
					this.options = value;
					this.lastExpression = null;
				}
			}
		}

		/// <summary>
		/// If a partial match is sufficient for operator to return true. (Default=false)
		/// </summary>
		public bool PartialMatch
		{
			get => this.partialMatch;
			set => this.partialMatch = value;
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(IElement CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			if (!(CheckAgainst.AssociatedObjectValue is string sl))
				return PatternMatchResult.NoMatch;

			if (this.right is ConstantElement RightConstant &&
				RightConstant.Constant.AssociatedObjectValue is string sr)
			{
				ExpressionTransform h = this.TransformExpression;
				if (!(h is null))
					sr = h(sr);

				Match M = this.Matches(sl, sr, out string[] GroupNames);

				if (M.Success)
				{
					if (!(GroupNames is null))
					{
						foreach (string GroupName in GroupNames)
						{
							Group G = M.Groups[GroupName];
							if (G.Success)
							{
								string Value = G.Value;
								object ObjValue;

								if (Expression.TryParse(Value, out double d))
									ObjValue = d;
								else
									ObjValue = Value;

								if (AlreadyFound.TryGetValue(GroupName, out IElement E) &&
									!E.AssociatedObjectValue.Equals(ObjValue))
								{
									return PatternMatchResult.NoMatch;
								}

								AlreadyFound[GroupName] = Expression.Encapsulate(ObjValue);
							}
						}
					}

					return this.left.PatternMatch(CheckAgainst, AlreadyFound);
				}
			}

			return PatternMatchResult.NoMatch;
		}

	}
}
