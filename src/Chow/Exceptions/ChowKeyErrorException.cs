namespace Chow.Interpreter.Exceptions
{
    internal class ChowKeyErrorException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        private string _keyRepr;

        public string KeyRepr => _keyRepr;

        public ChowKeyErrorException(string keyRepr, int lineNumber = -1)
            : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            _keyRepr = keyRepr;
        }
    }
}
