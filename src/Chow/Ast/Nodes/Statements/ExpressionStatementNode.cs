namespace Chow.Ast.Nodes
{
    sealed class ExpressionStatementNode : Node
    {
        public Node Expression { get; }

        public ExpressionStatementNode(Node expr, int line) : base(line)
        {
            Expression = expr;
        }

    }
}
