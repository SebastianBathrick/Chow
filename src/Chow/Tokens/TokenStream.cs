using System;
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

        public int LineNumber => SelectedToken.LineNumber;
        
        /// <summary>Whether there are any more tokens to consume.</summary>
        public bool IsEndOfStream => _tokenIdx == _tokensList.Count;

        /// <summary>Creates an empty token stream to be populated with <see cref="Add"/>.</summary>
        public TokenStream()
        {
            _tokensList = new List<Token>();
        }

        /// <summary>Creates a token stream over the given list of tokens.</summary>
        /// <param name="tokensList">The tokens this stream will select from, in order.</param>
        /// <remarks>This constructor is temporary and will be removed after the scanner
        /// refactor.</remarks>
        public TokenStream(List<Token> tokensList)
        {
            _tokensList = tokensList;
        }
        
        /// <inheritdoc/>
        public void Add(Token token)
        {
            if (_tokenIdx != 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(TokenStream)} instances become readonly after a token is consumed.");
            }
            
            _tokensList.Add(token);
        }

        /// <inheritdoc/>
        public TokenType Peek()
        {
            return SelectedToken.Type;
        }
        
        /// <inheritdoc/>
        public Token Consume()
        {
            return _tokensList[_tokenIdx++];
        }
        
        /// <inheritdoc/>
        public Token ConsumeMatch(TokenType expectedType)
        {
            if (IsMatch(expectedType))
            {
                return Consume();
            }
            
            var exLineNum = IsEndOfStream ? _tokensList[_tokenIdx - 1].LineNumber : LineNumber;
            throw new SyntaxException(expectedType.ToString(), exLineNum);
        }
        
        /// <inheritdoc/>
        public Token ConsumeMatches(
            TokenType expectedType1, 
            TokenType expectedType2, 
            params TokenType[] expectedTypes)
        {
            ConsumeMatch(expectedType1);
            ConsumeMatch(expectedType2);

            if (expectedTypes == null || expectedTypes.Length == 0)
            {
                return SelectedToken;
            }

            foreach (var type in expectedTypes)
            {
                ConsumeMatch(type);
            }

            return SelectedToken;
        }

        /// <inheritdoc/>
        public bool IsMatch(TokenType targetType)
        {
            return !IsEndOfStream && SelectedToken.Type == targetType;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public bool IsNextMatch(TokenType targetType)
        {
            return _tokenIdx + 1 < _tokensList.Count 
                && _tokensList[_tokenIdx + 1].Type == targetType;
        }

        /// <inheritdoc/>
        public bool TryConsumeMatch(TokenType targetType)
        {
            return TryConsumeMatch(targetType, out _);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
