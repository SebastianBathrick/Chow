namespace Chow.Interpreter.Exceptions
{
    internal class ApiNameErrorException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        private string _undefinedName;

        public string UndefinedName => _undefinedName;

        public ApiNameErrorException(string undefinedName) : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined")
        {
            _undefinedName = undefinedName;
        }
    }
}
