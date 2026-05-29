using Chow.Interpreter.Core;
using Chow.Interpreter.SyntaxTrees.Scope;
namespace Chow.Interpreter.SyntaxTrees.Statements
{
    sealed class VariableAssignStatementNode : Node
    {
        public string Name { get; }

        public Node Expression { get; }

        /// <summary>
        /// How this binding resolves at runtime. Stamped by <see cref="SemanticAnalyzer"/> before
        /// the compiler runs. Defaults to <see cref="ScopeType.Local"/>.
        /// </summary>
        public ScopeType Resolution { get; set; }

        public VariableAssignStatementNode(string name, Node expr, int line) : base(line)
        {
            Name = name;
            Expression = expr;
        }

    }
}
