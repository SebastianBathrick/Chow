using System;

namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class VariableAssignNode : Node
    {
        string _name;
        Node _expr;

        public string Name => _name;
        public Node Expression => _expr;

        public VariableAssignNode(string name, Node expr, int line) : base(line)
        {
            _name = name;
            _expr = expr;
        }

        public override string ToString()
        {
            return $"VariableAssignment({_name}, {_expr}) line={LineNum}";
        }
    }
}