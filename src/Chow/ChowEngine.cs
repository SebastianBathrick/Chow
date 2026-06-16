using System;
using Chow.Bytecode;
using Chow.Bytecode.Compilation;
using Chow.Semantics;
using Chow.SourceData;
using Chow.StandardLibrary.BuiltIns;
using Chow.Syntax;
using Chow.Syntax.Parsing;
using Chow.Tokens;
using Chow.Tokens.Scanning;
using Chow.VM;

namespace Chow
{
    public static class ChowEngine
    {
        #region Public API

        /// <summary>Compiles and interprets Chow source code contained in a <see langword="string"/>.</summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns>
        /// <see cref="SourceValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.
        /// </returns>
        public static ChowValue Execute(string sourceCode, bool useBuiltIns = true)
        {
            return (ChowValue)RunPipeline(sourceCode, useBuiltIns, false);
        }

        public static ChowValue ScopedExecute(string sourceCode, bool useBuiltIns = true)
        {
            return (ChowValue)RunPipeline(sourceCode, useBuiltIns, true);
        }

        #endregion

        static IChowValue RunPipeline(string sourceCode, bool useBuiltIns, bool doesReturnScope)
        {
            var globalScope = CreateInitialScope(useBuiltIns);

            var chunk = CompileFromSourceCode(sourceCode);

            return ExecuteBytecode(globalScope, chunk, doesReturnScope);
        }

        #region Scope Methods

        static Scope CreateInitialScope(bool useBuiltIns)
        {
            var globalScope = new Scope();
            return useBuiltIns ? ImportBuiltIns(globalScope) : globalScope;
        }

        static Scope ImportBuiltIns(Scope globalScope)
        {
            var namedInvocableObjects = BuiltInFunctions.NamedInvocableObjects;

            foreach (var namedInvocable in namedInvocableObjects)
            {
                var chowValue = new SourceValue(namedInvocable.callableObject);
                globalScope.AssignVariableValue(namedInvocable.name, ref chowValue);
            }

            return globalScope;
        }

        #endregion

        #region Bytecode Compilation Methods

        static BytecodeChunk CompileFromSourceCode(string sourceCode)
        {

            var tokenStream = TokenizeSourceCode(sourceCode);

            var astRoot = CreateAbstractSyntaxTree(tokenStream);

            // Mutates abstract syntax tree for scope resolution
            AnalyzeAstSemantics(astRoot);

            var chunk = CompileFromAst(astRoot);
            return chunk;
        }

        static ITokenStream TokenizeSourceCode(string sourceCode)
        {

            var scanner = new Scanner(sourceCode, TokenStreamFactory.Create());
            var tokenStream = scanner.TokenizeSourceCode();
            return tokenStream;
        }

        static Node CreateAbstractSyntaxTree(ITokenStream tokenStream)
        {

            var parser = new Parser(tokenStream);
            var syntaxTreeRoot = parser.BuildAst();
            return syntaxTreeRoot;
        }

        static void AnalyzeAstSemantics(Node astRoot)
        {

            var semanticAnalyzer = new SemanticAnalyzer(astRoot);
            semanticAnalyzer.Analyze();
        }

        static BytecodeChunk CompileFromAst(Node astRoot)
        {
            var compiler = new Compiler(astRoot);
            var chunk = compiler.CompileRoot();
            return chunk;
        }

        #endregion

        #region Virtual Machine Methods

        static IChowValue ExecuteBytecode(Scope globalScope, BytecodeChunk chunk, bool doesReturnScope = false)
        {
            // Returns the result of the last expression statement to execute or SourceValue.None
            var result = ProcessBytecodeChunk(globalScope, chunk);

            return CreateVirtualMachineOutput(ref result, doesReturnScope);
        }

        static SourceValue ProcessBytecodeChunk(Scope globalScope, BytecodeChunk bytecodeChunk)
        {

            var processor = new InstructionProcessor(globalScope, bytecodeChunk);
            var result = processor.Execute();
            return result;
        }

        static IChowValue CreateVirtualMachineOutput(ref SourceValue result, bool doesReturnScope)
        {
            if (!doesReturnScope)
            {
                return ChowValueFactory.Create(ref result);
            }

            var srcScope = SourceObjectFactory.CreateNewObject(DataType.Scope);
            srcScope.SetAttribute(SourceObjectConsts.ExpressionResultAttribute, result);

            return ChowValueFactory.Create(srcScope.ToSourceValue());
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

        #endregion
    }
}
