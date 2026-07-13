using System;

namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// The base type for exceptions raised while executing Chow source code, such as type, name,
    /// subscript, and division-by-zero errors. Hosts can catch this type to handle any Chow runtime
    /// error.
    /// </summary>
    public abstract class RuntimeException : Exception
    {
        // This is for exceptions not thrown due to a line inside of Chow source code
        const int NoLineNumber = -1;

        // TODO: BinaryAdd line's source code to the exception messageS

        /// <summary>Initializes a new <see cref="RuntimeException"/>.</summary>
        /// <param name="exceptionAlias">The Chow-facing name of the error (e.g.
        /// <c>ZeroDivisionError</c>) used to prefix the message.</param>
        /// <param name="message">A description of what went wrong.</param>
        /// <param name="lineNumber">The line of Chow source the error occurred on, or -1 when it did
        /// not originate from a line of Chow source.</param>
        protected RuntimeException(string exceptionAlias, string message, int lineNumber = NoLineNumber) : base(
            $"{exceptionAlias}: {message} on line {lineNumber}")
        {
        }
    }
}
