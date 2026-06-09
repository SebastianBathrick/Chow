using System.Collections.Generic;
namespace Chow.Ast
{
    sealed class DictionaryNode : Node
    {
        public List<Node> Keys { get; }

        public List<Node> Values { get; }

        public DictionaryNode(List<Node> keys, List<Node> values, int line) : base(line)
        {
            Keys = keys;
            Values = values;
        }

    }
}
