namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class IfNode : Node
    {
        public Node Expr { get; }

        public Node Block { get; }

        public Node Branch { get; }

        public IfNode(Node expr, Node block, Node branch, int line) : base(line)
        {
            Expr = expr;
            Block = block;
            Branch = branch;
        }

        public override string ToString()
        {
            var result = $"if {Expr}\n{{\n{Block}\n}}";

            if (Branch != null)
            {
                result += $"\n{Branch}";
            }

            return result;
        }
    }
}
