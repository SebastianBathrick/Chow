using System.Collections.Generic;
namespace Chow.Ast.Nodes
{
    sealed class ListNode : Node
    {
        public List<Node> Elements { get; }

        public ListNode(List<Node> elements, int line) : base(line)
        {
            Elements = elements;
        }

    }
}
