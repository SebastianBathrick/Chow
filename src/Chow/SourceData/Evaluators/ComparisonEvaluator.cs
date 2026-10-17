using System;
using System.Collections.Generic;
using Chow.Code;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.VM;

namespace Chow.SourceData
{
    /// <summary>
    /// Static service class for <see cref="Processor"/> that performs comparison
    /// operations that behave the same as Python's comparisons for Chow's supported types.
    /// </summary>
    static class ComparisonEvaluator
    {
        // Python treats bool as a subtype of int; any float operand promotes the whole comparison to float.
        static readonly IReadOnlyDictionary<(DataType, DataType), DataType> DataTypeConversionMap =
            new Dictionary<(DataType left, DataType right), DataType>
            {
                {
                    (DataType.Bool, DataType.Bool), DataType.Long
                },
                {
                    (DataType.Bool, DataType.Long), DataType.Long
                },
                {
                    (DataType.Long, DataType.Bool), DataType.Long
                },
                {
                    (DataType.Bool, DataType.Double), DataType.Double
                },
                {
                    (DataType.Double, DataType.Bool), DataType.Double
                },
                {
                    (DataType.Long, DataType.Long), DataType.Long
                },
                {
                    (DataType.Long, DataType.Double), DataType.Double
                },
                {
                    (DataType.Double, DataType.Long), DataType.Double
                },
                {
                    (DataType.Double, DataType.Double), DataType.Double
                },
                {
                    (DataType.Str, DataType.Str), DataType.Str
                }
            };

        #region Equality Operations

        public static SourceValue EvaluateEqual(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(IsEqual(ref l, ref r));
        }

        public static SourceValue EvaluateNotEqual(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(!IsEqual(ref l, ref r));
        }

        #endregion

        #region Identity Operations

        public static SourceValue EvaluateIs(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(IsIdentical(ref l, ref r));
        }

        public static SourceValue EvaluateIsNot(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(!IsIdentical(ref l, ref r));
        }

        #endregion

        #region Ordering Operations

        public static SourceValue EvaluateLess(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(EvaluateOrdering(ref l, ref r, Operator.Less));
        }

        public static SourceValue EvaluateGreater(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(EvaluateOrdering(ref l, ref r, Operator.Greater));
        }

        public static SourceValue EvaluateLessEqual(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(EvaluateOrdering(ref l, ref r, Operator.LessEqual));
        }

        public static SourceValue EvaluateGreaterEqual(ref SourceValue r, ref SourceValue l)
        {
            return new SourceValue(EvaluateOrdering(ref l, ref r, Operator.GreaterEqual));
        }

        #endregion

        #region Helper Methods

        static bool EvaluateOrdering(ref SourceValue l, ref SourceValue r, Operator op)
        {
            switch (GetConversionDataType(ref l, ref r, op))
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

        static bool CompareLong(long left, long right, Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                    return left < right;
                case Operator.Greater:
                    return left > right;
                case Operator.LessEqual:
                    return left <= right;
                case Operator.GreaterEqual:
                    return left >= right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool CompareDouble(double left, double right, Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                    return left < right;
                case Operator.Greater:
                    return left > right;
                case Operator.LessEqual:
                    return left <= right;
                case Operator.GreaterEqual:
                    return left >= right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool CompareResult(int comparison, Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                    return comparison < 0;
                case Operator.Greater:
                    return comparison > 0;
                case Operator.LessEqual:
                    return comparison <= 0;
                case Operator.GreaterEqual:
                    return comparison >= 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        static bool IsEqual(ref SourceValue l, ref SourceValue r)
        {
            if (DataTypeConversionMap.TryGetValue((l.DataType, r.DataType), out var convDataType))
            {
                // Numeric equality follows Python promotion: True == 1 and 1 == 1.0 are both true.
                switch (convDataType)
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
                    return SourceDict.ElementsEqual(
                        (SourceDict)l.ToObject(),
                        (SourceDict)r.ToObject());
                case DataType.DictKeys:
                    // Keys views compare like sets: same size and same keys, order ignored.
                    return ((SourceObject)l.ToObject()).EqualsTo((SourceObject)r.ToObject());
                case DataType.Range:
                case DataType.Object:
                case DataType.DictValues:
                case DataType.Iterator:
                    // Chow ranges/objects do not have Python structural equality yet; preserve identity behavior.
                    // Values views and iterators have no structural equality in Python either.
                    return ReferenceEquals(l.ToObject(), r.ToObject());
                default:
                    return false;
            }
        }

        static bool IsIdentical(ref SourceValue l, ref SourceValue r)
        {
            if (l.DataType != r.DataType)
            {
                // Unlike ==, no numeric promotion: True is 1 and 1 is 1.0 are both false in Python.
                return false;
            }

            switch (l.DataType)
            {
                case DataType.None:
                    return true;
                case DataType.Bool:
                case DataType.Long:
                    // Primitives have no object identity in a SourceValue, so identity falls back to value.
                    return l.ToLong() == r.ToLong();
                case DataType.Double:
                    // Bitwise so a NaN is itself and 0.0 is not -0.0.
                    return BitConverter.DoubleToInt64Bits(l.ToDouble())
                        == BitConverter.DoubleToInt64Bits(r.ToDouble());
                default:
                    return ReferenceEquals(l.ToObject(), r.ToObject());
            }
        }

        static DataType GetConversionDataType(ref SourceValue l, ref SourceValue r, Operator op)
        {
            var mapKey = (left: l.DataType, right: r.DataType);

            if (DataTypeConversionMap.TryGetValue(mapKey, out var convDataType))
            {
                return convDataType;
            }

            throw new DataTypeException(
                $"unsupported operand type(s) for {OperatorStrings.ToSource(op)}: "
                + $"'{DataTypeNames.GetTypeName(l)}' and '{DataTypeNames.GetTypeName(r)}'");
        }

        #endregion
    }
}
