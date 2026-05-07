using System;

namespace Chow.Interpreter.Syntax.Trees.Expressions
{
    internal class LiteralNode : Node
    {
        object _value;
        LiteralDataType _type;

        public object Value => _value;
        public LiteralDataType Type => _type;

        public LiteralNode(object value, int lineNumber) : base(lineNumber)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value is int)
            {
                _type = LiteralDataType.Integer;
            }
            else if (value is float)
            {
                _type = LiteralDataType.Float;
            }
            else if (value is null)
            {
                _type = LiteralDataType.None;
            }
            else
            {
                throw new ArgumentException($"Unsupported literal type: {value.GetType().Name}", nameof(value));
            }

            _value = value;
        }

        public override string ToString()
        {
            return $"{_value} line={LineNumber}";
        }
    }
}
