namespace Chow.Syntax
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
    }
}
