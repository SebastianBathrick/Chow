using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Syntax.Trees
{
    internal class FunctionNode : Node
    {
        string _name;
        List<Node> _params;
        Node _body;

        public string Name => _name;

        public List<Node> Params => _params;

        public Node Body => _body;

        public FunctionNode(string name, List<Node> paramList, Node body, int line) : base(line)
        {
            _name = name;
            _params = paramList;
            _body = body;
        }

        public override string ToString()
        {
            return $"def {_name}({string.Join(", ", _params)}) {{\n{_body}\n}}";
        }
    }
}
