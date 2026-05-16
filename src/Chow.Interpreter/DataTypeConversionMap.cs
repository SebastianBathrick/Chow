using System;
using System.Collections.Generic;
using System.Linq;
using Chow.Interpreter.Exceptions;

namespace Chow.Interpreter
{
    static class DataTypeConversionMap
    {
        #region Fields

        static readonly Dictionary<(ExpressionOperator op, DataType left, DataType right), ConversionCase> _binaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType, DataType), ConversionCase>();

        static readonly Dictionary<(ExpressionOperator op, DataType operand), ConversionCase> _unaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType), ConversionCase>();
        
        static readonly HashSet<ExpressionOperator> _noConversionOps = new HashSet<ExpressionOperator>
            { ExpressionOperator.And, ExpressionOperator.Or, };
        
        static readonly DataType[] NumericTags = { DataType.Bool, DataType.Int, DataType.Float };

        static readonly DataType[] AllTags = (DataType[])Enum.GetValues(typeof(DataType));
        
        #endregion
        
        #region Public API
        
        public static ConversionCase GetLeftRightConversionCase(ExpressionOperator op, DataType left, DataType right)
        {
            if (_noConversionOps.Contains(op))
            {
                return ConversionCase.NoConversion;
            }

            if (_binaryTypeMap.TryGetValue((op, left, right), out var conversion))
            {
                return conversion;
            }

            throw new TypeException($"unsupported operand type(s) for {op}: '{left}' and '{right}'");
        }

        public static ConversionCase GetOperandConversionCase(ExpressionOperator op, DataType operand)
        {
            if (_unaryTypeMap.TryGetValue((op, operand), out var conversion))
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
                    ConversionCase conversionCase = ConversionCase.PromoteToInt;
                    
                    if (EitherIsFloat(left, right))
                    {
                        conversionCase = ConversionCase.PromoteToFloat;
                    }

                    _binaryTypeMap[(op, left, right)] = conversionCase;
                }
            }
        }

        static void AddAlwaysFloatPromotionRules(ExpressionOperator op)
        {
            foreach (var left in NumericTags)
            {
                foreach (var right in NumericTags)
                {
                    _binaryTypeMap[(op, left, right)] = ConversionCase.PromoteToFloat;
                }
            }
        }

        static void AddComparisonPromotionRules(ExpressionOperator op)
        {
            AddArithmeticPromotionRules(op);
            _binaryTypeMap[(op, DataType.Str, DataType.Str)] = ConversionCase.NoConversion;
        }

        static void AddEqualityRules(ExpressionOperator op)
        {
            AddArithmeticPromotionRules(op);

            foreach (var left in AllTags)
            {
                foreach (var right in AllTags)
                {
                    if (!_binaryTypeMap.ContainsKey((op, left, right)))
                    { 
                        _binaryTypeMap[(op, left, right)] = ConversionCase.NoConversion;
                    }
                }
            }
        }

        static void AddContainerCarveOuts()
        {
            _binaryTypeMap[(ExpressionOperator.Add, DataType.Str, DataType.Str)] = ConversionCase.NoConversion;
            _binaryTypeMap[(ExpressionOperator.Add, DataType.List, DataType.List)] = ConversionCase.NoConversion;
            _binaryTypeMap[(ExpressionOperator.Multiply, DataType.List, DataType.Int)] = ConversionCase.NoConversion;
            _binaryTypeMap[(ExpressionOperator.Multiply, DataType.Int, DataType.List)] = ConversionCase.NoConversion;
            _binaryTypeMap[(ExpressionOperator.Multiply, DataType.Str, DataType.Int)] = ConversionCase.NoConversion;
            _binaryTypeMap[(ExpressionOperator.Multiply, DataType.Int, DataType.Str)] = ConversionCase.NoConversion;
            _binaryTypeMap[(ExpressionOperator.BinaryOr, DataType.Dict, DataType.Dict)] = ConversionCase.NoConversion;
        }

        static void AddMembershipRules(ExpressionOperator op)
        {
            foreach (var leftTag in AllTags)
            {
                _binaryTypeMap[(op, leftTag, DataType.List)] = ConversionCase.NoConversion;
                _binaryTypeMap[(op, leftTag, DataType.Dict)] = ConversionCase.NoConversion;
                _binaryTypeMap[(op, leftTag, DataType.Range)] = ConversionCase.NoConversion;
            }

            _binaryTypeMap[(op, DataType.Str, DataType.Str)] = ConversionCase.NoConversion;
        }

        static void AddUnaryRules()
        {
            _unaryTypeMap[(ExpressionOperator.Negate, DataType.Bool)] = ConversionCase.PromoteToInt;
            _unaryTypeMap[(ExpressionOperator.Negate, DataType.Int)] = ConversionCase.PromoteToInt;
            _unaryTypeMap[(ExpressionOperator.Negate, DataType.Float)] = ConversionCase.PromoteToFloat;

            foreach (var tag in AllTags)
            {
                _unaryTypeMap[(ExpressionOperator.Not, tag)] = ConversionCase.NoConversion;
            }
        }

        static bool EitherIsFloat(DataType left, DataType right)
        {
            return left == DataType.Float || right == DataType.Float;
        }

        #endregion
    }
}
