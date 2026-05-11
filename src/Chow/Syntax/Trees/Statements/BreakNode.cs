namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class BreakNode : Node
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
