using Chow.Interpreter.Compilation;
using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter
{
    public class ChowModule
    {
        ChowEnvironment _enviro;

        public void Run(string sourceCode)
        {
            // Run source code with an environment if this instance has executed code before
            // Otherwise, environment will be null and a new environment will be created for the source code to run in
            _enviro = Run(sourceCode, _enviro);
        }

        static ChowEnvironment Run(string sourceCode, ChowEnvironment enviro)
        {
            Scanner scanner = new Scanner(sourceCode);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Node syntaxTreeRoot = parser.BuildSyntaxTree();

            Compiler compiler = new Compiler(syntaxTreeRoot);
            Chunk chunk = compiler.CompileSyntaxTreeRoot();

            // Executes the chunk with the provided environment, or if null, a new environment
            VirtualMachine vm = new VirtualMachine(chunk, enviro);

            // Return environment with all global variables and their values after executing the chunk
            return vm.ExecuteChunk();
        }
    }
}
