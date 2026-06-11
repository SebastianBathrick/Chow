namespace Chow.Ast
{
    sealed class AttributeAccessNode : Node
    {
        public Node Target { get; }

        public string AttributeName { get; }

        public AttributeAccessNode(Node target, string attrName, int line) : base(line)
        {
            Target = target;
            AttributeName = attrName;
        }

    }
}
