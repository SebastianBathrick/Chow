namespace Chow.Interpreter.Exceptions
{
    /// <summary>Python's <c>RuntimeError</c>, e.g. a dict changing size while it is iterated.</summary>
    class RuntimeErrorException : RuntimeException
    {
        const string ExceptionAlias = "RuntimeError";

        public RuntimeErrorException(string message, int lineNumber = -1) : base(ExceptionAlias, message, lineNumber)
        {
        }
    }
}
