namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class SubscriptAssignNode : Node
    {
        readonly Node _target;
        readonly Node _index;
        readonly Node _expr;

        public Node Target => _target;
        public Node Index => _index;
        public Node Expression => _expr;

        public SubscriptAssignNode(Node target, Node index, Node expr, int line) : base(line)
        {
            _target = target;
            _index = index;
            _expr = expr;
        }

        public override string ToString()
        {
            return $"SubscriptAssign({_target}[{_index}], {_expr}) line={LineNumber}";
        }
    }
}
