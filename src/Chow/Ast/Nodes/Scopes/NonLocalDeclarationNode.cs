using System.Collections.Generic;
namespace Chow.Ast.Nodes
{
    /// <summary>
    /// A <c>nonlocal name1, name2, ...</c> statement. Recorded as a node so semantic analysis can
    /// validate it; the compiler does not emit any bytecode for the declaration itself.
    /// </summary>
    sealed class NonLocalDeclarationNode : Node
    {
        public List<string> Names { get; }

        public NonLocalDeclarationNode(List<string> names, int line) : base(line)
        {
            Names = names;
        }

    }
}
