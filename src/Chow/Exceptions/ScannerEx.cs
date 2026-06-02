using System;
namespace Chow.Exceptions
{
    sealed class ScannerEx : Exception
    {
        public int LineNumber { get; }

        public ScannerEx(string message, int lineNumber) : base($"[line {lineNumber}] Error: {message}")
        {
            LineNumber = lineNumber;
        }
    }
}
