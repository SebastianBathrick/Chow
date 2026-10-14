using Chow.Interpreter.Semantics;

namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// Compile-time error raised by <see cref="SemanticAnalyzer"/> between parsing and compilation
    /// (e.g. invalid <c>global</c>/<c>nonlocal</c> declarations). A <see cref="CompileException"/>
    /// rather than a <see cref="RuntimeException"/> because the source code never starts executing.
    /// </summary>
    sealed class SemanticException : CompileException
    {
        const string ExceptionAlias = "SyntaxError";

        public SemanticException(string msg, int lineNum)
            : base(ExceptionAlias, msg, lineNum)
        {
        }
    }
}
