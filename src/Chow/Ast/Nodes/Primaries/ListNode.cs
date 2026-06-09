using System.Collections.Generic;

namespace Chow.Ast
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
