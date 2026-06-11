using System.Collections.Generic;
using Chow.Bytecode;

namespace Chow.Utility
{
    static class OperatorStrings
    {

        static readonly IReadOnlyDictionary<Operator, string> ExpressionOperatorMap = new Dictionary<Operator, string>()
        {
            { Operator.Add, "+" },
            { Operator.Subtract, "-" },
            { Operator.Multiply, "*" },
            { Operator.Divide, "/" },
            { Operator.Modulus, "%" },
            { Operator.Exponentiate, "**" },
            { Operator.FloorDivide, "//" },
            { Operator.Negate, "-" },
            { Operator.Equal, "==" },
            { Operator.NotEqual, "!=" },
            { Operator.Less, "<" },
            { Operator.Greater, ">" },
            { Operator.LessEqual, "<=" },
            { Operator.GreaterEqual, ">=" },
            { Operator.BinaryOr, "|" },
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
        
        public static string ToSource(Operator op)
        {
            return ExpressionOperatorMap[op];
        }
    }
}