using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    class TreeRootNode : Node
    {
        const int ROOT_NODE_LINE = 1;

        List<Node> _stmnts;

        public List<Node> Stmnts => _stmnts;
        public TreeRootNode(List<Node> stmnts) : base(ROOT_NODE_LINE)
        {
            _stmnts = stmnts;
        }

        public override string ToString()
        {
            // TODO: Write logic for this
            return string.Empty;
        }
    }
}
