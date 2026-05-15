namespace Chow.Interpreter.Tokens
{
    /// <summary>Represents one lexeme recognized by the scanner.</summary>
    readonly struct Token
    {
        public readonly TokenType type;

        public readonly string lexeme;

        public readonly int lineNum;

        // Can  be null
        public readonly object literal;

        public Token(TokenType type, string lexeme, int lineNum, object literal)
        {
            this.type = type;
            this.lexeme = lexeme;
            this.lineNum = lineNum;
            this.literal = literal;
        }

        public override string ToString()
        {
            return $"Token(type={type}, lexeme=\"{FormatLexeme(lexeme)}\", literal={FormatLiteral(literal)}, line={lineNum})";
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
