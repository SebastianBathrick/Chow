using Chow.Core;
namespace Chow.Ast.Nodes
{
    sealed class NameNode : Node
    {

        public NameNode(string name, int line) : base(line)
        {
            Name = name;
        }

        public string Name { get; }

        /// <summary>
        ///     How this read resolves at runtime. Stamped by <see cref="SemanticAnalyzer" /> before
        ///     the compiler runs. Defaults to <see cref="ScopeType.Local" />.
        /// </summary>
        public ScopeType Resolution { get; set; }
    }
}
