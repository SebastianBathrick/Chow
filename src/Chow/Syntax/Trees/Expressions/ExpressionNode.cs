using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax.Trees.Expressions
{
    internal class ExpressionNode : Node
    {
        ExpressionOperator _operator;
        Node _leftOperand;
        Node _rightOperand;

        public ExpressionOperator Operator => _operator;
        public Node Left => _leftOperand;
        public Node Right => _rightOperand;

        public ExpressionNode(ExpressionOperator operatorType, Node leftOperand, Node rightOperand, int lineNumber)
            : base(lineNumber)
        {
            _operator = operatorType;
            _leftOperand = leftOperand ?? throw new ArgumentNullException(nameof(leftOperand));
            _rightOperand = rightOperand;
        }

        public ExpressionNode(ExpressionOperator operatorType, Node operand, int lineNumber)
            : base(lineNumber)
        {
            if (operatorType != ExpressionOperator.Negate)
                throw new ArgumentException("Unary constructor is only valid for Negate.", nameof(operatorType));
            _operator = operatorType;
            _leftOperand = operand ?? throw new ArgumentNullException(nameof(operand));
        }

        public override string ToString()
        {
            string indentedLeft = IndentChildren(_leftOperand.ToString());

            if (_rightOperand == null)
            {
                return $"[{_operator} line={LineNumber}\n{indentedLeft}\n]";
            }

            string indentedRight = IndentChildren(_rightOperand.ToString());
            return $"[{_operator} line={LineNumber}\n{indentedLeft}\n{indentedRight}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
