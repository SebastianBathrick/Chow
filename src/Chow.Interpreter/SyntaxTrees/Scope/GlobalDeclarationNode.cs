using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Scope
{
    /// <summary>
    /// A <c>global name1, name2, ...</c> statement. Recorded as a node so semantic analysis can
    /// validate it; the compiler does not emit any bytecode for the declaration itself.
    /// </summary>
    sealed class GlobalDeclarationNode : Node
    {
        public IReadOnlyList<string> Names { get; }

        public GlobalDeclarationNode(IReadOnlyList<string> names, int line) : base(line)
        {
            Names = names;
        }

        public override string ToString()
        {
            return $"global {string.Join(", ", Names)}";
        }
    }
}
