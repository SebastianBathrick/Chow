using System;
namespace Chow.Interpreter.Exceptions
{
    class TypeException : Exception
    {
        public TypeException()
        {
        }

        public TypeException(string message) : base(message)
        {
        }
    }
}
