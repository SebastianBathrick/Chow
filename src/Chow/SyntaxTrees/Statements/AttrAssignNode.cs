namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class AttrAssignNode : Node
    {
        readonly Node _target;
        readonly string _attrName;
        readonly Node _expr;

        public Node Target => _target;
        public string AttrName => _attrName;
        public Node Expression => _expr;

        public AttrAssignNode(Node target, string attrName, Node expr, int line) : base(line)
        {
            _target = target;
            _attrName = attrName;
            _expr = expr;
        }

        public override string ToString()
        {
            return $"AttrAssign({_target}.{_attrName}, {_expr}) line={LineNumber}";
        }
    }
}
