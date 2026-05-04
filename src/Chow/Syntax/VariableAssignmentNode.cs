using Chow.Interpreter.Syntax.Trees;
using System;

namespace Chow.Interpreter.Syntax
{
    internal class VariableAssignmentNode : Node
    {
        string _identifier;
        Node _expression;

        public string Identifier => _identifier;
        public Node Expression => _expression;

        public VariableAssignmentNode(string identifier, Node expression, int lineNumber)
            : base(lineNumber)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (identifier.Length == 0)
            {
                throw new ArgumentException("Identifier name cannot be empty.", nameof(identifier));
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
