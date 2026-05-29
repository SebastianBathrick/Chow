using System.Collections.Generic;
using Chow.Interpreter.Core;
using Chow.Interpreter.SyntaxTrees.Scope;
namespace Chow.Interpreter.SyntaxTrees
{
    sealed class FunctionNode : Node
    {
        public string Name { get; }

        public List<Node> Params { get; }

        public Node Body { get; }

        /// <summary>
        /// How the binding of <see cref="Name"/> into the enclosing scope resolves at runtime
        /// (a <c>def foo()</c> in a function that declared <c>global foo</c> binds the module's
        /// <c>foo</c>). Stamped by <see cref="SemanticAnalyzer"/>; defaults to
        /// <see cref="ScopeType.Local"/>.
        /// </summary>
        public ScopeType Resolution { get; set; }

        public FunctionNode(string name, List<Node> paramList, Node body, int line) : base(line)
        {
            Name = name;
            Params = paramList;
            Body = body;
        }

    }
}
