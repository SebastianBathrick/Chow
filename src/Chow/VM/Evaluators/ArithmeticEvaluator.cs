using System;
using System.Collections.Generic;
using System.Text;
using Chow.Exceptions;
using Chow.Objects;
using Chow.Utility;
namespace Chow.VM.Utilities
{
    // TODO: Replace with more performant, refactored version
    class ArithmeticEvaluator
    {
        // TODO: Update project's const naming conventions from SNAKE_CASE to PascalCase
        const int IsDoubleEqualInteger = 0;

        // Python treats bool as a subtype of int; any float operand promotes the whole op to float.
        static readonly IReadOnlyDictionary<(DataType, DataType), DataType> NumericConversionMap =
            new Dictionary<(DataType left, DataType right), DataType>()
            {
                { (DataType.Bool,   DataType.Bool),   DataType.Long   },
                { (DataType.Bool,   DataType.Long),   DataType.Long   },
                { (DataType.Long,   DataType.Bool),   DataType.Long   },
                { (DataType.Bool,   DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Bool),   DataType.Double },
                { (DataType.Long,   DataType.Long),   DataType.Long   },
                { (DataType.Long,   DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Long),   DataType.Double },
                { (DataType.Double, DataType.Double), DataType.Double },
            };

        // Extends numeric pairs with sequence-concatenation pairs valid for `+`.
        static readonly IReadOnlyDictionary<(DataType, DataType), DataType> AdditionConversionMap =
            new Dictionary<(DataType left, DataType right), DataType>()
            {
                { (DataType.Bool,   DataType.Bool),   DataType.Long   },
                { (DataType.Bool,   DataType.Long),   DataType.Long   },
                { (DataType.Long,   DataType.Bool),   DataType.Long   },
                { (DataType.Bool,   DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Bool),   DataType.Double },
                { (DataType.Long,   DataType.Long),   DataType.Long   },
                { (DataType.Long,   DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Long),   DataType.Double },
                { (DataType.Double, DataType.Double), DataType.Double },
                { (DataType.Str,    DataType.Str),    DataType.Str    },
                { (DataType.List,   DataType.List),   DataType.List   },
            };

        // Extends numeric pairs with sequence-repetition pairs valid for `*`.
        static readonly IReadOnlyDictionary<(DataType, DataType), DataType> MultiplicationConversionMap =
            new Dictionary<(DataType left, DataType right), DataType>()
            {
                { (DataType.Bool,   DataType.Bool),   DataType.Long   },
                { (DataType.Bool,   DataType.Long),   DataType.Long   },
                { (DataType.Long,   DataType.Bool),   DataType.Long   },
                { (DataType.Bool,   DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Bool),   DataType.Double },
                { (DataType.Long,   DataType.Long),   DataType.Long   },
                { (DataType.Long,   DataType.Double), DataType.Double },
                { (DataType.Double, DataType.Long),   DataType.Double },
                { (DataType.Double, DataType.Double), DataType.Double },
                { (DataType.Str,    DataType.Long),   DataType.Str    },
                { (DataType.Str,    DataType.Bool),   DataType.Str    },
                { (DataType.Long,   DataType.Str),    DataType.Str    },
                { (DataType.Bool,   DataType.Str),    DataType.Str    },
                { (DataType.List,   DataType.Long),   DataType.List   },
                { (DataType.List,   DataType.Bool),   DataType.List   },
                { (DataType.Long,   DataType.List),   DataType.List   },
                { (DataType.Bool,   DataType.List),   DataType.List   },
            };

        public static SourceValue Add(SourceValue r, SourceValue l)
        {
            switch (GetConversionDataType(AdditionConversionMap, l.DataType, r.DataType, ExpressionOperator.Add))
            {
                case DataType.Long:   return new SourceValue(l.ToLong()   + r.ToLong());
                case DataType.Double: return new SourceValue(l.ToDouble() + r.ToDouble());
                
                // Python overloads `+` for sequence concatenation; strings keep string results.
                case DataType.Str:    return new SourceValue(l.ToString() + r.ToString());
                
                // List concatenation creates a new list; neither operand list is mutated.
                case DataType.List:   return new SourceValue(SourceList.Concat((SourceList)l.ToObject(), (SourceList)r.ToObject()));
                default:              throw new UnreachableException(nameof(Add));
            }
        }

