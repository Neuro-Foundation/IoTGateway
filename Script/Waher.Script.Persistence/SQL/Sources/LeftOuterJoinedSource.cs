﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using Waher.Script.Model;
using Waher.Script.Persistence.SQL.Enumerators;

namespace Waher.Script.Persistence.SQL.Sources
{
    /// <summary>
    /// Data source formed through an LEFT [OUTER] JOIN of two sources.
    /// </summary>
    public class LeftOuterJoinedSource : JoinedSource
	{
		/// <summary>
		/// Data source formed through an LEFT [OUTER] JOIN of two sources.
		/// </summary>
		/// <param name="Left">Left source</param>
		/// <param name="Right">Right source</param>
		/// <param name="Conditions">Conditions for join.</param>
		public LeftOuterJoinedSource(IDataSource Left, IDataSource Right, ScriptNode Conditions)
			: base(Left, Right, Conditions)
		{
		}

		/// <summary>
		/// If sources should be flipped in the <see cref="JoinedObject"/> instances created.
		/// </summary>
		protected virtual bool Flipped => false;

		/// <summary>
		/// Finds objects matching filter conditions in <paramref name="Where"/>.
		/// </summary>
		/// <param name="Offset">Offset at which to return elements.</param>
		/// <param name="Top">Maximum number of elements to return.</param>
		/// <param name="Generic">If objects of type <see cref="GenericObject"/> should be returned.</param>
		/// <param name="Where">Filter conditions.</param>
		/// <param name="Variables">Current set of variables.</param>
		/// <param name="Order">Order at which to order the result set.</param>
		/// <param name="Node">Script node performing the evaluation.</param>
		/// <returns>Enumerator.</returns>
		public override async Task<IResultSetEnumerator> Find(int Offset, int Top, bool Generic, ScriptNode Where, Variables Variables,
			KeyValuePair<VariableReference, bool>[] Order, ScriptNode Node)
		{
			ScriptNode LeftWhere = await Reduce(this.Left, Where);
			KeyValuePair<VariableReference, bool>[] LeftOrder = await Reduce(this.Left, Order);

			IResultSetEnumerator e = await this.Left.Find(0, int.MaxValue, Generic, LeftWhere, Variables, LeftOrder, Node);

			ScriptNode RightWhere = this.Combine(await Reduce(this.Right, this.Left, Where), this.Conditions);

			e = new LeftOuterJoinEnumerator(e, this.Left.Name, this.Right, this.Right.Name, Generic, RightWhere, Variables, this.Flipped);

			if (!(Where is null))
				e = new ConditionalEnumerator(e, Variables, Where);

			if (Offset > 0)
				e = new OffsetEnumerator(e, Offset);

			if (Top != int.MaxValue)
				e = new MaxCountEnumerator(e, Top);

			return e;
		}

		internal class LeftOuterJoinEnumerator : IResultSetEnumerator
		{
			private readonly IResultSetEnumerator left;
			private readonly IDataSource rightSource;
			private readonly ScriptNode conditions;
			private readonly Variables variables;
			private readonly string leftName;
			private readonly string rightName;
			private readonly bool hasLeftName;
			private readonly bool flipped;
			private readonly bool generic;
			private bool rightFirst;
			private IResultSetEnumerator right;
			private JoinedObject current = null;
			private GenericObject defaultRight = null;
			private ObjectProperties leftVariables = null;

			public LeftOuterJoinEnumerator(IResultSetEnumerator Left, string LeftName, IDataSource RightSource, string RightName, 
				bool Generic, ScriptNode Conditions, Variables Variables, bool Flipped)
			{
				this.left = Left;
				this.leftName = LeftName;
				this.rightName = RightName;
				this.rightSource = RightSource;
				this.generic = Generic;
				this.conditions = Conditions;
				this.variables = Variables;
				this.hasLeftName = !string.IsNullOrEmpty(this.leftName);
				this.flipped = Flipped;
			}

			public object Current => this.current;

			public bool MoveNext()
			{
				return this.MoveNextAsync().Result;
			}

			public async Task<bool> MoveNextAsync()
			{
				while (true)
				{
					if (!(this.right is null))
					{
						bool First = this.rightFirst;
						this.rightFirst = false;

						if (await this.right.MoveNextAsync())
						{
							if (this.flipped)
								this.current = new JoinedObject(this.right.Current, this.rightName, this.left.Current, this.leftName);
							else
								this.current = new JoinedObject(this.left.Current, this.leftName, this.right.Current, this.rightName);

							return true;
						}
						else
						{
							this.right = null;

							if (First)
							{
								if (this.defaultRight is null)
								{
									this.defaultRight = new GenericObject(this.rightSource.CollectionName,
										typeof(GenericObject).FullName, Guid.Empty, new KeyValuePair<string, object>[0]);
								}

								if (this.flipped)
									this.current = new JoinedObject(this.defaultRight, this.rightName, this.left.Current, this.leftName);
								else
									this.current = new JoinedObject(this.left.Current, this.leftName, this.defaultRight, this.rightName);

								return true;
							}
						}
					}

					if (!await this.left.MoveNextAsync())
						return false;

					if (this.leftVariables is null)
						this.leftVariables = new ObjectProperties(this.left.Current, this.variables);
					else
						this.leftVariables.Object = this.left.Current;

					if (this.hasLeftName)
						this.leftVariables[this.leftName] = this.left.Current;

					this.right = await this.rightSource.Find(0, int.MaxValue, this.generic, this.conditions, this.leftVariables, 
						null, this.conditions);

					this.rightFirst = true;
				}
			}

			public void Reset()
			{
				this.current = null;
				this.right = null;
				this.left.Reset();
			}
		}
	
	}
}
