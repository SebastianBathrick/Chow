using System;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Jit;
using System.Collections.Generic;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Evaluation;

namespace Chow.Interpreter
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
