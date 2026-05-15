namespace Chow.Interpreter.SyntaxTrees.Subscripts
{
    sealed class SubscriptNode : Node
    {
        public Node Target { get; }

        public Node Index { get; }

        public SubscriptNode(Node target, Node index, int line) : base(line)
        {
            Target = target;
            Index = index;
        }

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
