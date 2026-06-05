using System;
using System.Collections.Generic;
using Chow.DataTypes;
using Chow.Exceptions;
using Chow.Bytecode;

namespace Chow.Expressions
{
    // WARNING: This class is still in the development phase. It should not be implemented.
    /// <summary>
    /// Static service class for <see cref="Chow.Core.VirtualMachine"/> that performs arithmetic
    /// operations that behave the same as Python's arithmetic.
    /// </summary>
    static class ArithmeticEvaluator
    {
        // TODO: Update project's const naming conventions from SNAKE_CASE to PascalCase 
        const int IsDoubleEqualInteger = 0; 
        
        // Python treats bool as a subtype of int; any float operand promotes the whole op to float.
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
            };

        #region Non-Type Specific Operations

        public static TaggedUnion EvaluateAddition(ref TaggedUnion l, ref TaggedUnion r)
        {
            // No float on either side → int result; otherwise both operands promote to float.
            if (GetConversionTag(l.Tag, r.Tag, ExpressionOperator.Add) == Tag.Long)
            {
                return new TaggedUnion(l.ToLong() + r.ToLong());
            }
            
            return new TaggedUnion(l.ToDouble() + r.ToDouble());
        }

        public static TaggedUnion EvaluateSubtraction(ref TaggedUnion l, ref TaggedUnion r)
        {
            if (GetConversionTag(l.Tag, r.Tag, ExpressionOperator.Subtract) == Tag.Long)
            {
                return new TaggedUnion(l.ToLong() - r.ToLong());
            }
            
            return new TaggedUnion(l.ToDouble() - r.ToDouble());
        }

        public static TaggedUnion EvaluateMultiplication(ref TaggedUnion l, ref TaggedUnion r)
        {
            if (GetConversionTag(l.Tag, r.Tag, ExpressionOperator.Multiply) == Tag.Long)
            {
                return new TaggedUnion(l.ToLong() * r.ToLong());
            }
            
            return new TaggedUnion(l.ToDouble() * r.ToDouble());
        }

        public static TaggedUnion EvaluateDivision(ref TaggedUnion l, ref TaggedUnion r)
        {
            // Python: `/` always yields float (e.g. 9 / 3 → 3.0), even for int operands.
            // Lookup validates operand types; result type is always double regardless of map value.
            GetConversionTag(l.Tag, r.Tag, ExpressionOperator.Divide);

            var rightDbl = r.ToDouble();
            var leftDbl = l.ToDouble();

            if (IsDoubleValueZero(rightDbl))
            {
                throw new ZeroDivisionException();
            }

            return new TaggedUnion(leftDbl / rightDbl);
        }

        #endregion

        #region Modulus Methods

        public static TaggedUnion EvaluateModulus(ref TaggedUnion l, ref TaggedUnion r)
        {
            var convTag = GetConversionTag(l.Tag, r.Tag, ExpressionOperator.Modulus);

            if (convTag == Tag.Long)
            {
                return new TaggedUnion(ModLong(l.ToLong(), r.ToLong()));
            }

            return new TaggedUnion(ModDouble(l.ToDouble(), r.ToDouble()));
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

        public static TaggedUnion EvaluateFloorDivision(ref TaggedUnion l, ref TaggedUnion r)
        {
            var convTag = GetConversionTag(l.Tag, r.Tag, ExpressionOperator.FloorDivide);
            
            if (convTag == Tag.Long)
            {
                // Integer // stays in longs so large quotients are not rounded via double.
                return new TaggedUnion(FloorDivideLong(l.ToLong(), r.ToLong()));
            }
            
            return new TaggedUnion(FloorDivideDouble(l.ToDouble(), r.ToDouble()));
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
        public static TaggedUnion EvaluateExponent(ref TaggedUnion baseValue, ref TaggedUnion exponentValue)
        {
            var convTag = GetConversionTag(baseValue.Tag, exponentValue.Tag, ExpressionOperator.Exponentiate);

            if (convTag == Tag.Long)
            {
                var exponentLong = exponentValue.ToLong();

                if (exponentLong >= 0L)
                {
                    // Exact integer path; avoids double precision loss (e.g. 10 ** 16).
                    var result = ExponentiateLong(baseValue.ToLong(), exponentLong);
                    return new TaggedUnion(result);
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

            return new TaggedUnion(Math.Pow(baseDbl, exponentDbl));
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

        #region Helper Methods

        static Tag GetConversionTag(Tag leftTag, Tag rightTag, ExpressionOperator op)
        {
            var mapKey = (left: leftTag, right: rightTag);

            if (TagConversionMap.TryGetValue(mapKey, out var convTag))
            {
                return convTag;
            }

            throw new TypeException(
                // Message shape mirrors Python's TypeError wording and type names.
                $"TypeError: unsupported operand type(s) for {OperatorStrings.EnumToString(op)}: "
                + $"'{DataTypeNames.GetTypeName(leftTag)}' and '{DataTypeNames.GetTypeName(rightTag)}'");
        }

        static bool IsDoubleValueZero(double divisor)
        {
            // CompareTo treats -0.0 and +0.0 as equal; `divisor == 0.0` can miss -0.0 edge cases.
            return divisor.CompareTo(0.0) == IsDoubleEqualInteger;
        }

        #endregion

    }
}
