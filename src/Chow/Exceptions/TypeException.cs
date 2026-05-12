using System;

namespace Chow.Interpreter.Exceptions
{
    internal class TypeException : Exception
    {
        public TypeException()
        {
        }

        public TypeException(string message) : base(message)
        {
        }
    }
}
