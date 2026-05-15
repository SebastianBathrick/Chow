namespace Chow.Interpreter.SyntaxTrees.Attributes
{
    class AttributeAssignNode : Node
    {
        public Node Target { get; }

        public string AttrName { get; }

        public Node Expression { get; }

        public AttributeAssignNode(Node target, string attrName, Node expr, int line) : base(line)
        {
            Target = target;
            AttrName = attrName;
            Expression = expr;
        }

        public override string ToString()
        {
            return $"AttrAssign({Target}.{AttrName}, {Expression}) line={LineNumber}";
        }
    }
}
