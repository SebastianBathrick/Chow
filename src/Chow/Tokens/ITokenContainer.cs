namespace Chow.Tokens
{
    interface ITokenStream
    {
        Token Consume();
        
        Token ConsumeMatch(TokenType expectedType);
        
        bool IsMatch(TokenType targetType);

        bool IsMatch(params TokenType[] targetTypes);
        
        bool IsNextMatch(TokenType targetType);
        
        bool TryConsumeMatch(TokenType targetType);
        
        bool TryConsumeMatch(params TokenType[] targetTypes);
    }
}
