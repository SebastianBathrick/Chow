using System;

namespace Chow.Interpreter.Values
{
    /// <summary>
    /// Thrown by <see cref="ChowValue.AsType{TDataType}"/> when the requested conversion is not supported by
    /// the value's type. Inspect <see cref="FromType"/> and <see cref="ToType"/> to determine which conversion
    /// was attempted.
    /// </summary>
    public class InvalidCastException : Exception
    {
        /// <summary>Initialises a new <see cref="InvalidCastException"/> describing the failed conversion.</summary>
        /// <param name="fromType">The <see cref="ChowValue"/> subtype that was asked to convert.</param>
        /// <param name="toType">The target type that was requested.</param>
        /// <param name="value">The value instance that raised the error.</param>
        public InvalidCastException(Type fromType, Type toType, ChowValue value)
            : base($"Cannot convert value from {fromType} to {toType}.")
        {
            FromType = fromType;
            ToType = toType;
            Value = value;
        }

        /// <summary>Gets the <see cref="ChowValue"/> subtype that was asked to convert.</summary>
        public Type FromType { get; }

        /// <summary>Gets the target type that was requested.</summary>
        public Type ToType { get; }

        /// <summary>Gets the value instance that raised the error.</summary>
        public ChowValue Value { get; }
    }
}
