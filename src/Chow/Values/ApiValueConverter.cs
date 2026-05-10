using System;

namespace Chow.Interpreter.Values
{
    internal static class ApiValueConverter
    {
        public static TaggedUnion ToTaggedUnion(ChowValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.IsNone)
            {
                return TaggedUnion.None;
            }

            if (value.IsBoolValue)
            {
                return new TaggedUnion(value.BoolValue);
            }

            if (value.IsIntValue)
            {
                return new TaggedUnion(value.IntValue);
            }

            if (value.IsFloatValue)
            {
                return new TaggedUnion(value.FloatValue);
            }

            throw new NotImplementedException();
        }

        public static ChowValue ToChowValue(TaggedUnion taggedUnion)
        {
            switch (taggedUnion.Tag)
            {
                case Tag.None:
                    return ChowValue.None;
                case Tag.Integer:
                    return new ChowInteger(taggedUnion.IntegerValue);
                case Tag.Float:
                    return new ChowFloat(taggedUnion.FloatValue);
                case Tag.Boolean:
                    return new ChowBool(taggedUnion.BooleanValue);
                case Tag.String:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
