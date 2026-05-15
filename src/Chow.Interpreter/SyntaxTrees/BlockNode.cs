using System.Collections.Generic;
namespace Chow.Interpreter.SyntaxTrees
{
    class BlockNode : Node
    {
        readonly List<Node> _statements;

        public BlockNode(List<Node> statements, int line) : base(line)
        {
            _statements = statements;
        }

        public int StatementCount => _statements.Count;

        public IReadOnlyList<Node> Statements => _statements;

        public override string ToString()
        {
            return $"Block({StatementCount} statements) line={LineNumber}";
        }
    }
}
