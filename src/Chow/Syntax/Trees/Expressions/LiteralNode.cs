using System;

namespace Chow.Interpreter.Syntax.Trees.Expressions
{
    internal class LiteralNode : Node
    {
        object _val;
        LiteralDataType _type;

        public object Value => _val;
        public LiteralDataType Type => _type;

        public LiteralNode(object value, int lineNumber) : base(lineNumber)
        {
            if (value == null)
            {
                _type = LiteralDataType.None;
            }
            else if (value is int)
            {
                _type = LiteralDataType.Integer;
            }
            else if (value is double)
            {
                _type = LiteralDataType.Float;
            }
            else if (value is bool)
            {
                _type = LiteralDataType.Boolean;
            }
            else if (value is string)
            {
                _type = LiteralDataType.String;
            }
            else
            {
                throw new ArgumentException($"Unsupported literal type: {value.GetType().Name}", nameof(value));
            }

            _val = value;
        }

        public override string ToString()
        {
            return $"{_val} line={LineNum}";
        }
    }
}
