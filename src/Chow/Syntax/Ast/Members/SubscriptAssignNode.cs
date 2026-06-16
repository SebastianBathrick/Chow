namespace Chow.Syntax
{
    sealed class SubscriptAssignNode : Node
    {
        public Node Target { get; }

        public Node Index { get; }

        public Node Expression { get; }

        public SubscriptAssignNode(Node target, Node idx, Node expr, int line) : base(line)
        {
            Target = target;
            Index = idx;
            Expression = expr;
        }

    }
}
