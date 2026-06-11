using Chow.Tokens;
using Chow.Utility;

namespace Chow.Ast.Parsing
{
    static class SyntaxMaps
    {
        static readonly TokenType[] ComparisonOperatorTypes =
        {
            TokenType.SymbolEqualTo,
            TokenType.SymbolNotEqual,
            TokenType.SymbolLess,
            TokenType.SymbolGreater,
            TokenType.SymbolLessEqual,
            TokenType.SymbolGreaterEqual,
            TokenType.KeywordIn
        };
        
        static readonly TokenType[] ExpressionStartTypes =
        {
            TokenType.Name,
            TokenType.LiteralInt,
            TokenType.LiteralFloat,
            TokenType.LiteralStr,
            TokenType.LiteralFString,
            TokenType.KeywordNone,
            TokenType.KeywordTrue,
            TokenType.KeywordFalse,
            TokenType.KeywordNot,
            TokenType.SymbolLeftParen,
            TokenType.SymbolLeftBracket,
            TokenType.SymbolMinus,
            TokenType.SymbolLeftCurly
        };

        public static bool IsExpressionStart(TokenType checkType)
        {
            foreach (var type in ExpressionStartTypes)
            {
                if (type == checkType)
                {
                    return true;
                }
            }
            
            return false;
        }

        public static bool IsComparisonOperator(TokenType checkType)
        {
            foreach (var type in ComparisonOperatorTypes)
            {
                if (type == checkType)
                {
                    return true;
                }
            }

            return false;
        }

        public static Operator ToBinaryOperator(TokenType operatorType)
        {
            switch (operatorType)
            {
                case TokenType.SymbolPlus:
                    return Operator.Add;
                case TokenType.SymbolMinus:
                    return Operator.Subtract;
                case TokenType.SymbolMultiply:
                    return Operator.Multiply;
                case TokenType.SymbolDivide:
                    return Operator.Divide;
                case TokenType.SymbolPercent:
                    return Operator.Modulus;
                case TokenType.SymbolExponent:
                    return Operator.Exponentiate;
                case TokenType.SymbolFloorDivide:
                    return Operator.FloorDivide;
                case TokenType.SymbolEqualTo:
                    return Operator.Equal;
                case TokenType.SymbolNotEqual:
                    return Operator.NotEqual;
                case TokenType.SymbolLess:
                    return Operator.Less;
                case TokenType.SymbolGreater:
                    return Operator.Greater;
                case TokenType.SymbolLessEqual:
                    return Operator.LessEqual;
                case TokenType.SymbolGreaterEqual:
                    return Operator.GreaterEqual;
                case TokenType.KeywordAnd:
                    return Operator.And;
                case TokenType.KeywordOr:
                    return Operator.Or;
                case TokenType.SymbolPipe:
                    return Operator.BinaryOr;
                case TokenType.KeywordIn:
                    return Operator.In;
                default:
                    throw new UnreachableException(nameof(ToBinaryOperator));
            }
        }
    }
}
