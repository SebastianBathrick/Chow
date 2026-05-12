namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class BreakNode : Node
    {
        public BreakNode(int line) : base(line)
        {
        }

        public override string ToString()
        {
            return "break";
        }
    }
}
