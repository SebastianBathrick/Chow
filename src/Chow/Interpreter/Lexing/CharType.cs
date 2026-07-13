namespace Chow.Interpreter.Lexing
{
    enum CharType : byte
    {
        Digit,
        Letter,
        Indent,
        FormFeed,
        Newline,
        CommentPrefix,
        Quote,
        FStringPrefix,
        IdentifierPrefix,
        IdentifierSuffix,
    }
}
