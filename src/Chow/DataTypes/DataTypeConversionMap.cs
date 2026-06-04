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

        static readonly Dictionary<(ExpressionOp op, Tag left, Tag right), ConversionCase> BinaryTypeMap =
            new Dictionary<(ExpressionOp, Tag, Tag), ConversionCase>();

        static readonly Dictionary<(ExpressionOp op, Tag operand), ConversionCase> UnaryTypeMap =
            new Dictionary<(ExpressionOp, Tag), ConversionCase>();

        static readonly Tag[] NumericTags = { Tag.Bool, Tag.Long, Tag.Double };

        static readonly Tag[] AllTags = (Tag[])Enum.GetValues(typeof(Tag));

        #endregion

        #region Public API

        public static ConversionCase GetLeftRightConversionCase(ExpressionOp op, Tag left, Tag right)
        {
            // Note: ExpressionOperator.And/Or are not registered here. The Compiler short-circuits them
            // into jump opcodes (see CompileShortCircuit), so this lookup is never invoked for those ops.
            // If a defensive caller ever queries them, the TypeException below is the correct response.
            return BinaryTypeMap.TryGetValue((op, left, right), out var conversion) 
                ? conversion 
                : throw new TypeException(
                    $"unsupported operand type(s) for {op}: '{left}' and '{right}'");

        }

        public static ConversionCase GetOperandConversionCase(ExpressionOp op, Tag operand)
        {
            return UnaryTypeMap.TryGetValue((op, operand), out var conversion) 
                ? conversion 
                : throw new TypeException($"bad operand type for unary {op}: '{operand}'");

        }

        #endregion

        #region Initialization

        static DataTypeConversionMap()
        {
            AddArithmeticPromotionRules(ExpressionOp.Add);
            AddArithmeticPromotionRules(ExpressionOp.Subtract);
            AddArithmeticPromotionRules(ExpressionOp.Multiply);
            AddArithmeticPromotionRules(ExpressionOp.Modulus);
            AddArithmeticPromotionRules(ExpressionOp.FloorDivide);
            AddArithmeticPromotionRules(ExpressionOp.Exponentiate);

            AddAlwaysFloatPromotionRules(ExpressionOp.Divide);

            AddComparisonPromotionRules(ExpressionOp.Less);
            AddComparisonPromotionRules(ExpressionOp.Greater);
            AddComparisonPromotionRules(ExpressionOp.LessEqual);
            AddComparisonPromotionRules(ExpressionOp.GreaterEqual);

            AddEqualityRules(ExpressionOp.Equal);
            AddEqualityRules(ExpressionOp.NotEqual);

            AddContainerCarveOuts();

            AddMembershipRules(ExpressionOp.In);
            AddMembershipRules(ExpressionOp.NotIn);

            AddUnaryRules();
        }

        static void AddArithmeticPromotionRules(ExpressionOp op)
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

                    BinaryTypeMap[(op, left, right)] = conversionCase;
                }
            }
        }

        static void AddAlwaysFloatPromotionRules(ExpressionOp op)
        {
            foreach (var left in NumericTags)
            {
                foreach (var right in NumericTags)
                {
                    BinaryTypeMap[(op, left, right)] = ConversionCase.ToFloat;
                }
            }
        }

        static void AddComparisonPromotionRules(ExpressionOp op)
        {
            AddArithmeticPromotionRules(op);
            BinaryTypeMap[(op, Tag.Str, Tag.Str)] = ConversionCase.Nothing;
        }

        static void AddEqualityRules(ExpressionOp op)
        {
            AddArithmeticPromotionRules(op);

            foreach (var left in AllTags)
            {
                foreach (var right in AllTags)
                {
                    if (!BinaryTypeMap.ContainsKey((op, left, right)))
                    {
                        BinaryTypeMap[(op, left, right)] = ConversionCase.Nothing;
                    }
                }
            }
        }

        static void AddContainerCarveOuts()
        {
            BinaryTypeMap[(ExpressionOp.Add, Tag.Str, Tag.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Add, Tag.List, Tag.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.List, Tag.Long)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.Long, Tag.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.Str, Tag.Long)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.Long, Tag.Str)] = ConversionCase.Nothing;
            // Python treats bool as a subtype of int, so `True * "ab"` and `[1] * True` are valid.
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.List, Tag.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.Bool, Tag.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.Str, Tag.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, Tag.Bool, Tag.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.BinaryOr, Tag.Dict, Tag.Dict)] = ConversionCase.Nothing;
        }

        static void AddMembershipRules(ExpressionOp op)
        {
            foreach (var leftTag in AllTags)
            {
                BinaryTypeMap[(op, leftTag, Tag.List)] = ConversionCase.Nothing;
                BinaryTypeMap[(op, leftTag, Tag.Dict)] = ConversionCase.Nothing;
                BinaryTypeMap[(op, leftTag, Tag.Range)] = ConversionCase.Nothing;
            }

            BinaryTypeMap[(op, Tag.Str, Tag.Str)] = ConversionCase.Nothing;
        }

        static void AddUnaryRules()
        {
            UnaryTypeMap[(ExpressionOp.Negate, Tag.Bool)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOp.Negate, Tag.Long)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOp.Negate, Tag.Double)] = ConversionCase.ToFloat;

            foreach (var tag in AllTags)
            {
                UnaryTypeMap[(ExpressionOp.Not, tag)] = ConversionCase.Nothing;
                UnaryTypeMap[(ExpressionOp.ToStr, tag)] = ConversionCase.Nothing;
            }
        }

        static bool EitherIsFloat(Tag left, Tag right)
        {
            return left == Tag.Double || right == Tag.Double;
        }

        #endregion

    }
}
