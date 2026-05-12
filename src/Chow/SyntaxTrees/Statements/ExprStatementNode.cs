namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ExprStatementNode : Node
    {
        Node _expr;

        public Node Expression => _expr;

        public ExprStatementNode(Node expr, int line) : base(line)
        {
            _expr = expr;
        }

        public override string ToString()
        {
            return _expr.ToString();
        }
    }
}
