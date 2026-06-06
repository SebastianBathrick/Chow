using Chow.Core;
namespace Chow.Ast.Nodes
{
    sealed class AssignStatementNode : Node
    {
        public string Name { get; }

        public Node Expression { get; }

        /// <summary>
        /// How this binding resolves at runtime. Stamped by <see cref="SemanticAnalyzer"/> before
        /// the compiler runs. Defaults to <see cref="ScopeType.Local"/>.
        /// </summary>
        public ScopeType Resolution { get; set; }

        public AssignStatementNode(string name, Node expr, int line) : base(line)
        {
            Name = name;
            Expression = expr;
        }

    }
}
