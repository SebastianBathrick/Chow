using System;

namespace Chow.Interpreter.Values
{
    internal class ChowBool : ChowValue
    {
        private const string TRUE_STRING = "True";
        private const string FALSE_STRING = "False";

        private bool _val;

        public ChowBool(bool val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)_val;
            }

            if (typeof(DataType) == typeof(int))
            {
                return (DataType)(object)(_val ? 1 : 0);
            }

            if (typeof(DataType) == typeof(float))
            {
                return (DataType)(object)(_val ? 1f : 0f);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool IsTypeOf<DataType>()
        {
            return typeof(DataType) == typeof(bool);
        }

        public override string ToString() => _val ? TRUE_STRING : FALSE_STRING;
    }
}
