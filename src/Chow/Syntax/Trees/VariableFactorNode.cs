using System;

namespace Chow.Interpreter.Syntax.Trees
{
    internal class VariableFactorNode : Node
    {
        string _identifier;

        public string Identifier => _identifier;

        public VariableFactorNode(string identifier, int lineNumber)
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

            _identifier = identifier;
        }

        public override string ToString()
        {
            return $"VariableFactor({_identifier}) line={LineNumber}";
        }
    }
}
