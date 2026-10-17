using System.Collections.Generic;

namespace Chow.Syntax
{
    /// <summary>A set display such as <c>{1, 2}</c>. An empty <c>{}</c> is a dict, never a set.</summary>
    sealed class SetNode : Node
    {
        public List<Node> Elements { get; }

        public SetNode(List<Node> elements, int line) : base(line)
        {
            Elements = elements;
        }
    }
}
