namespace Chow
{
    /// <summary>
    /// Represents one lexeme recognized by the scanner.
    /// </summary>
    internal sealed class Token
    {
        /// <summary>
        /// Gets the token category used by later compiler phases.
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// Gets the exact source code slice that produced this token.
        /// </summary>
        public string Lexeme { get; }

        /// <summary>
        /// Gets the parsed literal value for literal tokens, or null for tokens without a literal value.
        /// </summary>
        public object LiteralValue { get; }

        /// <summary>
        /// Gets the one-based source code line where this token begins.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Creates a token from its scanner-produced data.
        /// </summary>
        /// <param name="type">The token category.</param>
        /// <param name="lexeme">The exact source code slice that produced the token.</param>
        /// <param name="literalValue">The parsed literal value, or null when the token has no literal value.</param>
        /// <param name="lineNumber">The one-based source code line where the token begins.</param>
        public Token(TokenType type, string lexeme, object literalValue, int lineNumber)
        {
            Type = type;
            Lexeme = lexeme;
            LiteralValue = literalValue;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Determines whether this token has the supplied token type.
        /// </summary>
        /// <param name="type">The token type to compare against.</param>
        /// <returns>True when <see cref="Type"/> equals <paramref name="type"/>; otherwise false.</returns>
        public bool IsOfType(TokenType type)
        {
            return Type == type;
        }

        /// <summary>
        /// Returns a debugging representation of the token.
        /// </summary>
        /// <returns>The token type, lexeme, and literal value separated by spaces.</returns>
        public override string ToString()
        {
            return $"{Type} {Lexeme} {LiteralValue}";
        }
    }
}
