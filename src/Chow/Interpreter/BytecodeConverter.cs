using System;
using System.Collections.Generic;
using Chow.Bytecode;
using Chow.Interpreter.Compilation;
using Chow.Interpreter.Lexing;
using Chow.Interpreter.Semantics;
using Chow.Interpreter.Syntax;
using Chow.Syntax;
using Chow.Tokens;

namespace Chow.Interpreter
{
    static class BytecodeConverter
    {
        public static BytecodeChunk Compile(string sourceCode)
        {
            // Every identifier in the program. CPython interns them while parsing, which decides
            // how it builds some constant sets.
            var names = new HashSet<string>(StringComparer.Ordinal);

            var tokenStream = TokenizeSourceCode(sourceCode, names);

            var astRoot = CreateAbstractSyntaxTree(tokenStream, names);

            // Mutates abstract syntax tree for scope resolution
            AnalyzeAstSemantics(astRoot);

            var chunk = CompileFromAst(astRoot, names);
            return chunk;
        }

        // 1.) Lexical Analysis
        static ITokenStream TokenizeSourceCode(string sourceCode, HashSet<string> names)
        {

            var scanner = new Scanner(sourceCode, TokenStreamFactory.Create(), names);
            var tokenStream = scanner.TokenizeSourceCode();
            return tokenStream;
        }

        //  2.) Syntax Analysis
        static Node CreateAbstractSyntaxTree(ITokenStream tokenStream, HashSet<string> names)
        {

            var parser = new Parser(tokenStream, names);
            var syntaxTreeRoot = parser.BuildAst();
            return syntaxTreeRoot;
        }

        // 3.) Semantic Analysis
        static void AnalyzeAstSemantics(Node astRoot)
        {

            var semanticAnalyzer = new SemanticAnalyzer(astRoot);
            semanticAnalyzer.Analyze();
        }

        // 4.) Bytecode Compilation
        static BytecodeChunk CompileFromAst(Node astRoot, HashSet<string> programNames)
        {
            var compiler = new Compiler(astRoot, programNames);
            var chunk = compiler.CompileRoot();
            return chunk;
        }
    }
}
