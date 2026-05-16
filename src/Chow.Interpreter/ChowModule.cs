using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.Tokens;

namespace Chow.Interpreter
{
    /// <summary>
    /// The primary entry point for embedding the Chow interpreter. Manages a persistent global scope
    /// across multiple <see cref="Execute"/> calls so that variables and functions defined in one call
    /// are available in subsequent calls. The standard built-in functions are seeded into the global
    /// scope at construction.
    /// </summary>
    public class ChowModule
    {
        readonly Scope _globalScope = new Scope();

        public ChowModule()
        {
            foreach (var type in BuiltIns.AllTypes)
            {
                _globalScope.AssignVariableValue(BuiltIns.NameOf(type), new ChowValue((object)BuiltIns.DefaultOf(type)));
            }
        }

        /// <summary>
        /// Compiles and executes a string of Chow source code. The global scope persists across calls, so
        /// variables and functions defined in one call are available in subsequent calls.
        /// <see langword="null"/>, empty, and whitespace-only strings are accepted and treated as no-ops.
        /// </summary>
        /// <param name="sourceCode">The Chow source code to execute.</param>
        /// <exception cref="Exceptions.ScannerException">The source code contains a lexical error.</exception>
        /// <exception cref="Exceptions.ParserException">The source code contains a syntax error.</exception>
        /// <exception cref="Exceptions.ChowRuntimeException">A runtime error occurs during execution.</exception>
        public void Execute(string sourceCode)
        {
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildTree();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new VirtualMachine(_globalScope, chunk);

            vm.EvaluateChunk();
        }
    }
}
