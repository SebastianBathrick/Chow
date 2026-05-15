namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ExprStatementNode : Node
    {
        public ExprStatementNode(Node expr, int line) : base(line)
        {
            Expression = expr;
        }
        public Node Expression { get; }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}
