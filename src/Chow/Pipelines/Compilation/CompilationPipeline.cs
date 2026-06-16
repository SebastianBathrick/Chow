using Chow.Tokens.Scanning;
using Chow.Tokens;
using Chow.Syntax.Parsing;
using Chow.Syntax;
using Chow.Semantics;
using Chow.Bytecode;
using Chow.Bytecode.Compilation;


namespace Chow.Pipeline
{
    static class CompilationPipeline
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
