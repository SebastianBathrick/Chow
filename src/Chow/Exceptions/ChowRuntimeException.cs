using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Exceptions
{
    public abstract class ChowRuntimeException : Exception
    {
        // TODO: Add line's source code to the exception message
        int _lineNumber;

        protected ChowRuntimeException(string exceptionAlias, string message, int lineNumber)
            : base($"{exceptionAlias}: {message} on line {lineNumber}")
        {
            _lineNumber = lineNumber;
        }
    }
}