        public static SourceValue Subtract(SourceValue r, SourceValue l)
        {
            var convDataType = GetConversionDataType(NumericConversionMap, l.DataType, r.DataType, ExpressionOperator.Subtract);
            return convDataType == DataType.Long
                ? new SourceValue(l.ToLong()   - r.ToLong())
                : new SourceValue(l.ToDouble() - r.ToDouble());
        }

        public static SourceValue Multiply(SourceValue r, SourceValue l)
        {
            switch (GetConversionDataType(MultiplicationConversionMap, l.DataType, r.DataType, ExpressionOperator.Multiply))
            {
                case DataType.Long:   return new SourceValue(l.ToLong()   * r.ToLong());
                case DataType.Double: return new SourceValue(l.ToDouble() * r.ToDouble());
                case DataType.Str:
                {
                    // Python repeats sequences with int-like counts; bool counts as 0 or 1.
                    var str   = l.DataType == DataType.Str 
                        ? l.ToString() 
                        : r.ToString();
                    
                    var count = l.DataType == DataType.Str 
                        ? ToRepeatCount(ref r) 
                        : ToRepeatCount(ref l);
                    
                    return new SourceValue(RepeatString(str, count));
                }
                case DataType.List:
                {
                    // SourceList.Repeat mirrors Python's non-positive counts by returning an empty list.
                    var list  = l.DataType == DataType.List 
                        ? (SourceList)l.ToObject() 
                        : (SourceList)r.ToObject();
                    var count = l.DataType == DataType.List 
                        ? ToRepeatCount(ref r) 
                        : ToRepeatCount(ref l);
                    return new SourceValue(SourceList.Repeat(list, count));
                }
                default: throw new UnreachableException(nameof(Multiply));
            }
        }

        public static SourceValue Divide(SourceValue r, SourceValue l)
        {
            // Python: `/` always yields float (e.g. 9 / 3 → 3.0), even for int operands.
            // Lookup validates operand types; result type is always double regardless of map value.
            GetConversionDataType(
                NumericConversionMap, l.DataType, r.DataType, ExpressionOperator.Divide);

            var rightDbl = r.ToDouble();
            var leftDbl = l.ToDouble();

            if (IsDoubleValueZero(rightDbl))
            {
                throw new ZeroDivisionException();
            }

            return new SourceValue(leftDbl / rightDbl);
        }

        #region Modulus Methods

        public static SourceValue Pow(SourceValue r, SourceValue l)
        {
            var convDataType = GetConversionDataType(
                NumericConversionMap, l.DataType, r.DataType, ExpressionOperator.Modulus);

            if (convDataType == DataType.Long)
            {
                return new SourceValue(ModLong(l.ToLong(), r.ToLong()));
            }

            return new SourceValue(ModDouble(l.ToDouble(), r.ToDouble()));
        }

        static double ModDouble(double l, double r)
        {
            if (IsDoubleValueZero(r))
            {
                throw new ZeroDivisionException();
            }

            // Same divisor-sign fix as ModLong; needed because float `%` also follows C# rules.
            return (l % r + r) % r;
        }

        static long ModLong(long l, long r)
        {
            if (r == 0L)
            {
                throw new ZeroDivisionException();
            }

            // C# `%` keeps the dividend's sign; Python keeps the divisor's sign.
            // Adding r before the second `%` shifts the result into [0, |r|) or (-|r|, 0].
            return (l % r + r) % r;
        }

        #endregion

        #region Floor Division Methods

        public static SourceValue EvaluateFloorDivision(SourceValue r, SourceValue l)
        {
            var convDataType = GetConversionDataType(
                NumericConversionMap, l.DataType, r.DataType, ExpressionOperator.FloorDivide);

            if (convDataType == DataType.Long)
            {
                // Integer // stays in longs so large quotients are not rounded via double.
                return new SourceValue(FloorDivideLong(l.ToLong(), r.ToLong()));
            }

            return new SourceValue(FloorDivideDouble(l.ToDouble(), r.ToDouble()));
        }


        static double FloorDivideDouble(double l, double r)
        {
            if (IsDoubleValueZero(r))
            {
                throw new ZeroDivisionException();
            }

            // Python `//` on floats floors toward negative infinity, not toward zero.
            return Math.Floor(l / r);
        }

        static long FloorDivideLong(long l, long r)
        {
            if (r == 0L)
            {
                throw new ZeroDivisionException();
            }

            var q = l / r;

            // C# truncates toward zero; Python floors toward -∞ when signs differ and there is a remainder.
            // `l < 0L != r < 0L` is `(l < 0) != (r < 0)` — true when exactly one operand is negative.
            if (l % r != 0L && l < 0L != r < 0L)
            {
                q--;
            }

            return q;
        }

