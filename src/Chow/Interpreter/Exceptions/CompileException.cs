using System;

namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// The base type for exceptions raised while compiling Chow source code, such as lexical, syntax,
    /// and scoping errors. None of the source code has executed when one is thrown. Hosts can catch
    /// this type to handle any Chow compile error.
    /// </summary>
    public abstract class CompileException : Exception
    {
        /// <summary>The line of Chow source the error occurred on.</summary>
        public int LineNumber { get; }

        /// <summary>Initializes a new <see cref="CompileException"/>.</summary>
        /// <param name="message">The complete error message.</param>
        /// <param name="lineNumber">The line of Chow source the error occurred on.</param>
        protected CompileException(string message, int lineNumber) : base(message)
        {
            LineNumber = lineNumber;
        }
    }
}
