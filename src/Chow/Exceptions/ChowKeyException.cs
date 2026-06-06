namespace Chow.Exceptions
{
    class ChowKeyException : ChowException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        public string KeyRepr { get; }

        public ChowKeyException(string keyRepr, int lineNumber = -1) : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            KeyRepr = keyRepr;
        }
    }
}
