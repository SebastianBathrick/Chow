using System.Collections.Generic;

namespace Chow.Syntax
{
    sealed class CallNode : Node
    {
        public Node FunctionName { get; }

        public List<Node> Args { get; }

        public CallNode(Node funcName, List<Node> args, int line) : base(line)
        {
            FunctionName = funcName;
            Args = args;
        }
    }
}
