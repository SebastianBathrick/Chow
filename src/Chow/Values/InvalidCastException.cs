using System;

namespace Chow.Interpreter.Values
{
    public class InvalidCastException : Exception
    {
        readonly Type _fromType;
        readonly Type _toType;

        readonly ChowValue _value;

        public InvalidCastException(Type fromType, Type toType, ChowValue value)
            : base($"Cannot convert value from {fromType} to {toType}.")
        {
            _fromType = fromType;
            _toType = toType;
            _value = value;
        }

        public Type FromType => _fromType;
        public Type ToType => _toType;
        public ChowValue Value => _value;
    }
}
