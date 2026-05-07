using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class ReturnNode : Node
    {
        Node _expression;

        public Node Expression => _expression;

        public ReturnNode(Node expression, int lineNumber) : base(lineNumber)
        {
            _expression = expression;
        }

        public override string ToString()
        {
            return $"return {_expression}";
        }
    }
}
