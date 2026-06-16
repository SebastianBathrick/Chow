namespace Chow.Syntax
{
    sealed class ReturnStatementNode : Node
    {
        /// <summary>
        /// The expression to be evaluated and returned by the return statement or null.
        /// </summary>
        public Node Expression { get; }

        public ReturnStatementNode(Node expr, int line) : base(line)
        {
            Expression = expr;
        }
    }
}
