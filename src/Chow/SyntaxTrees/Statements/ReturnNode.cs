namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ReturnNode : Node
    {
        readonly Node _expr;

        /// <summary>
        /// Node representing the expression to be evaluated and returned by the return statement or null.
        /// </summary>
        public Node Expression => _expr;

        /// <param name="expr">Node representing the expression to be evaluated and returned by the return statement or null.</param>
        /// <param name="line">The line number of the return statement.</param>
        public ReturnNode(Node expr, int line) : base(line)
        {
            _expr = expr;
        }

        public override string ToString()
        {
            return $"return {_expr}";
        }
    }
}
