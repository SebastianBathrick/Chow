namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class AttrAssignNode : Node
    {
        public AttrAssignNode(Node target, string attrName, Node expr, int line) : base(line)
        {
            Target = target;
            AttrName = attrName;
            Expression = expr;
        }
        public Node Target { get; }

        public string AttrName { get; }

        public Node Expression { get; }

        public override string ToString()
        {
            return $"AttrAssign({Target}.{AttrName}, {Expression}) line={LineNumber}";
        }
    }
}
