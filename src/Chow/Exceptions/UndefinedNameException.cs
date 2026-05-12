namespace Chow.Interpreter.Exceptions
{
    class UndefinedNameException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        string _undefinedName;

        public string UndefinedName => _undefinedName;

        public UndefinedNameException(string undefinedName, int lineNumber) : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined", lineNumber)
        {
            _undefinedName = undefinedName;
        }
    }
}
