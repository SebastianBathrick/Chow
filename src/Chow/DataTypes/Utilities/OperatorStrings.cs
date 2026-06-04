using System.Collections.Generic;
using Chow.DataTypes;

static class OperatorStrings
{

    static readonly IReadOnlyDictionary<ExpressionOperator, string> OperatorStringMap = new Dictionary<ExpressionOperator, string>()
    {
        { ExpressionOperator.Add, "+" },
        { ExpressionOperator.Subtract, "-" },
        { ExpressionOperator.Multiply, "*" },
        { ExpressionOperator.Divide, "/" },
        { ExpressionOperator.Modulus, "%" },
        { ExpressionOperator.Exponentiate, "**" },
    };
    public static string GetOperatorString(ExpressionOperator op)
    {
        return OperatorStringMap[op];
    }
}