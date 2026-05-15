using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class CallNode : Node
    {
        public CallNode(Node callName, List<Node> args, int line) : base(line)
        {
            CallName = callName;
            Args = args;
        }
        public Node CallName { get; }

        public List<Node> Args { get; }

        public override string ToString()
        {
            return $"{CallName}({string.Join(", ", Args)})";
        }
    }
}
