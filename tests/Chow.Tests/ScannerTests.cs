using Chow;

namespace Chow.Tests
{
    [TestFixture]
    public class ScannerTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static List<Token> Tokenize(string source) => new Scanner(source).ScanTokens();

        static void AssertToken(
            Token token,
            TokenType expectedType,
            string expectedLexeme,
            int expectedLineNum,
            object? expectedLiteral = null)
        {
            Assert.Multiple(() =>
            {
                Assert.That(token.Type, Is.EqualTo(expectedType));
                Assert.That(token.Lexeme, Is.EqualTo(expectedLexeme));
                Assert.That(token.LineNum, Is.EqualTo(expectedLineNum));
                Assert.That(token.Literal, Is.EqualTo(expectedLiteral));
            });
        }

        // Runs ScanTokens on a worker thread; fails the test cleanly on timeout
        // instead of hanging the runner. Used for inputs that may infinite-loop
        // on the current Scanner implementation.
        static void AssertScanThrowsWithinTimeout(string source, int timeoutMs = 1000)
        {
            Exception? captured = null;
            var task = Task.Run(() =>
            {
                try { new Scanner(source).ScanTokens(); }
                catch (Exception ex) { captured = ex; }
            });

            bool finished = task.Wait(timeoutMs);

            Assert.That(finished, Is.True,
                $"Scanner did not terminate within {timeoutMs}ms on input {Repr(source)} — likely infinite loop.");
            Assert.That(captured, Is.Not.Null,
                $"Scanner terminated without throwing on input {Repr(source)}.");
        }

        static string Repr(string s) => "\"" + s
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t") + "\"";

        // ============================================================================================================
        // A. Empty / minimal input
        // ============================================================================================================

        [Test]
        public void ScanTokens_EmptySource_ReturnsOnlyEndOfCodeAtLineOne()
        {
            var tokens = Tokenize("");

            Assert.That(tokens, Has.Count.EqualTo(1));
            AssertToken(tokens[0], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_SingleLineFeed_EmitsOneNewlineThenEndOfCode()
        {
            var tokens = Tokenize("\n");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 2, null);
        }

        [Test]
        public void ScanTokens_SingleCarriageReturnLineFeed_EmitsOneNewlineWithCanonicalLexeme()
        {
            var tokens = Tokenize("\r\n");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 2, null);
        }

