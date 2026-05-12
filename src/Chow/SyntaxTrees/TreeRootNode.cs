using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    class TreeRootNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        readonly List<Node> _statements;

        public List<Node> Statements => _statements;
        
        public TreeRootNode(List<Node> statements) : base(ROOT_NODE_LINE)
        {
            _statements = statements;
        }

        public override string ToString()
        {
            // TODO: Write logic for this
            return string.Empty;
        }
    }
}
