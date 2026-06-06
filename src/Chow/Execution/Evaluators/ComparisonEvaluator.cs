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
        static readonly IReadOnlyDictionary<(DataType, DataType), DataType> TagConversionMap =
            new Dictionary<(DataType left, DataType right), DataType>()
            {
                { (DataType.Bool, DataType.Bool), DataType.Long },
                { (DataType.Bool, DataType.Long), DataType.Long },
                { (DataType.Long, DataType.Bool), DataType.Long },
                { (DataType.Bool, DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Bool), DataType.Double },
                { (DataType.Long, DataType.Long), DataType.Long },
                { (DataType.Long, DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Long), DataType.Double },
                { (DataType.Double, DataType.Double), DataType.Double },
                { (DataType.Str, DataType.Str), DataType.Str },
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
            switch (GetConversionTag(l.DataType, r.DataType, op))
            {
                case DataType.Long:
                    return CompareLong(l.ToLong(), r.ToLong(), op);
                case DataType.Double:
                    // Use relational operators instead of CompareTo so NaN comparisons stay false like Python.
                    return CompareDouble(l.ToDouble(), r.ToDouble(), op);
                case DataType.Str:
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
            if (TagConversionMap.TryGetValue((l.DataType, r.DataType), out var convTag))
            {
                // Numeric equality follows Python promotion: True == 1 and 1 == 1.0 are both true.
                switch (convTag)
                {
                    case DataType.Long:
                        return l.ToLong() == r.ToLong();
                    case DataType.Double:
                        return l.ToDouble() == r.ToDouble();
                    case DataType.Str:
                        return l.ToString() == r.ToString();
                }
            }

            if (l.DataType != r.DataType)
            {
                // Python equality across unrelated types returns False instead of raising TypeError.
                return false;
            }

            switch (l.DataType)
            {
                case DataType.None:
                    // None is a singleton in Python semantics, so None == None is always true.
                    return true;
                case DataType.List:
                    // List equality is structural and order-sensitive.
                    return SourceList.ElementsEqual((SourceList)l.ToObject(), (SourceList)r.ToObject());
                case DataType.Dict:
                    // Dict equality is structural; key insertion order does not decide equality.
                    return SourceDictionary.ElementsEqual((SourceDictionary)l.ToObject(), (SourceDictionary)r.ToObject());
                case DataType.Range:
                case DataType.Object:
                    // Chow ranges/objects do not have Python structural equality yet; preserve identity behavior.
                    return ReferenceEquals(l.ToObject(), r.ToObject());
                default:
                    return false;
            }
        }

        static DataType GetConversionTag(DataType leftDataType, DataType rightDataType, ExpressionOperator op)
        {
            var mapKey = (left: leftDataType, right: rightDataType);

            if (TagConversionMap.TryGetValue(mapKey, out var convTag))
            {
                return convTag;
            }

            throw new DataTypeException(
                $"TypeError: unsupported operand type(s) for {OperatorStrings.ToSource(op)}: "
                + $"'{DataTypeNames.GetTypeName(leftDataType)}' and '{DataTypeNames.GetTypeName(rightDataType)}'");
        }

        #endregion
    }
}
