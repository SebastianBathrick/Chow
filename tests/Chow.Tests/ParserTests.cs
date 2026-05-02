using Chow.Syntax;
using Chow.Tokens;

namespace Chow.Tests
{
    [TestFixture]
    public class ParserTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static Node Parse(string source) =>
            new Parser(new Scanner(source).ScanTokens()).BuildSyntaxTree();

        static Node ParseTokens(params Token[] tokens) =>
            new Parser(new List<Token>(tokens)).BuildSyntaxTree();

        static Token Token(TokenType type, string lexeme, int lineNumber, object literal = null!) =>
            new Token(type, lexeme, lineNumber, literal);

        static void AssertLiteral(Node node, object expectedValue, LiteralNode.DataType expectedType)
        {
            Assert.That(node, Is.InstanceOf<LiteralNode>());
            LiteralNode literal = (LiteralNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(literal.Value, Is.EqualTo(expectedValue));
                Assert.That(literal.Type, Is.EqualTo(expectedType));
            });
        }

        static ExpressionOperationNode AssertBinary(Node node, ExpressionOperationNode.OperatorType expectedOp)
        {
            Assert.That(node, Is.InstanceOf<ExpressionOperationNode>());
            ExpressionOperationNode op = (ExpressionOperationNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(op.Operator, Is.EqualTo(expectedOp));
                Assert.That(op.Right, Is.Not.Null);
            });
            return op;
        }

        static ExpressionOperationNode AssertUnary(Node node)
        {
            Assert.That(node, Is.InstanceOf<ExpressionOperationNode>());
            ExpressionOperationNode op = (ExpressionOperationNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(op.Operator, Is.EqualTo(ExpressionOperationNode.OperatorType.Negate));
                Assert.That(op.Right, Is.Null);
            });
            return op;
        }

        // ============================================================================================================
        // Constructor
        // ============================================================================================================

        [Test]
        public void Constructor_NullTokens_ThrowsArgumentNullException()
        {
            Assert.That(() => new Parser(null!), Throws.TypeOf<ArgumentNullException>());
        }

        // ============================================================================================================
        // Literals
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_IntegerLiteral_ReturnsLiteralNodeWithIntegerType()
        {
            Node result = Parse("42");
            AssertLiteral(result, 42, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_FloatLiteral_ReturnsLiteralNodeWithFloatType()
        {
            Node result = Parse("3.14");
            AssertLiteral(result, 3.14f, LiteralNode.DataType.Float);
        }

        [Test]
        public void BuildSyntaxTree_Literal_AssignsLiteralTokenLineNumber()
        {
            Node result = ParseTokens(
                Token(TokenType.Integer, "42", 7, 42),
                Token(TokenType.EndOfCode, string.Empty, 7));

            Assert.That(result.LineNumber, Is.EqualTo(7));
        }

        [Test]
        public void LiteralNode_ToString_IncludesLineNumber()
        {
            Node result = ParseTokens(
                Token(TokenType.Integer, "42", 7, 42),
                Token(TokenType.EndOfCode, string.Empty, 7));

            Assert.That(result.ToString(), Is.EqualTo("42 line=7"));
        }

        // ============================================================================================================
        // Binary operators
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_Addition_BuildsAddNode()
        {
            AssertBinary(Parse("1 + 2"), ExpressionOperationNode.OperatorType.Add);
        }

        [Test]
        public void BuildSyntaxTree_Subtraction_BuildsSubtractNode()
        {
            AssertBinary(Parse("3 - 1"), ExpressionOperationNode.OperatorType.Subtract);
        }

        [Test]
        public void BuildSyntaxTree_Multiplication_BuildsMultiplyNode()
        {
            AssertBinary(Parse("2 * 3"), ExpressionOperationNode.OperatorType.Multiply);
        }

        [Test]
        public void BuildSyntaxTree_Division_BuildsDivideNode()
        {
            AssertBinary(Parse("6 / 2"), ExpressionOperationNode.OperatorType.Divide);
        }

        [Test]
        public void BuildSyntaxTree_BinaryOperation_AssignsOperatorTokenLineNumber()
        {
            Node result = ParseTokens(
                Token(TokenType.Integer, "1", 2, 1),
                Token(TokenType.Plus, "+", 3),
                Token(TokenType.Integer, "2", 4, 2),
                Token(TokenType.EndOfCode, string.Empty, 4));

            Assert.That(result.LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void ExpressionOperationNode_ToString_IncludesLineNumber()
        {
            Node result = ParseTokens(
                Token(TokenType.Integer, "1", 2, 1),
                Token(TokenType.Plus, "+", 3),
                Token(TokenType.Integer, "2", 4, 2),
                Token(TokenType.EndOfCode, string.Empty, 4));

            Assert.That(result.ToString(), Is.EqualTo("[Add line=3\n  1 line=2\n  2 line=4\n]"));
        }

        [Test]
        public void BuildSyntaxTree_PrecedenceMulOverAdd_GroupsCorrectly()
        {
            // 1 + 2 * 3 => Add(1, Multiply(2, 3))
            Node result = Parse("1 + 2 * 3");
            ExpressionOperationNode add = AssertBinary(result, ExpressionOperationNode.OperatorType.Add);
            AssertLiteral(add.Left, 1, LiteralNode.DataType.Integer);
            ExpressionOperationNode mul = AssertBinary(add.Right!, ExpressionOperationNode.OperatorType.Multiply);
            AssertLiteral(mul.Left, 2, LiteralNode.DataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeAddition_BuildsLeftLeaningTree()
        {
            // 1 + 2 + 3 => Add(Add(1, 2), 3)
            Node result = Parse("1 + 2 + 3");
            ExpressionOperationNode outer = AssertBinary(result, ExpressionOperationNode.OperatorType.Add);
            ExpressionOperationNode inner = AssertBinary(outer.Left, ExpressionOperationNode.OperatorType.Add);
            AssertLiteral(inner.Left, 1, LiteralNode.DataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralNode.DataType.Integer);
            AssertLiteral(outer.Right!, 3, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeSubtraction_BuildsLeftLeaningTree()
        {
            // 5 - 2 - 1 => Subtract(Subtract(5, 2), 1) — catches accidental right-associativity
            Node result = Parse("5 - 2 - 1");
            ExpressionOperationNode outer = AssertBinary(result, ExpressionOperationNode.OperatorType.Subtract);
            ExpressionOperationNode inner = AssertBinary(outer.Left, ExpressionOperationNode.OperatorType.Subtract);
            AssertLiteral(inner.Left, 5, LiteralNode.DataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralNode.DataType.Integer);
            AssertLiteral(outer.Right!, 1, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeOperations_AssignEachOperatorLineNumber()
        {
            Node result = ParseTokens(
                Token(TokenType.Integer, "5", 1, 5),
                Token(TokenType.Minus, "-", 2),
                Token(TokenType.Integer, "2", 3, 2),
                Token(TokenType.Minus, "-", 4),
                Token(TokenType.Integer, "1", 5, 1),
                Token(TokenType.EndOfCode, string.Empty, 5));

            ExpressionOperationNode outer = AssertBinary(result, ExpressionOperationNode.OperatorType.Subtract);
            ExpressionOperationNode inner = AssertBinary(outer.Left, ExpressionOperationNode.OperatorType.Subtract);

            Assert.Multiple(() =>
            {
                Assert.That(inner.LineNumber, Is.EqualTo(2));
                Assert.That(outer.LineNumber, Is.EqualTo(4));
            });
        }

        // ============================================================================================================
        // Parentheses
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_ParenthesesOverridePrecedence_GroupsExplicitly()
        {
            // (1 + 2) * 3 => Multiply(Add(1, 2), 3)
            Node result = Parse("(1 + 2) * 3");
            ExpressionOperationNode mul = AssertBinary(result, ExpressionOperationNode.OperatorType.Multiply);
            ExpressionOperationNode add = AssertBinary(mul.Left, ExpressionOperationNode.OperatorType.Add);
            AssertLiteral(add.Left, 1, LiteralNode.DataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralNode.DataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_NestedParentheses_ParsesInnerFirst()
        {
            Node result = Parse("((1 + 2))");
            ExpressionOperationNode add = AssertBinary(result, ExpressionOperationNode.OperatorType.Add);
            AssertLiteral(add.Left, 1, LiteralNode.DataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralNode.DataType.Integer);
        }

        // ============================================================================================================
        // Unary minus
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_UnaryMinusOnInteger_BuildsNegateNode()
        {
            Node result = Parse("-5");
            ExpressionOperationNode negate = AssertUnary(result);
            AssertLiteral(negate.Left, 5, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryOperation_AssignsMinusTokenLineNumber()
        {
            Node result = ParseTokens(
                Token(TokenType.Minus, "-", 6),
                Token(TokenType.Integer, "5", 7, 5),
                Token(TokenType.EndOfCode, string.Empty, 7));

            Assert.That(result.LineNumber, Is.EqualTo(6));
        }

        [Test]
        public void LiteralNode_InvalidLineNumber_ThrowsArgumentOutOfRangeException()
        {
            Assert.That(() => new LiteralNode(1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void BuildSyntaxTree_DoubleUnaryMinus_BuildsNegateOfNegate()
        {
            Node result = Parse("--5");
            ExpressionOperationNode outer = AssertUnary(result);
            ExpressionOperationNode inner = AssertUnary(outer.Left);
            AssertLiteral(inner.Left, 5, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryMinusOnParenthesizedExpression_NegatesGroup()
        {
            // -(1 + 2) => Negate(Add(1, 2))
            Node result = Parse("-(1 + 2)");
            ExpressionOperationNode negate = AssertUnary(result);
            ExpressionOperationNode add = AssertBinary(negate.Left, ExpressionOperationNode.OperatorType.Add);
            AssertLiteral(add.Left, 1, LiteralNode.DataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryMinusBindsTighterThanMultiply()
        {
            // -2 * 3 => Multiply(Negate(2), 3)
            Node result = Parse("-2 * 3");
            ExpressionOperationNode mul = AssertBinary(result, ExpressionOperationNode.OperatorType.Multiply);
            ExpressionOperationNode negate = AssertUnary(mul.Left);
            AssertLiteral(negate.Left, 2, LiteralNode.DataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralNode.DataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_BinaryMinusVersusUnary_DistinguishesByPosition()
        {
            // 1 - -2 => Subtract(1, Negate(2))
            Node result = Parse("1 - -2");
            ExpressionOperationNode sub = AssertBinary(result, ExpressionOperationNode.OperatorType.Subtract);
            AssertLiteral(sub.Left, 1, LiteralNode.DataType.Integer);
            ExpressionOperationNode negate = AssertUnary(sub.Right!);
            AssertLiteral(negate.Left, 2, LiteralNode.DataType.Integer);
        }

        // ============================================================================================================
        // Trailing newline / end-of-input
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_TrailingNewline_ParsesExpression()
        {
            Node result = Parse("1 + 2\n");
            AssertBinary(result, ExpressionOperationNode.OperatorType.Add);
        }

        // ============================================================================================================
        // Error cases
        // ============================================================================================================

        [TestCase("(1 + 2")]
        [TestCase("1 + 2 3")]
        [TestCase("+ 1")]
        [TestCase(")")]
        [TestCase("   ")]
        public void BuildSyntaxTree_MalformedSource_ThrowsParserException(string source)
        {
            Assert.That(() => Parse(source), Throws.TypeOf<ParserException>());
        }
    }
}
