namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class WhileStatementNode : Node
    {
        public Node Expr { get; }

        public Node Block { get; }

        public WhileStatementNode(Node expr, Node block, int line) : base(line)
        {
            Expr = expr;
            Block = block;
        }

        public override string ToString()
        {
            return $"while {Expr}\n{{\n{Block}\n}}";
        }
    }
}
