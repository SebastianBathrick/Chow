using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Scope
{
    /// <summary>
    /// A <c>nonlocal name1, name2, ...</c> statement. Recorded as a node so semantic analysis can
    /// validate it; the compiler does not emit any bytecode for the declaration itself.
    /// </summary>
    sealed class NonlocalDeclarationNode : Node
    {
        public List<string> Names { get; }

        public NonlocalDeclarationNode(List<string> names, int line) : base(line)
        {
            Names = names;
        }

        public override string ToString()
        {
            return $"nonlocal {string.Join(", ", Names)}";
        }
    }
}
