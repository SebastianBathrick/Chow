using System;
using Chow.Tokens;
using Chow.Syntax;
using Chow.Jit;
using System.Collections.Generic;
using Chow.Syntax.Trees;
using Chow.Evaluation;

namespace Chow
{
    public static class ChowState
    {
        public static ChowValue ExecuteSourceCode(string sourceCode)
        {
            Scanner scanner = new Scanner(sourceCode);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Node syntaxTreeRoot = parser.BuildSyntaxTree();

            Compiler compiler = new Compiler(syntaxTreeRoot);
            Chunk chunk = compiler.CompileSyntaxTreeRoot();

            VirtualMachine vm = new VirtualMachine(chunk);
            vm.ExecuteChunk();
            return null;
        }
    }
}
