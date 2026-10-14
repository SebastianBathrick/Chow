namespace Chow.Interpreter.Exceptions
{
    sealed class ScannerException : CompileException
    {
        const string ExceptionAlias = "SyntaxError";

        public ScannerException(string message, int lineNumber)
            : base(ExceptionAlias, message, lineNumber)
        {
        }
    }
}
