﻿using System.Collections;
using System.Collections.Generic;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Objects.Matrices;
using Waher.Script.Operators.Matrices;
using Waher.Script.Operators.Vectors;

namespace Waher.Content.Semantic
{
	/// <summary>
	/// In-memory semantic model.
	/// </summary>
	public class InMemorySemanticModel : ISemanticModel, IToMatrix, IToVector
	{
		/// <summary>
		/// Triples in model.
		/// </summary>
		protected readonly LinkedList<ISemanticTriple> triples;

		/// <summary>
		/// In-memory semantic model.
		/// </summary>
		public InMemorySemanticModel()
		{
			this.triples = new LinkedList<ISemanticTriple>();
		}

		/// <summary>
		/// In-memory semantic model.
		/// </summary>
		/// <param name="Triples">Triples.</param>
		public InMemorySemanticModel(IEnumerable<ISemanticTriple> Triples)
		{
			this.triples = Triples as LinkedList<ISemanticTriple>;

			if (this.triples is null)
			{
				this.triples = new LinkedList<ISemanticTriple>();

				foreach (ISemanticTriple Triple in Triples)
					this.triples.AddLast(Triple);
			}
		}

		/// <summary>
		/// Gets an enumerator of available triples.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		public IEnumerator<ISemanticTriple> GetEnumerator()
		{
			return this.triples.GetEnumerator();
		}

		/// <summary>
		/// Gets an enumerator of available triples.
		/// </summary>
		/// <returns>Enumerator object.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.triples.GetEnumerator();
		}

		/// <summary>
		/// Adds a triple to the model.
		/// </summary>
		/// <param name="Triple">Semantic triple to add.</param>
		public virtual void Add(ISemanticTriple Triple)
		{
			this.triples.AddLast(Triple);
		}

		/// <summary>
		/// Converts the object to a matrix.
		/// </summary>
		/// <returns>Matrix.</returns>
		public IMatrix ToMatrix()
		{
			LinkedList<IElement> Elements = new LinkedList<IElement>();
			int Rows = 0;

			foreach (ISemanticTriple T in this.triples)
			{
				Elements.AddLast(T.Subject);
				Elements.AddLast(T.Predicate);
				Elements.AddLast(T.Object);
				Rows++;
			}

			return new ObjectMatrix(Rows, 3, Elements)
			{
				ColumnNames = new string[] { "Subject", "Predicate", "Object" }
			};
		}

		/// <summary>
		/// Converts the object to a vector.
		/// </summary>
		/// <returns>Matrix.</returns>
		public IElement ToVector()
		{
			return VectorDefinition.Encapsulate(this.triples, false, null);
		}
	}
}
