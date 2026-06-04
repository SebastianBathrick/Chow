using System;
using System.Collections.Generic;
using Chow.Exceptions;
namespace Chow.DataTypes
{
    static class DataTypeConversionMap
    {
        // TODO: Add left/right specific conversion cases to avoid trying to convert both operands
        // Note: Check compiled code to see if this would actually make a difference
        
        #region Fields

        static readonly Dictionary<(ExpressionOperator op, Tag left, Tag right), ConversionCase> BinaryTypeMap =
            new Dictionary<(ExpressionOperator, Tag, Tag), ConversionCase>();

        static readonly Dictionary<(ExpressionOperator op, Tag operand), ConversionCase> UnaryTypeMap =
            new Dictionary<(ExpressionOperator, Tag), ConversionCase>();

        static readonly Tag[] NumericTags = { Tag.Bool, Tag.Long, Tag.Double };

        static readonly Tag[] AllTags = (Tag[])Enum.GetValues(typeof(Tag));

        #endregion

        #region Public API

        public static ConversionCase GetLeftRightConversionCase(ExpressionOperator @operator, Tag left, Tag right)
        {
            // Note: ExpressionOperator.And/Or are not registered here. The Compiler short-circuits them
            // into jump opcodes (see CompileShortCircuit), so this lookup is never invoked for those ops.
            // If a defensive caller ever queries them, the TypeException below is the correct response.
            return BinaryTypeMap.TryGetValue((@operator, left, right), out var conversion) 
                ? conversion 
                : throw new TypeException(
                    $"unsupported operand type(s) for {@operator}: '{left}' and '{right}'");

        }

        public static ConversionCase GetOperandConversionCase(ExpressionOperator @operator, Tag operand)
        {
            return UnaryTypeMap.TryGetValue((@operator, operand), out var conversion) 
                ? conversion 
                : throw new TypeException($"bad operand type for unary {@operator}: '{operand}'");

        }

        #endregion

        #region Initialization

        static DataTypeConversionMap()
        {
            AddArithmeticPromotionRules(ExpressionOperator.Add);
            AddArithmeticPromotionRules(ExpressionOperator.Subtract);
            AddArithmeticPromotionRules(ExpressionOperator.Multiply);
            AddArithmeticPromotionRules(ExpressionOperator.Modulus);
            AddArithmeticPromotionRules(ExpressionOperator.FloorDivide);
            AddArithmeticPromotionRules(ExpressionOperator.Exponentiate);

            AddAlwaysFloatPromotionRules(ExpressionOperator.Divide);

            AddComparisonPromotionRules(ExpressionOperator.Less);
            AddComparisonPromotionRules(ExpressionOperator.Greater);
            AddComparisonPromotionRules(ExpressionOperator.LessEqual);
            AddComparisonPromotionRules(ExpressionOperator.GreaterEqual);

            AddEqualityRules(ExpressionOperator.Equal);
            AddEqualityRules(ExpressionOperator.NotEqual);

            AddContainerCarveOuts();

            AddMembershipRules(ExpressionOperator.In);
            AddMembershipRules(ExpressionOperator.NotIn);

            AddUnaryRules();
        }

        static void AddArithmeticPromotionRules(ExpressionOperator @operator)
        {
            foreach (var left in NumericTags)
            {
                foreach (var right in NumericTags)
                {
                    var conversionCase = ConversionCase.ToInt;

                    if (EitherIsFloat(left, right))
                    {
                        conversionCase = ConversionCase.ToFloat;
                    }

                    BinaryTypeMap[(@operator, left, right)] = conversionCase;
                }
            }
        }

        static void AddAlwaysFloatPromotionRules(ExpressionOperator @operator)
        {
            foreach (var left in NumericTags)
            {
                foreach (var right in NumericTags)
                {
                    BinaryTypeMap[(@operator, left, right)] = ConversionCase.ToFloat;
                }
            }
        }

        static void AddComparisonPromotionRules(ExpressionOperator @operator)
        {
            AddArithmeticPromotionRules(@operator);
            BinaryTypeMap[(@operator, Tag.Str, Tag.Str)] = ConversionCase.Nothing;
        }

        static void AddEqualityRules(ExpressionOperator @operator)
        {
            AddArithmeticPromotionRules(@operator);

            foreach (var left in AllTags)
            {
                foreach (var right in AllTags)
                {
                    if (!BinaryTypeMap.ContainsKey((@operator, left, right)))
                    {
                        BinaryTypeMap[(@operator, left, right)] = ConversionCase.Nothing;
                    }
                }
            }
        }

        static void AddContainerCarveOuts()
        {
            BinaryTypeMap[(ExpressionOperator.Add, Tag.Str, Tag.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Add, Tag.List, Tag.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.List, Tag.Long)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.Long, Tag.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.Str, Tag.Long)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.Long, Tag.Str)] = ConversionCase.Nothing;
            // Python treats bool as a subtype of int, so `True * "ab"` and `[1] * True` are valid.
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.List, Tag.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.Bool, Tag.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.Str, Tag.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, Tag.Bool, Tag.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.BinaryOr, Tag.Dict, Tag.Dict)] = ConversionCase.Nothing;
        }

        static void AddMembershipRules(ExpressionOperator @operator)
        {
            foreach (var leftTag in AllTags)
            {
                BinaryTypeMap[(@operator, leftTag, Tag.List)] = ConversionCase.Nothing;
                BinaryTypeMap[(@operator, leftTag, Tag.Dict)] = ConversionCase.Nothing;
                BinaryTypeMap[(@operator, leftTag, Tag.Range)] = ConversionCase.Nothing;
            }

            BinaryTypeMap[(@operator, Tag.Str, Tag.Str)] = ConversionCase.Nothing;
        }

        static void AddUnaryRules()
        {
            UnaryTypeMap[(ExpressionOperator.Negate, Tag.Bool)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOperator.Negate, Tag.Long)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOperator.Negate, Tag.Double)] = ConversionCase.ToFloat;

            foreach (var tag in AllTags)
            {
                UnaryTypeMap[(ExpressionOperator.Not, tag)] = ConversionCase.Nothing;
                UnaryTypeMap[(ExpressionOperator.ToStr, tag)] = ConversionCase.Nothing;
            }
        }

        static bool EitherIsFloat(Tag left, Tag right)
        {
            return left == Tag.Double || right == Tag.Double;
        }

        #endregion

    }
}
