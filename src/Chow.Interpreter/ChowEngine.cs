using Chow.Interpreter.Values;
using Chow.Interpreter.State;
using System;
namespace Chow.Interpreter
{
    public static class ChowEngine
    {
        /// <summary>  Compiles and interprets Chow source code contained in a <see langword="string"/>. </summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
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

        internal static ChowValue InvokeChowFunction(Scope moduleGlobalScope, string functionName, ChowValue[] args)
        {
            var vm = new VirtualMachine(moduleGlobalScope);
            return vm.CallGlobalFunction(functionName, args);
        }
    }
}
