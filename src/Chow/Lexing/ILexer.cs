using Chow.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Lexing
{
    interface ILexer
    {
        public ITokenStream ConvertToTokens(ITokenStream tokenStream, readonly string[] sourceCodeLines);
    }
}
