﻿namespace Waher.Persistence.Filters
{
	/// <summary>
	/// This filter selects objects that have a named field not equal to a given value.
	/// </summary>
	public class FilterFieldNotEqualTo : FilterFieldValue
	{
		/// <summary>
		/// This filter selects objects that have a named field not equal to a given value.
		/// </summary>
		/// <param name="FieldName">Field Name.</param>
		/// <param name="Value">Value.</param>
		public FilterFieldNotEqualTo(string FieldName, object Value)
			: base(FieldName, Value)
		{
		}

		/// <summary>
		/// Calculates the logical inverse of the filter.
		/// </summary>
		/// <returns>Logical inverse of the filter.</returns>
		public override Filter Negate()
		{
			return new FilterFieldEqualTo(this.FieldName, this.Value);
		}

		/// <summary>
		/// Creates a copy of the filter.
		/// </summary>
		/// <returns>Copy of filter.</returns>
		public override Filter Copy()
		{
			return new FilterFieldNotEqualTo(this.FieldName, this.Value);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.FieldName + "<>" + this.Value?.ToString();
		}

		/// <summary>
		/// Compares two object values.
		/// </summary>
		/// <param name="Value1">Value 1</param>
		/// <param name="Value2">Value 2</param>
		/// <returns>Result</returns>
		protected override bool Compare(object Value1, object Value2)
		{
			return !Value1.Equals(Value2);
		}

		/// <summary>
		/// Compares two numerical values.
		/// </summary>
		/// <param name="Value1">Value 1</param>
		/// <param name="Value2">Value 2</param>
		/// <returns>Result</returns>
		protected override bool Compare(double Value1, double Value2)
		{
			return Value1 != Value2;
		}

		/// <summary>
		/// Compares two string values.
		/// </summary>
		/// <param name="Value1">Value 1</param>
		/// <param name="Value2">Value 2</param>
		/// <returns>Result</returns>
		protected override bool Compare(string Value1, string Value2)
		{
			return Value1 != Value2;
		}
	}
}
