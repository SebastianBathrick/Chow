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
            {
                throw new ArgumentNullException(nameof(value));
            }

            _value = value;
        }
    }
}
