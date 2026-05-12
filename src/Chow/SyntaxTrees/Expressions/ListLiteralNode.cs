using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class ListLiteralNode : Node
    {
        readonly List<Node> _elements;

        public List<Node> Elements => _elements;

        public ListLiteralNode(List<Node> elements, int line) : base(line)
        {
            _elements = elements;
        }

        public override string ToString()
        {
            if (_elements.Count == 0)
            {
                return $"[List line={LineNumber}]";
            }

            var body = string.Empty;
            foreach (var element in _elements)
            {
                body += "\n" + IndentChildren(element.ToString());
            }
            return $"[List line={LineNumber}{body}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
