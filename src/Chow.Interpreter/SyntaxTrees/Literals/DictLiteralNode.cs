using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Literals
{
    sealed class DictLiteralNode : Node
    {
        public List<Node> Keys { get; }

        public List<Node> Values { get; }

        public DictLiteralNode(List<Node> keys, List<Node> values, int line) : base(line)
        {
            Keys = keys;
            Values = values;
        }

    }
}
