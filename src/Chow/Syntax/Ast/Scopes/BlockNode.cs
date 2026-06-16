using System.Collections.Generic;

namespace Chow.Syntax
{
    sealed class BlockNode : Node
    {
        public List<Node> Statements { get; }

        public BlockNode(List<Node> statements, int line) : base(line)
        {
            Statements = statements;
        }

    }
}
