using System.Collections.Generic;
using System.Linq;

namespace Chow.Tests
{
    [TestFixture]
    public class ScannerTests
    {
        [Test]
        public void Constructor_Throws_WhenSourceCodeNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => new Scanner(null!));
        }

        [Test]
        public void ScanTokens_ReturnsEndOfFileOnly_ForEmptySource()
        {
            var tokens = Scan(string.Empty);

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsEndOfFileOnly_ForWhitespaceOnlySource()
        {
            var tokens = Scan("   ");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsEndOfFileOnly_ForBlankLinesAndWhitespace()
        {
            var tokens = Scan("  \r\n\n    \r");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsSingleCharacterTokens()
        {
            var tokens = Scan("()[].,:+-*/%");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.LeftParenthesis,
                TokenType.RightParenthesis,
                TokenType.LeftBracket,
                TokenType.RightBracket,
                TokenType.Dot,
                TokenType.Comma,
                TokenType.Colon,
                TokenType.Plus,
                TokenType.Minus,
                TokenType.Star,
                TokenType.Slash,
                TokenType.Percent,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsSingleCharacterAssignmentAndComparisonTokens()
        {
            var tokens = Scan("= > <");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Equal,
                TokenType.Greater,
                TokenType.Less,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsTwoCharacterOperators()
        {
            var tokens = Scan("== != >= <= += -= *= /= %=");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.EqualEqual,
                TokenType.BangEqual,
                TokenType.GreaterEqual,
                TokenType.LessEqual,
                TokenType.PlusEqual,
                TokenType.MinusEqual,
                TokenType.StarEqual,
                TokenType.SlashEqual,
                TokenType.PercentEqual,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_DistinguishesIdentifiersAndKeywords()
        {
            var tokens = Scan("if branch and orchid True false None name_1");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.If,
                TokenType.Identifier,
                TokenType.And,
                TokenType.Identifier,
                TokenType.True,
                TokenType.False,
                TokenType.None,
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));

            Assert.That(tokens[1].Lexeme, Is.EqualTo("branch"));
            Assert.That(tokens[3].Lexeme, Is.EqualTo("orchid"));
            Assert.That(tokens[7].Lexeme, Is.EqualTo("name_1"));
        }

        [Test]
        public void ScanTokens_ReturnsEveryKeywordToken()
        {
            var tokens = Scan("and class def elif else False false for if in None none not or pass return True true while");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.And,
                TokenType.Class,
                TokenType.Def,
                TokenType.Elif,
                TokenType.Else,
                TokenType.False,
                TokenType.False,
                TokenType.For,
                TokenType.If,
                TokenType.In,
                TokenType.None,
                TokenType.None,
                TokenType.Not,
                TokenType.Or,
                TokenType.Pass,
                TokenType.Return,
                TokenType.True,
                TokenType.True,
                TokenType.While,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsIdentifier_ForKeywordPrefix()
        {
            var tokens = Scan("ifelse notional ordinary");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Identifier,
                TokenType.Identifier,
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_ReturnsIdentifier_ForLeadingUnderscore()
        {
            var tokens = Scan("_name _value2");

            Assert.Multiple(() =>
            {
                Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
                {
                    TokenType.Identifier,
                    TokenType.Identifier,
                    TokenType.NewLine,
                    TokenType.EndOfFile
                }));
                Assert.That(tokens[0].Lexeme, Is.EqualTo("_name"));
                Assert.That(tokens[1].Lexeme, Is.EqualTo("_value2"));
            });
        }

        [Test]
        public void ScanTokens_ReturnsStringAndNumberLiteralValues()
        {
            var tokens = Scan("'chow' 123 45.67");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(tokens[0].Lexeme, Is.EqualTo("'chow'"));
                Assert.That(tokens[0].LiteralValue, Is.EqualTo("chow"));
                Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Number));
                Assert.That(tokens[1].LiteralValue, Is.EqualTo(123.0d));
                Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Number));
                Assert.That(tokens[2].LiteralValue, Is.EqualTo(45.67d));
            });
        }

        [Test]
        public void ScanTokens_ReturnsDoubleQuotedStringLiteralValue()
        {
            var tokens = Scan("\"chow\"");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(tokens[0].Lexeme, Is.EqualTo("\"chow\""));
                Assert.That(tokens[0].LiteralValue, Is.EqualTo("chow"));
            });
        }

        [Test]
        public void ScanTokens_DoesNotEndStringAtEscapedQuote()
        {
            var tokens = Scan("'don\\'t'");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(tokens[0].Lexeme, Is.EqualTo("'don\\'t'"));
                Assert.That(tokens[0].LiteralValue, Is.EqualTo("don\\'t"));
            });
        }

        [Test]
        public void ScanTokens_DoesNotTreatTrailingDotAsNumberFraction()
        {
            var tokens = Scan("123. 45");

            Assert.Multiple(() =>
            {
                Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
                {
                    TokenType.Number,
                    TokenType.Dot,
                    TokenType.Number,
                    TokenType.NewLine,
                    TokenType.EndOfFile
                }));
                Assert.That(tokens[0].Lexeme, Is.EqualTo("123"));
                Assert.That(tokens[0].LiteralValue, Is.EqualTo(123.0d));
            });
        }

        [Test]
        public void ScanTokens_IgnoresCommentsAndWhitespace()
        {
            var tokens = Scan("value   # ignored\r\nnext");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_IgnoresCommentAtEndOfFile()
        {
            var tokens = Scan("value # ignored");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_IgnoresBlankAndCommentOnlyLines()
        {
            var tokens = Scan("# ignored\n\npass");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_IgnoresIndentedBlankAndCommentOnlyLinesInsideBlock()
        {
            var tokens = Scan("if true:\n    pass\n\n    # ignored\n    pass\npass");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.If,
                TokenType.True,
                TokenType.Colon,
                TokenType.NewLine,
                TokenType.Indent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.Dedent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_SuppressesNewLinesInsideBrackets()
        {
            var tokens = Scan("values = (\n    1\n)\nnext");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Identifier,
                TokenType.Equal,
                TokenType.LeftParenthesis,
                TokenType.Number,
                TokenType.RightParenthesis,
                TokenType.NewLine,
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_SuppressesIndentationInsideBrackets()
        {
            var tokens = Scan("values = [\n    1,\n    2\n]\nnext");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Identifier,
                TokenType.Equal,
                TokenType.LeftBracket,
                TokenType.Number,
                TokenType.Comma,
                TokenType.Number,
                TokenType.RightBracket,
                TokenType.NewLine,
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_EmitsIndentAndDedentForNestedBlocks()
        {
            var tokens = Scan("if true:\n    pass\n    if false:\n        pass\npass");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.If,
                TokenType.True,
                TokenType.Colon,
                TokenType.NewLine,
                TokenType.Indent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.If,
                TokenType.False,
                TokenType.Colon,
                TokenType.NewLine,
                TokenType.Indent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.Dedent,
                TokenType.Dedent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_DedentsToIntermediateIndentationLevel()
        {
            var tokens = Scan("if true:\n    if false:\n        pass\n    pass");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.If,
                TokenType.True,
                TokenType.Colon,
                TokenType.NewLine,
                TokenType.Indent,
                TokenType.If,
                TokenType.False,
                TokenType.Colon,
                TokenType.NewLine,
                TokenType.Indent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.Dedent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.Dedent,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_StoresIndentWhitespaceAsLexeme()
        {
            var tokens = Scan("if true:\n    pass");

            Token indent = tokens.Single(token => token.Type == TokenType.Indent);

            Assert.That(indent.Lexeme, Is.EqualTo("    "));
        }

        [Test]
        public void ScanTokens_EmitsRemainingDedentsBeforeEndOfFile()
        {
            var tokens = Scan("if true:\n    pass");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.If,
                TokenType.True,
                TokenType.Colon,
                TokenType.NewLine,
                TokenType.Indent,
                TokenType.Pass,
                TokenType.NewLine,
                TokenType.Dedent,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_TracksOneBasedLineNumbers()
        {
            var tokens = Scan("first\nsecond");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].LineNumber, Is.EqualTo(1));
                Assert.That(tokens[2].LineNumber, Is.EqualTo(2));
            });
        }

        [Test]
        public void ScanTokens_TracksOneBasedLineNumbers_WithCrLf()
        {
            var tokens = Scan("first\r\nsecond");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].LineNumber, Is.EqualTo(1));
                Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NewLine));
                Assert.That(tokens[1].LineNumber, Is.EqualTo(1));
                Assert.That(tokens[2].LineNumber, Is.EqualTo(2));
            });
        }

        [Test]
        public void ScanTokens_ThrowsForUnexpectedCharacter()
        {
            Assert.Throws<ScannerException>(() => Scan("@"));
        }

        [Test]
        public void ScanTokens_ReportsLineNumberForUnexpectedCharacter()
        {
            var exception = Assert.Throws<ScannerException>(() => Scan("pass\n@"));

            Assert.That(exception!.LineNumber, Is.EqualTo(2));
        }

        [Test]
        public void ScanTokens_ThrowsForUnexpectedClosingBracket()
        {
            Assert.Throws<ScannerException>(() => Scan(")"));
        }

        [TestCase("{")]
        [TestCase("}")]
        public void ScanTokens_ThrowsForCurlyBrace(string sourceCode)
        {
            Assert.Throws<ScannerException>(() => Scan(sourceCode));
        }

        [Test]
        public void ScanTokens_ThrowsForUnterminatedString()
        {
            Assert.Throws<ScannerException>(() => Scan("'unterminated"));
        }

        [Test]
        public void ScanTokens_ThrowsForInvalidDedent()
        {
            Assert.Throws<ScannerException>(() => Scan("if true:\n    pass\n  pass"));
        }

        [Test]
        public void ScanTokens_ThrowsForTabsInIndentation()
        {
            Assert.Throws<ScannerException>(() => Scan("\tpass"));
        }

        [Test]
        public void ScanTokens_ThrowsForUnterminatedGroupedExpression()
        {
            Assert.Throws<ScannerException>(() => Scan("("));
        }

        [Test]
        public void ScanTokens_ThrowsForBangWithoutEqual()
        {
            Assert.Throws<ScannerException>(() => Scan("!"));
        }

        [Test]
        public void ScanTokens_ReturnsIdentifier_ContainingDigits()
        {
            var tokens = Scan("name1 a1b2");

            Assert.Multiple(() =>
            {
                Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
                {
                    TokenType.Identifier,
                    TokenType.Identifier,
                    TokenType.NewLine,
                    TokenType.EndOfFile
                }));
                Assert.That(tokens[0].Lexeme, Is.EqualTo("name1"));
                Assert.That(tokens[1].Lexeme, Is.EqualTo("a1b2"));
            });
        }

        [Test]
        public void ScanTokens_ReturnsEmptyStringLiteral()
        {
            var tokens = Scan("''");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(tokens[0].LiteralValue, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void ScanTokens_ThrowsForStringContainingNewLine()
        {
            Assert.Throws<ScannerException>(() => Scan("'hello\nworld'"));
        }

        [Test]
        public void ScanTokens_EmitsSingleNewLine_BetweenStatementsWithBlankLineBetween()
        {
            var tokens = Scan("first\n\nsecond");

            Assert.That(TokenTypes(tokens), Is.EqualTo(new[]
            {
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.Identifier,
                TokenType.NewLine,
                TokenType.EndOfFile
            }));
        }

        [Test]
        public void ScanTokens_TracksLineNumber_ThroughBlankAndCommentLines()
        {
            var tokens = Scan("# comment\n\npass");

            Assert.That(tokens[0].LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void ScanTokens_StoresNumberLexeme()
        {
            var tokens = Scan("123 1.5");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].Lexeme, Is.EqualTo("123"));
                Assert.That(tokens[1].Lexeme, Is.EqualTo("1.5"));
            });
        }

        [Test]
        public void ScanTokens_HandlesNonQuoteEscapeSequenceInString()
        {
            var tokens = Scan(@"'\t'");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(tokens[0].LiteralValue, Is.EqualTo(@"\t"));
            });
        }

        [Test]
        public void ScanTokens_TreatsCarriageReturnAloneAsLineBreak()
        {
            var tokens = Scan("first\rsecond");

            Assert.Multiple(() =>
            {
                Assert.That(tokens[0].LineNumber, Is.EqualTo(1));
                Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NewLine));
                Assert.That(tokens[2].LineNumber, Is.EqualTo(2));
            });
        }

        private static List<Token> Scan(string sourceCode)
        {
            return new Scanner(sourceCode).ScanTokens();
        }

        private static TokenType[] TokenTypes(IEnumerable<Token> tokens)
        {
            return tokens.Select(token => token.Type).ToArray();
        }
    }
}
