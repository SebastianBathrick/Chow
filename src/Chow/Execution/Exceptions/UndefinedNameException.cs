namespace Chow.Exceptions
{
    class UndefinedNameException : RuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        public string UndefinedName { get; }

        public UndefinedNameException(string undefinedName, int lineNumber)
            : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined", lineNumber)
        {
            UndefinedName = undefinedName;
        }
    }
}
