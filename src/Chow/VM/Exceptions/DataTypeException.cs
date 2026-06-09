using System;
namespace Chow.VM
{
    class DataTypeException : Exception
    {   
        public DataTypeException(string message) : base(message)
        {
        }
    }
}
