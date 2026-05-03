using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
{
    internal class IdentifierNode : Node
    {
        string _name;

        public string Name => _name;

        public IdentifierNode(string name, int lineNumber)
            : base(lineNumber)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Identifier name cannot be empty.", nameof(name));
            }

            _name = name;
        }

        public override string ToString()
        {
            return $"{_name} line={LineNumber}";
        }
    }
}
