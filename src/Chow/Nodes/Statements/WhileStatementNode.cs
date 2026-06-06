namespace Chow.SyntaxTrees.Statements
{
    sealed class WhileStatementNode : Node
    {
        public Node Expression { get; }

        public Node Block { get; }

        public WhileStatementNode(Node expr, Node block, int line) : base(line)
        {
            Expression = expr;
            Block = block;
        }

    }
}
