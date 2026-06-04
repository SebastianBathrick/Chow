using System;
using System.Collections.Generic;
using Chow.DataTypes;
using Chow.Exceptions;
using Chow.Bytecode;
namespace Chow.Expressions
{
    static class ArithmeticEvaluator
    {
        static readonly IReadOnlyDictionary<(Tag, Tag), Tag> TagConversionMap =
            new Dictionary<(Tag, Tag), Tag>()
            {
                { (Tag.Bool, Tag.Long), Tag.Long },
                { (Tag.Long, Tag.Double), Tag.Double },
                { (Tag.Double, Tag.Long), Tag.Long },
            };

        public static TaggedUnion Evaluate(
            ref TaggedUnion left,
            ref TaggedUnion right,
            Bytecode.OperationCode op)
        {


            switch (op)
            {
                case OperationCode.Add:
                    return EvaluateAdd(ref left, ref right, op);
                default:
                    throw new InvalidOperationException(nameof(Evaluate));
            }
        }

        static TaggedUnion EvaluateAdd(
            ref TaggedUnion left,
            ref TaggedUnion right,
            OperationCode op)
        {
            switch (GetConversionTag(left.Tag, right.Tag))
            {
                case Tag.Long:
                    return new TaggedUnion(left.ToLong() + right.ToLong());
                case Tag.Double:
                    return new TaggedUnion(left.ToDouble() + right.ToDouble());
                default:
                    throw new TypeException(left.Tag, right.Tag, op);
            }
        }

        static Tag GetConversionTag(Tag left, Tag right)
        {
            var mapKey = (left, right);
            var convertToTag = TagConversionMap.TryGetValue(mapKey, out var conversion)
                ? conversion
                : throw new TypeException(left, right, OperationCode.Add);

            return convertToTag;
        }
    }
}
