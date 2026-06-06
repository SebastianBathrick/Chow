namespace Chow.Ast.Nodes
{
    sealed class SubscriptAssignNode : Node
    {
        public Node Target { get; }

        public Node Index { get; }

        public Node Expression { get; }

        public SubscriptAssignNode(Node target, Node index, Node expr, int line) : base(line)
        {
            Target = target;
            Index = index;
            Expression = expr;
        }

    }
}
