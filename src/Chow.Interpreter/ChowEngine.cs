using Chow.Interpreter.Values;
using Chow.Interpreter.State;
using System;
namespace Chow.Interpreter
{
    public static class ChowEngine
    {
        public static ChowValue ExecuteCode(string sourceCode)
        {
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildTree();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new VirtualMachine(new Scope(), chunk);
            return vm.EvaluateChunk();
        }
        
        internal static ChowValue ExecuteModuleCode(string sourceCode, Scope moduleGlobalScope)
        {
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildTree();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new VirtualMachine(moduleGlobalScope, chunk);
            return vm.EvaluateChunk();
        }

        internal static ChowValue CallModuleFunction(string sourceCode, Scope moduleGlobalScope)
        {
            throw new NotImplementedException();
        }
    }
}
