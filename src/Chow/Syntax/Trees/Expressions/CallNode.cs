using System.Collections.Generic;

namespace Chow.Interpreter.Syntax.Trees.Expressions
{
    internal class CallNode : Node
    {
        string _name;
        List<Node> _args;

        public string Name => _name;

        public List<Node> Args => _args;

        public CallNode(string name, List<Node> args, int line) : base(line)
        {
            _name = name;
            _args = args;
        }

        public override string ToString()
        {
            return $"{_name}({string.Join(", ", _args)})";
        }
    }
}
