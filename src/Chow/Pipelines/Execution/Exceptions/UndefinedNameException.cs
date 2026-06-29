using Chow;

namespace Chow.VM
{
    class UndefinedNameException : RuntimeException
    {
        const string ExceptionAlias = "NameError";

        public string UndefinedName { get; }

        public UndefinedNameException(string undefinedName, int lineNumber)
            : base(ExceptionAlias, $"name '{undefinedName}' is not defined", lineNumber)
        {
            UndefinedName = undefinedName;
        }
    }
}
