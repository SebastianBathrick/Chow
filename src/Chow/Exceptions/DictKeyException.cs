namespace Chow.Interpreter.Exceptions
{
    internal class DictKeyException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        private string _keyRepr;

        public string KeyRepr => _keyRepr;

        public DictKeyException(string keyRepr, int lineNumber = -1) : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            _keyRepr = keyRepr;
        }
    }
}
