namespace Chow.SyntaxTrees.Subscripts
{
    sealed class SubscriptNode : Node
    {
        public Node Target { get; }

        public Node Index { get; }

        public SubscriptNode(Node target, Node index, int line) : base(line)
        {
            Target = target;
            Index = index;
        }

    }
}
