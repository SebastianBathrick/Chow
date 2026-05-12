namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class VarAssignNode : Node
    {
        readonly string _name;
        readonly Node _expr;

        public string Name => _name;
        public Node Expression => _expr;

        public VarAssignNode(string name, Node expr, int line) : base(line)
        {
            _name = name;
            _expr = expr;
        }

        public override string ToString()
        {
            return $"VariableAssignment({_name}, {_expr}) line={LineNumber}";
        }
    }
}