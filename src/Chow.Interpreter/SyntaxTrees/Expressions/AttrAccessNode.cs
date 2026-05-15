namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class AttrAccessNode : Node
    {
        public AttrAccessNode(Node target, string attrName, int line) : base(line)
        {
            Target = target;
            AttrName = attrName;
        }
        public Node Target { get; }

        public string AttrName { get; }

        public override string ToString()
        {
            var indentedTarget = IndentChildren(Target.ToString());
            return $"[AttrAccess line={LineNumber} attr={AttrName}\n{indentedTarget}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
