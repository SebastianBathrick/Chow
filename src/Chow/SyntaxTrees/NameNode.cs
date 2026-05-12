namespace Chow.Interpreter.SyntaxTrees
{
    class NameNode : Node
    {
        readonly string _nameNode;

        public string Name => _nameNode;

        public NameNode(string name, int line) : base(line)
        {
            _nameNode = name;
        }

        public override string ToString()
        {
            return $"VariableFactor({_nameNode}) line={LineNumber}";
        }
    }
}
