using Chow.Interpreter.Exceptions;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Statements;
using Chow.Interpreter.Tokens;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class ParserTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static Node Parse(string source) =>
            UnwrapAssignmentExpression(new Parser(new Scanner("x = " + source).ScanTokens()).BuildTree());

        static Node ParseTokens(params Token[] tokens)
        {
            var wrapped = new List<Token>
            {
                Token(TokenType.Identifier, "x", 1),
                Token(TokenType.SymbolAssign, "=", 1),
            };
            wrapped.AddRange(tokens);
            return UnwrapAssignmentExpression(new Parser(wrapped).BuildTree());
        }

        static Node UnwrapAssignmentExpression(Node root)
        {
            var statement = ((TreeRootNode)root).Statements[0];
            return ((VarAssignNode)statement).Expression;
        }

        static Token Token(TokenType type, string lexeme, int lineNumber, object literal = null!) =>
            new Token(type, lexeme, lineNumber, literal);

        static void AssertLiteral(Node node, object expectedValue, LiteralDataType expectedType)
        {
            Assert.That(node, Is.InstanceOf<LiteralNode>());
            var literal = (LiteralNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(literal.Value, Is.EqualTo(expectedValue));
                Assert.That(literal.Type, Is.EqualTo(expectedType));
            });
        }

        static ExprNode AssertBinary(Node node, ExprOperator expectedOp)
        {
            Assert.That(node, Is.InstanceOf<ExprNode>());
            var op = (ExprNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(op.Operator, Is.EqualTo(expectedOp));
                Assert.That(op.Right, Is.Not.Null);
            });
            return op;
        }

        static ExprNode AssertUnary(Node node)
        {
            Assert.That(node, Is.InstanceOf<ExprNode>());
            var op = (ExprNode)node;
            Assert.Multiple(() =>
            {
                Assert.That(op.Operator, Is.EqualTo(ExprOperator.Negate));
                Assert.That(op.Right, Is.Null);
            });
            return op;
        }

        // ============================================================================================================
        // Literals
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_IntegerLiteral_ReturnsLiteralNodeWithIntegerType()
        {
            var result = Parse("42");
            AssertLiteral(result, 42, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_FloatLiteral_ReturnsLiteralNodeWithFloatType()
        {
            var result = Parse("3.14");
            AssertLiteral(result, 3.14, LiteralDataType.Float);
        }

        [Test]
        public void BuildSyntaxTree_TrueLiteral_ReturnsLiteralNodeWithBooleanType()
        {
            var result = Parse("True");
            AssertLiteral(result, true, LiteralDataType.Boolean);
        }

        [Test]
        public void BuildSyntaxTree_FalseLiteral_ReturnsLiteralNodeWithBooleanType()
        {
            var result = Parse("False");
            AssertLiteral(result, false, LiteralDataType.Boolean);
        }

        [Test]
        public void BuildSyntaxTree_Literal_AssignsLiteralTokenLineNumber()
        {
            var result = ParseTokens(
                Token(TokenType.LiteralInt, "42", 7, 42L),
                Token(TokenType.EndOfCode, string.Empty, 7));

            Assert.That(result.LineNumber, Is.EqualTo(7));
        }

        [Test]
        public void LiteralNode_ToString_IncludesLineNumber()
        {
            var result = ParseTokens(
                Token(TokenType.LiteralInt, "42", 7, 42L),
                Token(TokenType.EndOfCode, string.Empty, 7));

            Assert.That(result.ToString(), Is.EqualTo("42 line=7"));
        }

        // ============================================================================================================
        // Binary operators
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_Addition_BuildsAddNode()
        {
            AssertBinary(Parse("1 + 2"), ExprOperator.Add);
        }

        [Test]
        public void BuildSyntaxTree_Subtraction_BuildsSubtractNode()
        {
            AssertBinary(Parse("3 - 1"), ExprOperator.Subtract);
        }

        [Test]
        public void BuildSyntaxTree_Multiplication_BuildsMultiplyNode()
        {
            AssertBinary(Parse("2 * 3"), ExprOperator.Multiply);
        }

        [Test]
        public void BuildSyntaxTree_Division_BuildsDivideNode()
        {
            AssertBinary(Parse("6 / 2"), ExprOperator.Divide);
        }

        [Test]
        public void BuildSyntaxTree_Modulus_BuildsModulusNode()
        {
            AssertBinary(Parse("7 % 2"), ExprOperator.Modulus);
        }

        [Test]
        public void BuildSyntaxTree_Exponent_BuildsExponentiateNode()
        {
            AssertBinary(Parse("2 ** 3"), ExprOperator.Exponentiate);
        }

        [Test]
        public void BuildSyntaxTree_FloorDivide_BuildsFloorDivideNode()
        {
            AssertBinary(Parse("7 // 2"), ExprOperator.FloorDivide);
        }

        [Test]
        public void BuildSyntaxTree_BinaryOperation_AssignsOperatorTokenLineNumber()
        {
            var result = ParseTokens(
                Token(TokenType.LiteralInt, "1", 2, 1L),
                Token(TokenType.SymbolPlus, "+", 3),
                Token(TokenType.LiteralInt, "2", 4, 2L),
                Token(TokenType.EndOfCode, string.Empty, 4));

            Assert.That(result.LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void ExpressionOperationNode_ToString_IncludesLineNumber()
        {
            var result = ParseTokens(
                Token(TokenType.LiteralInt, "1", 2, 1L),
                Token(TokenType.SymbolPlus, "+", 3),
                Token(TokenType.LiteralInt, "2", 4, 2L),
                Token(TokenType.EndOfCode, string.Empty, 4));

            Assert.That(result.ToString(), Is.EqualTo("[Add line=3\n  1 line=2\n  2 line=4\n]"));
        }

        [Test]
        public void BuildSyntaxTree_PrecedenceMulOverAdd_GroupsCorrectly()
        {
            // 1 + 2 * 3 => Add(1, Multiply(2, 3))
            var result = Parse("1 + 2 * 3");
            var add = AssertBinary(result, ExprOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            var mul = AssertBinary(add.Right!, ExprOperator.Multiply);
            AssertLiteral(mul.Left, 2, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeAddition_BuildsLeftLeaningTree()
        {
            // 1 + 2 + 3 => Add(Add(1, 2), 3)
            var result = Parse("1 + 2 + 3");
            var outer = AssertBinary(result, ExprOperator.Add);
            var inner = AssertBinary(outer.Left, ExprOperator.Add);
            AssertLiteral(inner.Left, 1, LiteralDataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralDataType.Integer);
            AssertLiteral(outer.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeSubtraction_BuildsLeftLeaningTree()
        {
            // 5 - 2 - 1 => Subtract(Subtract(5, 2), 1) — catches accidental right-associativity
            var result = Parse("5 - 2 - 1");
            var outer = AssertBinary(result, ExprOperator.Subtract);
            var inner = AssertBinary(outer.Left, ExprOperator.Subtract);
            AssertLiteral(inner.Left, 5, LiteralDataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralDataType.Integer);
            AssertLiteral(outer.Right!, 1, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_RightAssociativeExponent_BuildsRightLeaningTree()
        {
            // 2 ** 3 ** 2 => Exp(2, Exp(3, 2)) — right-associative matches Python: 2**(3**2) = 512
            var result = Parse("2 ** 3 ** 2");
            var outer = AssertBinary(result, ExprOperator.Exponentiate);
            AssertLiteral(outer.Left, 2, LiteralDataType.Integer);
            var inner = AssertBinary(outer.Right!, ExprOperator.Exponentiate);
            AssertLiteral(inner.Left, 3, LiteralDataType.Integer);
            AssertLiteral(inner.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_PrecedenceExpOverNegate_BindsExponentTighter()
        {
            // -2 ** 2 => Negate(Exp(2, 2)) — Python: ** binds tighter than unary minus
            var result = Parse("-2 ** 2");
            var negate = AssertUnary(result);
            var exp = AssertBinary(negate.Left, ExprOperator.Exponentiate);
            AssertLiteral(exp.Left, 2, LiteralDataType.Integer);
            AssertLiteral(exp.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_ExponentRightOperandUnary_AllowsNegativeExponent()
        {
            // 2 ** -3 => Exp(2, Negate(3))
            var result = Parse("2 ** -3");
            var exp = AssertBinary(result, ExprOperator.Exponentiate);
            AssertLiteral(exp.Left, 2, LiteralDataType.Integer);
            var negate = AssertUnary(exp.Right!);
            AssertLiteral(negate.Left, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_PrecedenceModEqualToMul_GroupsLeftToRight()
        {
            // 6 % 4 * 2 => Multiply(Modulus(6, 4), 2) — % same precedence as *
            var result = Parse("6 % 4 * 2");
            var mul = AssertBinary(result, ExprOperator.Multiply);
            var mod = AssertBinary(mul.Left, ExprOperator.Modulus);
            AssertLiteral(mod.Left, 6, LiteralDataType.Integer);
            AssertLiteral(mod.Right!, 4, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_LeftAssociativeOperations_AssignEachOperatorLineNumber()
        {
            var result = ParseTokens(
                Token(TokenType.LiteralInt, "5", 1, 5L),
                Token(TokenType.SymbolMinus, "-", 2),
                Token(TokenType.LiteralInt, "2", 3, 2L),
                Token(TokenType.SymbolMinus, "-", 4),
                Token(TokenType.LiteralInt, "1", 5, 1L),
                Token(TokenType.EndOfCode, string.Empty, 5));

            var outer = AssertBinary(result, ExprOperator.Subtract);
            var inner = AssertBinary(outer.Left, ExprOperator.Subtract);

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
            var result = Parse("(1 + 2) * 3");
            var mul = AssertBinary(result, ExprOperator.Multiply);
            var add = AssertBinary(mul.Left, ExprOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_NestedParentheses_ParsesInnerFirst()
        {
            var result = Parse("((1 + 2))");
            var add = AssertBinary(result, ExprOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralDataType.Integer);
        }

        // ============================================================================================================
        // Unary minus
        // ============================================================================================================

        [Test]
        public void BuildSyntaxTree_UnaryMinusOnInteger_BuildsNegateNode()
        {
            var result = Parse("-5");
            var negate = AssertUnary(result);
            AssertLiteral(negate.Left, 5, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryOperation_AssignsMinusTokenLineNumber()
        {
            var result = ParseTokens(
                Token(TokenType.SymbolMinus, "-", 6),
                Token(TokenType.LiteralInt, "5", 7, 5L),
                Token(TokenType.EndOfCode, string.Empty, 7));

            Assert.That(result.LineNumber, Is.EqualTo(6));
        }

        [Test]
        public void BuildSyntaxTree_DoubleUnaryMinus_BuildsNegateOfNegate()
        {
            var result = Parse("--5");
            var outer = AssertUnary(result);
            var inner = AssertUnary(outer.Left);
            AssertLiteral(inner.Left, 5, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryMinusOnParenthesizedExpression_NegatesGroup()
        {
            // -(1 + 2) => Negate(Add(1, 2))
            var result = Parse("-(1 + 2)");
            var negate = AssertUnary(result);
            var add = AssertBinary(negate.Left, ExprOperator.Add);
            AssertLiteral(add.Left, 1, LiteralDataType.Integer);
            AssertLiteral(add.Right!, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_UnaryMinusBindsTighterThanMultiply()
        {
            // -2 * 3 => Multiply(Negate(2), 3)
            var result = Parse("-2 * 3");
            var mul = AssertBinary(result, ExprOperator.Multiply);
            var negate = AssertUnary(mul.Left);
            AssertLiteral(negate.Left, 2, LiteralDataType.Integer);
            AssertLiteral(mul.Right!, 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_BinaryMinusVersusUnary_DistinguishesByPosition()
        {
            // 1 - -2 => Subtract(1, Negate(2))
            var result = Parse("1 - -2");
            var sub = AssertBinary(result, ExprOperator.Subtract);
            AssertLiteral(sub.Left, 1, LiteralDataType.Integer);
            var negate = AssertUnary(sub.Right!);
            AssertLiteral(negate.Left, 2, LiteralDataType.Integer);
        }

        // ============================================================================================================
        // Error cases
        // ============================================================================================================

        [TestCase("1 + 2 3")]
        [TestCase("+ 1")]
        public void BuildSyntaxTree_MalformedSource_ThrowsParserException(string source)
        {
            Assert.That(() => Parse(source), Throws.TypeOf<ParserEx>());
        }

        // ============================================================================================================
        // List literals, subscript, slicing, attribute access, invoke, postfix chains, extended assignment
        // ============================================================================================================

        static Node ParseStmt(string source)
        {
            var root = new Parser(new Scanner(source).ScanTokens()).BuildTree();
            return ((TreeRootNode)root).Statements[0];
        }

        static ListLiteralNode AssertList(Node node, int expectedCount)
        {
            Assert.That(node, Is.InstanceOf<ListLiteralNode>());
            var list = (ListLiteralNode)node;
            Assert.That(list.Elements.Count, Is.EqualTo(expectedCount));
            return list;
        }

        static SubscriptNode AssertSubscript(Node node)
        {
            Assert.That(node, Is.InstanceOf<SubscriptNode>());
            return (SubscriptNode)node;
        }

        static SliceNode AssertSlice(Node node)
        {
            Assert.That(node, Is.InstanceOf<SliceNode>());
            return (SliceNode)node;
        }

        static AttrAccessNode AssertAttr(Node node, string expectedName)
        {
            Assert.That(node, Is.InstanceOf<AttrAccessNode>());
            var attr = (AttrAccessNode)node;
            Assert.That(attr.AttrName, Is.EqualTo(expectedName));
            return attr;
        }

        static CallNode AssertCall(Node node, int expectedArgCount)
        {
            Assert.That(node, Is.InstanceOf<CallNode>());
            var call = (CallNode)node;
            Assert.That(call.Args.Count, Is.EqualTo(expectedArgCount));
            return call;
        }

        static NameNode AssertName(Node node, string expectedName)
        {
            Assert.That(node, Is.InstanceOf<NameNode>());
            var name = (NameNode)node;
            Assert.That(name.Name, Is.EqualTo(expectedName));
            return name;
        }

        // ------------------------------------------------------------------------------------------------------------
        // List literals
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void BuildSyntaxTree_EmptyList_ReturnsListLiteralWithZeroElements()
        {
            var result = Parse("[]");
            AssertList(result, 0);
        }

        [Test]
        public void BuildSyntaxTree_SingleElementList_ReturnsListLiteral()
        {
            var result = Parse("[1]");
            var list = AssertList(result, 1);
            AssertLiteral(list.Elements[0], 1, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_MultiElementList_ReturnsListLiteralPreservingOrder()
        {
            var result = Parse("[1, 2, 3]");
            var list = AssertList(result, 3);
            AssertLiteral(list.Elements[0], 1, LiteralDataType.Integer);
            AssertLiteral(list.Elements[1], 2, LiteralDataType.Integer);
            AssertLiteral(list.Elements[2], 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_TrailingComma_IsAllowed()
        {
            var result = Parse("[1, 2,]");
            var list = AssertList(result, 2);
            AssertLiteral(list.Elements[0], 1, LiteralDataType.Integer);
            AssertLiteral(list.Elements[1], 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_NestedList_ReturnsNestedListLiterals()
        {
            var result = Parse("[[1, 2], [3]]");
            var outer = AssertList(result, 2);
            var inner0 = AssertList(outer.Elements[0], 2);
            AssertLiteral(inner0.Elements[0], 1, LiteralDataType.Integer);
            AssertLiteral(inner0.Elements[1], 2, LiteralDataType.Integer);
            var inner1 = AssertList(outer.Elements[1], 1);
            AssertLiteral(inner1.Elements[0], 3, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_MultiLineList_ParsesAcrossNewlines()
        {
            var result = Parse("[\n  1,\n  2\n]");
            AssertList(result, 2);
        }

        [Test]
        public void BuildSyntaxTree_ListWithExpressionElements_ParsesEachAsFullExpression()
        {
            var result = Parse("[1 + 2, a * b]");
            var list = AssertList(result, 2);
            AssertBinary(list.Elements[0], ExprOperator.Add);
            AssertBinary(list.Elements[1], ExprOperator.Multiply);
        }

        [Test]
        public void BuildSyntaxTree_List_AssignsLeftBracketLineNumber()
        {
            // Token stream: x = <newline> [ 1 ]  with `[` on line 3
            var result = ParseTokens(
                Token(TokenType.SymbolLeftBracket, "[", 3),
                Token(TokenType.LiteralInt, "1", 3, 1L),
                Token(TokenType.SymbolRightBracket, "]", 3),
                Token(TokenType.EndOfCode, string.Empty, 3));
            Assert.That(result.LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void BuildSyntaxTree_UnclosedList_ThrowsScannerException()
        {
            // Scanner enforces bracket balance at EOF before the parser runs.
            Assert.That(() => Parse("[1, 2"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void BuildSyntaxTree_LeadingCommaList_ThrowsParserException()
        {
            Assert.That(() => Parse("[,]"), Throws.TypeOf<ParserEx>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // Subscript (index form)
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void BuildSyntaxTree_SimpleSubscript_ReturnsSubscriptNode()
        {
            var result = Parse("a[0]");
            var sub = AssertSubscript(result);
            AssertName(sub.Target, "a");
            AssertLiteral(sub.Index, 0, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_SubscriptWithExpressionIndex_ParsesIndexAsExpression()
        {
            var result = Parse("a[i + 1]");
            var sub = AssertSubscript(result);
            AssertBinary(sub.Index, ExprOperator.Add);
        }

        [Test]
        public void BuildSyntaxTree_ChainedSubscript_NestsLeftAssociatively()
        {
            var result = Parse("arr[0][1]");
            var outer = AssertSubscript(result);
            var inner = AssertSubscript(outer.Target);
            AssertName(inner.Target, "arr");
            AssertLiteral(inner.Index, 0, LiteralDataType.Integer);
            AssertLiteral(outer.Index, 1, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_NestedIndex_ParsesInnerSubscriptAsIndex()
        {
            var result = Parse("a[b[c]]");
            var outer = AssertSubscript(result);
            var inner = AssertSubscript(outer.Index);
            AssertName(inner.Target, "b");
            AssertName(inner.Index, "c");
        }

        [Test]
        public void BuildSyntaxTree_UnclosedSubscript_ThrowsScannerException()
        {
            Assert.That(() => Parse("a[0"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void BuildSyntaxTree_EmptySubscript_ThrowsParserException()
        {
            Assert.That(() => Parse("a[]"), Throws.TypeOf<ParserEx>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // Slicing
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void BuildSyntaxTree_TwoPartSlice_PopulatesStartAndStop()
        {
            var result = Parse("a[1:5]");
            var sub = AssertSubscript(result);
            var slice = AssertSlice(sub.Index);
            AssertLiteral(slice.Start, 1, LiteralDataType.Integer);
            AssertLiteral(slice.Stop, 5, LiteralDataType.Integer);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void BuildSyntaxTree_ThreePartSlice_PopulatesStartStopAndStep()
        {
            var result = Parse("a[1:5:2]");
            var sub = AssertSubscript(result);
            var slice = AssertSlice(sub.Index);
            AssertLiteral(slice.Start, 1, LiteralDataType.Integer);
            AssertLiteral(slice.Stop, 5, LiteralDataType.Integer);
            AssertLiteral(slice.Step, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_SliceOmitStart_LeavesStartNull()
        {
            var result = Parse("a[:5]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            Assert.That(slice.Start, Is.Null);
            AssertLiteral(slice.Stop, 5, LiteralDataType.Integer);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void BuildSyntaxTree_SliceOmitStop_LeavesStopNull()
        {
            var result = Parse("a[1:]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            AssertLiteral(slice.Start, 1, LiteralDataType.Integer);
            Assert.That(slice.Stop, Is.Null);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void BuildSyntaxTree_SliceStepOnly_LeavesStartAndStopNull()
        {
            var result = Parse("a[::2]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            Assert.That(slice.Start, Is.Null);
            Assert.That(slice.Stop, Is.Null);
            AssertLiteral(slice.Step, 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_FullSliceColon_LeavesAllPartsNull()
        {
            var result = Parse("a[:]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            Assert.That(slice.Start, Is.Null);
            Assert.That(slice.Stop, Is.Null);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void BuildSyntaxTree_SliceStartOnlyDoubleColon_LeavesStopAndStepNull()
        {
            var result = Parse("a[1::]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            AssertLiteral(slice.Start, 1, LiteralDataType.Integer);
            Assert.That(slice.Stop, Is.Null);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void BuildSyntaxTree_DoubleColonNoParts_LeavesAllPartsNull()
        {
            var result = Parse("a[::]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            Assert.That(slice.Start, Is.Null);
            Assert.That(slice.Stop, Is.Null);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void BuildSyntaxTree_SliceWithExpressionParts_ParsesEachAsExpression()
        {
            var result = Parse("a[i + 1:j - 1:k * 2]");
            var slice = AssertSlice(AssertSubscript(result).Index);
            AssertBinary(slice.Start, ExprOperator.Add);
            AssertBinary(slice.Stop, ExprOperator.Subtract);
            AssertBinary(slice.Step, ExprOperator.Multiply);
        }

        // ------------------------------------------------------------------------------------------------------------
        // Attribute access
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void BuildSyntaxTree_SimpleAttribute_ReturnsAttrAccessNode()
        {
            var result = Parse("a.b");
            var attr = AssertAttr(result, "b");
            AssertName(attr.Target, "a");
        }

        [Test]
        public void BuildSyntaxTree_ChainedAttribute_NestsLeftAssociatively()
        {
            var result = Parse("a.b.c");
            var outer = AssertAttr(result, "c");
            var inner = AssertAttr(outer.Target, "b");
            AssertName(inner.Target, "a");
        }

        [Test]
        public void BuildSyntaxTree_AttributeOnParenthesizedExpression_TargetIsExpression()
        {
            var result = Parse("(1 + 2).x");
            var attr = AssertAttr(result, "x");
            AssertBinary(attr.Target, ExprOperator.Add);
        }

        [TestCase("a.")]
        [TestCase("a.1")]
        public void BuildSyntaxTree_MalformedAttribute_ThrowsParserException(string source)
        {
            Assert.That(() => Parse(source), Throws.TypeOf<ParserEx>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // Method calls and general invoke
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void BuildSyntaxTree_MethodCallNoArgs_BuildsCallOverAttrAccess()
        {
            var result = Parse("a.b()");
            var call = AssertCall(result, 0);
            var attr = AssertAttr(call.CallName, "b");
            AssertName(attr.Target, "a");
        }

        [Test]
        public void BuildSyntaxTree_MethodCallWithArgs_PreservesArgsInOrder()
        {
            var result = Parse("a.b(1, 2)");
            var call = AssertCall(result, 2);
            AssertLiteral(call.Args[0], 1, LiteralDataType.Integer);
            AssertLiteral(call.Args[1], 2, LiteralDataType.Integer);
        }

        [Test]
        public void BuildSyntaxTree_CallOnSubscript_CalleeIsSubscriptNode()
        {
            var result = Parse("arr[0](x)");
            var call = AssertCall(result, 1);
            var sub = AssertSubscript(call.CallName);
            AssertName(sub.Target, "arr");
        }

        [Test]
        public void BuildSyntaxTree_CallOnParenthesizedName_CalleeIsNameNode()
        {
            var result = Parse("(f)()");
            var call = AssertCall(result, 0);
            AssertName(call.CallName, "f");
        }

        [Test]
        public void BuildSyntaxTree_BareIdentifierCall_CalleeIsNameNode()
        {
            var result = Parse("f(x)");
            var call = AssertCall(result, 1);
            AssertName(call.CallName, "f");
        }

        [Test]
        public void BuildSyntaxTree_DoubleCall_OuterCalleeIsCallNode()
        {
            var result = Parse("f(x)(y)");
            var outer = AssertCall(result, 1);
            Assert.That(outer.CallName, Is.InstanceOf<CallNode>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // Mixed postfix chains
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void BuildSyntaxTree_AttrThenSubscript_SubscriptWrapsAttrAccess()
        {
            var result = Parse("a.b[0]");
            var sub = AssertSubscript(result);
            var attr = AssertAttr(sub.Target, "b");
            AssertName(attr.Target, "a");
        }

        [Test]
        public void BuildSyntaxTree_SubscriptThenAttr_AttrAccessWrapsSubscript()
        {
            var result = Parse("a[0].b");
            var attr = AssertAttr(result, "b");
            var sub = AssertSubscript(attr.Target);
            AssertName(sub.Target, "a");
        }

        [Test]
        public void BuildSyntaxTree_DeepMixedChain_NestsInOrderEncountered()
        {
            // a.b[0].c(x)[1].d
            var result = Parse("a.b[0].c(x)[1].d");

            var level1 = AssertAttr(result, "d");
            var level2 = AssertSubscript(level1.Target);
            AssertLiteral(level2.Index, 1, LiteralDataType.Integer);
            var level3 = AssertCall(level2.Target, 1);
            var level4 = AssertAttr(level3.CallName, "c");
            var level5 = AssertSubscript(level4.Target);
            AssertLiteral(level5.Index, 0, LiteralDataType.Integer);
            var level6 = AssertAttr(level5.Target, "b");
            AssertName(level6.Target, "a");
        }

        [Test]
        public void BuildSyntaxTree_CallThenAttr_AttrAccessWrapsCallNode()
        {
            var result = Parse("a().b");
            var attr = AssertAttr(result, "b");
            Assert.That(attr.Target, Is.InstanceOf<CallNode>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // Extended assignment LHS
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void ParseStmt_SimpleNameAssignment_StillProducesVarAssignNode()
        {
            var stmt = ParseStmt("a = 1");
            Assert.That(stmt, Is.InstanceOf<VarAssignNode>());
            var var = (VarAssignNode)stmt;
            Assert.That(var.Name, Is.EqualTo("a"));
            AssertLiteral(var.Expression, 1, LiteralDataType.Integer);
        }

        [Test]
        public void ParseStmt_SubscriptAssignment_ProducesSubscriptAssignNode()
        {
            var stmt = ParseStmt("a[0] = x");
            Assert.That(stmt, Is.InstanceOf<SubscriptAssignNode>());
            var assign = (SubscriptAssignNode)stmt;
            AssertName(assign.Target, "a");
            AssertLiteral(assign.Index, 0, LiteralDataType.Integer);
            AssertName(assign.Expression, "x");
        }

        [Test]
        public void ParseStmt_SliceAssignment_IndexIsSliceNode()
        {
            var stmt = ParseStmt("a[1:5] = x");
            Assert.That(stmt, Is.InstanceOf<SubscriptAssignNode>());
            var assign = (SubscriptAssignNode)stmt;
            AssertSlice(assign.Index);
        }

        [Test]
        public void ParseStmt_FullSliceAssignment_IndexIsEmptySliceNode()
        {
            var stmt = ParseStmt("a[:] = x");
            var assign = (SubscriptAssignNode)stmt;
            var slice = AssertSlice(assign.Index);
            Assert.That(slice.Start, Is.Null);
            Assert.That(slice.Stop, Is.Null);
            Assert.That(slice.Step, Is.Null);
        }

        [Test]
        public void ParseStmt_AttributeAssignment_ProducesAttrAssignNode()
        {
            var stmt = ParseStmt("a.b = x");
            Assert.That(stmt, Is.InstanceOf<AttrAssignNode>());
            var assign = (AttrAssignNode)stmt;
            AssertName(assign.Target, "a");
            Assert.That(assign.AttrName, Is.EqualTo("b"));
            AssertName(assign.Expression, "x");
        }

        [Test]
        public void ParseStmt_ChainedAttrThenSubscriptAssign_SubscriptAssignWrapsAttrAccess()
        {
            var stmt = ParseStmt("a.b[0] = x");
            var assign = (SubscriptAssignNode)stmt;
            AssertAttr(assign.Target, "b");
        }

        [Test]
        public void ParseStmt_ChainedAttrAssign_AttrAssignWrapsAttrAccess()
        {
            var stmt = ParseStmt("a.b.c = x");
            var assign = (AttrAssignNode)stmt;
            Assert.That(assign.AttrName, Is.EqualTo("c"));
            AssertAttr(assign.Target, "b");
        }

        [Test]
        public void ParseStmt_SubscriptThenAttrAssign_AttrAssignWrapsSubscript()
        {
            var stmt = ParseStmt("a[0].b = x");
            var assign = (AttrAssignNode)stmt;
            Assert.That(assign.AttrName, Is.EqualTo("b"));
            AssertSubscript(assign.Target);
        }

        [TestCase("1 = x")]
        [TestCase("(a + b) = x")]
        [TestCase("f() = x")]
        [TestCase("a.b. = x")]
        [TestCase("a[] = x")]
        public void ParseStmt_InvalidAssignmentTarget_ThrowsParserException(string source)
        {
            Assert.That(() => ParseStmt(source), Throws.TypeOf<ParserEx>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // Standalone expression statements
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void ParseStmt_StandaloneSubscript_WrapsInExprStatementNode()
        {
            var stmt = ParseStmt("a[0]");
            Assert.That(stmt, Is.InstanceOf<ExprStatementNode>());
            var exprStmt = (ExprStatementNode)stmt;
            AssertSubscript(exprStmt.Expression);
        }

        [Test]
        public void ParseStmt_StandaloneMethodCall_WrapsInvokeInExprStatementNode()
        {
            var stmt = ParseStmt("a.b()");
            Assert.That(stmt, Is.InstanceOf<ExprStatementNode>());
            var exprStmt = (ExprStatementNode)stmt;
            AssertCall(exprStmt.Expression, 0);
        }

        [Test]
        public void ParseStmt_IfBlock_ReturnsIfNode()
        {
            var stmt = ParseStmt("if True:\n    x = 1");

            var ifNode = (IfNode)stmt;
            Assert.Multiple(() =>
            {
                Assert.That(ifNode.Expr, Is.InstanceOf<LiteralNode>());
                Assert.That(ifNode.Block, Is.InstanceOf<BlockNode>());
                Assert.That(ifNode.Branch, Is.Null);
            });
        }

        [Test]
        public void ParseStmt_IfElifElse_ReturnsBranchChain()
        {
            var stmt = ParseStmt("if False:\n    x = 1\nelif True:\n    x = 2\nelse:\n    x = 3");

            var ifNode = (IfNode)stmt;
            Assert.That(ifNode.Branch, Is.InstanceOf<BranchStmntNode>());
        }

        [Test]
        public void ParseStmt_WhileBlock_ReturnsWhileNode()
        {
            var stmt = ParseStmt("while True:\n    break");

            var whileNode = (WhileNode)stmt;
            Assert.Multiple(() =>
            {
                Assert.That(whileNode.Expr, Is.InstanceOf<LiteralNode>());
                Assert.That(whileNode.Block, Is.InstanceOf<BlockNode>());
            });
        }

        [Test]
        public void ParseStmt_FunctionDefinition_ReturnsFunctionNode()
        {
            var stmt = ParseStmt("def add(a, b):\n    return a + b");

            var function = (FunctionNode)stmt;
            Assert.Multiple(() =>
            {
                Assert.That(function.Name, Is.EqualTo("add"));
                Assert.That(function.Params, Has.Count.EqualTo(2));
                Assert.That(function.Body, Is.InstanceOf<BlockNode>());
            });
        }

        [Test]
        public void ParseStmt_Return_ReturnsReturnNode()
        {
            var stmt = ParseStmt("return 1");

            Assert.That(stmt, Is.InstanceOf<ReturnNode>());
        }

        [Test]
        public void ParseStmt_Break_ReturnsBreakNode()
        {
            var stmt = ParseStmt("break");

            Assert.That(stmt, Is.InstanceOf<BreakNode>());
        }

        [Test]
        public void ParseStmt_Continue_ReturnsContinueNode()
        {
            var stmt = ParseStmt("continue");

            Assert.That(stmt, Is.InstanceOf<ContinueNode>());
        }

        // ------------------------------------------------------------------------------------------------------------
        // ToString smoke
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void ToString_NewNodes_IncludeLineNumber()
        {
            var list = Parse("[1]");
            Assert.That(list.ToString(), Does.Contain("line=1"));

            var sub = Parse("a[0]");
            Assert.That(sub.ToString(), Does.Contain("Subscript"));

            var slice = Parse("a[1:5]");
            Assert.That(slice.ToString(), Does.Contain("Slice"));

            var attr = Parse("a.b");
            Assert.That(attr.ToString(), Does.Contain("AttrAccess"));

            var call = Parse("a.b()");
            Assert.That(call.ToString(), Does.Contain("AttrAccess"));

            var subAssign = ParseStmt("a[0] = x");
            Assert.That(subAssign.ToString(), Does.Contain("SubscriptAssign"));

            var attrAssign = ParseStmt("a.b = x");
            Assert.That(attrAssign.ToString(), Does.Contain("AttrAssign"));
        }
    }
}
