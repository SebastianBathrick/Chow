namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class SubscriptNode : Node
    {
        Node _target;
        Node _index;

        public Node Target => _target;
        public Node Index => _index;

        public SubscriptNode(Node target, Node index, int line) : base(line)
        {
            _target = target;
            _index = index;
        }

        public override string ToString()
        {
            var indentedTarget = IndentChildren(_target.ToString());
            var indentedIndex = IndentChildren(_index.ToString());
            return $"[Subscript line={LineNum}\n{indentedTarget}\n{indentedIndex}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
