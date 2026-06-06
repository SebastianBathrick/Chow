using System.Collections.Generic;
namespace Chow.SyntaxTrees.Literals
{
    sealed class LiteralDictNode : Node
    {
        public List<Node> Keys { get; }

        public List<Node> Values { get; }

        public LiteralDictNode(List<Node> keys, List<Node> values, int line) : base(line)
        {
            Keys = keys;
            Values = values;
        }

    }
}
