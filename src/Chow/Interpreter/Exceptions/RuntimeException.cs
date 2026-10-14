namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// The base type for exceptions raised while executing Chow source code, such as type, name,
    /// subscript, and division-by-zero errors. Hosts can catch this type to handle any Chow runtime
    /// error.
    /// </summary>
    public abstract class RuntimeException : ChowException
    {
        // This is for exceptions not thrown due to a line inside of Chow source code
        const int NoLineNumber = -1;

        /// <summary>Initializes a new <see cref="RuntimeException"/>.</summary>
        /// <param name="exceptionAlias">The Chow-facing name of the error (e.g.
        /// <c>ZeroDivisionError</c>) used to prefix the message.</param>
        /// <param name="message">A description of what went wrong.</param>
        /// <param name="lineNumber">The line of Chow source the error occurred on, or -1 when it is
        /// not known where it is thrown. An error raised while the virtual machine is executing
        /// is given the line of the instruction that raised it.</param>
        protected RuntimeException(string exceptionAlias, string message, int lineNumber = NoLineNumber)
            : base(exceptionAlias, message, lineNumber == NoLineNumber ? (int?)null : lineNumber)
        {
        }
    }
}
