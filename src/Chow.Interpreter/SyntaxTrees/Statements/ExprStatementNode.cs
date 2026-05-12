namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ExprStatementNode : Node
    {
        public Node Expression { get; }

        public ExprStatementNode(Node expr, int line) : base(line)
        {
            Expression = expr;
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}
