namespace Chow.Interpreter.SyntaxTrees.Expressions
{
    class ExprNode : Node
    {
        public ExprOperator Operator { get; }

        public Node Left { get; }

        public Node Right { get; }

        public ExprNode(ExprOperator opType, Node l, Node r, int line) : base(line)
        {
            Operator = opType;
            Left = l;
            Right = r;
        }

        /// <summary>
        /// Initializes a node representing a negated expression (using unary minus or logical not).
        /// </summary>
        public ExprNode(ExprOperator opType, Node l, int line) : base(line)
        {
            Operator = opType;
            Left = l;
            Right = null;
        }

        public override string ToString()
        {
            var indentedLeft = IndentChildren(Left.ToString());

            if (Right == null)
            {
                return $"[{Operator} line={LineNumber}\n{indentedLeft}\n]";
            }

            var indentedRight = IndentChildren(Right.ToString());
            return $"[{Operator} line={LineNumber}\n{indentedLeft}\n{indentedRight}\n]";
        }

        static string IndentChildren(string nodeString)
        {
            return "  " + nodeString.Replace("\n", "\n  ");
        }
    }
}
