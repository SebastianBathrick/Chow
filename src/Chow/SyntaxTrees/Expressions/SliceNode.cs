namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class SliceNode : Node
    {
        readonly Node _start;
        readonly Node _stop;
        readonly Node _step;

        public Node Start => _start;
        public Node Stop => _stop;
        public Node Step => _step;

        public SliceNode(Node start, Node stop, Node step, int line) : base(line)
        {
            _start = start;
            _stop = stop;
            _step = step;
        }

        public override string ToString()
        {
            var startStr = _start == null ? "None" : _start.ToString();
            var stopStr = _stop == null ? "None" : _stop.ToString();
            var stepStr = _step == null ? "None" : _step.ToString();
            return $"[Slice line={LineNumber}\n  start={IndentChildren(startStr)}\n  stop={IndentChildren(stopStr)}\n  step={IndentChildren(stepStr)}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return nodeString.Replace("\n", "\n  ");
        }
    }
}
