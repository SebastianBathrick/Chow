namespace Chow.Ast
{
    /// <summary>Represents the root node of an abstract syntax tree (AST).</summary>
    sealed class ModuleNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        /// <summary>Node containing all top-level statement and defined function nodes.</summary>
        public Node Block { get; }

        public ModuleNode(Node block) : base(ROOT_NODE_LINE)
        {
            Block = block;
        }
    }
}
