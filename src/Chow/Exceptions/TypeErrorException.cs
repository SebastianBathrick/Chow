using System;

namespace Chow.Interpreter.Exceptions
{
    internal class TypeErrorException : Exception
    {
        public TypeErrorException()
        {
        }

        public TypeErrorException(string message) : base(message)
        {
        }
    }
}
