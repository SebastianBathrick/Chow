using System;

namespace Chow.Syntax
{
    sealed class LiteralNode : Node
    {
        public object Value { get; }

        public LiteralNodeType Type { get; }

        public LiteralNode(object value, int line) : base(line)
        {
            switch (value)
            {
                case null:
                    Type = LiteralNodeType.None;
                    break;
                case long _:
                    Type = LiteralNodeType.Integer;
                    break;
                case double _:
                    Type = LiteralNodeType.Float;
                    break;
                case bool _:
                    Type = LiteralNodeType.Boolean;
                    break;
                case string _:
                    Type = LiteralNodeType.String;
                    break;
                default:
                    throw new ArgumentException($"Unsupported literal type: {value.GetType().Name}", nameof(value));
            }

            Value = value;
        }
    }
}
