using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;

namespace Chow.Tests
{
    [TestFixture]
    public class ScannerTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static List<Token> Tokenize(string source) => new Scanner(source).ScanTokens();

        static List<TokenType> TokenTypes(string source) => Tokenize(source).Select(token => token.type).ToList();

        static void AssertToken(
            Token token,
            TokenType expectedType,
            string expectedLexeme,
            int expectedLineNum,
            object? expectedLiteral = null)
        {
            Assert.Multiple(() =>
            {
                Assert.That(token.type, Is.EqualTo(expectedType));
                Assert.That(token.lexeme, Is.EqualTo(expectedLexeme));
                Assert.That(token.lineNum, Is.EqualTo(expectedLineNum));
                Assert.That(token.literal, Is.EqualTo(expectedLiteral));
            });
        }

        // ============================================================================================================
        // A. Empty / minimal input
        // ============================================================================================================

        [Test]
        public void ScanTokens_EmptySource_ReturnsEndOfCode()
        {
            var tokens = new Scanner("").ScanTokens();
            Assert.That(tokens.Select(t => t.type), Is.EqualTo(new[] { TokenType.EndOfCode }));
        }

        // ============================================================================================================
        // B. Newline variants & line numbering
        // ============================================================================================================

        [Test]
        public void ScanTokens_CrlfPair_TreatedAsSingleNewline()
        {
            var tokens = Tokenize("42\r\n42\r\n");

            Assert.That(tokens, Has.Count.EqualTo(5));
            AssertToken(tokens[0], TokenType.LiteralInt, "42", 1, 42);
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.LiteralInt, "42", 2, 42);
            AssertToken(tokens[3], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[4], TokenType.EndOfCode, "", 3, null);
        }

        [Test]
        public void ScanTokens_MixedLineEndingsWithinSource_AllTreatedAsNewlinesIndividually()
        {
            var tokens = Tokenize("1\n2\r\n3\r4\n");

            Assert.That(tokens, Has.Count.EqualTo(9));
            AssertToken(tokens[0], TokenType.LiteralInt, "1", 1, 1);
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.LiteralInt, "2", 2, 2);
            AssertToken(tokens[3], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[4], TokenType.LiteralInt, "3", 3, 3);
            AssertToken(tokens[5], TokenType.Newline, "\n", 3, null);
            AssertToken(tokens[6], TokenType.LiteralInt, "4", 4, 4);
            AssertToken(tokens[7], TokenType.Newline, "\n", 4, null);
            AssertToken(tokens[8], TokenType.EndOfCode, "", 5, null);
        }

        [Test]
        public void ScanTokens_BlankLineBetweenStatements_PreservesBlankNewline()
        {
            var tokens = Tokenize("1\n\n2\n");

            Assert.That(tokens, Has.Count.EqualTo(6));
            AssertToken(tokens[0], TokenType.LiteralInt, "1", 1, 1);
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[3], TokenType.LiteralInt, "2", 3, 2);
            AssertToken(tokens[4], TokenType.Newline, "\n", 3, null);
            AssertToken(tokens[5], TokenType.EndOfCode, "", 4, null);
        }

        [TestCase("\n")]
        [TestCase("\r\n")]
        [TestCase("\r")]
        public void ScanTokens_NewlineLexemeAlwaysCanonicalLineFeed(string newline)
        {
            var tokens = Tokenize("1" + newline);

            var nlToken = tokens.First(t => t.type == TokenType.Newline);
            Assert.That(nlToken.lexeme, Is.EqualTo("\n"));
        }

        // ============================================================================================================
        // C. Integer literals
        // ============================================================================================================

        [Test]
        public void ScanTokens_Zero_ProducesIntegerWithValueZero()
        {
            var tokens = Tokenize("0");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.LiteralInt, "0", 1, 0);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_TwoDigitInteger_ProducesIntegerWithMatchingValue()
        {
            var tokens = Tokenize("42");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.LiteralInt, "42", 1, 42);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_NineDigitInteger_ParsesCorrectly()
        {
            var tokens = Tokenize("123456789");

            AssertToken(tokens[0], TokenType.LiteralInt, "123456789", 1, 123456789);
        }

        [Test]
        public void ScanTokens_LongMaxValueBoundary_ParsesAsInteger()
        {
            var tokens = Tokenize("9223372036854775807");

            AssertToken(tokens[0], TokenType.LiteralInt, "9223372036854775807", 1, long.MaxValue);
        }

        [Test]
        public void ScanTokens_IntegerOverflow_ThrowsOverflowException()
        {
            Assert.That(() => Tokenize("9223372036854775808"), Throws.TypeOf<OverflowException>());
        }

        [Test]
        public void ScanTokens_LeadingZeros_LexemePreservedValueParsedNumerically()
        {
            var tokens = Tokenize("007");

            AssertToken(tokens[0], TokenType.LiteralInt, "007", 1, 7);
        }

        [Test]
        public void ScanTokens_IntegersOnSeparateLines_TrackLineNumbers()
        {
            var tokens = Tokenize("1\n2\n3\n");

            var integers = tokens.Where(t => t.type == TokenType.LiteralInt).ToList();
            Assert.That(integers, Has.Count.EqualTo(3));
            Assert.That(integers[0].lineNum, Is.EqualTo(1));
            Assert.That(integers[1].lineNum, Is.EqualTo(2));
            Assert.That(integers[2].lineNum, Is.EqualTo(3));
        }

        [Test]
        public void ScanTokens_IntegerLiteralIsBoxedInt64()
        {
            var tokens = Tokenize("42");

            Assert.That(tokens[0].literal, Is.TypeOf<long>());
        }

        // ============================================================================================================
        // D. Float literals
        // ============================================================================================================

        [Test]
        public void ScanTokens_DecimalLiteral_ProducesFloatToken()
        {
            var tokens = Tokenize("3.14");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.LiteralFloat, "3.14", 1, 3.14);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_ZeroPointZero_ProducesFloatZero()
        {
            var tokens = Tokenize("0.0");

            AssertToken(tokens[0], TokenType.LiteralFloat, "0.0", 1, 0.0);
        }

        [Test]
        public void ScanTokens_TrailingDotFloat_AcceptedAsFloat()
        {
            var tokens = Tokenize("3.");

            AssertToken(tokens[0], TokenType.LiteralFloat, "3.", 1, 3.0);
        }

        [Test]
        public void ScanTokens_FloatOnLaterLine_HasCorrectLineNumber()
        {
            var tokens = Tokenize("\n3.14\n");

            var floatToken = tokens.First(t => t.type == TokenType.LiteralFloat);
            Assert.That(floatToken.lineNum, Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_FloatLiteralIsBoxedDouble()
        {
            var tokens = Tokenize("3.14");

            Assert.That(tokens[0].literal, Is.TypeOf<double>());
        }

        [Test]
        public void ScanTokens_IntegerVsFloat_PresenceOfDotChangesType()
        {
            var intTokens = Tokenize("42");
            var floatTokens = Tokenize("42.0");

            Assert.That(intTokens[0].type, Is.EqualTo(TokenType.LiteralInt));
            Assert.That(floatTokens[0].type, Is.EqualTo(TokenType.LiteralFloat));
        }

        // ============================================================================================================
        // E. Single-character lexemes
        // ============================================================================================================

        [TestCase(",", TokenType.SymbolComma)]
        [TestCase(".", TokenType.SymbolDot)]
        [TestCase(":", TokenType.SymbolColon)]
        [TestCase("+", TokenType.SymbolPlus)]
        [TestCase("-", TokenType.SymbolMinus)]
        [TestCase("*", TokenType.SymbolMultiply)]
        [TestCase("/", TokenType.SymbolDivide)]
        [TestCase("%", TokenType.SymbolPercent)]
        [TestCase("=", TokenType.SymbolAssign)]
        [TestCase(">", TokenType.SymbolGreater)]
        [TestCase("<", TokenType.SymbolLess)]
        public void ScanTokens_SingleCharacterLexeme_ProducesExpectedToken(string source, object expectedType)
        {
            var tokens = Tokenize(source);

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], (TokenType)expectedType, source, 1, null);
            AssertToken(tokens[1], TokenType.EndOfCode, string.Empty, 1, null);
        }

        [TestCase("()", TokenType.SymbolLeftParen, TokenType.SymbolRightParen)]
        [TestCase("[]", TokenType.SymbolLeftBracket, TokenType.SymbolRightBracket)]
        [TestCase("{}", TokenType.SymbolLeftCurly, TokenType.SymbolRightCurly)]
        public void ScanTokens_MatchedClosingDelimiter_ProducesExpectedTokens(
            string source,
            object expectedOpenType,
            object expectedCloseType)
        {
            var tokens = Tokenize(source);

            Assert.That(tokens, Has.Count.EqualTo(3));
            AssertToken(tokens[0], (TokenType)expectedOpenType, source[0].ToString(), 1, null);
            AssertToken(tokens[1], (TokenType)expectedCloseType, source[1].ToString(), 1, null);
            AssertToken(tokens[2], TokenType.EndOfCode, string.Empty, 1, null);
        }

        [TestCase("]")]
        [TestCase("}")]
        public void ScanTokens_UnmatchedClosingDelimiter_ThrowsScannerException(string source)
        {
            Assert.That(() => Tokenize(source), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void ScanTokens_SingleCharacterLexemeSequence_ProducesTokenForEachCharacter()
        {
            var tokenTypes = TokenTypes("()[]{} ,.:+-*/%=><".Replace(" ", string.Empty));

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.SymbolLeftParen,
                TokenType.SymbolRightParen,
                TokenType.SymbolLeftBracket,
                TokenType.SymbolRightBracket,
                TokenType.SymbolLeftCurly,
                TokenType.SymbolRightCurly,
                TokenType.SymbolComma,
                TokenType.SymbolDot,
                TokenType.SymbolColon,
                TokenType.SymbolPlus,
                TokenType.SymbolMinus,
                TokenType.SymbolMultiply,
                TokenType.SymbolDivide,
                TokenType.SymbolPercent,
                TokenType.SymbolAssign,
                TokenType.SymbolGreater,
                TokenType.SymbolLess,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_SingleCharacterLexemeOnLaterLine_HasCurrentLineNumber()
        {
            var tokens = Tokenize("\n+");

            AssertToken(tokens[0], TokenType.SymbolPlus, "+", 2, null);
        }

        // ============================================================================================================
        // E2. Multi-character arithmetic operator lexemes
        // ============================================================================================================

        [Test]
        public void ScanTokens_StarStarLexeme_ProducesStarStarToken()
        {
            var tokens = Tokenize("**");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.SymbolExponent, "**", 1, null);
            AssertToken(tokens[1], TokenType.EndOfCode, string.Empty, 1, null);
        }

        [Test]
        public void ScanTokens_SlashSlashLexeme_ProducesSlashSlashToken()
        {
            var tokens = Tokenize("//");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.SymbolFloorDivide, "//", 1, null);
            AssertToken(tokens[1], TokenType.EndOfCode, string.Empty, 1, null);
        }

        [Test]
        public void ScanTokens_StarFollowedBySpaceStar_ProducesTwoStarTokens()
        {
            var tokens = Tokenize("* *");

            Assert.That(tokens[0].type, Is.EqualTo(TokenType.SymbolMultiply));
            Assert.That(tokens[1].type, Is.EqualTo(TokenType.SymbolMultiply));
        }

        [Test]
        public void ScanTokens_SlashFollowedBySpaceSlash_ProducesTwoSlashTokens()
        {
            var tokens = Tokenize("/ /");

            Assert.That(tokens[0].type, Is.EqualTo(TokenType.SymbolDivide));
            Assert.That(tokens[1].type, Is.EqualTo(TokenType.SymbolDivide));
        }

        // ============================================================================================================
        // F. Indent / Dedent
        // ============================================================================================================

        [Test]
        public void ScanTokens_IndentedSecondLine_EmitsIndentAndFinalDedent()
        {
            var tokens = Tokenize("1\n    2\n");

            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_DedentToBaseLine_EmitsExactlyOneDedent()
        {
            var tokens = Tokenize("1\n    2\n3\n");

            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_SameIndentTwoConsecutiveLines_EmitsNoAdditionalIndentOrDedentUntilEof()
        {
            var tokens = Tokenize("1\n    2\n    3\n");

            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_DedentSpanningMultipleLevels_EmitsOneDedentPerClosedIndent()
        {
            var tokens = Tokenize("1\n    2\n        3\n1\n");

            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(2));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_NestedIndentAtEof_EmitsOneDedentPerRemainingIndent()
        {
            var tokenTypes = TokenTypes("1\n    2\n        3");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Indent,
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Indent,
                TokenType.LiteralInt,
                TokenType.Dedent,
                TokenType.Dedent,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_DedentToPreviousIndentLevel_IsAccepted()
        {
            var tokenTypes = TokenTypes("1\n    2\n        3\n    4\n");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Indent,
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Indent,
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Dedent,
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Dedent,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_DedentToUnmatchedIndentLevel_ThrowsScannerException()
        {
            Assert.That(() => Tokenize("1\n    2\n  3\n"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void ScanTokens_DedentBeforeContent_EmitsDedentBeforeLineToken()
        {
            var tokenTypes = TokenTypes("1\n    2\n3");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Indent,
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Dedent,
                TokenType.LiteralInt,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_IndentToken_HasSingleSpaceLexeme()
        {
            var tokens = Tokenize("1\n    2\n");

            var indent = tokens.First(t => t.type == TokenType.Indent);
            Assert.That(indent.lexeme, Is.EqualTo(" "));
        }

        [Test]
        public void ScanTokens_DedentToken_HasEmptyLexeme()
        {
            var tokens = Tokenize("1\n    2\n3\n");

            var dedent = tokens.First(t => t.type == TokenType.Dedent);
            Assert.That(dedent.lexeme, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ScanTokens_IndentAndDedent_LiteralIsNull()
        {
            var tokens = Tokenize("1\n    2\n3\n");

            var indent = tokens.First(t => t.type == TokenType.Indent);
            var dedent = tokens.First(t => t.type == TokenType.Dedent);
            Assert.That(indent.literal, Is.Null);
            Assert.That(dedent.literal, Is.Null);
        }

        [Test]
        public void ScanTokens_IndentLineNumber_MatchesNewlyIndentedLine()
        {
            var tokens = Tokenize("1\n    2\n");

            var indent = tokens.First(t => t.type == TokenType.Indent);
            Assert.That(indent.lineNum, Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_TabFromColumnZero_EquivalentToEightSpaces()
        {
            var tabbed = Tokenize("1\n\t2\n");
            var spaced = Tokenize("1\n        2\n");

            Assert.That(tabbed.Count(t => t.type == TokenType.Indent),
                Is.EqualTo(spaced.Count(t => t.type == TokenType.Indent)));
            Assert.That(tabbed.Count(t => t.type == TokenType.Dedent),
                Is.EqualTo(spaced.Count(t => t.type == TokenType.Dedent)));
        }

        [Test]
        public void ScanTokens_TabAfterPartialColumn_RoundsUpToNextMultipleOfEight()
        {
            // line 2 = 4 spaces (col 4). line 3 = 4 spaces + tab -> col 8 (deeper).
            var tokens = Tokenize("1\n    2\n    \t3\n");

            // Two distinct indent levels established -> two Indent tokens.
            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(2));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_BlankLineBetweenIndentedLines_DoesNotPerturbIndentTracking()
        {
            var tokens = Tokenize("1\n    2\n\n    3\n");

            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_WhitespaceOnlyLine_TreatedAsBlankForIndentPurposes()
        {
            var tokens = Tokenize("1\n    2\n        \n    3\n");

            Assert.That(tokens.Count(t => t.type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_FormFeedAtLineStart_IgnoredForIndentCalculation()
        {
            var tokenTypes = TokenTypes("\f    42");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.Indent,
                TokenType.LiteralInt,
                TokenType.Dedent,
                TokenType.EndOfCode
            }));
        }

        // ============================================================================================================
        // G. EndOfCode terminal token
        // ============================================================================================================

        [TestCase("42")]
        [TestCase("42\n")]
        [TestCase("1\n    2\n")]
        public void ScanTokens_EndOfCodeIsAlwaysLastToken(string source)
        {
            var tokens = Tokenize(source);

            Assert.That(tokens[^1].type, Is.EqualTo(TokenType.EndOfCode));
        }

        [TestCase("42")]
        [TestCase("42\n")]
        [TestCase("1\n    2\n")]
        public void ScanTokens_ExactlyOneEndOfCodeEmitted(string source)
        {
            var tokens = Tokenize(source);

            Assert.That(tokens.Count(t => t.type == TokenType.EndOfCode), Is.EqualTo(1));
        }

        [TestCase("42", 1)]
        [TestCase("42\n", 2)]
        public void ScanTokens_EndOfCodeLineNumber_EqualsOnePlusNewlineCount(string source, int expectedLine)
        {
            var tokens = Tokenize(source);

            var eoc = tokens.Single(t => t.type == TokenType.EndOfCode);
            Assert.That(eoc.lineNum, Is.EqualTo(expectedLine));
        }

        [Test]
        public void ScanTokens_EndOfCode_LexemeIsEmptyAndLiteralIsNull()
        {
            var tokens = Tokenize("42");

            var eoc = tokens.Single(t => t.type == TokenType.EndOfCode);
            Assert.That(eoc.lexeme, Is.EqualTo(string.Empty));
            Assert.That(eoc.literal, Is.Null);
        }

        // ============================================================================================================
        // H. Cross-cutting line-number correctness
        // ============================================================================================================

        [Test]
        public void ScanTokens_NewlineToken_TaggedWithLineThatJustEnded()
        {
            var tokens = Tokenize("42\n");

            var newline = tokens.Single(t => t.type == TokenType.Newline);
            Assert.That(newline.lineNum, Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_IntegerOnFifthLine_HasLineNumberFive()
        {
            var tokens = Tokenize("\n\n\n\n42\n");

            var integer = tokens.Single(t => t.type == TokenType.LiteralInt);
            Assert.That(integer.lineNum, Is.EqualTo(5));
        }

        [Test]
        public void ScanTokens_IndentOnThirdLine_HasLineNumberThree()
        {
            var tokens = Tokenize("1\n2\n    3\n");

            var indent = tokens.Single(t => t.type == TokenType.Indent);
            Assert.That(indent.lineNum, Is.EqualTo(3));
        }

        // ============================================================================================================
        // I. State / idempotency
        // ============================================================================================================

        [Test]
        public void ScanTokens_CalledTwiceOnSameInstance_ReturnsEquivalentTokens()
        {
            var scanner = new Scanner("1\n    2\n3\n");

            var first = scanner.ScanTokens();
            var second = scanner.ScanTokens();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ScanTokens_TwoIndependentInstancesOnSameInput_ReturnEqualSequences()
        {
            var s1 = new Scanner("1\n    2\n3\n");
            var s2 = new Scanner("1\n    2\n3\n");

            Assert.That(s2.ScanTokens(), Is.EqualTo(s1.ScanTokens()));
        }

        [Test]
        public void ScanTokens_CalledTwiceOnWhitespaceOnlySource_ReturnsEquivalentTokens()
        {
            var scanner = new Scanner("    ");

            var first = scanner.ScanTokens();
            var second = scanner.ScanTokens();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ScanTokens_CalledAgainAfterScannerException_ThrowsSameScannerException()
        {
            var scanner = new Scanner("@");

            Assert.That(() => scanner.ScanTokens(), Throws.TypeOf<ScannerEx>());
            Assert.That(() => scanner.ScanTokens(), Throws.TypeOf<ScannerEx>());
        }

        // ============================================================================================================
        // J. Scanner/parser boundary for indentation
        // ============================================================================================================

        [Test]
        public void ScanTokens_LeadingSpacesOnFirstLine_ThrowsScannerException()
        {
            Assert.That(() => TokenTypes("    42\n"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void ScanTokens_UnclosedLeftParen_ThrowsScannerException()
        {
            Assert.That(() => Tokenize("(1 + 2"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void ScanTokens_UnmatchedRightParen_ThrowsScannerException()
        {
            Assert.That(() => Tokenize(")"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void ScanTokens_LeadingTabOnFirstLine_ThrowsScannerException()
        {
            Assert.That(() => TokenTypes("\t42\n"), Throws.TypeOf<ScannerEx>());
        }

        [Test]
        public void ScanTokens_IndentedBlankLineBeforeCode_DoesNotThrow()
        {
            var tokenTypes = TokenTypes("    \n42\n");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_LeadingNewlinesBeforeCode_NotEmittedAsTokens()
        {
            var tokenTypes = TokenTypes("\n\n42\n");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_NewlinesOnlySource_ReturnsEndOfCode()
        {
            var tokens = new Scanner("\n\n\n").ScanTokens();
            Assert.That(tokens.Select(t => t.type), Is.EqualTo(new[] { TokenType.EndOfCode }));
        }

        // ============================================================================================================
        // K. Constructor
        // ============================================================================================================

        [Test]
        public void ScanTokens_NullSource_ReturnsEndOfCode()
        {
            var tokens = new Scanner(null!).ScanTokens();
            Assert.That(tokens.Select(t => t.type), Is.EqualTo(new[] { TokenType.EndOfCode }));
        }

        // ============================================================================================================
        // L. Comments
        // ============================================================================================================

        [Test]
        public void ScanTokens_InlineComment_SkipsRestOfLine()
        {
            var tokenTypes = TokenTypes("42 # this is a comment\n");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_CommentOnlySource_ReturnsEndOfCode()
        {
            var tokens = new Scanner("# only a comment").ScanTokens();

            Assert.That(tokens.Select(t => t.type), Is.EqualTo(new[] { TokenType.EndOfCode }));
        }

        [Test]
        public void ScanTokens_IndentedCommentLine_DoesNotAffectIndentStack()
        {
            var tokenTypes = TokenTypes("1\n    # indented comment\n2\n");

            Assert.That(tokenTypes, Is.EqualTo(new[]
            {
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.Newline,
                TokenType.LiteralInt,
                TokenType.Newline,
                TokenType.EndOfCode
            }));
        }

        [Test]
        public void ScanTokens_CommentLine_AdvancesLineNumber()
        {
            var tokens = Tokenize("1\n# comment\n2\n");

            var integers = tokens.Where(t => t.type == TokenType.LiteralInt).ToList();
            Assert.That(integers[0].lineNum, Is.EqualTo(1));
            Assert.That(integers[1].lineNum, Is.EqualTo(3));
        }

        // ============================================================================================================
        // M. Lexeme correctness for literal slices
        // ============================================================================================================

        [TestCase("0")]
        [TestCase("42")]
        [TestCase("007")]
        [TestCase("2147483647")]
        public void ScanTokens_IntegerLexeme_EqualsExactSourceSlice(string source)
        {
            var tokens = Tokenize(source);

            var integer = tokens.Single(t => t.type == TokenType.LiteralInt);
            Assert.That(integer.lexeme, Is.EqualTo(source));
        }

        [TestCase("0.0")]
        [TestCase("3.14")]
        [TestCase("3.")]
        public void ScanTokens_FloatLexeme_EqualsExactSourceSlice(string source)
        {
            var tokens = Tokenize(source);

            var floatToken = tokens.Single(t => t.type == TokenType.LiteralFloat);
            Assert.That(floatToken.lexeme, Is.EqualTo(source));
        }

        // ============================================================================================================
        // N. String literals
        // ============================================================================================================

        [Test]
        public void ScanTokens_SingleQuotedString_ProducesLiteralStrToken()
        {
            var tokens = Tokenize("'hello'");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.LiteralStr, "'hello'", 1, "hello");
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_DoubleQuotedString_ProducesLiteralStrToken()
        {
            var tokens = Tokenize("\"hello\"");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.LiteralStr, "\"hello\"", 1, "hello");
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [TestCase("''")]
        [TestCase("\"\"")]
        public void ScanTokens_EmptyString_ProducesLiteralStrTokenWithEmptyLiteral(string source)
        {
            var tokens = Tokenize(source);

            AssertToken(tokens[0], TokenType.LiteralStr, source, 1, "");
        }

        [Test]
        public void ScanTokens_SingleQuoteInsideDoubleQuotedString_KeptVerbatim()
        {
            var tokens = Tokenize("\"it's\"");

            AssertToken(tokens[0], TokenType.LiteralStr, "\"it's\"", 1, "it's");
        }

        [Test]
        public void ScanTokens_DoubleQuoteInsideSingleQuotedString_KeptVerbatim()
        {
            var tokens = Tokenize("'say \"hi\"'");

            AssertToken(tokens[0], TokenType.LiteralStr, "'say \"hi\"'", 1, "say \"hi\"");
        }

        [Test]
        public void ScanTokens_StringFollowedByNewline_EmitsStringThenNewline()
        {
            var tokens = Tokenize("'x'\n");

            Assert.That(tokens, Has.Count.EqualTo(3));
            AssertToken(tokens[0], TokenType.LiteralStr, "'x'", 1, "x");
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.EndOfCode, "", 2, null);
        }

        [TestCase("'abc")]
        [TestCase("\"abc")]
        [TestCase("'")]
        public void ScanTokens_UnterminatedStringAtEof_ThrowsScannerException(string source)
        {
            Assert.That(() => Tokenize(source), Throws.TypeOf<ScannerEx>());
        }

        [TestCase("'abc\n'")]
        [TestCase("\"abc\nxyz\"")]
        public void ScanTokens_NewlineInsideString_ThrowsScannerException(string source)
        {
            Assert.That(() => Tokenize(source), Throws.TypeOf<ScannerEx>());
        }
    }
}
