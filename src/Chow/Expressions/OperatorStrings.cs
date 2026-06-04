using System.Collections.Generic;
using Chow.Bytecode;
using Chow.DataTypes;

namespace Chow.Expressions
{
    static class OperatorStrings
    {

        static readonly IReadOnlyDictionary<ExpressionOperator, string> ExpressionOperatorMap = new Dictionary<ExpressionOperator, string>()
        {
            { DataTypes.ExpressionOperator.Add, "+" },
            { DataTypes.ExpressionOperator.Subtract, "-" },
            { DataTypes.ExpressionOperator.Multiply, "*" },
            { DataTypes.ExpressionOperator.Divide, "/" },
            { DataTypes.ExpressionOperator.Modulus, "%" },
            { DataTypes.ExpressionOperator.Exponentiate, "**" },
        };


        static readonly IReadOnlyDictionary<OperationCode, string> OperationCodeMap = new Dictionary<OperationCode, string>()
        {
            { OperationCode.Add, "+" },
            { OperationCode.Subtract, "-" },
            { OperationCode.Multiply, "*" },
            { OperationCode.Divide, "/" },
            { OperationCode.Modulus, "%" },
            { OperationCode.Exponentiate, "**" },
        };
        public static string EnumToString(ExpressionOperator op)
        {
            return ExpressionOperatorMap[op];
        }

        public static string EnumToString(OperationCode op)
        {
            return OperationCodeMap[op];
        }
    }
}