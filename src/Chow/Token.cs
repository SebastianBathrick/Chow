namespace Chow
{
    /// <summary>
    /// Represents one lexeme recognized by the scanner.
    /// </summary>
    readonly struct Token
    {
        TokenType Type { get; }

        string Lexeme { get; }

        int LineNum { get; }

        // Can  be null
        object Literal { get; }

        public Token(TokenType type, string lexeme, int lineNum, object literal)
        {
            Type = type;
            Lexeme = lexeme;
            LineNum = lineNum;
            Literal = literal;
        }
    }
}
