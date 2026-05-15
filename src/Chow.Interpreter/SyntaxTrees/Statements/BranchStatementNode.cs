namespace Chow.Interpreter.SyntaxTrees.Statements
{
    sealed class BranchStatementNode : Node
    {
        public Node Branch { get; }

        public Node Expression { get; }

        public Node Block { get; }

        public bool IsElse => Expression == null;

        public BranchStatementNode(Node expr, Node block, Node branch, int line) : base(line)
        {
            Expression = expr;
            Block = block;
            Branch = branch;
        }

        public override string ToString()
        {
            string result;

            if (IsElse)
            {
                result = $"else\n{{\n{Block}\n}}";
            }
            else
            {
                result = $"elif {Expression}\n{{\n{Block}\n}}";
            }

            if (Branch != null)
            {
                result += $"\n{Branch}";
            }

            return result;
        }
    }
}
