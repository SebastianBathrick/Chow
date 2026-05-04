using Chow.Interpreter;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Syntax.Trees.Expressions;
using Chow.Interpreter.Syntax.Trees.Statements;
using Chow.Interpreter.Tokens;

namespace Chow.Tests
{
    [TestFixture]
    public class ParserTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static Node Parse(string source) =>
            UnwrapAssignmentExpression(new Parser(new Scanner("x = " + source).ScanTokens()).BuildSyntaxTree());

        static Node ParseTokens(params Token[] tokens)
        {
            List<Token> wrapped = new List<Token>
            {
                Token(TokenType.Identifier, "x", 1),
                Token(TokenType.Equal, "=", 1),
            };
            wrapped.AddRange(tokens);
            return UnwrapAssignmentExpression(new Parser(wrapped).BuildSyntaxTree());
        }

        static Node UnwrapAssignmentExpression(Node root)
        {
            Node statement = ((BlockNode)((SyntaxTreeRoot)root).TopLevelBlock).Statements[0];
            return ((VariableAssignNode)statement).Expression;
        }

        static Token Token(TokenType type, string lexeme, int lineNumber, object literal = null!) =>
            new Token(type, lexeme, lineNumber, literal);

        static void AssertLiteral(Node node, object expectedValue, LiteralDataType expectedType)
        {
            Assert.That(node, Is.InstanceOf<LiteralNode>());
            LiteralNode literal = (LiteralNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(literal.Value, Is.EqualTo(expectedValue));
                Assert.That(literal.Type, Is.EqualTo(expectedType));
            });
        }

        static ExpressionNode AssertBinary(Node node, ExpressionOperator expectedOp)
        {
            Assert.That(node, Is.InstanceOf<ExpressionNode>());
            ExpressionNode op = (ExpressionNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(op.Operator, Is.EqualTo(expectedOp));
                Assert.That(op.Right, Is.Not.Null);
            });
            return op;
        }

        static ExpressionNode AssertUnary(Node node)
        {
            Assert.That(node, Is.InstanceOf<ExpressionNode>());
            ExpressionNode op = (ExpressionNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(op.Operator, Is.EqualTo(ExpressionOperator.Negate));
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
            AssertLiteral(result, 42, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_FloatLiteral_ReturnsLiteralNodeWithFloatType()
        {
            Node result = Parse("3.14");
            AssertLiteral(result, 3.14f, LiteralDataType.Float);
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
            AssertBinary(Parse("1 + 2"), ExpressionOperator.Add);
        }

        [Test]
        public void BuildSyntaxTree_Subtraction_BuildsSubtractNode()
        {
            AssertBinary(Parse("3 - 1"), ExpressionOperator.Subtract);
        }

        [Test]
        public void BuildSyntaxTree_Multiplication_BuildsMultiplyNode()
        {
            AssertBinary(Parse("2 * 3"), ExpressionOperator.Multiply);
        }

        [Test]
        public void BuildSyntaxTree_Division_BuildsDivideNode()
        {
            AssertBinary(Parse("6 / 2"), ExpressionOperator.Divide);
        }

        [Test]
        public void BuildSyntaxTree_Modulus_BuildsModulusNode()
        {
            AssertBinary(Parse("7 % 2"), ExpressionOperator.Modulus);
        }

        [Test]
        public void BuildSyntaxTree_Exponent_BuildsExponentiateNode()
        {
            AssertBinary(Parse("2 ** 3"), ExpressionOperator.Exponentiate);
        }

        [Test]
        public void BuildSyntaxTree_FloorDivide_BuildsFloorDivideNode()
        {
            AssertBinary(Parse("7 // 2"), ExpressionOperator.FloorDivide);
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
            ExpressionNode add = AssertBinary(result, ExpressionOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            ExpressionNode mul = AssertBinary(add.Right!, ExpressionOperator.Multiply);
            AssertLiteral(mul.Left, 2, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeAddition_BuildsLeftLeaningTree()
        {
            // 1 + 2 + 3 => Add(Add(1, 2), 3)
            Node result = Parse("1 + 2 + 3");
            ExpressionNode outer = AssertBinary(result, ExpressionOperator.Add);
            ExpressionNode inner = AssertBinary(outer.Left, ExpressionOperator.Add);
            AssertLiteral(inner.Left, 1, LiteralDataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralDataType.Integer);
            AssertLiteral(outer.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeSubtraction_BuildsLeftLeaningTree()
        {
            // 5 - 2 - 1 => Subtract(Subtract(5, 2), 1) — catches accidental right-associativity
            Node result = Parse("5 - 2 - 1");
            ExpressionNode outer = AssertBinary(result, ExpressionOperator.Subtract);
            ExpressionNode inner = AssertBinary(outer.Left, ExpressionOperator.Subtract);
            AssertLiteral(inner.Left, 5, LiteralDataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralDataType.Integer);
            AssertLiteral(outer.Right!, 1, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_RightAssociativeExponent_BuildsRightLeaningTree()
        {
            // 2 ** 3 ** 2 => Exp(2, Exp(3, 2)) — right-associative matches Python: 2**(3**2) = 512
            Node result = Parse("2 ** 3 ** 2");
            ExpressionNode outer = AssertBinary(result, ExpressionOperator.Exponentiate);
            AssertLiteral(outer.Left, 2, LiteralDataType.Integer);
            ExpressionNode inner = AssertBinary(outer.Right!, ExpressionOperator.Exponentiate);
            AssertLiteral(inner.Left, 3, LiteralDataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_PrecedenceExpOverNegate_BindsExponentTighter()
        {
            // -2 ** 2 => Negate(Exp(2, 2)) — Python: ** binds tighter than unary minus
            Node result = Parse("-2 ** 2");
            ExpressionNode negate = AssertUnary(result);
            ExpressionNode exp = AssertBinary(negate.Left, ExpressionOperator.Exponentiate);
            AssertLiteral(exp.Left, 2, LiteralDataType.Integer);
            AssertLiteral(exp.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_ExponentRightOperandUnary_AllowsNegativeExponent()
        {
            // 2 ** -3 => Exp(2, Negate(3))
            Node result = Parse("2 ** -3");
            ExpressionNode exp = AssertBinary(result, ExpressionOperator.Exponentiate);
            AssertLiteral(exp.Left, 2, LiteralDataType.Integer);
            ExpressionNode negate = AssertUnary(exp.Right!);
            AssertLiteral(negate.Left, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_PrecedenceModEqualToMul_GroupsLeftToRight()
        {
            // 6 % 4 * 2 => Multiply(Modulus(6, 4), 2) — % same precedence as *
            Node result = Parse("6 % 4 * 2");
            ExpressionNode mul = AssertBinary(result, ExpressionOperator.Multiply);
            ExpressionNode mod = AssertBinary(mul.Left, ExpressionOperator.Modulus);
            AssertLiteral(mod.Left, 6, LiteralDataType.Integer);
            AssertLiteral(mod.Right!, 4, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 2, LiteralDataType.Integer);
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

            ExpressionNode outer = AssertBinary(result, ExpressionOperator.Subtract);
            ExpressionNode inner = AssertBinary(outer.Left, ExpressionOperator.Subtract);

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
            ExpressionNode mul = AssertBinary(result, ExpressionOperator.Multiply);
            ExpressionNode add = AssertBinary(mul.Left, ExpressionOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_NestedParentheses_ParsesInnerFirst()
        {
            Node result = Parse("((1 + 2))");
            ExpressionNode add = AssertBinary(result, ExpressionOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralDataType.Integer);
        }

        // ============================================================================================================
        // Unary minus
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_UnaryMinusOnInteger_BuildsNegateNode()
        {
            Node result = Parse("-5");
            ExpressionNode negate = AssertUnary(result);
            AssertLiteral(negate.Left, 5, LiteralDataType.Integer);
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
            ExpressionNode outer = AssertUnary(result);
            ExpressionNode inner = AssertUnary(outer.Left);
            AssertLiteral(inner.Left, 5, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryMinusOnParenthesizedExpression_NegatesGroup()
        {
            // -(1 + 2) => Negate(Add(1, 2))
            Node result = Parse("-(1 + 2)");
            ExpressionNode negate = AssertUnary(result);
            ExpressionNode add = AssertBinary(negate.Left, ExpressionOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryMinusBindsTighterThanMultiply()
        {
            // -2 * 3 => Multiply(Negate(2), 3)
            Node result = Parse("-2 * 3");
            ExpressionNode mul = AssertBinary(result, ExpressionOperator.Multiply);
            ExpressionNode negate = AssertUnary(mul.Left);
            AssertLiteral(negate.Left, 2, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_BinaryMinusVersusUnary_DistinguishesByPosition()
        {
            // 1 - -2 => Subtract(1, Negate(2))
            Node result = Parse("1 - -2");
            ExpressionNode sub = AssertBinary(result, ExpressionOperator.Subtract);
            AssertLiteral(sub.Left, 1, LiteralDataType.Integer);
            ExpressionNode negate = AssertUnary(sub.Right!);
            AssertLiteral(negate.Left, 2, LiteralDataType.Integer);
        }

        // ============================================================================================================
        // Trailing newline / end-of-input
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_TrailingNewline_ParsesExpression()
        {
            Node result = Parse("1 + 2\n");
            AssertBinary(result, ExpressionOperator.Add);
        }

        // ============================================================================================================
        // Error cases
        // ============================================================================================================

        [TestCase("(1 + 2")]
        [TestCase("1 + 2 3")]
        [TestCase("+ 1")]
        [TestCase(")")]
        public void BuildSyntaxTree_MalformedSource_ThrowsParserException(string source)
        {
            Assert.That(() => Parse(source), Throws.TypeOf<ParserException>());
        }
    }
}
