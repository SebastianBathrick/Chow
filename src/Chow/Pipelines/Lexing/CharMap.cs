namespace Chow.Code
{
    static class CharMap
    {
        public static bool IsOfType(char c, CharType type)
        {
            switch (type)
            {
                case CharType.Digit:
                    return IsDigit(c);
                case CharType.Letter:
                    return IsLetter(c);
                case CharType.Indent:
                    return IsIndent(c);
                case CharType.FormFeed:
                    return IsFormFeed(c);
                case CharType.Newline:
                    return IsNewline(c);
                case CharType.CommentPrefix:
                    return IsCommentPrefix(c);
                case CharType.Quote:
                    return IsQuote(c);
                case CharType.FStringPrefix:
                    return IsFStringPrefix(c);
                case CharType.IdentifierPrefix:
                    return IsIdentifierPrefix(c);
                case CharType.IdentifierSuffix:
                    return IsIdentifierSuffix(c);
                default:
                    return false;
            }
        }

        
        public static bool TryDecodeEscape(char c, out char result)
        {
            switch (c)
            {
                case 'n':
                    result = '\n'; 
                    break;
                case 't':
                    result = '\t';
                    break;
                case 'r':
                    result = '\r';
                    break;
                case '\\':
                    result = '\\';
                    break;
                case '\'':
                    result = '\'';
                    break;
                case '"':
                    result = '"';
                    break;
                case '0':
                    result = '\0';
                    break;
                case 'a':
                    result = '\a';
                    break;
                case 'b':
                    result = '\b';
                    break;
                case 'f':
                    result = '\f';
                    break;
                case 'v':
                    result = '\v';
                    break;
                default:
                    result = default;
                    return false;
            }
            
            return true;
        }

        static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        static bool IsLetter(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        static bool IsIndent(char c)
        {
            return c == ' ' || c == '\t';
        }

        static bool IsFormFeed(char c)
        {
            return c == '\f';
        }

        static bool IsNewline(char c)
        {
            return c == '\n' || c == '\r';
        }

        static bool IsCommentPrefix(char c)
        {
            return c == '#';
        }

        static bool IsQuote(char c)
        {
            return c == '\'' || c == '"';
        }

        static bool IsFStringPrefix(char c)
        {
            return c == 'f' || c == 'F';
        }

        static bool IsIdentifierPrefix(char c)
        {
            return IsLetter(c) || c == '_';
        }

        static bool IsIdentifierSuffix(char c)
        {
            return IsLetter(c) || IsDigit(c) || c == '_';
        }
    }
}
