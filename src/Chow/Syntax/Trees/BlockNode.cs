using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Syntax.Trees
{
    internal class BlockNode : Node
    {
        List<Node> _statements;

        public int Count => _statements.Count;

        public IReadOnlyList<Node> Statements => _statements;

        public BlockNode(List<Node> statements, int lineNumber) : base(lineNumber)
        {
            _statements = statements;
        }

        public override string ToString()
        {
            return $"Block({_statements.Count} statements) line={LineNumber}";
        }
    }
}
