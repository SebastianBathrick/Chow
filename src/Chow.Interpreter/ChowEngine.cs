using Chow.Interpreter.Core;
using Chow.Interpreter.State;
using Chow.Interpreter.StandardLibrary;
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
        public static ChowValue Execute(string sourceCode, bool useBuiltInFunctions = true)
        {
            var globalScope = new Scope();
            if (useBuiltInFunctions)
            {
                ImportBuiltIns(globalScope);
            }

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

        static void ImportBuiltIns(Scope globalScope)
        {
            var namedInvocableObjects = BuiltInFunctions.NamedInvocableObjects;
            foreach (var namedInvocable in namedInvocableObjects)
            {
                var chowValue = new ChowValue(namedInvocable.callableObject);
                globalScope.AssignVariableValue(namedInvocable.name, chowValue);
            }
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
