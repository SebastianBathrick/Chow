namespace Chow.Syntax
{
    /// <summary>Represents the root node of an abstract syntax tree (AST).</summary>
    sealed class ModuleNode : Node
    {
        const int RootNodeLine = 1;

        /// <summary>Node containing all top-level statement and defined function nodes.</summary>
        public Node Block { get; }

        public ModuleNode(Node block) : base(RootNodeLine)
        {
            Block = block;
        }
    }
}
