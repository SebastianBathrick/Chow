using Chow.Utility;

namespace Chow.Syntax
{
    /// <summary>
    /// Represents an arithmetic, logical, or comparison expression that has an operator and one or
    /// two operands.
    /// </summary>
    sealed class ExpressionNode : Node
    {
        public Operator Operator { get; }

        /// <summary>
        /// The left operand of a binary expression or the operand of a unary expression.
        /// </summary>
        public Node Left { get; }

        /// <summary>The right operand of a binary expression or null</summary>
        public Node Right { get; }

        /// <summary>Initializes a node representing a binary expression.</summary>
        public ExpressionNode(Operator operatorType, Node left, Node right, int line)
            : base(line)
        {
            Operator = operatorType;
            Left = left;
            Right = right;
        }

        /// <summary>Initializes a node representing a unary expression.</summary>
        public ExpressionNode(Operator operatorType, Node left, int line) : base(line)
        {
            Operator = operatorType;
            Left = left;
            Right = null;
        }
    }
}
