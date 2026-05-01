using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Tokens
{
    readonly struct Token
    {
        public readonly TokenType type;
        public readonly int lineIndex;

        // Using the column index and length, we can extract the token's lexeme without needing to store its actual string
        public readonly int columnIndex;
        public readonly int length;
        
        public bool IsOfType(TokenType type)
        {
            return this.type == type;
        }
    }
}
