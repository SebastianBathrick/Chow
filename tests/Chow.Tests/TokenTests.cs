namespace Chow.Tests
{
    [TestFixture]
    public class TokenTests
    {
        [Test]
        public void Constructor_AssignsProperties()
        {
            var token = new Token(TokenType.Identifier, "answer", 42.0d, 7);

            Assert.Multiple(() =>
            {
                Assert.That(token.Type, Is.EqualTo(TokenType.Identifier));
                Assert.That(token.Value, Is.EqualTo("answer"));
                Assert.That(token.LiteralValue, Is.EqualTo(42.0d));
                Assert.That(token.LineNumber, Is.EqualTo(7));
            });
        }

        [Test]
        public void IsOfType_ReturnsTrue_WhenTokenTypeMatches()
        {
            var token = new Token(TokenType.Return, "return", null, 3);

            Assert.That(token.IsOfTokenType(TokenType.Return), Is.True);
        }

        [Test]
        public void IsOfType_ReturnsFalse_WhenTokenTypeDoesNotMatch()
        {
            var token = new Token(TokenType.Return, "return", null, 3);

            Assert.That(token.IsOfTokenType(TokenType.Identifier), Is.False);
        }

        [Test]
        public void ToString_IncludesTypeLexemeAndLiteralValue()
        {
            var token = new Token(TokenType.Integer, "123", 123.0d, 1);

            Assert.That(token.ToString(), Is.EqualTo("Number 123 123"));
        }
    }
}
