namespace Chow.Exceptions
{
    class SubscriptException : RuntimeException
    {
        const string EXCEPTION_ALIAS = "KeyError";

        public string KeyRepr { get; }

        public SubscriptException(string keyRepr, int lineNumber = -1) : base(EXCEPTION_ALIAS, keyRepr, lineNumber)
        {
            KeyRepr = keyRepr;
        }
    }
}
