using System.Collections.Generic;
using Chow.Bytecode;

namespace Chow.Utility
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
            { ExpressionOperator.BinaryOr, "|" },
        };


        static readonly IReadOnlyDictionary<OperationCode, string> OperationCodeMap = new Dictionary<OperationCode, string>()
        {
            { OperationCode.BinaryAdd, "+" },
            { OperationCode.BinarySubtract, "-" },
            { OperationCode.BinaryMultiply, "*" },
            { OperationCode.BinaryDivide, "/" },
            { OperationCode.BinaryModulus, "%" },
            { OperationCode.BinaryPow, "**" },
            { OperationCode.BinaryFloor, "//" },
            { OperationCode.UnaryNegate, "-" },
        };
        
        public static string ToSource(ExpressionOperator op)
        {
            return ExpressionOperatorMap[op];
        }
    }
}