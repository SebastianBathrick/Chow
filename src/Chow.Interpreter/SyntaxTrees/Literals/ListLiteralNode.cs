using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Literals
{
    sealed class ListLiteralNode : Node
    {
        public List<Node> Elements { get; }

        public ListLiteralNode(List<Node> elements, int line) : base(line)
        {
            Elements = elements;
        }

    }
}
