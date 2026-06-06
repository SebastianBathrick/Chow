using Chow.DataTypes;
namespace Chow.Ast.Nodes
{
    sealed class ExpressionNode : Node
    {
        public ExpressionOperator Operator { get; }

        public Node Left { get; }

        public Node Right { get; }

        public ExpressionNode(ExpressionOperator operatorType, Node left, Node right, int line) : base(line)
        {
            Operator = operatorType;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Initializes a node representing a negated expression (using unary minus or logical not).
        /// </summary>
        public ExpressionNode(ExpressionOperator operatorType, Node left, int line) : base(line)
        {
            Operator = operatorType;
            Left = left;
            Right = null;
        }

    }
}
