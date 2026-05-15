namespace Chow.Interpreter.SyntaxTrees.Subscripts
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

        public override string ToString()
        {
            return $"SubscriptAssign({Target}[{Index}], {Expression}) line={LineNumber}";
        }
    }
}
