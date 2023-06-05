﻿namespace Waher.Content.Semantic.Model.Literals
{
    /// <summary>
    /// Represents a 16-bit unsigned integer literal.
    /// </summary>
    public class UInt16Literal : SemanticNumericLiteral
	{
        /// <summary>
        /// Represents a 16-bit unsigned integer literal.
        /// </summary>
        /// <param name="Value">Literal value</param>
        public UInt16Literal()
            : base()
        {
        }

        /// <summary>
        /// Represents a 16-bit unsigned integer literal.
        /// </summary>
        /// <param name="Value">Parsed value</param>
        public UInt16Literal(ushort Value)
            : base(Value, Value.ToString())
        {
        }

        /// <summary>
        /// Represents a 16-bit unsigned integer literal.
        /// </summary>
        /// <param name="Value">Parsed value</param>
        /// <param name="StringValue">String value.</param>
        public UInt16Literal(ushort Value, string StringValue)
            : base(Value, StringValue)
        {
        }

        /// <summary>
        /// Type name
        /// </summary>
        public override string StringType => "http://www.w3.org/2001/XMLSchema#unsignedShort";

		/// <summary>
		/// Comparable numeric value.
		/// </summary>
		public override double ComparableValue => (ushort)this.Value;

		/// <summary>
		/// Tries to parse a string value of the type supported by the class..
		/// </summary>
		/// <param name="Value">String value.</param>
		/// <param name="DataType">Data type.</param>
		/// <param name="Language">Language code if available.</param>
		/// <returns>Parsed literal.</returns>
		public override ISemanticLiteral Parse(string Value, string DataType, string Language)
        {
            if (ushort.TryParse(Value, out ushort i))
                return new UInt16Literal(i, Value);
            else
                return new CustomLiteral(Value, DataType, Language);
        }

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is UInt16Literal Typed &&
				Typed.StringValue == this.StringValue;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return this.StringValue.GetHashCode();
		}
	}
}
