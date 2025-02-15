﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;

namespace Waher.Script.Objects.Sets
{
    /// <summary>
    /// Represents a finite set.
    /// </summary>
    public sealed class FiniteSet : Set
    {
        private readonly Dictionary<IElement, bool> elements;

        /// <summary>
        /// Represents a finite set.
        /// </summary>
        /// <param name="Elements">Elements of set.</param>
        public FiniteSet(IEnumerable<IElement> Elements)
        {
            this.elements = new Dictionary<IElement, bool>();
            foreach (IElement E in Elements)
                this.elements[E] = true;
        }

		/// <summary>
		/// Represents a finite set.
		/// </summary>
		/// <param name="Elements">Elements of set.</param>
		public FiniteSet(IEnumerable Elements)
		{
			this.elements = new Dictionary<IElement, bool>();
            
            foreach (object Item in Elements)
            {
                IElement E = Expression.Encapsulate(Item);
				this.elements[E] = true;
			}
		}

		/// <summary>
		/// Checks if the set contains an element.
		/// </summary>
		/// <param name="Element">Element.</param>
		/// <returns>If the element is contained in the set.</returns>
		public override bool Contains(IElement Element)
        {
            return this.elements.ContainsKey(Element);
        }

        /// <summary>
        /// Compares the element to another.
        /// </summary>
        /// <param name="obj">Other element to compare against.</param>
        /// <returns>If elements are equal.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FiniteSet S))
                return false;

            if (this.elements.Count != S.elements.Count)
                return false;

            foreach (IElement E in this.elements.Keys)
            {
                if (!S.elements.ContainsKey(E))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates a hash code of the element.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            int i = 0;

            foreach (IElement E in this.elements.Keys)
				i ^= i << 5 ^ E.GetHashCode();

            return i;
        }

        /// <summary>
        /// An enumeration of child elements. If the element is a scalar, this property will return null.
        /// </summary>
        public override ICollection<IElement> ChildElements => this.elements.Keys;

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = null;

            foreach (IElement Element in this.elements.Keys)
            {
                if (sb is null)
                    sb = new StringBuilder("{");
                else
                    sb.Append(", ");

                sb.Append(Element.ToString());
            }

            if (sb is null)
                return "{}";
            else
            {
                sb.Append('}');
                return sb.ToString();
            }
        }

        /// <summary>
        /// Associated object value.
        /// </summary>
        public override object AssociatedObjectValue
        {
            get
            {
                object[] Elements = new object[this.elements.Count];
                int i = 0;

                foreach (IElement E in this.elements.Keys)
                    Elements[i++] = E.AssociatedObjectValue;

                return Elements;
            }
        }

        /// <summary>
        /// Size of set, if finite and known, otherwise null is returned.
        /// </summary>
        public override int? Size
        {
            get { return this.elements.Count; }
        }

    }
}
