using System;

namespace Chow.Interpreter.Values.Internal
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
                return new TaggedUnion(strValue.Value);
            }

            if (value is ChowList listValue)
            {
                return new TaggedUnion(listValue.Internal);
            }

            if (value is ChowDict dictValue)
            {
                return new TaggedUnion(dictValue.Internal);
            }

            if (value is ChowDynamic dynamicValue)
            {
                return new TaggedUnion(dynamicValue.Value);
            }

            if (value.Is<bool>())
            {
                return new TaggedUnion(value.As<bool>());
            }

            if (value.Is<int>())
            {
                return new TaggedUnion(value.As<int>());
            }

            if (value.Is<double>())
            {
                return new TaggedUnion(value.As<double>());
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
                case Tag.Str:
                    return new ChowStr(taggedUnion.StringValue);
                case Tag.List:
                    return new ChowList(taggedUnion.ListValue);
                case Tag.Dict:
                    return new ChowDict(taggedUnion.DictValue);
                case Tag.Object:
                    return new ChowDynamic(taggedUnion.ObjectValue);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
