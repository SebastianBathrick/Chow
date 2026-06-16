namespace Chow.Syntax
{
    sealed class ForStatementNode : Node
    {
        public NameNode Target { get; }

        public Node Iterable { get; }

        public Node Block { get; }

        // TODO: Add compiler and VM support to have branching for loops.
        /// <summary>
        /// Node representing an else clause runs only when the iterable is exhausted normally; a
        /// break statement skips it. This property is null when the source omits the clause.
        /// </summary>
        public Node ElseBranch { get; }

        public ForStatementNode(
            NameNode target,
            Node iterable,
            Node block,
            Node elseBranch,
            int line)
            : base(line)
        {
            Target = target;
            Iterable = iterable;
            Block = block;
            ElseBranch = elseBranch;
        }
    }
}
