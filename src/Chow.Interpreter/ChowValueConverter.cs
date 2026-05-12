using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter
{
    static class ChowValueConverter
    {
        public static TaggedUnion ToTaggedUnion(ChowValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            switch (value)
            {
                case var noneValue when noneValue.IsNone:
                    return TaggedUnion.None;
                case ChowStr strValue:
                    return new TaggedUnion(strValue.Value);
                case ChowList listValue:
                    return new TaggedUnion(listValue.Internal);
                case ChowDict dictValue:
                    return new TaggedUnion(dictValue.Internal);
                case ChowDynamic dynamicValue:
                    return new TaggedUnion(dynamicValue.Value);
                default:
                    return ToPrimitiveTaggedUnion(value);
            }
        }

        static TaggedUnion ToPrimitiveTaggedUnion(ChowValue value)
        {
            if (value.Is<bool>())
            {
                return new TaggedUnion(value.As<bool>());
            }

            if (value.Is<long>())
            {
                return new TaggedUnion(value.As<long>());
            }

            if (value.Is<double>())
            {
                return new TaggedUnion(value.As<double>());
            }

            throw new NotImplementedException();
        }

        public static ChowValue ToChowValue(TaggedUnion taggedUnion)
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
