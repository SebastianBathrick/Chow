
namespace Chow.Syntax
{
    sealed class AssignStatementNode : Node
    {
        public string Name { get; }

        public Node Expression { get; }
        
        public ScopeType Resolution { get; set; }

        public AssignStatementNode(string name, Node expr, int line) : base(line)
        {
            Name = name;
            Expression = expr;
        }

    }
}
