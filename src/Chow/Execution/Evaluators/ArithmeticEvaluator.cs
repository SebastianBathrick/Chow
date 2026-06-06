using System;
using System.Collections.Generic;
using System.Text;
using Chow.DataTypes;
using Chow.Exceptions;
using Chow.Bytecode;

namespace Chow.Expressions
{
    // TODO: Replace with more performant, refactored version
    class ArithmeticEvaluator : IEvaluator
    {
        // TODO: Update project's const naming conventions from SNAKE_CASE to PascalCase 
        const int IsDoubleEqualInteger = 0; 
        
        // Python treats bool as a subtype of int; any float operand promotes the whole op to float.
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
            };
        
        public RuntimeValue EvaluateBinary(RuntimeValue right, RuntimeValue left)
        {
            throw new NotImplementedException();
        }
        
        public static RuntimeValue EvaluateAddition(ref RuntimeValue l, ref RuntimeValue r)
        {
            // No float on either side → int result; otherwise both operands promote to float.
            if (TryGetConversionTag(l.DataType, r.DataType, out var convTag))
            {
                return convTag == DataType.Long 
                    ? new RuntimeValue(l.ToLong() + r.ToLong()) 
                    : new RuntimeValue(l.ToDouble() + r.ToDouble());

            }

            switch (l.DataType)
            {
                case DataType.Str when r.DataType == DataType.Str:
                    // Python overloads `+` for sequence concatenation; strings keep string results.
                    return new RuntimeValue(l.ToString() + r.ToString());
                case DataType.List when r.DataType == DataType.List:
                    // List concatenation creates a new list; neither operand list is mutated.
                    return new RuntimeValue(
                        SourceList.Concat((SourceList)l.ToObject(), (SourceList)r.ToObject()));
                default:
                    throw UnsupportedBinary(l.DataType, r.DataType, ExpressionOperator.Add);
            }

        }

        public static RuntimeValue EvaluateSubtraction(ref RuntimeValue l, ref RuntimeValue r)
        {
            return GetConversionTag(l.DataType, r.DataType, ExpressionOperator.Subtract) == DataType.Long ? new RuntimeValue(l.ToLong() - r.ToLong()) : new RuntimeValue(l.ToDouble() - r.ToDouble());

        }

        public static RuntimeValue EvaluateMultiplication(ref RuntimeValue l, ref RuntimeValue r)
        {
            if (TryGetConversionTag(l.DataType, r.DataType, out var convTag))
            {
                return convTag == DataType.Long 
                    ? new RuntimeValue(l.ToLong() * r.ToLong()) 
                    : new RuntimeValue(l.ToDouble() * r.ToDouble());

            }

            if (l.DataType == DataType.Str && IsIntegerTag(r.DataType))
            {
                // Python repeats sequences with int-like counts; bool counts as 0 or 1.
                return new RuntimeValue(RepeatString(l.ToString(), ToRepeatCount(ref r)));
            }

            if (IsIntegerTag(l.DataType) && r.DataType == DataType.Str)
            {
                // Repetition is commutative for sequence/int operands: 3 * "ab" == "ab" * 3.
                return new RuntimeValue(RepeatString(r.ToString(), ToRepeatCount(ref l)));
            }

            if (l.DataType == DataType.List && IsIntegerTag(r.DataType))
            {
                // SourceList.Repeat mirrors Python's non-positive counts by returning an empty list.
                return new RuntimeValue(SourceList.Repeat((SourceList)l.ToObject(), ToRepeatCount(ref r)));
            }

            if (IsIntegerTag(l.DataType) && r.DataType == DataType.List)
            {
                // Keep list repetition order-independent for int/list operands, as Python does.
                return new RuntimeValue(SourceList.Repeat((SourceList)r.ToObject(), ToRepeatCount(ref l)));
            }

            throw UnsupportedBinary(l.DataType, r.DataType, ExpressionOperator.Multiply);
        }

