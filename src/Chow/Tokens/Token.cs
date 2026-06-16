namespace Chow.Tokens
{
    /// <summary>
    /// Represents one lexeme recognized by the scanner.
    /// </summary>
    readonly struct Token
    {
        const int EmptyTokenLineNumber = -1;
        const string EmptyTokenLexeme = "";
        const object EmptyTokenLiteral = null;

        public readonly TokenType Type;

        public readonly string Lexeme;

        public readonly int LineNumber;

        // Can be null
        public readonly object Literal;

        public Token(
            TokenType type = TokenType.EmptyToken,
            string lexeme = EmptyTokenLexeme,
            int lineNumber = EmptyTokenLineNumber,
            object literal = EmptyTokenLiteral)
        {
            Type = type;
            Lexeme = lexeme;
            LineNumber = lineNumber;
            Literal = literal;
        }

        public override string ToString()
        {
            return
                $"Token(type={Type}, lexeme=\"{FormatLexeme(Lexeme)}\", literal={FormatLiteral(Literal)}, line={LineNumber})";
        }

        static string FormatLexeme(string lexeme)
        {
            return lexeme
                .Replace("\\", @"\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\f", "\\f");
        }

        static string FormatLiteral(object literal)
        {
            switch (literal)
            {
                case null:
                    return "null";
                case string strLiteral:
                    return $"\"{FormatLexeme(strLiteral)}\"";
                default:
                    return literal.ToString();
            }

        }
    }
}
