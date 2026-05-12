namespace Chow.Interpreter.Exceptions
{
    class DictKeyException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        string _keyRepr;

        public string KeyRepr => _keyRepr;

        public DictKeyException(string keyRepr, int lineNumber = -1) : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            _keyRepr = keyRepr;
        }
    }
}
