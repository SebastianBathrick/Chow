using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
{
    internal class BlockNode : Node
    {
        List<Node> _statements;

        public int Count => _statements.Count;

        public Node this[int index] => _statements[index];

        public BlockNode(List<Node> statements, int lineNumber)
            : base(lineNumber)
        {
            if (statements == null)
            {
                throw new ArgumentNullException(nameof(statements));
            }

            if (statements.Count == 0)
            {
                throw new ArgumentException("A block must contain at least one statement.", nameof(statements));
            }

            _statements = statements;
        }

        public override string ToString()
        {
            return $"Block({_statements.Count} statements) line={LineNumber}";
        }
    }
}
