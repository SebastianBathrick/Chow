using System.Collections.Generic;
namespace Chow.SyntaxTrees.Literals
{
    sealed class LiteralListNode : Node
    {
        public List<Node> Elements { get; }

        public LiteralListNode(List<Node> elements, int line) : base(line)
        {
            Elements = elements;
        }

    }
}
