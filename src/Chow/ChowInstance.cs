using System;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Compilation;
using System.Collections.Generic;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Evaluation;
using System.Text;
using Chow.Interpreter.Values;

namespace Chow.Interpreter
{
    public class ChowInstance
    {
        Compiler _compiler;
        VirtualMachine _vm;

        // TODO: Make scanner, parser, compiler, and VM instance members of the class so they store state and info
        public ChowValue Run(string sourceCode)
        {
            Scanner scanner = new Scanner(sourceCode);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Node syntaxTreeRoot = parser.BuildSyntaxTree();

            _compiler = new Compiler(syntaxTreeRoot);
            Chunk chunk = _compiler.CompileSyntaxTreeRoot();

            _vm = new VirtualMachine(chunk);
            _vm.ExecuteChunk();
            return null;
        }

        // TODO: Remove when no longer needed. This is for debugging developement
        public string GetVariableDebugInfo()
        {
            List<(string name, TaggedUnion union)> varInfoList = _vm.GetVariableDebugInfo();
            StringBuilder sb = new StringBuilder();

            foreach (var (name, union) in varInfoList)
            {
                sb.AppendLine($"Variable \"{name}\":\n\t{union}\n");
            }
            return sb.ToString();
        }

        public void Compile(string sourceCode)
        {
            throw new NotImplementedException();
        }

        public ChowValue Run()
        {
            throw new NotImplementedException();
        }
    }
}
