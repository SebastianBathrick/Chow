using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
{
    internal class LiteralNode : Node
    {
        public enum DataType
        {
            Integer,
            Float
        }

        object _value;
        DataType _type;

        public object Value => _value;
        public DataType Type => _type;

        public LiteralNode(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value is int)
                _type = DataType.Integer;
            else if (value is float)
                _type = DataType.Float;
            else
                throw new ArgumentException($"Unsupported literal type: {value.GetType().Name}", nameof(value));

            _value = value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
