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

            if (value is ChowStr strValue)
            {
                return new TaggedUnion((object)strValue.Value);
            }

            if (value is ChowDynamic dynamicValue)
            {
                return new TaggedUnion(dynamicValue.Value);
            }

            if (value.IsTypeOf<bool>())
            {
                return new TaggedUnion(value.As<bool>());
            }

            if (value.IsTypeOf<int>())
            {
                return new TaggedUnion(value.As<int>());
            }

            if (value.IsTypeOf<float>())
            {
                return new TaggedUnion(value.As<float>());
            }

            throw new NotImplementedException();
        }

        public static ChowValue ToApiClassObj(TaggedUnion taggedUnion)
        {
            switch (taggedUnion.Tag)
            {
                case Tag.None:
                    return ChowValue.None;
                case Tag.Int:
                    return new ChowInt(taggedUnion.IntegerValue);
                case Tag.Float:
                    return new ChowFloat(taggedUnion.FloatValue);
                case Tag.Boolean:
                    return new ChowBool(taggedUnion.BooleanValue);
                case Tag.Object:
                    if (taggedUnion.ObjectValue is string s)
                    {
                        return new ChowStr(s);
                    }
                    return new ChowDynamic(taggedUnion.ObjectValue);
                case Tag.String:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
