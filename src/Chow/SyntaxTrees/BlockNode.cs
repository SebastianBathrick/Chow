using System.Collections.Generic;

namespace Chow.Interpreter.SyntaxTrees
{
    class BlockNode : Node
    {
        List<Node> _stmnts;

        public int StatmentCount => _stmnts.Count;

        public IReadOnlyList<Node> Statements => _stmnts;

        public BlockNode(List<Node> stmnts, int line) : base(line)
        {
            _stmnts = stmnts;
        }

        public override string ToString()
        {
            return $"Block({StatmentCount} statements) line={LineNum}";
        }
    }
}
