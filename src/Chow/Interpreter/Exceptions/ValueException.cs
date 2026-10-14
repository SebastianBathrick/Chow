namespace Chow.Interpreter.Exceptions
{
    class ValueException : RuntimeException
    {
        const string ExceptionAlias = "ValueError";

        public ValueException(string message, int lineNumber = -1) : base(ExceptionAlias, message, lineNumber)
        {
        }
    }
}
