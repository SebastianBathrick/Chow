namespace Chow.Interpreter.SyntaxTrees.Statements
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

        public override string ToString()
        {
            return $"while {Expression}\n{{\n{Block}\n}}";
        }
    }
}
