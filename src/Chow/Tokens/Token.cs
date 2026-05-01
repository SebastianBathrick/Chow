using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Tokens
{
    internal readonly struct Token
    {
        public readonly TokenType type;
        public readonly int lineNumber;

        // Using the start column index and length, we can extract the token's lexeme without needing to store its actual string.
        public readonly int startColumnIndex;
        public readonly int length;

        public Token(TokenType type, int lineNumber, int startColumnIndex, int length)
        {
            this.type = type;
            this.lineNumber = lineNumber;
            this.startColumnIndex = startColumnIndex;
            this.length = length;
        }

        public bool IsOfType(TokenType type)
        {
            return this.type == type;
        }
    }
}
