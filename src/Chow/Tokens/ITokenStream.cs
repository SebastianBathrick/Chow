namespace Chow.Tokens
{
    interface ITokenStream
    {
        int LineNumber { get; }
        
        TokenType Peek();
        
        Token Consume();
        
        Token ConsumeMatch(TokenType expectedType);
        
        Token ConsumeMatches(TokenType targetType1, TokenType targetType2, params TokenType[] expectedTypes);
        
        bool IsMatch(TokenType targetType);

        bool IsMatch(params TokenType[] targetTypes);
        
        bool IsNextMatch(TokenType targetType);
        
        bool TryConsumeMatch(TokenType targetType);
        
        bool TryConsumeMatch(params TokenType[] targetTypes);
    }
}
