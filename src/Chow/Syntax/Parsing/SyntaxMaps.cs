using Chow.Tokens;
using Chow.Utility;

namespace Chow.Syntax.Parsing
{
    /// <summary>Classifies token types by their syntactic role.</summary>
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

        /// <summary>
        /// Determines whether <paramref name="checkType"/> can begin an expression.
        /// </summary>
        /// <param name="checkType">The token type to classify.</param>
        /// <returns>
        /// <c>true</c> if a token of <paramref name="checkType"/> can begin an
        /// expression; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Determines whether <paramref name="checkType"/> is a comparison operator.
        /// </summary>
        /// <param name="checkType">The token type to classify.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="checkType"/> is a comparison operator;
        /// otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Maps <paramref name="operatorType"/> to the binary operator it represents.
        /// </summary>
        /// <param name="operatorType">The token type to map.</param>
        /// <returns>The binary operator represented by <paramref name="operatorType"/>.</returns>
        /// <exception cref="UnreachableException">
        /// <paramref name="operatorType"/> does not
        /// represent a binary operator.
        /// </exception>
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
