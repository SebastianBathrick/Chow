using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class ListLiteralNode : Node
    {
        List<Node> _elements;

        public List<Node> Elements => _elements;

        public ListLiteralNode(List<Node> elements, int line) : base(line)
        {
            _elements = elements;
        }

        public override string ToString()
        {
            if (_elements.Count == 0)
            {
                return $"[List line={LineNum}]";
            }

            var body = string.Empty;
            for (var i = 0; i < _elements.Count; i++)
            {
                body += "\n" + IndentChildren(_elements[i].ToString());
            }
            return $"[List line={LineNum}{body}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
