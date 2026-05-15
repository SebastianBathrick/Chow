using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    sealed class CallNode : Node
    {
        public Node CallName { get; }

        public List<Node> Args { get; }

        public CallNode(Node callName, List<Node> args, int line) : base(line)
        {
            CallName = callName;
            Args = args;
        }

    }
}
