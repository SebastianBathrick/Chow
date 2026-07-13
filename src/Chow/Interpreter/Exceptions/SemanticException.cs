using System;
using Chow.Interpreter.Semantics;

namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// Compile-time error raised by <see cref="SemanticAnalyzer"/> between parsing and compilation
    /// (e.g. invalid <c>global</c>/<c>nonlocal</c> declarations). Not a
    /// <see cref="RuntimeException"/> because the source code never starts executing; mirrors the
    /// shape of <see cref="SyntaxException"/>.
    /// </summary>
    sealed class SemanticException : Exception
    {
        const string ExceptionAlias = "SyntaxError";

        public int LineNum { get; }

        public SemanticException(string msg, int lineNum) 
            : base($"[line {lineNum}] {ExceptionAlias}: {msg}")
        {
            LineNum = lineNum;
        }
    }
}
