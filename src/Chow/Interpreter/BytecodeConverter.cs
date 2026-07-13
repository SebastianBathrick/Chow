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
            var tokenStream = TokenizeSourceCode(sourceCode);

            var astRoot = CreateAbstractSyntaxTree(tokenStream);

            // Mutates abstract syntax tree for scope resolution
            AnalyzeAstSemantics(astRoot);

            var chunk = CompileFromAst(astRoot);
            return chunk;
        }
        
        // 1.) Lexical Analysis
        static ITokenStream TokenizeSourceCode(string sourceCode)
        {

            var scanner = new Scanner(sourceCode, TokenStreamFactory.Create());
            var tokenStream = scanner.TokenizeSourceCode();
            return tokenStream;
        }
        
        //  2.) Syntax Analysis
        static Node CreateAbstractSyntaxTree(ITokenStream tokenStream)
        {

            var parser = new Parser(tokenStream);
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
        static BytecodeChunk CompileFromAst(Node astRoot)
        {
            var compiler = new Compiler(astRoot);
            var chunk = compiler.CompileRoot();
            return chunk;
        }
    }
}
