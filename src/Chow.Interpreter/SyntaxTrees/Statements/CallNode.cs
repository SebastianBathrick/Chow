using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Statements
{
    class CallNode : Node
    {
        public Node CallName { get; }

        public List<Node> Args { get; }

        public CallNode(Node callName, List<Node> args, int line) : base(line)
        {
            CallName = callName;
            Args = args;
        }

        public override string ToString()
        {
            return $"{CallName}({string.Join(", ", Args)})";
        }
    }
}
