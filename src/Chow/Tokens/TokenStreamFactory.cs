namespace Chow.Tokens
{
    static class TokenStreamFactory
    {
        public static ITokenStream Create()
        {
            return new TokenStream();
        }
    }
}
