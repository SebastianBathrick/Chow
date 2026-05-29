using Chow.Interpreter.Core;
using Chow.Interpreter.State;
namespace Chow.Interpreter
{
    public sealed class ChowEngine
    {
        // Use the singleton pattern so that ChowEngine fields can easily be reset to their defaults
        // This is used for testing.
        static ChowEngine _instance;

        ChowEngine()
        {
        }

        internal static void Reset()
        {
            _instance = new ChowEngine();
        }
        
        /// <summary>Compiles and interprets Chow source code contained in a <see langword="string"/>.</summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
        // KNOWN PYTHON-PARITY GAPS (surfaced by failing ChowEngineTests expression-statement cases):
        //
        //   LOGIC ERROR (the op is implemented, but a code path wrongly rejects it):
        //     - A statement that STARTS with unary '-' (e.g. "-(4 + 1)", or nested "-(-(5))") throws
        //       ParserEx "Expected statement" — the leading '-', not the nesting depth, is what is
        //       rejected. Negation itself works (Parser.ParseFactor); the bug is that
        //       Parser.IsPrimaryToken() omits SymbolMinus, so '-' is not accepted as a statement start.
        //
        //   FEATURE NOT IMPLEMENTED (the VM has no branch for the str type):
        //     - String subscript ("abc"[0]), string slice ("abc"[1:3]), and string membership
        //       ("a" in "abc") throw TypeException. VirtualMachine.EvaluateSubscript /
        //       EvaluateSubscriptSlice / EvaluateIn only handle List and Dict.
        public static ChowValue Execute(string sourceCode)
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
