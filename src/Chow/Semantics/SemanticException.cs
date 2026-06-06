using System;
using Chow.Core;
namespace Chow.Exceptions
{
    /// <summary>
    /// Compile-time error raised by <see cref="SemanticAnalyzer"/> between parsing and compilation
    /// (e.g. invalid <c>global</c>/<c>nonlocal</c> declarations). Not a <see cref="ChowException"/>
    /// because the source code never starts executing; mirrors the shape of <see cref="ParserException"/>.
    /// </summary>
    sealed class SemanticException : Exception
    {
        const string EXCEPTION_ALIAS = "SyntaxError";

        public int LineNum { get; }

        public SemanticException(string msg, int lineNum) : base($"[line {lineNum}] {EXCEPTION_ALIAS}: {msg}")
        {
            LineNum = lineNum;
        }
    }
}
