using System;

namespace Chow.Interpreter.Values
{
    public class InvalidCastException : Exception
    {
        public InvalidCastException(Type fromType, Type toType, ChowValue value)
            : base($"Cannot convert value from {fromType} to {toType}.")
        {
            FromType = fromType;
            ToType = toType;
            Value = value;
        }

        public Type FromType { get; }

        public Type ToType { get; }

        public ChowValue Value { get; }
    }
}
