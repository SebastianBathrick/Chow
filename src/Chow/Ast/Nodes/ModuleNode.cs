namespace Chow.Ast
{
    /// <summary>Represents the root node of an abstract syntax tree (AST).</summary>
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
