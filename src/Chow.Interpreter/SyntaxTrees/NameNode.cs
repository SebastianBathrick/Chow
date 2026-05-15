using Chow.Interpreter.SyntaxTrees.Scope;
namespace Chow.Interpreter.SyntaxTrees
{
    class NameNode : Node
    {
        public string Name { get; }

        /// <summary>
        /// How this read resolves at runtime. Stamped by <see cref="SemanticAnalyzer"/> before
        /// the compiler runs. Defaults to <see cref="ScopeType.Local"/>.
        /// </summary>
        public ScopeType Resolution { get; set; }

        public NameNode(string name, int line) : base(line)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"VariableFactor({Name}) line={LineNumber}";
        }
    }
}
