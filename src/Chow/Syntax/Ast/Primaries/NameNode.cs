namespace Chow.Syntax
{
    sealed class NameNode : Node
    {
        public string Name { get; }

        public ScopeType Resolution { get; set; }


        public NameNode(string name, int line) : base(line)
        {
            Name = name;
        }
    }
}
