﻿namespace Waher.Content.Semantic.TurtleModel
{
	/// <summary>
	/// Represents a 32-bit unsigned integer literal.
	/// </summary>
	public class UInt32Literal : SemanticLiteral
	{
		/// <summary>
		/// Represents a 32-bit unsigned integer literal.
		/// </summary>
		/// <param name="Value">Literal value</param>
		public UInt32Literal()
			: base()
		{
		}

		/// <summary>
		/// Represents a 32-bit unsigned integer literal.
		/// </summary>
		/// <param name="Value">Parsed value</param>
		public UInt32Literal(uint Value)
			: base(Value, Value.ToString())
		{
		}

		/// <summary>
		/// Represents a 32-bit unsigned integer literal.
		/// </summary>
		/// <param name="Value">Parsed value</param>
		/// <param name="StringValue">String value.</param>
		public UInt32Literal(uint Value, string StringValue)
			: base(Value, StringValue)
		{
		}

		/// <summary>
		/// Type name
		/// </summary>
		public override string StringType => "http://www.w3.org/2001/XMLSchema#unsignedInt";

		/// <summary>
		/// Tries to parse a string value of the type supported by the class..
		/// </summary>
		/// <param name="Value">String value.</param>
		/// <param name="DataType">Data type.</param>
		/// <returns>Parsed literal.</returns>
		public override ISemanticLiteral Parse(string Value, string DataType)
		{
			if (uint.TryParse(Value, out uint i))
				return new UInt32Literal(i, Value);
			else
				return new CustomLiteral(Value, DataType);
		}
	}
}