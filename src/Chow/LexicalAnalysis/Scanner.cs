using Chow.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.LexicalAnalysis
{
    internal class Scanner
    {
        string _sourceCode;
        TokenStream _tokens;

        public Scanner(string sourceCode)
        {
            // TODO: Refactor TokenStream to use a string and not a string[] _tokens = new TokenStream(_sourceCode);
            _sourceCode = sourceCode;
        }

        public TokenStream ScanTokens()
        {

        }

    }
}
