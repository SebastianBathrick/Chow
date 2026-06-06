using System.Collections.Generic;
namespace Chow.SyntaxTrees
{
    sealed class TopLevelNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        public List<Node> Statements { get; }

        public TopLevelNode(List<Node> statements) : base(ROOT_NODE_LINE)
        {
            Statements = statements;
        }
    }
}
