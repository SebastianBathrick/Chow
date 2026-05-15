namespace Chow.Interpreter.Exceptions
{
    class DictKeyException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        public DictKeyException(string keyRepr, int lineNumber = -1) : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            KeyRepr = keyRepr;
        }

        public string KeyRepr { get; }
    }
}
