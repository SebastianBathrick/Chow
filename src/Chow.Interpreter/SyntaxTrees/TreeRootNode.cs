using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    sealed class TreeRootNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        public List<Node> Statements { get; }

        public TreeRootNode(List<Node> statements) : base(ROOT_NODE_LINE)
        {
            Statements = statements;
        }

        public override string ToString()
        {
            // TODO: Write logic for this
            return string.Empty;
        }
    }
}
