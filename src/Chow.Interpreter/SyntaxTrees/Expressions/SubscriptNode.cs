namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class SubscriptNode : Node
    {
        public SubscriptNode(Node target, Node index, int line) : base(line)
        {
            Target = target;
            Index = index;
        }
        public Node Target { get; }

        public Node Index { get; }

        public override string ToString()
        {
            var indentedTarget = IndentChildren(Target.ToString());
            var indentedIndex = IndentChildren(Index.ToString());
            return $"[Subscript line={LineNumber}\n{indentedTarget}\n{indentedIndex}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
