namespace Chow.Interpreter.Exceptions
{
    /// <summary>Python's <c>RecursionError</c>, raised when nested calls exceed the recursion limit.</summary>
    class RecursionException : RuntimeException
    {
        const string ExceptionAlias = "RecursionError";

        public RecursionException(string message, int lineNumber = -1) : base(ExceptionAlias, message, lineNumber)
        {
        }
    }
}
