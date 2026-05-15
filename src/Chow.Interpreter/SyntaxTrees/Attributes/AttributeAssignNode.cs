namespace Chow.Interpreter.SyntaxTrees.Attributes
{
    sealed class AttributeAssignNode : Node
    {
        public Node Target { get; }

        public string AttributeName { get; }

        public Node Expression { get; }

        public AttributeAssignNode(Node target, string attributeName, Node expr, int line) : base(line)
        {
            Target = target;
            AttributeName = attributeName;
            Expression = expr;
        }

        public override string ToString()
        {
            return $"AttrAssign({Target}.{AttributeName}, {Expression}) line={LineNumber}";
        }
    }
}
