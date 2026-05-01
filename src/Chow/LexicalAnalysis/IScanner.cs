using Chow.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Lexing
{
    interface IScanner
    {
        ITokenStream TokenizeSourceCode(ITokenStream tokenStream, string[] sourceCodeLines);
    }
}
