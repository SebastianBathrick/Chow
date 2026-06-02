using System;
namespace Chow.SyntaxTrees.Literals
{
    sealed class LiteralNode : Node
    {
        public object Value { get; }

        public LiteralDataType Type { get; }

        public LiteralNode(object value, int line) : base(line)
        {
            switch (value)
            {
                case null:
                    Type = LiteralDataType.None;
                    break;
                case long _:
                    Type = LiteralDataType.Integer;
                    break;
                case double _:
                    Type = LiteralDataType.Float;
                    break;
                case bool _:
                    Type = LiteralDataType.Boolean;
                    break;
                case string _:
                    Type = LiteralDataType.String;
                    break;
                default:
                    throw new ArgumentException($"Unsupported literal type: {value.GetType().Name}", nameof(value));
            }

            Value = value;
        }

    }
}
