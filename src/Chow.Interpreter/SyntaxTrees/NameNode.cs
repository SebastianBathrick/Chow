namespace Chow.Interpreter.SyntaxTrees
{
    class NameNode : Node
    {
        public NameNode(string name, int line) : base(line)
        {
            Name = name;
        }
        public string Name { get; }

        public override string ToString()
        {
            return $"VariableFactor({Name}) line={LineNumber}";
        }
    }
}