        [Test]
        public void ScanTokens_SingleBareCarriageReturn_TreatedAsNewline()
        {
            var tokens = Tokenize("\r");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 2, null);
        }

        [Test]
        public void ScanTokens_ConsecutiveNewlines_ProducesOneNewlineTokenEach()
        {
            var tokens = Tokenize("\n\n\n");

            Assert.That(tokens, Has.Count.EqualTo(4));
            AssertToken(tokens[0], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[1], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[2], TokenType.Newline, "\n", 3, null);
            AssertToken(tokens[3], TokenType.EndOfCode, "", 4, null);
        }

        // ============================================================================================================
        // B. Newline variants & line numbering
        // ============================================================================================================

        [Test]
        public void ScanTokens_CrlfPair_TreatedAsSingleNewline()
        {
            var tokens = Tokenize("42\r\n42\r\n");

            Assert.That(tokens, Has.Count.EqualTo(5));
            AssertToken(tokens[0], TokenType.Integer, "42", 1, 42);
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.Integer, "42", 2, 42);
            AssertToken(tokens[3], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[4], TokenType.EndOfCode, "", 3, null);
        }

        [Test]
        public void ScanTokens_MixedLineEndingsWithinSource_AllTreatedAsNewlinesIndividually()
        {
            var tokens = Tokenize("1\n2\r\n3\r4\n");

            Assert.That(tokens, Has.Count.EqualTo(9));
            AssertToken(tokens[0], TokenType.Integer, "1", 1, 1);
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.Integer, "2", 2, 2);
            AssertToken(tokens[3], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[4], TokenType.Integer, "3", 3, 3);
            AssertToken(tokens[5], TokenType.Newline, "\n", 3, null);
            AssertToken(tokens[6], TokenType.Integer, "4", 4, 4);
            AssertToken(tokens[7], TokenType.Newline, "\n", 4, null);
            AssertToken(tokens[8], TokenType.EndOfCode, "", 5, null);
        }

        [Test]
        public void ScanTokens_BlankLineBetweenStatements_PreservesBlankNewline()
        {
            var tokens = Tokenize("1\n\n2\n");

            Assert.That(tokens, Has.Count.EqualTo(6));
            AssertToken(tokens[0], TokenType.Integer, "1", 1, 1);
            AssertToken(tokens[1], TokenType.Newline, "\n", 1, null);
            AssertToken(tokens[2], TokenType.Newline, "\n", 2, null);
            AssertToken(tokens[3], TokenType.Integer, "2", 3, 2);
            AssertToken(tokens[4], TokenType.Newline, "\n", 3, null);
            AssertToken(tokens[5], TokenType.EndOfCode, "", 4, null);
        }

        [TestCase("\n")]
        [TestCase("\r\n")]
        [TestCase("\r")]
        public void ScanTokens_NewlineLexemeAlwaysCanonicalLineFeed(string newline)
        {
            var tokens = Tokenize(newline);

            var nlToken = tokens.First(t => t.Type == TokenType.Newline);
            Assert.That(nlToken.Lexeme, Is.EqualTo("\n"));
        }

        // ============================================================================================================
        // C. Integer literals
        // ============================================================================================================

        [Test]
        public void ScanTokens_Zero_ProducesIntegerWithValueZero()
        {
            var tokens = Tokenize("0");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.Integer, "0", 1, 0);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_TwoDigitInteger_ProducesIntegerWithMatchingValue()
        {
            var tokens = Tokenize("42");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.Integer, "42", 1, 42);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_NineDigitInteger_ParsesCorrectly()
        {
            var tokens = Tokenize("123456789");

            AssertToken(tokens[0], TokenType.Integer, "123456789", 1, 123456789);
        }

        [Test]
        public void ScanTokens_IntMaxValueBoundary_ParsesAsInteger()
        {
            var tokens = Tokenize("2147483647");

            AssertToken(tokens[0], TokenType.Integer, "2147483647", 1, int.MaxValue);
        }

        [Test]
        public void ScanTokens_IntegerOverflow_ThrowsOverflowException()
        {
            Assert.That(() => Tokenize("2147483648"), Throws.TypeOf<OverflowException>());
        }

        [Test]
        public void ScanTokens_LeadingZeros_LexemePreservedValueParsedNumerically()
        {
            var tokens = Tokenize("007");

            AssertToken(tokens[0], TokenType.Integer, "007", 1, 7);
        }

        [Test]
        public void ScanTokens_IntegersOnSeparateLines_TrackLineNumbers()
        {
            var tokens = Tokenize("1\n2\n3\n");

            var integers = tokens.Where(t => t.Type == TokenType.Integer).ToList();
            Assert.That(integers, Has.Count.EqualTo(3));
            Assert.That(integers[0].LineNum, Is.EqualTo(1));
            Assert.That(integers[1].LineNum, Is.EqualTo(2));
            Assert.That(integers[2].LineNum, Is.EqualTo(3));
        }

        [Test]
        public void ScanTokens_IntegerLiteralIsBoxedInt32()
        {
            var tokens = Tokenize("42");

            Assert.That(tokens[0].Literal, Is.TypeOf<int>());
        }

        // ============================================================================================================
        // D. Float literals
        // ============================================================================================================

        [Test]
        public void ScanTokens_DecimalLiteral_ProducesFloatToken()
        {
            var tokens = Tokenize("3.14");

            Assert.That(tokens, Has.Count.EqualTo(2));
            AssertToken(tokens[0], TokenType.Float, "3.14", 1, 3.14f);
            AssertToken(tokens[1], TokenType.EndOfCode, "", 1, null);
        }

        [Test]
        public void ScanTokens_ZeroPointZero_ProducesFloatZero()
        {
            var tokens = Tokenize("0.0");

            AssertToken(tokens[0], TokenType.Float, "0.0", 1, 0.0f);
        }

        [Test]
        public void ScanTokens_TrailingDotFloat_AcceptedAsFloat()
        {
            var tokens = Tokenize("3.");

            AssertToken(tokens[0], TokenType.Float, "3.", 1, 3.0f);
        }

        [Test]
        public void ScanTokens_FloatOnLaterLine_HasCorrectLineNumber()
        {
            var tokens = Tokenize("\n3.14\n");

            var floatToken = tokens.First(t => t.Type == TokenType.Float);
            Assert.That(floatToken.LineNum, Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_FloatLiteralIsBoxedSingle()
        {
            var tokens = Tokenize("3.14");

            Assert.That(tokens[0].Literal, Is.TypeOf<float>());
        }

        [Test]
        public void ScanTokens_IntegerVsFloat_PresenceOfDotChangesType()
        {
            var intTokens = Tokenize("42");
            var floatTokens = Tokenize("42.0");

            Assert.That(intTokens[0].Type, Is.EqualTo(TokenType.Integer));
            Assert.That(floatTokens[0].Type, Is.EqualTo(TokenType.Float));
        }

        // ============================================================================================================
        // E. Indent / Dedent
        // ============================================================================================================

        [Test]
        public void ScanTokens_IndentedSecondLine_EmitsExactlyOneIndentRegardlessOfSpaceCount()
        {
            var tokens = Tokenize("1\n    2\n");

            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.Type == TokenType.Dedent), Is.EqualTo(0));
        }

        [Test]
        public void ScanTokens_DedentToBaseLine_EmitsExactlyOneDedent()
        {
            var tokens = Tokenize("1\n    2\n3\n");

            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.Type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_SameIndentTwoConsecutiveLines_EmitsNoIndentOrDedent()
        {
            var tokens = Tokenize("1\n    2\n    3\n");

            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.Type == TokenType.Dedent), Is.EqualTo(0));
        }

        [Test]
        public void ScanTokens_DedentSpanningMultipleLevels_EmitsSingleDedent()
        {
            var tokens = Tokenize("1\n    2\n        3\n1\n");

            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(2));
            Assert.That(tokens.Count(t => t.Type == TokenType.Dedent), Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_IndentToken_HasSingleSpaceLexeme()
        {
            var tokens = Tokenize("1\n    2\n");

            var indent = tokens.First(t => t.Type == TokenType.Indent);
            Assert.That(indent.Lexeme, Is.EqualTo(" "));
        }

        [Test]
        public void ScanTokens_DedentToken_HasEmptyLexeme()
        {
            var tokens = Tokenize("1\n    2\n3\n");

            var dedent = tokens.First(t => t.Type == TokenType.Dedent);
            Assert.That(dedent.Lexeme, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ScanTokens_IndentAndDedent_LiteralIsNull()
        {
            var tokens = Tokenize("1\n    2\n3\n");

            var indent = tokens.First(t => t.Type == TokenType.Indent);
            var dedent = tokens.First(t => t.Type == TokenType.Dedent);
            Assert.That(indent.Literal, Is.Null);
            Assert.That(dedent.Literal, Is.Null);
        }

        [Test]
        public void ScanTokens_IndentLineNumber_MatchesNewlyIndentedLine()
        {
            var tokens = Tokenize("1\n    2\n");

            var indent = tokens.First(t => t.Type == TokenType.Indent);
            Assert.That(indent.LineNum, Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_TabFromColumnZero_EquivalentToEightSpaces()
        {
            var tabbed = Tokenize("1\n\t2\n");
            var spaced = Tokenize("1\n        2\n");

            Assert.That(tabbed.Count(t => t.Type == TokenType.Indent),
                Is.EqualTo(spaced.Count(t => t.Type == TokenType.Indent)));
            Assert.That(tabbed.Count(t => t.Type == TokenType.Dedent),
                Is.EqualTo(spaced.Count(t => t.Type == TokenType.Dedent)));
        }

        [Test]
        public void ScanTokens_TabAfterPartialColumn_RoundsUpToNextMultipleOfEight()
        {
            // line 2 = 4 spaces (col 4). line 3 = 4 spaces + tab → col 8 (deeper).
            var tokens = Tokenize("1\n    2\n    \t3\n");

            // Two distinct indent levels established → two Indent tokens.
            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_BlankLineBetweenIndentedLines_DoesNotPerturbIndentTracking()
        {
            var tokens = Tokenize("1\n    2\n\n    3\n");

            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.Type == TokenType.Dedent), Is.EqualTo(0));
        }

        [Test]
        public void ScanTokens_WhitespaceOnlyLine_TreatedAsBlankForIndentPurposes()
        {
            var tokens = Tokenize("1\n    2\n        \n    3\n");

            Assert.That(tokens.Count(t => t.Type == TokenType.Indent), Is.EqualTo(1));
            Assert.That(tokens.Count(t => t.Type == TokenType.Dedent), Is.EqualTo(0));
        }

        // ============================================================================================================
        // F. EndOfCode terminal token
        // ============================================================================================================

        [TestCase("")]
        [TestCase("42")]
        [TestCase("42\n")]
        [TestCase("1\n    2\n")]
        public void ScanTokens_EndOfCodeIsAlwaysLastToken(string source)
        {
            var tokens = Tokenize(source);

            Assert.That(tokens[^1].Type, Is.EqualTo(TokenType.EndOfCode));
        }

        [TestCase("")]
        [TestCase("42")]
        [TestCase("42\n")]
        [TestCase("1\n    2\n")]
        public void ScanTokens_ExactlyOneEndOfCodeEmitted(string source)
        {
            var tokens = Tokenize(source);

            Assert.That(tokens.Count(t => t.Type == TokenType.EndOfCode), Is.EqualTo(1));
        }

        [TestCase("", 1)]
        [TestCase("42", 1)]
        [TestCase("42\n", 2)]
        [TestCase("\n\n", 3)]
        [TestCase("\n\n\n", 4)]
        public void ScanTokens_EndOfCodeLineNumber_EqualsOnePlusNewlineCount(string source, int expectedLine)
        {
            var tokens = Tokenize(source);

            var eoc = tokens.Single(t => t.Type == TokenType.EndOfCode);
            Assert.That(eoc.LineNum, Is.EqualTo(expectedLine));
        }

        [Test]
        public void ScanTokens_EndOfCode_LexemeIsEmptyAndLiteralIsNull()
        {
            var tokens = Tokenize("42");

            var eoc = tokens.Single(t => t.Type == TokenType.EndOfCode);
            Assert.That(eoc.Lexeme, Is.EqualTo(string.Empty));
            Assert.That(eoc.Literal, Is.Null);
        }

        // ============================================================================================================
        // G. Cross-cutting line-number correctness
        // ============================================================================================================

        [Test]
        public void ScanTokens_NewlineToken_TaggedWithLineThatJustEnded()
        {
            var tokens = Tokenize("42\n");

            var newline = tokens.Single(t => t.Type == TokenType.Newline);
            Assert.That(newline.LineNum, Is.EqualTo(1));
        }

        [Test]
        public void ScanTokens_IntegerOnFifthLine_HasLineNumberFive()
        {
            var tokens = Tokenize("\n\n\n\n42\n");

            var integer = tokens.Single(t => t.Type == TokenType.Integer);
            Assert.That(integer.LineNum, Is.EqualTo(5));
        }

        [Test]
        public void ScanTokens_IndentOnThirdLine_HasLineNumberThree()
        {
            var tokens = Tokenize("1\n2\n    3\n");

            var indent = tokens.Single(t => t.Type == TokenType.Indent);
            Assert.That(indent.LineNum, Is.EqualTo(3));
        }

        // ============================================================================================================
        // H. State / idempotency
        // ============================================================================================================

        [Test]
        public void ScanTokens_CalledTwiceOnSameInstance_ReturnsEquivalentSequences()
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
        public void ScanTokens_SecondCallDoesNotAppendToFirstResult()
        {
            var scanner = new Scanner("42\n");

            var first = scanner.ScanTokens();
            int firstCount = first.Count;
            var second = scanner.ScanTokens();

            Assert.That(second.Count, Is.EqualTo(firstCount));
        }

        // ============================================================================================================
        // I. Invalid input — must throw (timeout-guarded; will fail until Scanner enforces these)
        // ============================================================================================================

        [Test]
        public void ScanTokens_LeadingSpacesOnFirstLine_Throws()
        {
            AssertScanThrowsWithinTimeout("    42\n");
        }

        [Test]
        public void ScanTokens_LeadingTabOnFirstLine_Throws()
        {
            AssertScanThrowsWithinTimeout("\t42\n");
        }

        [Test]
        public void ScanTokens_WhitespaceOnlySourceWithoutNewline_Throws()
        {
            AssertScanThrowsWithinTimeout("    ");
        }

        // ============================================================================================================
        // J. Constructor
        // ============================================================================================================

        [Test]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            Assert.That(() => new Scanner(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ScanTokens_EmptyString_DoesNotThrow()
        {
            Assert.That(() => new Scanner("").ScanTokens(), Throws.Nothing);
        }

        // ============================================================================================================
        // K. Lexeme correctness for literal slices
        // ============================================================================================================

        [TestCase("0")]
        [TestCase("42")]
        [TestCase("007")]
        [TestCase("2147483647")]
        public void ScanTokens_IntegerLexeme_EqualsExactSourceSlice(string source)
        {
            var tokens = Tokenize(source);

            var integer = tokens.Single(t => t.Type == TokenType.Integer);
            Assert.That(integer.Lexeme, Is.EqualTo(source));
        }

        [TestCase("0.0")]
        [TestCase("3.14")]
        [TestCase("3.")]
        public void ScanTokens_FloatLexeme_EqualsExactSourceSlice(string source)
        {
            var tokens = Tokenize(source);

            var floatToken = tokens.Single(t => t.Type == TokenType.Float);
            Assert.That(floatToken.Lexeme, Is.EqualTo(source));
        }
    }
}
