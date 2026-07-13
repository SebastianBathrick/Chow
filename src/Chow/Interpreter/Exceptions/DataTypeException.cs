using System;

namespace Chow.Interpreter.Exceptions
{
    class DataTypeException : Exception
    {
        public DataTypeException(string message) : base(message)
        {
        }
    }
}
