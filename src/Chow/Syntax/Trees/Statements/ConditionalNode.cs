namespace Chow.Interpreter.Syntax.Trees.Statements
{
    abstract class ConditionalNode
    {
        Node _block;
        Node _expression;

        protected ConditionalNode(Node block, Node expression)
        {
            _block = block;

            if (expression == null)
            {
                _expression = null;
            }
        }
    }
}
