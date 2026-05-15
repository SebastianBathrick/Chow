using System;
namespace Chow.Interpreter.SyntaxTrees.Literals
{
    sealed class LiteralNode : Node
    {
        public object Value { get; }

        public LiteralDataType Type { get; }

        public LiteralNode(object value, int line) : base(line)
        {
            if (value == null)
            {
                Type = LiteralDataType.None;
            }
            else if (value is long)
            {
                Type = LiteralDataType.Integer;
            }
            else if (value is double)
            {
                Type = LiteralDataType.Float;
            }
            else if (value is bool)
            {
                Type = LiteralDataType.Boolean;
            }
            else if (value is string)
            {
                Type = LiteralDataType.String;
            }
            else
            {
                throw new ArgumentException($"Unsupported literal type: {value.GetType().Name}", nameof(value));
            }

            Value = value;
        }

    }
}
