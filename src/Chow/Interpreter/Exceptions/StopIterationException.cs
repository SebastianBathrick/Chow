namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// Python's <c>StopIteration</c>, raised by <c>next()</c> on an exhausted iterator when no default
    /// is given. Like Python's, it carries no description.
    /// </summary>
    class StopIterationException : RuntimeException
    {
        const string ExceptionAlias = "StopIteration";

        public StopIterationException(int lineNumber = -1) : base(ExceptionAlias, string.Empty, lineNumber)
        {
        }
    }
}
