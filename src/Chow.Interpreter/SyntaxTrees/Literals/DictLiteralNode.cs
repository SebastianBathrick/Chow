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

        public override string ToString()
        {
            if (Keys.Count == 0)
            {
                return $"[Dict line={LineNumber}]";
            }

            var body = string.Empty;
            for (var i = 0; i < Keys.Count; i++)
            {
                body += "\n" + IndentChildren(Keys[i].ToString());
                body += "\n" + IndentChildren(Values[i].ToString());
            }
            return $"[Dict line={LineNumber}{body}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
