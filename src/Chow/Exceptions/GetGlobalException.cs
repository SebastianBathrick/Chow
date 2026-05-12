namespace Chow.Interpreter.Exceptions
{
    internal class GetGlobalException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "NameError";

        private string _undefinedName;

        public string UndefinedName => _undefinedName;

        public GetGlobalException(string undefinedName) : base(EXCEPTION_ALIAS, $"name '{undefinedName}' is not defined")
        {
            _undefinedName = undefinedName;
        }
    }
}
