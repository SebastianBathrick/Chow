namespace Chow.Interpreter.SyntaxTrees.Attributes
{
    sealed class AttributeAccessNode : Node
    {
        public Node Target { get; }

        public string AttributeName { get; }

        public AttributeAccessNode(Node target, string attributeName, int line) : base(line)
        {
            Target = target;
            AttributeName = attributeName;
        }

        public override string ToString()
        {
            var indentedTarget = IndentChildren(Target.ToString());
            return $"[AttrAccess line={LineNumber} attr={AttributeName}\n{indentedTarget}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
