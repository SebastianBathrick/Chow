namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class BranchStmntNode : Node
    {
        readonly Node _expr;
        readonly Node _block;
        readonly Node _branch;

        public Node Branch => _branch;
        public Node Expr => _expr;
        public Node Block => _block;

        public bool IsElse => _expr == null;

        public BranchStmntNode(Node expr, Node block, Node branch, int line) : base(line)
        {
            _expr = expr;
            _block = block;
            _branch = branch;
        }


        public override string ToString()
        {
            string result;

            if (IsElse)
            {
                result = $"else\n{{\n{_block}\n}}";
            }
            else
            {
                result = $"elif {_expr}\n{{\n{_block}\n}}";
            }

            if (_branch != null)
            {
                result += $"\n{_branch}";
            }

            return result;
        }
    }
}
