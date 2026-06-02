using System;
namespace Chow.Exceptions
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
