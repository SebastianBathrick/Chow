namespace Chow
{
    /// <summary>
    /// Represents one lexeme recognized by the scanner.
    /// </summary>
    readonly struct Token
    {
        internal TokenType Type { get; }

        internal string Lexeme { get; }

        internal int LineNum { get; }

        // Can  be null
        internal object Literal { get; }

        public Token(TokenType type, string lexeme, int lineNum, object literal)
        {
            Type = type;
            Lexeme = lexeme;
            LineNum = lineNum;
            Literal = literal;
        }

        public override string ToString()
        {
            string literalText = Literal == null ? "null" : Literal.ToString();

            return $"{Type} {FormatLexeme(Lexeme)} {literalText} line {LineNum}";
        }

        static string FormatLexeme(string lexeme)
        {
            return lexeme
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\f", "\\f");
        }
    }
}
