namespace Chow.Ast.Nodes
{
    sealed class SubscriptSliceNode : Node
    {
        public Node Start { get; }

        public Node Stop { get; }

        public Node Step { get; }

        public SubscriptSliceNode(Node start, Node stop, Node step, int line) : base(line)
        {
            Start = start;
            Stop = stop;
            Step = step;
        }

    }
}
