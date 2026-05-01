using System;
using System.Collections.Generic;

namespace Chow.Tokens
{
    internal sealed class TokenStream
    {
        private const string EmptyStreamMessage = "Token stream is empty.";

        private readonly string[] sourceCodeLines;
        private readonly List<Token> tokens;
        private int index;

        public TokenStream(string[] sourceCodeLines)
        {
            this.sourceCodeLines = sourceCodeLines ?? throw new ArgumentNullException(nameof(sourceCodeLines));
            this.tokens = new List<Token>();
            this.index = 0;
        }

        public bool IsTokenQueued => index < tokens.Count;

        public void Enqueue(Token token)
        {
            tokens.Add(token);
        }

        public void Dequeue()
        {
            if (!IsTokenQueued)
                throw new InvalidOperationException(EmptyStreamMessage);

            index++;
        }

        public Token DequeueAndReturn()
        {
            if (!IsTokenQueued)
                throw new InvalidOperationException(EmptyStreamMessage);

            return tokens[index++];
        }

        public Token Peek()
        {
            if (!IsTokenQueued)
                throw new InvalidOperationException(EmptyStreamMessage);

            return tokens[index];
        }

        public string PeekLexeme()
        {
            if (!IsTokenQueued)
                throw new InvalidOperationException(EmptyStreamMessage);

            Token token = tokens[index];
            return sourceCodeLines[token.lineNumber].Substring(token.startColumnIndex, token.length);
        }
    }
}
