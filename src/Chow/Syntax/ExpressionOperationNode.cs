using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
{
    internal class ExpressionOperationNode : Node
    {
        public  enum OperatorType
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }

        OperatorType _operator;
        Node _leftOperand;
        Node _rightOperand;

        public ExpressionOperationNode(OperatorType operatorType, Node leftOperand, Node rightOperand)
        {
            _operator = operatorType;
            _leftOperand = leftOperand ?? throw new ArgumentNullException(nameof(leftOperand));
            _rightOperand = rightOperand ?? throw new ArgumentNullException(nameof(rightOperand));
        }
    }
}
