namespace Chow.Syntax
{
    sealed class SubscriptNode : Node
    {
        public Node Target { get; }

        public Node Index { get; }

        public SubscriptNode(Node target, Node idx, int line) : base(line)
        {
            Target = target;
            Index = idx;
        }
    }
}
