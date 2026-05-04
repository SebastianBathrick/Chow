using System;

namespace Chow.Interpreter.Syntax
{
    internal sealed class ParserException : Exception
    {
        public int LineNumber { get; }

        public ParserException(string message, int lineNumber)
            : base($"[line {lineNumber}] Error: {message}")
        {
            LineNumber = lineNumber;
        }
    }
}
