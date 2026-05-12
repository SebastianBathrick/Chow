namespace Chow.Interpreter.SyntaxTrees.Statements
{
    internal class ContinueNode : Node
    {
        public ContinueNode(int line) : base(line)
        {
        }

        public override string ToString()
        {
            return "continue";
        }
    }
}
