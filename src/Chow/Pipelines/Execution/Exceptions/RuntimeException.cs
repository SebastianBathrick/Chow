using System;

namespace Chow.VM
{
    public abstract class RuntimeException : Exception
    {
        // This is for exceptions not thrown due to a line inside of Chow source code
        const int NoLineNumber = -1;

        // TODO: BinaryAdd line's source code to the exception messageS

        protected RuntimeException(string exceptionAlias, string message, int lineNumber = NoLineNumber) : base(
            $"{exceptionAlias}: {message} on line {lineNumber}")
        {
        }
    }
}
