namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class AttrAccessNode : Node
    {
        readonly Node _target;
        readonly string _attrName;

        public Node Target => _target;
        public string AttrName => _attrName;

        public AttrAccessNode(Node target, string attrName, int line) : base(line)
        {
            _target = target;
            _attrName = attrName;
        }

        public override string ToString()
        {
            var indentedTarget = IndentChildren(_target.ToString());
            return $"[AttrAccess line={LineNumber} attr={_attrName}\n{indentedTarget}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
