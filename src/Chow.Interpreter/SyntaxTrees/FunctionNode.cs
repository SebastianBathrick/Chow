using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    class FunctionNode : Node
    {
        public string Name { get; }

        public List<Node> Params { get; }

        public Node Body { get; }

        /// <summary>
        /// How the binding of <see cref="Name"/> into the enclosing scope resolves at runtime
        /// (a <c>def foo()</c> in a function that declared <c>global foo</c> binds the module's
        /// <c>foo</c>). Stamped by <see cref="SemanticAnalyzer"/>; defaults to
        /// <see cref="ScopeKind.Local"/>.
        /// </summary>
        public ScopeKind Resolution { get; set; }

        public FunctionNode(string name, List<Node> paramList, Node body, int line) : base(line)
        {
            Name = name;
            Params = paramList;
            Body = body;
        }

        public override string ToString()
        {
            return $"def {Name}({string.Join(", ", Params)}) {{\n{Body}\n}}";
        }
    }
}
