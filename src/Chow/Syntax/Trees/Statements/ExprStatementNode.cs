using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class ExprStatementNode : Node
    {
        Node _expr;

        public Node Expression => _expr;

        public ExprStatementNode(Node expression, int lineNumber) : base(lineNumber)
        {
            _expr = expression;
        }

        public override string ToString()
        {
            return _expr.ToString();
        }
    }
}
