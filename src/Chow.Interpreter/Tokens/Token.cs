namespace Chow.Interpreter.Tokens
{
    /// <summary>
    /// Represents one lexeme recognized by the scanner.
    /// </summary>
    readonly struct Token
    {
        public readonly TokenType Type;

        public readonly string Lexeme;

        public readonly int LineNum;

        // Can  be null
        public readonly object Literal;

        public Token(TokenType type, string lexeme, int lineNum, object literal)
        {
            this.Type = type;
            this.Lexeme = lexeme;
            this.LineNum = lineNum;
            this.Literal = literal;
        }

        public override string ToString()
        {
            return $"Token(type={Type}, lexeme=\"{FormatLexeme(Lexeme)}\", literal={FormatLiteral(Literal)}, line={LineNum})";
        }

        static string FormatLexeme(string lexeme)
        {
            return lexeme
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\f", "\\f");
        }

        static string FormatLiteral(object literal)
        {
            if (literal == null)
            {
                return "null";
            }

            if (literal is string strLiteral)
            {
                return $"\"{FormatLexeme(strLiteral)}\"";
            }

            return literal.ToString();
        }
    }
}
