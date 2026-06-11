using System.Collections.Generic;

namespace Chow.Tokens
{
    /// <summary>
    /// A forward-only stream of scanner tokens that are selected and consumed one at a time.
    /// </summary>
    class TokenStream : ITokenStream
    {
        const int InitialTokenIndex = 0;
        
        List<Token> _tokensList;
        int _tokenIdx = InitialTokenIndex;
        
        Token SelectedToken => _tokensList[_tokenIdx];

        /// <summary>The line number of the selected token.</summary>
        public int LineNumber => SelectedToken.LineNumber;
        
        /// <summary>Whether there are any more tokens to consume.</summary>
        public bool IsEndOfStream => _tokenIdx == _tokensList.Count;

        /// <summary>Creates a token stream over the given list of tokens.</summary>
        /// <param name="tokensList">The tokens this stream will select from, in order.</param>
        /// <remarks>This constructor is temporary and will be removed after the scanner
        /// refactor.</remarks>
        public TokenStream(List<Token> tokensList)
        {
            _tokensList = tokensList;
        }

        public TokenType Peek()
        {
            return SelectedToken.Type;
        }
        
        /// <summary>Selects the next token or reaches the end of this stream.</summary>
        public Token Consume()
        {
            return _tokensList[_tokenIdx++];
        }

        /// <summary>
        /// Selects the next token in the stream if the currently selected token is
        /// <paramref name="expectedType"/>; otherwise, an exception will be thrown.
        /// </summary>
        /// <param name="expectedType">The token type the selected token's type will be compared
        /// to.</param>
        /// <returns>The token selected before this method is called.</returns>
        /// <exception cref="SyntaxException">An exception containing the expected token type and
        /// the line of the token selected before this method is called.</exception>
        public Token ConsumeMatch(TokenType expectedType)
        {
            if (IsMatch(expectedType))
            {
                return Consume();
            }
            
            var exLineNum = IsEndOfStream ? _tokensList[_tokenIdx - 1].LineNumber : LineNumber;
            throw new SyntaxException(expectedType.ToString(), exLineNum);
        }

        /// <summary>
        /// Determines whether the selected token is <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The token type the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if the selected token is <paramref name="targetType"/>;
        /// <c>false</c> if it is not or the end of this stream has been reached.</returns>
        public bool IsMatch(TokenType targetType)
        {
            return !IsEndOfStream && SelectedToken.Type == targetType;
        }

        /// <summary>
        /// Determines whether the selected token is any of <paramref name="targetTypes"/>.
        /// </summary>
        /// <param name="targetTypes">The token types the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if the selected token is any of <paramref name="targetTypes"/>;
        /// <c>false</c> if it is none of them or the end of this stream has been reached.</returns>
        public bool IsMatch(params TokenType[] targetTypes)
        {
            foreach (var type in targetTypes)
            {
                if (IsMatch(type))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Determines whether the token after the selected token is <paramref name="targetType"/>,
        /// without selecting it.
        /// </summary>
        /// <param name="targetType">The token type the next token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if the token after the selected token is
        /// <paramref name="targetType"/>; <c>false</c> if it is not or no token follows the selected
        /// token.</returns>
        public bool IsNextMatch(TokenType targetType)
        {
            return _tokenIdx + 1 < _tokensList.Count 
                && _tokensList[_tokenIdx + 1].Type == targetType;
        }

        /// <summary>
        /// Selects the next token in the stream if the currently selected token is
        /// <paramref name="targetType"/>; otherwise, this stream is left unchanged.
        /// </summary>
        /// <param name="targetType">The token type the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if a token was consumed; otherwise, <c>false</c>.</returns>
        public bool TryConsumeMatch(TokenType targetType)
        {
            return TryConsumeMatch(targetType, out _);
        }

        /// <summary>
        /// Selects the next token in the stream if the currently selected token is
        /// <paramref name="targetType"/>; otherwise, this stream is left unchanged.
        /// </summary>
        /// <param name="targetType">The token type the selected token's type will be compared
        /// to.</param>
        /// <param name="token">The consumed token, or <c>default</c> if no token was
        /// consumed.</param>
        /// <returns><c>true</c> if a token was consumed; otherwise, <c>false</c>.</returns>
        public bool TryConsumeMatch(TokenType targetType, out Token token)
        {
            if (IsEndOfStream || !IsMatch(targetType))
            {
                token = default;
                return false;
            }

            token = Consume();
            return true;
        }

        /// <summary>
        /// Selects the next token in the stream if the currently selected token is any of
        /// <paramref name="targetTypes"/>; otherwise, this stream is left unchanged.
        /// </summary>
        /// <param name="targetTypes">The token types the selected token's type will be compared
        /// to.</param>
        /// <returns><c>true</c> if a token was consumed; otherwise, <c>false</c>.</returns>
        public bool TryConsumeMatch(params TokenType[] targetTypes)
        {
            if (IsEndOfStream)
            {
                return false;
            }
            
            foreach (var type in targetTypes)
            {
                if (IsMatch(type))
                {
                    Consume();
                    return true;
                }
            }
            
            return false;
        }
    }
}
