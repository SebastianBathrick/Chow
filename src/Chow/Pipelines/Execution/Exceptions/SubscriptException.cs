namespace Chow.VM
{
    class SubscriptException : RuntimeException
    {
        const string ExceptionAlias = "KeyError";

        public string KeyRepr { get; }

        public SubscriptException(string keyRepr, int lineNumber = -1) : base(ExceptionAlias, keyRepr, lineNumber)
        {
            KeyRepr = keyRepr;
        }
    }
}
