using Chow.Syntax;
using Chow.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Parsing
{
    class Parser
    {
        List<Token> _tokens;
    
        public Parser(List<Token> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public Node BuildSyntaxTree()
        {

        }
    }
}
