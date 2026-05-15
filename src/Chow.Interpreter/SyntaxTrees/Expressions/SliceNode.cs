namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class SliceNode : Node
    {
        public SliceNode(Node start, Node stop, Node step, int line) : base(line)
        {
            Start = start;
            Stop = stop;
            Step = step;
        }
        public Node Start { get; }

        public Node Stop { get; }

        public Node Step { get; }

        public override string ToString()
        {
            var startStr = Start == null ? "None" : Start.ToString();
            var stopStr = Stop == null ? "None" : Stop.ToString();
            var stepStr = Step == null ? "None" : Step.ToString();
            return
                $"[Slice line={LineNumber}\n  start={IndentChildren(startStr)}\n  stop={IndentChildren(stopStr)}\n  step={IndentChildren(stepStr)}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return nodeString.Replace("\n", "\n  ");
        }
    }
}
