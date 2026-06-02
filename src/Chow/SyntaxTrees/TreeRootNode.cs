using System.Collections.Generic;
namespace Chow.SyntaxTrees
{
    sealed class TreeRootNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        public List<Node> Statements { get; }

        public TreeRootNode(List<Node> statements) : base(ROOT_NODE_LINE)
        {
            Statements = statements;
        }

    }
}
