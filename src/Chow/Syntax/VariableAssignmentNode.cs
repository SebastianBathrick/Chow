using Chow.Syntax.Trees;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
{
    internal class VariableAssignmentNode : Node
    {
        Node _identifier;
        Node _expression;

        public Node Identifier => _identifier;
        public Node Expression => _expression;

        public VariableAssignmentNode(Node identifier, Node expression, int lineNumber)
            : base(lineNumber)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            _identifier = identifier;
            _expression = expression;
        }

        public override string ToString()
        {
            return $"VariableAssignment({_identifier}, {_expression}) line={LineNumber}";
        }
    }
}
