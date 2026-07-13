namespace Chow.Interpreter.Exceptions
{
    class AttributeException : RuntimeException
    {
        const string ExceptionAlias = "AttributeError";

        public string TypeName { get; }

        public string AttrName { get; }

        public AttributeException(string typeName, string attrName, int lineNumber)
            : base(ExceptionAlias, $"'{typeName}' object has no attribute '{attrName}'", lineNumber)
        {
            TypeName = typeName;
            AttrName = attrName;
        }

        public AttributeException(string typeName, string attrName, int lineNumber, string message)
            : base(ExceptionAlias, message, lineNumber)
        {
            TypeName = typeName;
            AttrName = attrName;
        }
    }
}
