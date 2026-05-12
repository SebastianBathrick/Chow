using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class DictLiteralNode : Node
    {
        List<Node> _keys;
        List<Node> _values;

        public List<Node> Keys => _keys;
        public List<Node> Values => _values;

        public DictLiteralNode(List<Node> keys, List<Node> values, int line) : base(line)
        {
            _keys = keys;
            _values = values;
        }

        public override string ToString()
        {
            if (_keys.Count == 0)
            {
                return $"[Dict line={LineNum}]";
            }

            string body = string.Empty;
            for (int i = 0; i < _keys.Count; i++)
            {
                body += "\n" + IndentChildren(_keys[i].ToString());
                body += "\n" + IndentChildren(_values[i].ToString());
            }
            return $"[Dict line={LineNum}{body}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
