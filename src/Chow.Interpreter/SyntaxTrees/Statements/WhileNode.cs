namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class WhileNode : Node
    {
        public WhileNode(Node expr, Node block, int line) : base(line)
        {
            Expr = expr;
            Block = block;
        }
        public Node Expr { get; }

        public Node Block { get; }

        public override string ToString()
        {
            return $"while {Expr}\n{{\n{Block}\n}}";
        }
    }
}
