namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class ContinueStatementNode : Node
    {
        public ContinueStatementNode(int line) : base(line)
        {
        }

        public override string ToString()
        {
            return "continue";
        }
    }
}
