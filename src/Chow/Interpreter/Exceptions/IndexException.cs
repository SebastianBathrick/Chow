namespace Chow.Interpreter.Exceptions
{
    class IndexException : RuntimeException
    {
        const string ExceptionAlias = "IndexError";

        public IndexException(string message, int lineNumber = -1) : base(ExceptionAlias, message, lineNumber)
        {
        }
    }
}
