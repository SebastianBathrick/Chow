namespace Chow.Interpreter.Exceptions
{
    internal class ChowNameErrorException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        private string _undefinedName;

        public string UndefinedName => _undefinedName;

        public ChowNameErrorException(string undefinedName, int lineNumber) : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined", lineNumber)
        {
            _undefinedName = undefinedName;
        }
    }
}
 