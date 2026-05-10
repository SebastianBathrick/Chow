namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class IfNode : Node
    {
        Node _expr;
        Node _block;
        Node _branch;

        public Node Expr => _expr;
        public Node Block => _block;
        public Node Branch => _branch;

        public IfNode(Node expr, Node block, Node branch, int line) : base(line)
        {
            _expr = expr;
            _block = block;
            _branch = branch;
        }

        public override string ToString()
        {
            string result = $"if {_expr}\n{{\n{_block}\n}}";

            if (_branch != null)
            {
                result += $"\n{_branch}";
            }

            return result;
        }
    }
}
