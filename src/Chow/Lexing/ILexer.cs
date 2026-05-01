using Chow.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Lexing
{
    interface ILexer
    {
        ITokenStream ConvertToTokens(ITokenStream tokenStream, string[] sourceCodeLines);
    }
}
