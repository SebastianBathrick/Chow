using System;

namespace Chow.Interpreter.Exceptions
{
    internal class ChowTypeErrorException : Exception
    {
        public ChowTypeErrorException()
        {
        }

        public ChowTypeErrorException(string message) : base(message)
        {
        }
    }
}
