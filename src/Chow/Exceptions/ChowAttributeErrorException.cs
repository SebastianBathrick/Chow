namespace Chow.Interpreter.Exceptions
{
    internal class ChowAttributeErrorException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "AttributeError";

        private string _typeName;
        private string _attrName;

        public string TypeName => _typeName;
        public string AttrName => _attrName;

        public ChowAttributeErrorException(string typeName, string attrName, int lineNumber)
            : base(EXCEPTION_ALIAS, $"'{typeName}' object has no attribute '{attrName}'", lineNumber)
        {
            _typeName = typeName;
            _attrName = attrName;
        }
    }
}
