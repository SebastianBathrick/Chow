using System;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;
namespace Chow.Interpreter
{
    static class ApiConverter
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
                case ChowFunction functionValue:
                    return new TaggedUnion(functionValue.Value);
                case ChowDynamic dynamicValue:
                    return new TaggedUnion(dynamicValue.Value);
                default:
                    return ToPrimitiveTaggedUnion(value);
            }
        }

        static TaggedUnion ToPrimitiveTaggedUnion(ChowValue value)
        {
            if (value.IsType<bool>())
            {
                return new TaggedUnion(value.AsType<bool>());
            }

            if (value.IsType<long>())
            {
                return new TaggedUnion(value.AsType<long>());
            }

            if (value.IsType<double>())
            {
                return new TaggedUnion(value.AsType<double>());
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
                    if (taggedUnion.ObjectValue is Closure)
                    {
                        return new ChowFunction(taggedUnion.ObjectValue);
                    }

                    return new ChowDynamic(taggedUnion.ObjectValue);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
