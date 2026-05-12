using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    internal class CallNode : Node
    {
        Node _callName;
        List<Node> _args;

        public Node CallName => _callName;

        public List<Node> Args => _args;

        public CallNode(Node callName, List<Node> args, int line) : base(line)
        {
            _callName = callName;
            _args = args;
        }

        public override string ToString()
        {
            return $"{_callName}({string.Join(", ", _args)})";
        }
    }
}
