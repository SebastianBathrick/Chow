using Chow.Tokens;

namespace Chow.Ast.Parsing
{
    static class SyntaxDefinitions
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

        public bool IsExpressionStartType(TokenType checkType)
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
    }
}
