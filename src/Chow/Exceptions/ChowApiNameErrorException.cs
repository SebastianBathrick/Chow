namespace Chow.Interpreter.Exceptions
{
    internal class ChowApiNameErrorException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        private string _undefinedName;

        public string UndefinedName => _undefinedName;

        public ChowApiNameErrorException(string undefinedName) : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined")
        {
            _undefinedName = undefinedName;
        }
    }
}
