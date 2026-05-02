using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
{
    internal class ExpressionOperationNode : Node
    {
        public enum OperatorType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Negate
        }

        OperatorType _operator;
        Node _leftOperand;
        Node _rightOperand;

        public OperatorType Operator => _operator;
        public Node Left => _leftOperand;
        public Node Right => _rightOperand;

        public ExpressionOperationNode(OperatorType operatorType, Node leftOperand, Node rightOperand)
        {
            _operator = operatorType;
            _leftOperand = leftOperand ?? throw new ArgumentNullException(nameof(leftOperand));
            _rightOperand = rightOperand;
        }

        public ExpressionOperationNode(OperatorType operatorType, Node operand)
        {
            if (operatorType != OperatorType.Negate)
                throw new ArgumentException("Unary constructor is only valid for Negate.", nameof(operatorType));
            _operator = operatorType;
            _leftOperand = operand ?? throw new ArgumentNullException(nameof(operand));
        }

        public override string ToString()
        {
            string indentedLeft = IndentChildren(_leftOperand.ToString());

            if (_rightOperand == null)
            {
                return $"[{_operator}\n{indentedLeft}\n]";
            }

            string indentedRight = IndentChildren(_rightOperand.ToString());
            return $"[{_operator}\n{indentedLeft}\n{indentedRight}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
