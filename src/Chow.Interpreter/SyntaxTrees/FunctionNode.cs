using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    class FunctionNode : Node
    {
        public FunctionNode(string name, List<Node> paramList, Node body, int line) : base(line)
        {
            Name = name;
            Params = paramList;
            Body = body;
        }
        public string Name { get; }

        public List<Node> Params { get; }

        public Node Body { get; }

        public override string ToString()
        {
            return $"def {Name}({string.Join(", ", Params)}) {{\n{Body}\n}}";
        }
    }
}