        public static RuntimeValue EvaluateDivision(ref RuntimeValue l, ref RuntimeValue r)
        {
            // Python: `/` always yields float (e.g. 9 / 3 → 3.0), even for int operands.
            // Lookup validates operand types; result type is always double regardless of map value.
            GetConversionTag(l.DataType, r.DataType, ExpressionOperator.Divide);

            var rightDbl = r.ToDouble();
            var leftDbl = l.ToDouble();

            if (IsDoubleValueZero(rightDbl))
            {
                throw new ZeroDivisionException();
            }

            return new RuntimeValue(leftDbl / rightDbl);
        }
        
        #region Modulus Methods

        public static RuntimeValue EvaluateModulus(ref RuntimeValue l, ref RuntimeValue r)
        {
            var convTag = GetConversionTag(l.DataType, r.DataType, ExpressionOperator.Modulus);

            if (convTag == DataType.Long)
            {
                return new RuntimeValue(ModLong(l.ToLong(), r.ToLong()));
            }

            return new RuntimeValue(ModDouble(l.ToDouble(), r.ToDouble()));
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

        public static RuntimeValue EvaluateFloorDivision(ref RuntimeValue l, ref RuntimeValue r)
        {
            var convTag = GetConversionTag(l.DataType, r.DataType, ExpressionOperator.FloorDivide);
            
            if (convTag == DataType.Long)
            {
                // Integer // stays in longs so large quotients are not rounded via double.
                return new RuntimeValue(FloorDivideLong(l.ToLong(), r.ToLong()));
            }
            
            return new RuntimeValue(FloorDivideDouble(l.ToDouble(), r.ToDouble()));
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
        public static RuntimeValue EvaluateExponent(ref RuntimeValue baseValue, ref RuntimeValue exponentValue)
        {
            var convTag = GetConversionTag(baseValue.DataType, exponentValue.DataType, ExpressionOperator.Exponentiate);

            if (convTag == DataType.Long)
            {
                var exponentLong = exponentValue.ToLong();

                if (exponentLong >= 0L)
                {
                    // Exact integer path; avoids double precision loss (e.g. 10 ** 16).
                    var result = ExponentiateLong(baseValue.ToLong(), exponentLong);
                    return new RuntimeValue(result);
                }
                // Negative int exponent (e.g. 2 ** -3) falls through to float Math.Pow below.
            }

            var baseDbl = baseValue.ToDouble();
            var exponentDbl = exponentValue.ToDouble();

            // Python: 0 ** -n raises ZeroDivisionError; Math.Pow would return Infinity.
            if (IsDoubleValueZero(baseDbl) && exponentDbl < 0.0)
            {
                throw new ZeroDivisionException();
            }

            return new RuntimeValue(Math.Pow(baseDbl, exponentDbl));
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

        public static RuntimeValue EvaluateNegation(ref RuntimeValue operand)
        {
            switch (operand.DataType)
            {
                case DataType.Bool:
                case DataType.Long:
                    // Python treats bool as int here: -True -> -1, -False -> 0.
                    return new RuntimeValue(-operand.ToLong());
                case DataType.Double:
                    return new RuntimeValue(-operand.ToDouble());
                default:
                    throw UnsupportedUnary(operand.DataType, ExpressionOperator.Negate);
            }

        }

        #endregion

        #region Helper Methods

        static bool TryGetConversionTag(DataType leftDataType, DataType rightDataType, out DataType convDataType)
        {
            var mapKey = (left: leftDataType, right: rightDataType);
            return TagConversionMap.TryGetValue(mapKey, out convDataType);
        }

        static DataType GetConversionTag(DataType leftDataType, DataType rightDataType, ExpressionOperator op)
        {
            if (TryGetConversionTag(leftDataType, rightDataType, out var convTag))
            {
                return convTag;
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

        static bool IsIntegerTag(DataType dataType)
        {
            return dataType == DataType.Long || dataType == DataType.Bool;
        }

        static int ToRepeatCount(ref RuntimeValue value)
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
