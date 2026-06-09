using System;
using System.Collections.Generic;
using Chow.Exceptions;
using Chow.Utility;
using Chow.VM;
namespace Chow.Objects.Conversion
{
    static class DataTypeConversionMap
    {
        // TODO: BinaryAdd left/right specific conversion cases to avoid trying to convert both operands
        // Note: Check compiled code to see if this would actually make a difference
        
        #region Fields

        static readonly Dictionary<(ExpressionOperator op, DataType left, DataType right), ConversionCase> BinaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType, DataType), ConversionCase>();

        static readonly Dictionary<(ExpressionOperator op, DataType operand), ConversionCase> UnaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType), ConversionCase>();

        static readonly DataType[] NumericTags = { DataType.Bool, DataType.Long, DataType.Double };

        static readonly DataType[] AllTags = (DataType[])Enum.GetValues(typeof(DataType));

        #endregion

        #region Public API

        public static ConversionCase GetLeftRightConversionCase(ExpressionOperator @operator, DataType left, DataType right)
        {
            // Note: ExpressionOperator.And/Or are not registered here. The Compiler short-circuits them
            // into jump opcodes (see CompileShortCircuit), so this lookup is never invoked for those ops.
            // If a defensive caller ever queries them, the DataTypeException below is the correct response.
            return BinaryTypeMap.TryGetValue((@operator, left, right), out var conversion) 
                ? conversion 
                : throw new DataTypeException(
                    $"unsupported operand type(s) for {@operator}: '{left}' and '{right}'");

        }

        public static ConversionCase GetOperandConversionCase(ExpressionOperator @operator, DataType operand)
        {
            return UnaryTypeMap.TryGetValue((@operator, operand), out var conversion) 
                ? conversion 
                : throw new DataTypeException($"bad operand type for unary {@operator}: '{operand}'");

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
            BinaryTypeMap[(@operator, DataType.Str, DataType.Str)] = ConversionCase.Nothing;
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
            BinaryTypeMap[(ExpressionOperator.Add, DataType.Str, DataType.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Add, DataType.List, DataType.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.List, DataType.Long)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Long, DataType.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Str, DataType.Long)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Long, DataType.Str)] = ConversionCase.Nothing;
            // Python treats bool as a subtype of int, so `True * "ab"` and `[1] * True` are valid.
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.List, DataType.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Bool, DataType.List)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Str, DataType.Bool)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Bool, DataType.Str)] = ConversionCase.Nothing;
            BinaryTypeMap[(ExpressionOperator.BinaryOr, DataType.Dict, DataType.Dict)] = ConversionCase.Nothing;
        }

        static void AddMembershipRules(ExpressionOperator @operator)
        {
            foreach (var leftTag in AllTags)
            {
                BinaryTypeMap[(@operator, leftTag, DataType.List)] = ConversionCase.Nothing;
                BinaryTypeMap[(@operator, leftTag, DataType.Dict)] = ConversionCase.Nothing;
                BinaryTypeMap[(@operator, leftTag, DataType.Range)] = ConversionCase.Nothing;
            }

            BinaryTypeMap[(@operator, DataType.Str, DataType.Str)] = ConversionCase.Nothing;
        }

        static void AddUnaryRules()
        {
            UnaryTypeMap[(ExpressionOperator.Negate, DataType.Bool)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOperator.Negate, DataType.Long)] = ConversionCase.ToInt;
            UnaryTypeMap[(ExpressionOperator.Negate, DataType.Double)] = ConversionCase.ToFloat;

            foreach (var tag in AllTags)
            {
                UnaryTypeMap[(ExpressionOperator.Not, tag)] = ConversionCase.Nothing;
                UnaryTypeMap[(ExpressionOperator.ToStr, tag)] = ConversionCase.Nothing;
            }
        }

        static bool EitherIsFloat(DataType left, DataType right)
        {
            return left == DataType.Double || right == DataType.Double;
        }

        #endregion

    }
}
