using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    class TreeRootNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        public TreeRootNode(List<Node> statements) : base(ROOT_NODE_LINE)
        {
            Statements = statements;
        }

        public List<Node> Statements { get; }

        public override string ToString()
        {
            // TODO: Write logic for this
            return string.Empty;
        }
    }
}
