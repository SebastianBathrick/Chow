using System;
using System.Collections.Generic;
using Chow.DataTypes;
using Chow.Exceptions;

namespace Chow.Expressions
{
    /// <summary>
    /// Static service class for <see cref="Chow.Core.VirtualMachine"/> that performs comparison
    /// operations that behave the same as Python's comparisons for Chow's supported types.
    /// </summary>
    static class ComparisonEvaluator
    {
        // Python treats bool as a subtype of int; any float operand promotes the whole comparison to float.
        static readonly IReadOnlyDictionary<(Tag, Tag), Tag> TagConversionMap =
            new Dictionary<(Tag left, Tag right), Tag>()
            {
                { (Tag.Bool, Tag.Bool), Tag.Long },
                { (Tag.Bool, Tag.Long), Tag.Long },
                { (Tag.Long, Tag.Bool), Tag.Long },
                { (Tag.Bool, Tag.Double), Tag.Double },
                { (Tag.Double, Tag.Bool), Tag.Double },
                { (Tag.Long, Tag.Long), Tag.Long },
                { (Tag.Long, Tag.Double), Tag.Double },
                { (Tag.Double, Tag.Long), Tag.Double },
                { (Tag.Double, Tag.Double), Tag.Double },
                { (Tag.Str, Tag.Str), Tag.Str },
            };

        #region Equality Operations

        public static TaggedUnion EvaluateEqual(ref TaggedUnion l, ref TaggedUnion r)
        {
            return new TaggedUnion(AreEqual(ref l, ref r));
        }

        public static TaggedUnion EvaluateNotEqual(ref TaggedUnion l, ref TaggedUnion r)
        {
            return new TaggedUnion(!AreEqual(ref l, ref r));
        }

        #endregion

        #region Ordering Operations

        public static TaggedUnion EvaluateLess(ref TaggedUnion l, ref TaggedUnion r)
        {
            return new TaggedUnion(EvaluateOrdering(ref l, ref r, ExpressionOperator.Less));
        }

        public static TaggedUnion EvaluateGreater(ref TaggedUnion l, ref TaggedUnion r)
        {
            return new TaggedUnion(EvaluateOrdering(ref l, ref r, ExpressionOperator.Greater));
        }

        public static TaggedUnion EvaluateLessEqual(ref TaggedUnion l, ref TaggedUnion r)
        {
            return new TaggedUnion(EvaluateOrdering(ref l, ref r, ExpressionOperator.LessEqual));
        }

        public static TaggedUnion EvaluateGreaterEqual(ref TaggedUnion l, ref TaggedUnion r)
        {
            return new TaggedUnion(EvaluateOrdering(ref l, ref r, ExpressionOperator.GreaterEqual));
        }

        #endregion

        #region Helper Methods

        static bool EvaluateOrdering(ref TaggedUnion l, ref TaggedUnion r, ExpressionOperator op)
        {
            switch (GetConversionTag(l.Tag, r.Tag, op))
            {
                case Tag.Long:
                    return CompareLong(l.ToLong(), r.ToLong(), op);
                case Tag.Double:
                    // Use relational operators instead of CompareTo so NaN comparisons stay false like Python.
                    return CompareDouble(l.ToDouble(), r.ToDouble(), op);
                case Tag.Str:
                    // Python orders strings lexicographically by Unicode code point; ordinal compare matches that.
                    return CompareResult(string.CompareOrdinal(l.ToString(), r.ToString()), op);
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool CompareLong(long left, long right, ExpressionOperator op)
        {
            switch (op)
            {
                case ExpressionOperator.Less:
                    return left < right;
                case ExpressionOperator.Greater:
                    return left > right;
                case ExpressionOperator.LessEqual:
                    return left <= right;
                case ExpressionOperator.GreaterEqual:
                    return left >= right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool CompareDouble(double left, double right, ExpressionOperator op)
        {
            switch (op)
            {
                case ExpressionOperator.Less:
                    return left < right;
                case ExpressionOperator.Greater:
                    return left > right;
                case ExpressionOperator.LessEqual:
                    return left <= right;
                case ExpressionOperator.GreaterEqual:
                    return left >= right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool CompareResult(int comparison, ExpressionOperator op)
        {
            switch (op)
            {
                case ExpressionOperator.Less:
                    return comparison < 0;
                case ExpressionOperator.Greater:
                    return comparison > 0;
                case ExpressionOperator.LessEqual:
                    return comparison <= 0;
                case ExpressionOperator.GreaterEqual:
                    return comparison >= 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool AreEqual(ref TaggedUnion l, ref TaggedUnion r)
        {
            if (TagConversionMap.TryGetValue((l.Tag, r.Tag), out var convTag))
            {
                // Numeric equality follows Python promotion: True == 1 and 1 == 1.0 are both true.
                switch (convTag)
                {
                    case Tag.Long:
                        return l.ToLong() == r.ToLong();
                    case Tag.Double:
                        return l.ToDouble() == r.ToDouble();
                    case Tag.Str:
                        return l.ToString() == r.ToString();
                }
            }

            if (l.Tag != r.Tag)
            {
                // Python equality across unrelated types returns False instead of raising TypeError.
                return false;
            }

            switch (l.Tag)
            {
                case Tag.None:
                    // None is a singleton in Python semantics, so None == None is always true.
                    return true;
                case Tag.List:
                    // List equality is structural and order-sensitive.
                    return ChowList.ElementsEqual((ChowList)l.ToObject(), (ChowList)r.ToObject());
                case Tag.Dict:
                    // Dict equality is structural; key insertion order does not decide equality.
                    return ChowDictionary.ElementsEqual((ChowDictionary)l.ToObject(), (ChowDictionary)r.ToObject());
                case Tag.Range:
                case Tag.Object:
                    // Chow ranges/objects do not have Python structural equality yet; preserve identity behavior.
                    return ReferenceEquals(l.ToObject(), r.ToObject());
                default:
                    return false;
            }
        }

        static Tag GetConversionTag(Tag leftTag, Tag rightTag, ExpressionOperator op)
        {
            var mapKey = (left: leftTag, right: rightTag);

            if (TagConversionMap.TryGetValue(mapKey, out var convTag))
            {
                return convTag;
            }

            throw new TypeException(
                $"TypeError: unsupported operand type(s) for {OperatorStrings.EnumToString(op)}: "
                + $"'{DataTypeNames.GetTypeName(leftTag)}' and '{DataTypeNames.GetTypeName(rightTag)}'");
        }

        #endregion
    }
}
