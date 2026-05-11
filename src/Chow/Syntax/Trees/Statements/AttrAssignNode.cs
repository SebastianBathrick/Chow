namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class AttrAssignNode : Node
    {
        Node _target;
        string _attrName;
        Node _expr;

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
            return $"AttrAssign({_target}.{_attrName}, {_expr}) line={LineNum}";
        }
    }
}
