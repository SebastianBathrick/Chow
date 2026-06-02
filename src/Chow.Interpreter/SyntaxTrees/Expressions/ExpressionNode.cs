using Chow.Interpreter.DataTypes;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    sealed class ExpressionNode : Node
    {
        public ExpressionOperator Operator { get; }

        public Node Left { get; }

        public Node Right { get; }

        public ExpressionNode(ExpressionOperator opType, Node left, Node right, int line) : base(line)
        {
            Operator = opType;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Initializes a node representing a negated expression (using unary minus or logical not).
        /// </summary>
        public ExpressionNode(ExpressionOperator opType, Node left, int line) : base(line)
        {
            Operator = opType;
            Left = left;
            Right = null;
        }

    }
}
