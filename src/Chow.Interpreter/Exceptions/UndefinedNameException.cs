namespace Chow.Interpreter.Exceptions
{
    class UndefinedNameException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        public UndefinedNameException(string undefinedName, int lineNumber)
            : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined", lineNumber)
        {
            UndefinedName = undefinedName;
        }

        public string UndefinedName { get; }
    }
}
