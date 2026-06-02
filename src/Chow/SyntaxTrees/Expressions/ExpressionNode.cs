using Chow.DataTypes;
namespace Chow.SyntaxTrees.Expressions
{
    sealed class ExpressionNode : Node
    {
        public ExpressionOp Op { get; }

        public Node Left { get; }

        public Node Right { get; }

        public ExpressionNode(ExpressionOp opType, Node left, Node right, int line) : base(line)
        {
            Op = opType;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Initializes a node representing a negated expression (using unary minus or logical not).
        /// </summary>
        public ExpressionNode(ExpressionOp opType, Node left, int line) : base(line)
        {
            Op = opType;
            Left = left;
            Right = null;
        }

    }
}
