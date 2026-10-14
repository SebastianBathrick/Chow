namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// The base type for exceptions raised while compiling Chow source code, such as lexical, syntax,
    /// and scoping errors. None of the source code has executed when one is thrown. Hosts can catch
    /// this type to handle any Chow compile error.
    /// </summary>
    public abstract class CompileException : ChowException
    {
        /// <summary>Initializes a new <see cref="CompileException"/>.</summary>
        /// <param name="errorName">The Chow-facing name of the error (e.g. <c>SyntaxError</c>) used
        /// to prefix the message.</param>
        /// <param name="description">A description of what went wrong.</param>
        /// <param name="lineNumber">The line of Chow source the error occurred on.</param>
        protected CompileException(string errorName, string description, int lineNumber)
            : base(errorName, description, lineNumber)
        {
        }
    }
}
