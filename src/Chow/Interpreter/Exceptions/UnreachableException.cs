using System;

namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// Thrown when execution reaches a point in the interpreter that was believed to be
    /// unreachable, indicating an internal logic error rather than a fault in the Chow source code.
    /// </summary>
    class UnreachableException : Exception
    {
        /// <summary>Initializes a new <see cref="UnreachableException"/>.</summary>
        /// <param name="functionName">The name of the function in which the unreachable code was
        /// executed.</param>
        /// <param name="extraInfo">Optional additional context appended to the exception
        /// message.</param>
        public UnreachableException(string functionName, string extraInfo = "")
            : base(
                $"An instruction executed that was thought to be unreachable in {functionName}."
                + extraInfo)
        {
        }
    }
}
