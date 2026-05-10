namespace Chow.Interpreter.Syntax.Trees.Statements
{
    abstract class ConditionStmntNode : Node
    {
        Node _block;
        Node _expr;

        protected ConditionStmntNode(Node block, Node expr, int line) : base(line)
        {
            _block = block;
            _expr = expr;
        }
    }
}
