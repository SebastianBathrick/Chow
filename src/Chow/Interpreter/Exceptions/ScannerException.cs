namespace Chow.Interpreter.Exceptions
{
    sealed class ScannerException : CompileException
    {
        public ScannerException(string message, int lineNumber)
            : base($"[line {lineNumber}] Error: {message}", lineNumber)
        {
        }
    }
}
