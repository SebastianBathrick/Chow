namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class VarAssignNode : Node
    {
        public VarAssignNode(string name, Node expr, int line) : base(line)
        {
            Name = name;
            Expression = expr;
        }
        public string Name { get; }

        public Node Expression { get; }

        public override string ToString()
        {
            return $"VariableAssignment({Name}, {Expression}) line={LineNumber}";
        }
    }
}
