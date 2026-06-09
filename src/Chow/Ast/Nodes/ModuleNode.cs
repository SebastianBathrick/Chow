namespace Chow.Ast
{
    sealed class ModuleNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        public Node Block { get; }

        public ModuleNode(Node block) : base(ROOT_NODE_LINE)
        {
            Block = block;
        }
    }
}
