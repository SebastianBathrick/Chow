namespace Chow.Exceptions
{
    class DictKeyException : ChowException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        public string KeyRepr { get; }

        public DictKeyException(string keyRepr, int lineNumber = -1) : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            KeyRepr = keyRepr;
        }
    }
}
