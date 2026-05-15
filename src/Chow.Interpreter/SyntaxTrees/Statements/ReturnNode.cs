namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ReturnNode : Node
    {
        /// <summary>
        /// Node representing the expression to be evaluated and returned by the return statement or null.
        /// </summary>
        public Node Expression { get; }

        /// <param name="expr">Node representing the expression to be evaluated and returned by the return statement or null.</param>
        /// <param name="line">The line number of the return statement.</param>
        public ReturnNode(Node expr, int line) : base(line)
        {
            Expression = expr;
        }

        public override string ToString()
        {
            return $"return {Expression}";
        }
    }
}
