using System;
using System.Collections.Generic;
using System.Text;

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

            if (value.IsIntegerValue)
            {
                return new TaggedUnion(value.IntegerValue);
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
                    return ChowNone.Instance;
                case Tag.Integer:
                    return new ChowInteger(taggedUnion.IntegerValue);
                case Tag.Float:
                    return new ChowFloat(taggedUnion.FloatValue);
                case Tag.String:
                case Tag.Boolean:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
