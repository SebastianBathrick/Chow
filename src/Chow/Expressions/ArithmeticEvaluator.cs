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
            // Note: Division always produces a double (referred to as a 'float' in source code).
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

            return (l % r + r) % r;
        }

        static long ModLong(long l, long r)
        {
            if (r == 0L)
            {
                throw new ZeroDivisionException();
            }

            return (l % r + r) % r;
        }

        #endregion

        #region Floor Division Methods

        public static TaggedUnion EvaluateFloorDivision(ref TaggedUnion l, ref TaggedUnion r)
        {
            var convTag = GetConversionTag(l.Tag, r.Tag, ExpressionOperator.FloorDivide);
            
            if (convTag == Tag.Long)
            {
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

            return Math.Floor(l / r);
        }

        static long FloorDivideLong(long l, long r)
        {
            if (r == 0L)
            {
                throw new ZeroDivisionException();
            }

            var q = l / r;

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
                    var expandedForm = ExponentiateLong(baseValue.ToLong(), exponentLong);
                    return new TaggedUnion(expandedForm);
                }
            }

            var baseDbl = baseValue.ToDouble();
            var exponentDbl = exponentValue.ToDouble();

            if (IsDoubleValueZero(baseDbl) && exponentDbl < 0.0)
            {
                throw new ZeroDivisionException();
            }

            return new TaggedUnion(Math.Pow(baseDbl, exponentDbl));
        }

        // Exponent-by-squaring. Caller guarantees exponent >= 0.
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
                $"TypeError: unsupported operand type(s) for {OperatorStrings.EnumToString(op)}: "
                + $"'{DataTypeNames.GetTypeName(leftTag)}' and '{DataTypeNames.GetTypeName(rightTag)}'");
        }

        static bool IsDoubleValueZero(double divisor)
        {
            return divisor.CompareTo(0.0) == IsDoubleEqualInteger;
        }

        #endregion

    }
}
