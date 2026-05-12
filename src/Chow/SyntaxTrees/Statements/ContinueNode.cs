namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ContinueNode : Node
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
