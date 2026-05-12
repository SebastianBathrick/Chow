namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class BranchStmntNode : Node
    {
        public Node Branch { get; }

        public Node Expr { get; }

        public Node Block { get; }

        public bool IsElse => Expr == null;

        public BranchStmntNode(Node expr, Node block, Node branch, int line) : base(line)
        {
            Expr = expr;
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
                result = $"elif {Expr}\n{{\n{Block}\n}}";
            }

            if (Branch != null)
            {
                result += $"\n{Branch}";
            }

            return result;
        }
    }
}
