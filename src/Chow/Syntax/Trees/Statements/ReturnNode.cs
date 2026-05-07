using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Syntax.Trees.Statements
{
    internal class ReturnNode : Node
    {
        Node _expression;

        /// <summary>
        /// Node representing the expression to be evaluated and returned by the return statement or null.
        /// </summary>
        public Node Expression => _expression;

        /// <param name="expression">Node representing the expression to be evaluated and returned by the return statement or null.</param>
        /// <param name="lineNumber">The line number of the return statement.</param>
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
