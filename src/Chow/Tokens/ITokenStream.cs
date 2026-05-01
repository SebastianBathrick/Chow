using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Tokens
{
    internal interface ITokenStream
    {
        /// <summary>
        /// Gets a value indicating whether a token is currently queued for processing.
        /// </summary>
        public bool IsTokenQueued { get; }

        /// <summary>
        /// Adds the specified token to the end of the queue for processing.
        /// </summary>
        /// <param name="token">The token to enqueue. Cannot be null.</param>
        public void Enqueue(Token token);

        /// <summary>
        /// Removes the first token in the queue.
        /// </summary>
        public void Dequeue();

        /// <summary>
        /// Removes and returns the first token in the queue.
        /// </summary>
        /// <returns>The first token in the queue.</returns>
        public Token DequeueAndReturn();

        /// <summary>
        /// Returns the first token in the queue without removing it.
        /// </summary>
        /// <returns>The first token in the queue.</returns>
        public Token Peek();

        /// <summary>
        /// Returns the lexeme of the first token in the queue without removing it.
        /// </summary>
        /// <returns>The lexeme of the first token in the queue.</returns>
        public string PeekLexeme();


    }
}
