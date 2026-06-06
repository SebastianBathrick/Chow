using System.Collections.Generic;
namespace Chow.SyntaxTrees
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
