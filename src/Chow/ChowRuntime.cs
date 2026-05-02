using System;
using Chow.Tokens;
using Chow.Syntax;
using System.Collections.Generic;

namespace Chow
{
    public static class ChowRuntime
    {
        public static ChowValue ExecuteCode(string sourceCode)
        {
            Scanner scanner = new Scanner(sourceCode);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Node syntaxTreeRoot = parser.BuildSyntaxTree();

            Console.WriteLine(syntaxTreeRoot);
            return null;
        }
    }
}
