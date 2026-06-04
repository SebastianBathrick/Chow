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

        /// <summary>
        /// Converts an arithmetic operation's operands to their appropriate data types, evaluates
        /// the expression, and returns its result.
        /// </summary>
        /// <param name="l">The left operand.</param>
        /// <param name="r">The right operand.</param>
        /// <param name="op">The arithmetic operation to apply.</param>
        /// <returns>A <see cref="TaggedUnion"/> containing the result of the operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="op"/> is not a
        /// supported arithmetic operation.</exception>
        public static TaggedUnion Evaluate(
            ref TaggedUnion l,
            ref TaggedUnion r,
            OperationCode op)
        {
            switch (op)
            {
                case OperationCode.Add:
                    return EvaluateAddition(ref l, ref r, op);
                case OperationCode.Subtract:
                    return EvaluateSubtraction(ref l, ref r, op);
                case OperationCode.Multiply:
                    return EvaluateMultiplication(ref l, ref r, op);
                case OperationCode.Divide:
                    return EvaluateDivision(ref l, ref r, op);
                case OperationCode.Modulus:
                    return EvaluateModulus(ref l, ref r, op);
                case OperationCode.Exponentiate:
                    return EvaluateExponent(ref l, ref r, op);
                case OperationCode.FloorDivide:
                    return TypeSpecificFloorDivide(ref l, ref r, op);
                default:
                    throw new InvalidOperationException(nameof(Evaluate));
            }
        }

        #region Operation Methods
        
        static TaggedUnion EvaluateAddition(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            switch (GetConversionTag(l.Tag, r.Tag))
            {
                case Tag.Long:
                    return new TaggedUnion(l.ToLong() + r.ToLong());
                case Tag.Double:
                    return new TaggedUnion(l.ToDouble() + r.ToDouble());
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }

        static TaggedUnion EvaluateSubtraction(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            switch (GetConversionTag(l.Tag, r.Tag))
            {
                case Tag.Long:
                    return new TaggedUnion(l.ToLong() - r.ToLong());
                case Tag.Double:
                    return new TaggedUnion(l.ToDouble() - r.ToDouble());
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }

        static TaggedUnion EvaluateMultiplication(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            switch (GetConversionTag(l.Tag, r.Tag))
            {
                case Tag.Long:
                    return new TaggedUnion(l.ToLong() * r.ToLong());
                case Tag.Double:
                    return new TaggedUnion(l.ToDouble() * r.ToDouble());
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }

        // Python: `/` always produces float even for int / int.
        static TaggedUnion EvaluateDivision(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            switch (GetConversionTag(l.Tag, r.Tag))
            {
                case Tag.Long:
                case Tag.Double:
                    return EvaluateDivision(ref l, ref r);
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }
        
        // Python: result sign follows the divisor.
        static TaggedUnion EvaluateModulus(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            switch (GetConversionTag(l.Tag, r.Tag))
            {
                case Tag.Long:
                case Tag.Double:
                    return TypeSpecificMod(ref l, ref r);
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }
        
        // Python: negative integer exponent forces float result.
        static TaggedUnion EvaluateExponent(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            var convTag = GetConversionTag(l.Tag, r.Tag);
            
            if (convTag == Tag.Long && r.ToLong() < 0)
                convTag = Tag.Double;

            switch (convTag)
            {
                case Tag.Long:
                    return new TaggedUnion(IntPow(l.ToLong(), r.ToLong()));
                case Tag.Double:
                    return new TaggedUnion(Math.Pow(l.ToDouble(), r.ToDouble()));
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }
        
        static TaggedUnion EvaluateDivision(ref TaggedUnion l, ref TaggedUnion r)
        {
            switch (r.Tag)
            {
                case Tag.Long:
                case Tag.Bool:
                case Tag.Double:
                    return r.ToDouble() == 0.0 
                        ? throw new ZeroDivisionException() 
                        : new TaggedUnion(l.ToDouble() / r.ToDouble());

                default:
                    throw new ZeroDivisionException();
            }
        }
        
        #endregion

        #region Type Helper Methods
        
        static TaggedUnion TypeSpecificMod(ref TaggedUnion l, ref TaggedUnion r)
        {
            switch (r.Tag)
            {
                case Tag.Bool:
                case Tag.Long:
                    var rightLong = r.ToLong();

                    if (rightLong == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    var leftLong = l.ToLong();
                    return new TaggedUnion((leftLong % rightLong + rightLong) % rightLong);
                
                case Tag.Double:
                    var rightDbl = r.ToDouble();
                    
                    if (rightDbl == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }
                    
                    var leftDbl = l.ToDouble();
                    return new TaggedUnion((leftDbl % rightDbl + rightDbl) % rightDbl);
                
                default:
                    throw new ZeroDivisionException();
            }
        }

        // Python: floors toward negative infinity.
        static TaggedUnion TypeSpecificFloorDivide(ref TaggedUnion l, ref TaggedUnion r, OperationCode op)
        {
            switch (GetConversionTag(l.Tag, r.Tag))
            {
                case Tag.Long:
                    var leftLong = l.ToLong();
                    var rightLong = r.ToLong();

                    if (rightLong == 0L)
                    {
                        throw new ZeroDivisionException();
                    }
                    
                    var q = leftLong / rightLong;

                    if (leftLong % rightLong != 0L && leftLong < 0L != rightLong < 0L)
                    {
                        q--;
                    }
                    
                    return new TaggedUnion(q);
                
                case Tag.Double:
                    var divisor = r.ToDouble();
                    
                    if (divisor == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }
                    
                    return new TaggedUnion(Math.Floor(l.ToDouble() / divisor));
                
                default:
                    throw new TypeException(l.Tag, r.Tag, op);
            }
        }

        static Tag GetConversionTag(Tag l, Tag r)
        {
            var mapKey = (left: l, right: r);
            var convertToTag = TagConversionMap.TryGetValue(mapKey, out var conversion)
                ? conversion
                : throw new TypeException(l, r, OperationCode.Add);

            return convertToTag;
        }

        // Exponent-by-squaring. Caller guarantees exponent >= 0.
        static long IntPow(long l, long r)
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
        
    }
}
