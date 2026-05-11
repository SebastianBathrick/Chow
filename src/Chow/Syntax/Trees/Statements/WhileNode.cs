namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class WhileNode : Node
    {
        Node _expr;
        Node _block;

        public Node Expr => _expr;
        public Node Block => _block;

        public WhileNode(Node expr, Node block, int line) : base(line)
        {
            _expr = expr;
            _block = block;
        }

        public override string ToString()
        {
            return $"while {_expr}\n{{\n{_block}\n}}";
        }
    }
}
