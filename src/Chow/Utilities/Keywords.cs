using System.Collections.Generic;
using Chow.Tokens;

namespace Chow.Code
{
    static class Keywords
    {
        static readonly IReadOnlyDictionary<string, TokenType> KeywordTypeMap = new Dictionary<string, TokenType>
        {
            {
                "True", TokenType.KeywordTrue
            },
            {
                "False", TokenType.KeywordFalse
            },
            {
                "None", TokenType.KeywordNone
            },
            {
                "and", TokenType.KeywordAnd
            },
            {
                "or", TokenType.KeywordOr
            },
            {
                "not", TokenType.KeywordNot
            },
            {
                "is", TokenType.KeywordIs
            },
            {
                "in", TokenType.KeywordIn
            },
            {
                "def", TokenType.KeywordDef
            },
            {
                "return", TokenType.KeywordReturn
            },
            {
                "class", TokenType.KeywordClass
            },
            {
                "with", TokenType.KeywordWith
            },
            {
                "as", TokenType.KeywordAs
            },
            {
                "global", TokenType.KeywordGlobal
            },
            {
                "nonlocal", TokenType.KeywordNonlocal
            },
            {
                "if", TokenType.KeywordIf
            },
            {
                "else", TokenType.KeywordElse
            },
            {
                "elif", TokenType.KeywordElif
            },
            {
                "for", TokenType.KeywordFor
            },
            {
                "while", TokenType.KeywordWhile
            },
            {
                "break", TokenType.KeywordBreak
            },
            {
                "continue", TokenType.KeywordContinue
            },
            {
                "pass", TokenType.KeywordPass
            },
            {
                "try", TokenType.KeywordTry
            },
            {
                "except", TokenType.KeywordExcept
            },
            {
                "finally", TokenType.KeywordFinally
            },
            {
                "raise", TokenType.KeywordRaise
            },
            {
                "assert", TokenType.KeywordAssert
            }
        };

        static readonly IReadOnlyDictionary<TokenType, string> TypeToKeyword;

        static Keywords()
        {
            var reverse = new Dictionary<TokenType, string>(KeywordTypeMap.Count);

            foreach (var pair in KeywordTypeMap)
            {
                reverse[pair.Value] = pair.Key;
            }

            TypeToKeyword = reverse;
        }

        // Assumes Contains was called first
        public static TokenType GetTokenType(string keyword)
        {
            return KeywordTypeMap[keyword];
        }

        public static bool Contains(string keyword)
        {
            return KeywordTypeMap.ContainsKey(keyword);
        }

        public static string GetKeyword(TokenType tokenType)
        {
            return TypeToKeyword[tokenType];
        }
    }
}
