namespace Chow.Tokens
{
    /// <summary>
    /// A forward-only stream of scanner tokens that are selected and consumed one at a time.
    /// </summary>
    interface ITokenStream
    {
        /// <summary>The line number of the selected token.</summary>
        int LineNumber { get; }
        
        /// <summary>Whether there are any more tokens to consume.</summary>
        bool IsEndOfStream { get; }

        /// <summary>Appends a token to the end of this stream.</summary>
        /// <param name="token">The token to append.</param>
        /// <exception cref="System.InvalidOperationException">This stream is readonly because a
        /// token has already been consumed.</exception>
        void Add(Token token);

        /// <summary>Gets the type of the selected token without consuming it.</summary>
        /// <returns>The selected token's type.</returns>
        TokenType Peek();
        
        /// <summary>Selects the next token or reaches the end of this stream.</summary>
        Token Consume();
        
        /// <summary>
        /// Selects the next token in the stream if the currently selected token is
        /// <paramref name="expectedType"/>; otherwise, an exception will be thrown.
        /// </summary>
        /// <param name="expectedType">The token type the selected token's type will be compared
        /// to.</param>
        /// <returns>The token selected before this method is called.</returns>
        /// <exception cref="SyntaxException">An exception containing the expected token type and
        /// the line of the token selected before this method is called.</exception>
        Token ConsumeMatch(TokenType expectedType);
        
        /// <summary>
        /// Consumes two or more tokens in order, requiring each to match its expected type;
        /// otherwise, an exception will be thrown.
        /// </summary>
        /// <param name="expectedType1">The token type the first consumed token's type will be
        /// compared to.</param>
        /// <param name="expectedType2">The token type the second consumed token's type will be
        /// compared to.</param>
        /// <param name="expectedTypes">The token types any further consumed tokens' types will be
        /// compared to, in order.</param>
        /// <returns>The token selected after all expected tokens are consumed.</returns>
        /// <exception cref="SyntaxException">An exception containing the first expected token type
        /// that did not match and the line of the token it was compared to.</exception>
        Token ConsumeMatches(
            TokenType expectedType1, 
            TokenType expectedType2, 
            params TokenType[] expectedTypes);
        
        /// <summary>
        /// Determines whether the selected token is <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The token type the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if the selected token is <paramref name="targetType"/>;
        /// <c>false</c> if it is not or the end of this stream has been reached.</returns>
        bool IsMatch(TokenType targetType);

        /// <summary>
        /// Determines whether the selected token is any of <paramref name="targetTypes"/>.
        /// </summary>
        /// <param name="targetTypes">The token types the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if the selected token is any of <paramref name="targetTypes"/>;
        /// <c>false</c> if it is none of them or the end of this stream has been reached.</returns>
        bool IsMatch(params TokenType[] targetTypes);
        
        /// <summary>
        /// Determines whether the token after the selected token is <paramref name="targetType"/>,
        /// without selecting it.
        /// </summary>
        /// <param name="targetType">The token type the next token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if the token after the selected token is
        /// <paramref name="targetType"/>; <c>false</c> if it is not or no token follows the
        /// selected token.</returns>
        bool IsNextMatch(TokenType targetType);
        
        /// <summary>
        /// Selects the next token in the stream if the currently selected token is
        /// <paramref name="targetType"/>; otherwise, this stream is left unchanged.
        /// </summary>
        /// <param name="targetType">The token type the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if a token was consumed; otherwise, <c>false</c>.</returns>
        bool TryConsumeMatch(TokenType targetType);

        /// <summary>
        /// Selects the next token in the stream if the currently selected token is
        /// <paramref name="targetType"/>; otherwise, this stream is left unchanged.
        /// </summary>
        /// <param name="targetType">The token type the selected token's type will be compared
        /// to.</param>
        /// <param name="token">The consumed token, or <c>default</c> if no token was
        /// consumed.</param>
        /// <returns><c>true</c> if a token was consumed; otherwise, <c>false</c>.</returns>
        bool TryConsumeMatch(TokenType targetType, out Token token);

        /// <summary>
        /// Selects the next token in the stream if the currently selected token is any of
        /// <paramref name="targetTypes"/>; otherwise, this stream is left unchanged.
        /// </summary>
        /// <param name="targetTypes">The token types the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if a token was consumed; otherwise, <c>false</c>.</returns>
        bool TryConsumeMatch(params TokenType[] targetTypes);
    }
}
