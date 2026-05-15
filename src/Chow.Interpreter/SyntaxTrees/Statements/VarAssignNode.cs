namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class VarAssignNode : Node
    {
        public string Name { get; }

        public Node Expression { get; }

        /// <summary>
        /// How this binding resolves at runtime. Stamped by <see cref="SemanticAnalyzer"/> before
        /// the compiler runs. Defaults to <see cref="ScopeKind.Local"/>.
        /// </summary>
        public ScopeKind Resolution { get; set; }

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