        #endregion

        #region Exponentiation Methods

        // Python: negative integer exponent forces float result.
        public static SourceValue EvaluateExponent(SourceValue r, SourceValue l)
        {
            var convDataType = GetConversionDataType(
                NumericConversionMap, l.DataType, r.DataType, ExpressionOperator.Exponentiate);

            if (convDataType == DataType.Long)
            {
                var exponentLong = r.ToLong();

                if (exponentLong >= 0L)
                {
                    // Exact integer path; avoids double precision loss (e.g. 10 ** 16).
                    var result = ExponentiateLong(l.ToLong(), exponentLong);
                    return new SourceValue(result);
                }
                // Negative int exponent (e.g. 2 ** -3) falls through to float Math.Pow below.
            }

            var baseDbl = l.ToDouble();
            var exponentDbl = r.ToDouble();

            // Python: 0 ** -n raises ZeroDivisionError; Math.Pow would return Infinity.
            if (IsDoubleValueZero(baseDbl) && exponentDbl < 0.0)
            {
                throw new ZeroDivisionException();
            }

            return new SourceValue(Math.Pow(baseDbl, exponentDbl));
        }

        // Exponent-by-squaring. Caller guarantees exponent >= 0.
        // Overflow wraps silently (Python uses arbitrary-precision int; not modeled here).
        static long ExponentiateLong(long l, long r)
        {
            var result = 1L;

            while (r > 0L)
            {
                if ((r & 1L) == 1L)
                {
                    result *= l;
                }

                l *= l;
                r >>= 1;
            }

            return result;
        }

        #endregion

        #region Unary Operations

        public static SourceValue EvaluateNegation(SourceValue operand)
        {
            switch (operand.DataType)
            {
                case DataType.Bool:
                case DataType.Long:
                    // Python treats bool as int here: -True -> -1, -False -> 0.
                    return new SourceValue(-operand.ToLong());
                case DataType.Double:
                    return new SourceValue(-operand.ToDouble());
                default:
                    throw UnsupportedUnary(operand.DataType, ExpressionOperator.Negate);
            }

        }

        #endregion

        #region Helper Methods

        static bool TryGetConversionDataType(
            IReadOnlyDictionary<(DataType, DataType), DataType> map,
            DataType leftDataType, DataType rightDataType, out DataType convDataType)
        {
            return map.TryGetValue((left: leftDataType, right: rightDataType), out convDataType);
        }

        static DataType GetConversionDataType(
            IReadOnlyDictionary<(DataType, DataType), DataType> map,
            DataType leftDataType, DataType rightDataType, ExpressionOperator op)
        {
            if (TryGetConversionDataType(map, leftDataType, rightDataType, out var convDataType))
            {
                return convDataType;
            }

            throw UnsupportedBinary(leftDataType, rightDataType, op);
        }

        static DataTypeException UnsupportedBinary(DataType leftDataType, DataType rightDataType, ExpressionOperator op)
        {
            // Message shape mirrors Python's TypeError wording and type names.
            return new DataTypeException(
                $"TypeError: unsupported operand type(s) for {OperatorStrings.ToSource(op)}: "
                + $"'{DataTypeNames.GetTypeName(leftDataType)}' and '{DataTypeNames.GetTypeName(rightDataType)}'");
        }

        static DataTypeException UnsupportedUnary(DataType operandDataType, ExpressionOperator op)
        {
            return new DataTypeException(
                $"TypeError: bad operand type for unary {OperatorStrings.ToSource(op)}: "
                + $"'{DataTypeNames.GetTypeName(operandDataType)}'");
        }

        static bool IsDoubleValueZero(double divisor)
        {
            // CompareTo treats -0.0 and +0.0 as equal; `divisor == 0.0` can miss -0.0 edge cases.
            return divisor.CompareTo(0.0) == IsDoubleEqualInteger;
        }

        static int ToRepeatCount(ref SourceValue value)
        {
            // Repeat helpers currently take int counts; keep overflow explicit instead of silently truncating.
            return checked((int)value.ToLong());
        }

        static string RepeatString(string source, int count)
        {
            if (count <= 0 || source.Length == 0)
            {
                // Python returns the empty sequence for zero or negative repetition counts.
                return string.Empty;
            }

            var builder = new StringBuilder(source.Length * count);

            for (var index = 0; index < count; index++)
            {
                builder.Append(source);
            }

            return builder.ToString();
        }

        #endregion


    }
}
