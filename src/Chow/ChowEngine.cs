using Chow.Core;
using Chow.StandardLibrary;
using Chow.State;
namespace Chow
{
    public sealed class ChowEngine
    {
        /// <summary>Compiles and interprets Chow source code contained in a <see langword="string"/>.</summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="TaggedUnion.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
        public static TaggedUnion Execute(string sourceCode, bool useBuiltIns = true)
        {
            var globalScope = new Scope();
            
            if (useBuiltIns)
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

            var vm = new VirtualMachine(globalScope, chunk);
            return vm.EvaluateChunk();
        }

        static void ImportBuiltIns(Scope globalScope)
        {
            var namedInvocableObjects = BuiltInFunctions.NamedInvocableObjects;
            foreach (var namedInvocable in namedInvocableObjects)
            {
                var chowValue = new TaggedUnion(namedInvocable.callableObject);
                globalScope.AssignVariableValue(namedInvocable.name, chowValue);
            }
        }

        internal static TaggedUnion ExecuteModuleCode(string sourceCode, Scope moduleGlobalScope)
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

        internal static TaggedUnion InvokeChowFunction(Scope moduleGlobalScope, string functionName, TaggedUnion[] args)
        {
            var vm = new VirtualMachine(moduleGlobalScope);
            return vm.CallGlobalFunction(functionName, args);
        }
    }
}
