namespace Chow.Interpreter.Exceptions
{
    class DataTypeException : RuntimeException
    {
        const string ExceptionAlias = "TypeError";

        public DataTypeException(string message) : base(ExceptionAlias, message)
        {
        }
    }
}
