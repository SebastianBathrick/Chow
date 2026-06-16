namespace Chow.Syntax
{
    sealed class AttributeAccessNode : Node
    {
        public Node Target { get; }

        public string Name { get; }

        public AttributeAccessNode(Node target, string attrName, int line) : base(line)
        {
            Target = target;
            Name = attrName;
        }

    }
}
