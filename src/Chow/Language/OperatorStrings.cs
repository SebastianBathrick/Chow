using System.Collections.Generic;
using Chow.Bytecode;
using Chow.DataTypes;

namespace Chow.Expressions
{
    static class OperatorStrings
    {

        static readonly IReadOnlyDictionary<ExpressionOperator, string> ExpressionOperatorMap = new Dictionary<ExpressionOperator, string>()
        {
            { ExpressionOperator.Add, "+" },
            { ExpressionOperator.Subtract, "-" },
            { ExpressionOperator.Multiply, "*" },
            { ExpressionOperator.Divide, "/" },
            { ExpressionOperator.Modulus, "%" },
            { ExpressionOperator.Exponentiate, "**" },
            { ExpressionOperator.FloorDivide, "//" },
            { ExpressionOperator.Negate, "-" },
            { ExpressionOperator.Equal, "==" },
            { ExpressionOperator.NotEqual, "!=" },
            { ExpressionOperator.Less, "<" },
            { ExpressionOperator.Greater, ">" },
            { ExpressionOperator.LessEqual, "<=" },
            { ExpressionOperator.GreaterEqual, ">=" },
        };


        static readonly IReadOnlyDictionary<OperationCode, string> OperationCodeMap = new Dictionary<OperationCode, string>()
        {
            { OperationCode.Add, "+" },
            { OperationCode.Subtract, "-" },
            { OperationCode.Multiply, "*" },
            { OperationCode.Divide, "/" },
            { OperationCode.Modulus, "%" },
            { OperationCode.Exponentiate, "**" },
            { OperationCode.FloorDivide, "//" },
            { OperationCode.Negate, "-" },
        };
        
        public static string ToSource(ExpressionOperator op)
        {
            return ExpressionOperatorMap[op];
        }
    }
}