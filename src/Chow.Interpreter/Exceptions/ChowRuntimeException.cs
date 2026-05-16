using System;
namespace Chow.Interpreter.Exceptions
{
    public abstract class ChowRuntimeException : Exception
    {
        // This is for exceptions not thrown due to a line inside of Chow source code
        const int NO_LINE_NUMBER = -1;

        // TODO: Add line's source code to the exception message
        int _lineNumber;

        protected ChowRuntimeException(string exceptionAlias, string message, int lineNumber = NO_LINE_NUMBER) : base($"{exceptionAlias}: {message} on line {lineNumber}")
        {
            _lineNumber = lineNumber;
        }
    }
}
