namespace Chow.Interpreter.SyntaxTrees.Subscripts
{
    class SubscriptSliceNode : Node
    {
        public Node Start { get; }

        public Node Stop { get; }

        public Node Step { get; }

        public SubscriptSliceNode(Node start, Node stop, Node step, int line) : base(line)
        {
            Start = start;
            Stop = stop;
            Step = step;
        }

        public override string ToString()
        {
            var startStr = Start == null ? "None" : Start.ToString();
            var stopStr = Stop == null ? "None" : Stop.ToString();
            var stepStr = Step == null ? "None" : Step.ToString();
            return $"[Slice line={LineNumber}\n  start={IndentChildren(startStr)}\n  stop={IndentChildren(stopStr)}\n  step={IndentChildren(stepStr)}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return nodeString.Replace("\n", "\n  ");
        }
    }
}
