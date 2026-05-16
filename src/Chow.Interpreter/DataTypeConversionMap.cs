using System;
using System.Collections.Generic;
using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter
{
    static class DataTypeConversionMap
    {

        #region Fields

        static readonly Dictionary<(ExpressionOperator op, DataType left, DataType right), ConversionCase> BinaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType, DataType), ConversionCase>();

        static readonly Dictionary<(ExpressionOperator op, DataType operand), ConversionCase> UnaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType), ConversionCase>();

        static readonly DataType[] NumericTags = { DataType.Bool, DataType.Int, DataType.Float };

        static readonly DataType[] AllTags = (DataType[])Enum.GetValues(typeof(DataType));

        #endregion

        #region Public API

        public static ConversionCase GetLeftRightConversionCase(ExpressionOperator op, DataType left, DataType right)
        {
            // Note: ExpressionOperator.And/Or are not registered here. The Compiler short-circuits them
            // into jump opcodes (see CompileShortCircuit), so this lookup is never invoked for those ops.
            // If a defensive caller ever queries them, the TypeException below is the correct response.
            if (BinaryTypeMap.TryGetValue((op, left, right), out var conversion))
            {
                return conversion;
            }

            throw new TypeException($"unsupported operand type(s) for {op}: '{left}' and '{right}'");
        }

        public static ConversionCase GetOperandConversionCase(ExpressionOperator op, DataType operand)
        {
            if (UnaryTypeMap.TryGetValue((op, operand), out var conversion))
            {
                return conversion;
            }

            throw new TypeException($"bad operand type for unary {op}: '{operand}'");
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

        static void AddArithmeticPromotionRules(ExpressionOperator op)
        {
            foreach (var left in NumericTags)
            {
                foreach (var right in NumericTags)
                {
                    var conversionCase = ConversionCase.PromoteToInt;

                    if (EitherIsFloat(left, right))
                    {
                        conversionCase = ConversionCase.PromoteToFloat;
                    }

                    BinaryTypeMap[(op, left, right)] = conversionCase;
                }
            }
        }

        static void AddAlwaysFloatPromotionRules(ExpressionOperator op)
        {
            foreach (var left in NumericTags)
            {
                foreach (var right in NumericTags)
                {
                    BinaryTypeMap[(op, left, right)] = ConversionCase.PromoteToFloat;
                }
            }
        }

        static void AddComparisonPromotionRules(ExpressionOperator op)
        {
            AddArithmeticPromotionRules(op);
            BinaryTypeMap[(op, DataType.Str, DataType.Str)] = ConversionCase.NoConversion;
        }

        static void AddEqualityRules(ExpressionOperator op)
        {
            AddArithmeticPromotionRules(op);

            foreach (var left in AllTags)
            {
                foreach (var right in AllTags)
                {
                    if (!BinaryTypeMap.ContainsKey((op, left, right)))
                    {
                        BinaryTypeMap[(op, left, right)] = ConversionCase.NoConversion;
                    }
                }
            }
        }

        static void AddContainerCarveOuts()
        {
            BinaryTypeMap[(ExpressionOperator.Add, DataType.Str, DataType.Str)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Add, DataType.List, DataType.List)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.List, DataType.Int)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Int, DataType.List)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Str, DataType.Int)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Int, DataType.Str)] = ConversionCase.NoConversion;
            // Python treats bool as a subtype of int, so `True * "ab"` and `[1] * True` are valid.
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.List, DataType.Bool)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Bool, DataType.List)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Str, DataType.Bool)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.Multiply, DataType.Bool, DataType.Str)] = ConversionCase.NoConversion;
            BinaryTypeMap[(ExpressionOperator.BinaryOr, DataType.Dict, DataType.Dict)] = ConversionCase.NoConversion;
        }

        static void AddMembershipRules(ExpressionOperator op)
        {
            foreach (var leftTag in AllTags)
            {
                BinaryTypeMap[(op, leftTag, DataType.List)] = ConversionCase.NoConversion;
                BinaryTypeMap[(op, leftTag, DataType.Dict)] = ConversionCase.NoConversion;
                BinaryTypeMap[(op, leftTag, DataType.Range)] = ConversionCase.NoConversion;
            }

            BinaryTypeMap[(op, DataType.Str, DataType.Str)] = ConversionCase.NoConversion;
        }

        static void AddUnaryRules()
        {
            UnaryTypeMap[(ExpressionOperator.Negate, DataType.Bool)] = ConversionCase.PromoteToInt;
            UnaryTypeMap[(ExpressionOperator.Negate, DataType.Int)] = ConversionCase.PromoteToInt;
            UnaryTypeMap[(ExpressionOperator.Negate, DataType.Float)] = ConversionCase.PromoteToFloat;

            foreach (var tag in AllTags)
            {
                UnaryTypeMap[(ExpressionOperator.Not, tag)] = ConversionCase.NoConversion;
                UnaryTypeMap[(ExpressionOperator.ToStr, tag)] = ConversionCase.NoConversion;
            }
        }

        static bool EitherIsFloat(DataType left, DataType right)
        {
            return left == DataType.Float || right == DataType.Float;
        }

        #endregion

    }
}
