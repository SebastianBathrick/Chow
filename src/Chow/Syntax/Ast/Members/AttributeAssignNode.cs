 namespace Chow.Syntax
{
    sealed class AttributeAssignNode : Node
    {
        public Node Target { get; }

        public string AttributeName { get; }

        public Node Expression { get; }

        public AttributeAssignNode(Node target, string attrName, Node expr, int line) : base(line)
        {
            Target = target;
            AttributeName = attrName;
            Expression = expr;
        }

    }
}
