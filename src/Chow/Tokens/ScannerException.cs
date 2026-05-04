using System;

namespace Chow.Interpreter.Tokens
{
    internal sealed class ScannerException : Exception
    {
        public int LineNumber { get; }

        public ScannerException(string message, int lineNumber)
            : base($"[line {lineNumber}] Error: {message}")
        {
            LineNumber = lineNumber;
        }
    }
}
