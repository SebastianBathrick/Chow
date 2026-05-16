using System.Collections.Generic;

namespace Chow.Interpreter
{
    static class DataTypeConversionMap
    {
        static readonly HashSet<ExpressionOperator> _noConversionOps = new HashSet<ExpressionOperator>
        {
            
        }
        
        static readonly Dictionary<(ExpressionOperator op,  DataType left, DataType right), ConversionCase> _binaryTypeMap = 
            new Dictionary<(ExpressionOperator,  DataType, DataType), ConversionCase>
        {
        };

        static readonly Dictionary<(ExpressionOperator op, DataType operand), ConversionCase> _unaryTypeMap =
            new Dictionary<(ExpressionOperator, DataType), ConversionCase>
            {
            };
        
        static readonly Dictionary<(ExpressionOperator op, DataType initResultType), ConversionCase> _resultTypeMap =
            new Dictionary<(ExpressionOperator, DataType), ConversionCase>
            {
            };

        static ConversionCase GetResultConversionCase(ExpressionOperator op, DataType initResultType) 
        {
            
        }
        
        public static ConversionCase GetLeftRightConversionCase(ExpressionOperator op,  DataType left, DataType right)
        {
            // First always check if the operator requires conversion, and if not, return ConversionCase.NoConversion
            
            var typeMapKey = (op, left, right);

            if (_binaryTypeMap.ContainsKey(typeMapKey))
            {
                return _binaryTypeMap[typeMapKey];
            }
            
            // Throw some appropriate exception
        }

        public static ConversionCase GetOperandConversionCase(ExpressionOperator op, DataType operand)
        {
            
        }
        
    }

    enum ConversionCase
    {
        NoConversion,
        IntToFloat,
        BoolToInt,
        // Remaining...
    }
}