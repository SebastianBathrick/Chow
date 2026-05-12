namespace Chow.Interpreter.Exceptions
{
    class AttributeException : ChowRuntimeException
    {
        const string EXCEPTION_ALIAS = "AttributeError";

        public string TypeName { get; }

        public string AttrName { get; }

        public AttributeException(string typeName, string attrName, int lineNumber)
            : base(EXCEPTION_ALIAS, $"'{typeName}' object has no attribute '{attrName}'", lineNumber)
        {
            TypeName = typeName;
            AttrName = attrName;
        }

        public AttributeException(string typeName, string attrName, int lineNumber, string message)
            : base(EXCEPTION_ALIAS, message, lineNumber)
        {
            TypeName = typeName;
            AttrName = attrName;
        }
    }
}
