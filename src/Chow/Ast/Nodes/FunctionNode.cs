using System.Collections.Generic;
using Chow.Core;
using Chow.SyntaxTrees.Scope;
namespace Chow.SyntaxTrees
{
    sealed class FunctionNode : Node
    {
        public string Name { get; }

        public List<Node> Params { get; }

        public Node Block { get; }

        /// <summary>
        /// How the binding of <see cref="Name"/> into the enclosing scope resolves at runtime
        /// (a <c>def foo()</c> in a function that declared <c>global foo</c> binds the module's
        /// <c>foo</c>). Stamped by <see cref="SemanticAnalyzer"/>; defaults to
        /// <see cref="ScopeType.Local"/>.
        /// </summary>
        public ScopeType Resolution { get; set; }

        public FunctionNode(string name, List<Node> paramList, Node block, int line) : base(line)
        {
            Name = name;
            Params = paramList;
            Block = block;
        }

    }
}
