namespace Chow.Interpreter.SyntaxTrees.Statements
{
    sealed class ForStatementNode : Node
    {
        public NameNode Target { get; }

        public Node Iterable { get; }

        public Node Block { get; }

        // The else-clause runs only when the iterable exhausts naturally; `break` skips it.
        // Null when source omits the clause.
        public BranchStatementNode ElseBranch { get; }

        public ForStatementNode(NameNode target, Node iterable, Node block, BranchStatementNode elseBranch, int line) : base(line)
        {
            Target = target;
            Iterable = iterable;
            Block = block;
            ElseBranch = elseBranch;
        }

        public override string ToString()
        {
            var result = $"for {Target} in {Iterable}\n{{\n{Block}\n}}";

            if (ElseBranch != null)
            {
                result += $"\n{ElseBranch}";
            }

            return result;
        }
    }
}
