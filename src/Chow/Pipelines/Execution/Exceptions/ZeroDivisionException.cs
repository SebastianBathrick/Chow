namespace Chow.VM
{
    class ZeroDivisionException : RuntimeException
    {
        const string ExceptionAlias = "ZeroDivisionError";
        const string DefaultMessage = "division by zero";

        public ZeroDivisionException(int lineNumber = -1) : base(ExceptionAlias, DefaultMessage, lineNumber)
        {
        }

        public ZeroDivisionException(string message, int lineNumber = -1) : base(ExceptionAlias, message, lineNumber)
        {
        }
    }
}
