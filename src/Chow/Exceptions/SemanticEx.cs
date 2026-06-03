using System;
using Chow.Core;
namespace Chow.Exceptions
{
    /// <summary>
    /// Compile-time error raised by <see cref="SemanticAnalyzer"/> between parsing and compilation
    /// (e.g. invalid <c>global</c>/<c>nonlocal</c> declarations). Not a <see cref="ChowException"/>
    /// because the source code never starts executing; mirrors the shape of <see cref="ParserEx"/>.
    /// </summary>
    sealed class SemanticEx : Exception
    {
        const string EXCEPTION_ALIAS = "SyntaxError";

        public int LineNum { get; }

        public SemanticEx(string msg, int lineNum) : base($"[line {lineNum}] {EXCEPTION_ALIAS}: {msg}")
        {
            LineNum = lineNum;
        }
    }
}
