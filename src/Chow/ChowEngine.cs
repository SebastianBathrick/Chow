using Chow.Ast.Parsing;
using Chow.Bytecode.Compilation;
using Chow.Semantics;
using Chow.SourceData;
using Chow.StandardLibrary.BuiltIns;
using Chow.Tokens.Scanning;
using Chow.VM;

namespace Chow
{
    public sealed class ChowEngine
    {
        /// <summary>Compiles and interprets Chow source code contained in a <see langword="string"/>.</summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="SourceValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
        public static ChowValue Execute(string sourceCode, bool useBuiltIns = true)
        {
            var globalScope = new Scope();
            
            if (useBuiltIns)
            {
                ImportBuiltIns(globalScope);
            }

            var scanner = new Scanner(sourceCode);
            var tokens = scanner.TokenizeSourceCode();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildAst();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new Processor(globalScope, chunk);
            var result = vm.Execute();
            return new ChowValue(result);
        }

        static void ImportBuiltIns(Scope globalScope)
        {
            var namedInvocableObjects = BuiltInFunctions.NamedInvocableObjects;
            foreach (var namedInvocable in namedInvocableObjects)
            {
                var chowValue = new SourceValue(namedInvocable.callableObject);
                globalScope.AssignVariableValue(namedInvocable.name, ref chowValue);
            }
        }

        internal static SourceValue ExecuteModuleCode(string sourceCode, Scope moduleGlobalScope)
        {
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.TokenizeSourceCode();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildAst();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new Processor(moduleGlobalScope, chunk);
            return vm.Execute();
        }

        internal static SourceValue InvokeChowFunction(Scope moduleGlobalScope, string functionName, SourceValue[] args)
        {
            var vm = new Processor(moduleGlobalScope);
            return vm.CallGlobalFunction(functionName, args);
        }
    }
}
