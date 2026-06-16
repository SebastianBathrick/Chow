using System;
using Chow.Bytecode.Compilation;
using Chow.Semantics;
using Chow.SourceData;
using Chow.StandardLibrary.BuiltIns;
using Chow.Syntax.Parsing;
using Chow.Tokens;
using Chow.Tokens.Scanning;
using Chow.VM;

namespace Chow
{
    public static class ChowEngine
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

            var parser = new Parser(new TokenStream(tokens));
            var syntaxTreeRoot = parser.BuildAst();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new Processor(globalScope, chunk);
            var result = vm.Execute();
            
            return new ChowValue(result);
        }

        internal static SourceValue Call(SourceValue func, params SourceValue[] args)
        {
            if (func.DataType != DataType.Object)
            {
                throw new UnreachableException(
                    $"{nameof(Call)} call with invalid data type '{func.DataType}'");
            }

            var delegateFunc = (Func<SourceValue[], SourceValue>)func.ToObject();

            return delegateFunc.Invoke(args);
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
    }
}
