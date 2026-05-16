namespace Chow.Interpreter.Exceptions
{
    class ZeroDivisionException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "ZeroDivisionError";
        const string DEFAULT_MESSAGE = "division by zero";

        public ZeroDivisionException(int lineNumber = -1) : base(EXCEPTION_ALIAS, DEFAULT_MESSAGE, lineNumber)
        {
        }

        public ZeroDivisionException(string message, int lineNumber = -1) : base(EXCEPTION_ALIAS, message, lineNumber)
        {
        }
    }
}
