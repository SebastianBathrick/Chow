namespace Chow.Ast.Nodes
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

    }
}
