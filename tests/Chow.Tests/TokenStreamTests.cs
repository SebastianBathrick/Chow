using System;
using Chow.Tokens;

namespace Chow.Tests
{
    [TestFixture]
    public class TokenStreamTests
    {
        private static readonly string[] SingleLineSource = new[] { "let x = 42" };
        private static readonly string[] MultiLineSource = new[] { "let x = 42", "print(x)" };

        [Test]
        public void Constructor_Throws_WhenSourceCodeLinesNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TokenStream(null!));
        }

        [Test]
        public void IsTokenQueued_False_WhenNoTokensEnqueued()
        {
            var stream = new TokenStream(SingleLineSource);

            Assert.That(stream.IsTokenQueued, Is.False);
        }

        [Test]
        public void IsTokenQueued_True_AfterEnqueue()
        {
            var stream = new TokenStream(SingleLineSource);

            stream.Enqueue(new Token(default, 0, 0, 3));

            Assert.That(stream.IsTokenQueued, Is.True);
        }

        [Test]
        public void IsTokenQueued_False_AfterAllTokensDequeued()
        {
            var stream = new TokenStream(SingleLineSource);
            stream.Enqueue(new Token(default, 0, 0, 3));
            stream.Dequeue();

            Assert.That(stream.IsTokenQueued, Is.False);
        }

        [Test]
        public void Peek_ReturnsFirstToken_WithoutAdvancing()
        {
            var stream = new TokenStream(SingleLineSource);
            var first = new Token(default, 0, 0, 3);
            var second = new Token(default, 0, 4, 1);
            stream.Enqueue(first);
            stream.Enqueue(second);

            var firstPeek = stream.Peek();
            var secondPeek = stream.Peek();

            Assert.Multiple(() =>
            {
                Assert.That(firstPeek.columnIndex, Is.EqualTo(first.columnIndex));
                Assert.That(secondPeek.columnIndex, Is.EqualTo(first.columnIndex));
                Assert.That(stream.IsTokenQueued, Is.True);
            });
        }

        [Test]
        public void DequeueAndReturn_ReturnsTokensInFIFOOrder()
        {
            var stream = new TokenStream(SingleLineSource);
            var first = new Token(default, 0, 0, 3);
            var second = new Token(default, 0, 4, 1);
            var third = new Token(default, 0, 6, 1);
            stream.Enqueue(first);
            stream.Enqueue(second);
            stream.Enqueue(third);

            var a = stream.DequeueAndReturn();
            var b = stream.DequeueAndReturn();
            var c = stream.DequeueAndReturn();

            Assert.Multiple(() =>
            {
                Assert.That(a.columnIndex, Is.EqualTo(0));
                Assert.That(b.columnIndex, Is.EqualTo(4));
                Assert.That(c.columnIndex, Is.EqualTo(6));
            });
        }

        [Test]
        public void Dequeue_AdvancesPastFirstToken()
        {
            var stream = new TokenStream(SingleLineSource);
            var first = new Token(default, 0, 0, 3);
            var second = new Token(default, 0, 4, 1);
            stream.Enqueue(first);
            stream.Enqueue(second);

            stream.Dequeue();

            Assert.That(stream.Peek().columnIndex, Is.EqualTo(second.columnIndex));
        }

        [Test]
        public void Enqueue_AfterDequeue_MakesNewTokenAvailable()
        {
            var stream = new TokenStream(SingleLineSource);
            stream.Enqueue(new Token(default, 0, 0, 3));
            stream.Dequeue();

            stream.Enqueue(new Token(default, 0, 4, 1));

            Assert.Multiple(() =>
            {
                Assert.That(stream.IsTokenQueued, Is.True);
                Assert.That(stream.Peek().columnIndex, Is.EqualTo(4));
            });
        }

        [Test]
        public void PeekLexeme_ReturnsSubstringFromCachedSource()
        {
            var stream = new TokenStream(SingleLineSource);
            stream.Enqueue(new Token(default, 0, 4, 1));

            Assert.That(stream.PeekLexeme(), Is.EqualTo("x"));
        }

        [Test]
        public void PeekLexeme_ResolvesAcrossDifferentLines()
        {
            var stream = new TokenStream(MultiLineSource);
            stream.Enqueue(new Token(default, 0, 0, 3));
            stream.Enqueue(new Token(default, 1, 0, 5));

            var firstLexeme = stream.PeekLexeme();
            stream.Dequeue();
            var secondLexeme = stream.PeekLexeme();

            Assert.Multiple(() =>
            {
                Assert.That(firstLexeme, Is.EqualTo("let"));
                Assert.That(secondLexeme, Is.EqualTo("print"));
            });
        }

        [Test]
        public void PeekLexeme_DoesNotAdvanceIndex()
        {
            var stream = new TokenStream(SingleLineSource);
            stream.Enqueue(new Token(default, 0, 0, 3));

            stream.PeekLexeme();
            stream.PeekLexeme();

            Assert.That(stream.IsTokenQueued, Is.True);
        }

        [Test]
        public void Peek_Throws_WhenEmpty()
        {
            var stream = new TokenStream(SingleLineSource);

            Assert.Throws<InvalidOperationException>(() => stream.Peek());
        }

        [Test]
        public void Dequeue_Throws_WhenEmpty()
        {
            var stream = new TokenStream(SingleLineSource);

            Assert.Throws<InvalidOperationException>(() => stream.Dequeue());
        }

        [Test]
        public void DequeueAndReturn_Throws_WhenEmpty()
        {
            var stream = new TokenStream(SingleLineSource);

            Assert.Throws<InvalidOperationException>(() => stream.DequeueAndReturn());
        }

        [Test]
        public void PeekLexeme_Throws_WhenEmpty()
        {
            var stream = new TokenStream(SingleLineSource);

            Assert.Throws<InvalidOperationException>(() => stream.PeekLexeme());
        }

        [Test]
        public void Peek_Throws_AfterAllTokensDequeued()
        {
            var stream = new TokenStream(SingleLineSource);
            stream.Enqueue(new Token(default, 0, 0, 3));
            stream.Dequeue();

            Assert.Throws<InvalidOperationException>(() => stream.Peek());
        }
    }
}
