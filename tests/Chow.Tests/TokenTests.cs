using Chow;
using Chow.Tokens;

namespace Chow.Tests
{
    [TestFixture]
    public class TokenTests
    {
        [Test]
        public void ToString_FloatToken_ReturnsLabeledFields()
        {
            var token = new Token(TokenType.Float, "3.0", 2, 3.0f);

            Assert.That(token.ToString(), Is.EqualTo("Token(type=Float, lexeme=\"3.0\", literal=3, line=2)"));
        }

        [Test]
        public void ToString_NewlineToken_EscapesLexemeAndShowsNullLiteral()
        {
            var token = new Token(TokenType.Newline, "\n", 1, null);

            Assert.That(token.ToString(), Is.EqualTo("Token(type=Newline, lexeme=\"\\n\", literal=null, line=1)"));
        }

        [Test]
        public void ToString_EndOfCodeToken_ShowsEmptyLexeme()
        {
            var token = new Token(TokenType.EndOfCode, string.Empty, 2, null);

            Assert.That(token.ToString(), Is.EqualTo("Token(type=EndOfCode, lexeme=\"\", literal=null, line=2)"));
        }

        [Test]
        public void ToString_StringLiteral_EscapesLiteralText()
        {
            var token = new Token(TokenType.String, "\"quoted\"\n", 4, "\"quoted\"\n");

            Assert.That(token.ToString(), Is.EqualTo("Token(type=String, lexeme=\"\\\"quoted\\\"\\n\", literal=\"\\\"quoted\\\"\\n\", line=4)"));
        }
    }
}
