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

        static readonly Dictionary<(ExpressionOp op, DataType left, DataType right), ConversionCase> BinaryTypeMap =
            new Dictionary<(ExpressionOp, DataType, DataType), ConversionCase>();

        static readonly Dictionary<(ExpressionOp op, DataType operand), ConversionCase> UnaryTypeMap =
            new Dictionary<(ExpressionOp, DataType), ConversionCase>();

        static readonly DataType[] NumericTags = { DataType.Bool, DataType.Int, DataType.Float };

        static readonly DataType[] AllTags = (DataType[])Enum.GetValues(typeof(DataType));

        #endregion

        #region Public API

        public static ConversionCase GetLeftRightConversionCase(ExpressionOp op, DataType left, DataType right)
        {
            // Note: ExpressionOperator.And/Or are not registered here. The Compiler short-circuits them
            // into jump opcodes (see CompileShortCircuit), so this lookup is never invoked for those ops.
            // If a defensive caller ever queries them, the TypeException below is the correct response.
            return BinaryTypeMap.TryGetValue((op, left, right), out var conversion) 
                ? conversion 
                : throw new TypeException(
                    $"unsupported operand type(s) for {op}: '{left}' and '{right}'");

        }

        public static ConversionCase GetOperandConversionCase(ExpressionOp op, DataType operand)
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
            BinaryTypeMap[(op, DataType.Str, DataType.Str)] = ConversionCase.Nothing;
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
            BinaryTypeMap[(ExpressionOp.Add, DataType.Str, DataType.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Add, DataType.List, DataType.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.List, DataType.Int)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.Int, DataType.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.Str, DataType.Int)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.Int, DataType.Str)] = ConversionCase.Nothing;
            // Python treats bool as a subtype of int, so `True * "ab"` and `[1] * True` are valid.
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.List, DataType.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.Bool, DataType.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.Str, DataType.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.Multiply, DataType.Bool, DataType.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOp.BinaryOr, DataType.Dict, DataType.Dict)] = ConversionCase.Nothing;
        }

        static void AddMembershipRules(ExpressionOp op)
        {
            foreach (var leftTag in AllTags)
            {
                BinaryTypeMap[(op, leftTag, DataType.List)] = ConversionCase.Nothing;
                BinaryTypeMap[(op, leftTag, DataType.Dict)] = ConversionCase.Nothing;
                BinaryTypeMap[(op, leftTag, DataType.Range)] = ConversionCase.Nothing;
            }

            BinaryTypeMap[(op, DataType.Str, DataType.Str)] = ConversionCase.Nothing;
        }

        static void AddUnaryRules()
        {
            UnaryTypeMap[(ExpressionOp.Negate, DataType.Bool)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOp.Negate, DataType.Int)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOp.Negate, DataType.Float)] = ConversionCase.ToFloat;

            foreach (var tag in AllTags)
            {
                UnaryTypeMap[(ExpressionOp.Not, tag)] = ConversionCase.Nothing;
                UnaryTypeMap[(ExpressionOp.ToStr, tag)] = ConversionCase.Nothing;
            }
        }

        static bool EitherIsFloat(DataType left, DataType right)
        {
            return left == DataType.Float || right == DataType.Float;
        }

        #endregion

    }
}
