namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class ExprNode : Node
    {
        ExprOperator _operator;
        Node _l;
        Node _r;

        public ExprOperator Operator => _operator;
        public Node Left => _l;
        public Node Right => _r;

        public ExprNode(ExprOperator opType, Node l, Node r, int line) : base(line)
        {
            _operator = opType;
            _l = l;
            _r = r;
        }

        /// <summary>
        /// Initializes a node representing a negated expression (using unary minus or logical not).
        /// </summary>
        public ExprNode(ExprOperator opType, Node l, int line) : base(line)
        {
            _operator = opType;
            _l = l;
            _r = null;
        }

        public override string ToString()
        {
            string indentedLeft = IndentChildren(_l.ToString());

            if (_r == null)
            {
                return $"[{_operator} line={LineNum}\n{indentedLeft}\n]";
            }

            string indentedRight = IndentChildren(_r.ToString());
            return $"[{_operator} line={LineNum}\n{indentedLeft}\n{indentedRight}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
