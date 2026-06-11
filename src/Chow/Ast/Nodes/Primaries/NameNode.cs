using Chow.Semantics;

namespace Chow.Ast
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
