namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class VarAssignNode : Node
    {
        public string Name { get; }

        public Node Expression { get; }

        public VarAssignNode(string name, Node expr, int line) : base(line)
        {
            Name = name;
            Expression = expr;
        }

        public override string ToString()
        {
            return $"VariableAssignment({Name}, {Expression}) line={LineNumber}";
        }
    }
}