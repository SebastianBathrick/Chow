namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class BreakStatementNode : Node
    {
        public BreakStatementNode(int line) : base(line)
        {
        }

        public override string ToString()
        {
            return "break";
        }
    }
}
