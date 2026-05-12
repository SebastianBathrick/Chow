using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class ListLiteralNode : Node
    {
        public List<Node> Elements { get; }

        public ListLiteralNode(List<Node> elements, int line) : base(line)
        {
            Elements = elements;
        }

        public override string ToString()
        {
            if (Elements.Count == 0)
            {
                return $"[List line={LineNumber}]";
            }

            var body = string.Empty;
            foreach (var element in Elements)
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
