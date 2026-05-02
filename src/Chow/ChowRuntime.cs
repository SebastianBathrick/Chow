using System;
using Chow.Tokens;
using Chow.Syntax;
using Chow.Bytecode;
using System.Collections.Generic;

namespace Chow
{
    public static class ChowRuntime
    {
        public static ChowValue ExecuteSourceCode(string sourceCode)
        {
            Scanner scanner = new Scanner(sourceCode);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Node syntaxTreeRoot = parser.BuildSyntaxTree();

            Compiler compiler = new Compiler(syntaxTreeRoot);
            Chunk chunk = compiler.CompileSyntaxTree();

            VirtualMachine vm = new VirtualMachine(chunk);
            vm.ExecuteChunk();
            return null;
        }
    }
}
