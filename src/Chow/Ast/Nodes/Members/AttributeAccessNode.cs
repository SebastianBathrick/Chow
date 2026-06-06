namespace Chow.Ast.Nodes
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

    }
}
