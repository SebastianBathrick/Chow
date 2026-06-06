using System;

namespace Chow.Exceptions
{
    class DataTypeException : Exception
    {   
        public DataTypeException(string message) : base(message)
        {
        }
    }
}
