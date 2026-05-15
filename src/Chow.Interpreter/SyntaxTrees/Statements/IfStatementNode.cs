namespace Chow.Interpreter.SyntaxTrees.Statements
{
    sealed class IfStatementNode : Node
    {
        public Node Expression { get; }

        public Node Block { get; }

        public Node Branch { get; }

        public IfStatementNode(Node expr, Node block, Node branch, int line) : base(line)
        {
            Expression = expr;
            Block = block;
            Branch = branch;
        }

        public override string ToString()
        {
            var result = $"if {Expression}\n{{\n{Block}\n}}";

            if (Branch != null)
            {
                result += $"\n{Branch}";
            }

            return result;
        }
    }
}
