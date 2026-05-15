using System;
namespace Chow.Interpreter.Exceptions
{
    sealed class ScannerEx : Exception
    {
        public ScannerEx(string message, int lineNumber) : base($"[line {lineNumber}] Error: {message}")
        {
            LineNumber = lineNumber;
        }
        public int LineNumber { get; }
    }
}
