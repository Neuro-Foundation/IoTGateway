﻿using Waher.Script.Abstraction.Sets;

namespace Waher.Script.Abstraction.Elements
{
	/// <summary>
	/// Base class for all types of abelian group elements.
	/// </summary>
	public abstract class AbelianGroupElement : GroupElement, IAbelianGroupElement
	{
		/// <summary>
		/// Base class for all types of abelian group elements.
		/// </summary>
		public AbelianGroupElement()
		{
		}

		/// <summary>
		/// Tries to add an element to the current element, from the left.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override ISemiGroupElement AddLeft(ISemiGroupElement Element)
		{
			if (Element is IAbelianGroupElement E)
				return E.Add(this);
			else
				return null;
		}

		/// <summary>
		/// Tries to add an element to the current element, from the right.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public override ISemiGroupElement AddRight(ISemiGroupElement Element)
		{
			if (Element is IAbelianGroupElement E)
				return this.Add(E);
			else
				return null;
		}

		/// <summary>
		/// Tries to add an element to the current element.
		/// </summary>
		/// <param name="Element">Element to add.</param>
		/// <returns>Result, if understood, null otherwise.</returns>
		public abstract IAbelianGroupElement Add(IAbelianGroupElement Element);

		/// <summary>
		/// Associated Set.
		/// </summary>
		public override ISet AssociatedSet => this.AssociatedAbelianGroup;

		/// <summary>
		/// Associated Semi-Group.
		/// </summary>
		public override ISemiGroup AssociatedSemiGroup => this.AssociatedAbelianGroup;

		/// <summary>
		/// Associated Group.
		/// </summary>
		public override IGroup AssociatedGroup => this.AssociatedAbelianGroup;

		/// <summary>
		/// Associated Abelian Group.
		/// </summary>
		public abstract IAbelianGroup AssociatedAbelianGroup
		{
			get;
		}

		/// <summary>
		/// Returns the zero element of the group.
		/// </summary>
		public abstract IAbelianGroupElement Zero
		{
			get;
		}
	}
}
