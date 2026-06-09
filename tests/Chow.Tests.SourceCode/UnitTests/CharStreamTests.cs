using Chow.Tokens.Scanning;
namespace Chow.Interpreter.Tests;

[TestFixture]
public class CharStreamTests
{
    #region Character Classification Predicates

    [TestCase('0', ExpectedResult = true)]
    [TestCase('5', ExpectedResult = true)]
    [TestCase('9', ExpectedResult = true)]
    [TestCase('/', ExpectedResult = false)] // char immediately before '0'
    [TestCase(':', ExpectedResult = false)] // char immediately after '9'
    [TestCase('a', ExpectedResult = false)]
    [TestCase(' ', ExpectedResult = false)]
    public bool IsDigit_ReturnsWhetherSelectedCharIsDigit(char selected)
        => new CharStream(selected.ToString()).IsDigit();

    [TestCase('a', ExpectedResult = true)]
    [TestCase('z', ExpectedResult = true)]
    [TestCase('A', ExpectedResult = true)]
    [TestCase('Z', ExpectedResult = true)]
    [TestCase('m', ExpectedResult = true)]
    [TestCase('`', ExpectedResult = false)] // char immediately before 'a'
    [TestCase('{', ExpectedResult = false)] // char immediately after 'z'
    [TestCase('@', ExpectedResult = false)] // char immediately before 'A'
    [TestCase('[', ExpectedResult = false)] // char immediately after 'Z'
    [TestCase('0', ExpectedResult = false)]
    [TestCase('_', ExpectedResult = false)]
    public bool IsLetter_ReturnsWhetherSelectedCharIsLetter(char selected)
        => new CharStream(selected.ToString()).IsLetter();

    [TestCase(' ', ExpectedResult = true)]
    [TestCase('\t', ExpectedResult = true)]
    [TestCase('\n', ExpectedResult = false)]
    [TestCase('\r', ExpectedResult = false)]
    [TestCase('\f', ExpectedResult = false)]
    [TestCase('a', ExpectedResult = false)]
    public bool IsWhitespace_ReturnsWhetherSelectedCharIsSpaceOrTab(char selected)
        => new CharStream(selected.ToString()).IsWhitespace();

    [TestCase('\n', ExpectedResult = true)]
    [TestCase('\r', ExpectedResult = true)]
    [TestCase(' ', ExpectedResult = false)]
    [TestCase('\t', ExpectedResult = false)]
    [TestCase('a', ExpectedResult = false)]
    public bool IsNewline_ReturnsWhetherSelectedCharIsLineFeedOrCarriageReturn(char selected)
        => new CharStream(selected.ToString()).HasLineEnded;

    [TestCase('\f', ExpectedResult = true)]
    [TestCase(' ', ExpectedResult = false)]
    [TestCase('\n', ExpectedResult = false)]
    [TestCase('a', ExpectedResult = false)]
    public bool IsFormFeed_ReturnsWhetherSelectedCharIsFormFeed(char selected)
        => new CharStream(selected.ToString()).IsFormFeed();

    // NOTE: Despite its name, IsDoubleQuote matches both single (') and double (") quotes.
    [TestCase('"', ExpectedResult = true)]
    [TestCase('\'', ExpectedResult = true)]
    [TestCase('`', ExpectedResult = false)]
    [TestCase('a', ExpectedResult = false)]
    public bool IsDoubleQuote_ReturnsWhetherSelectedCharIsQuote(char selected)
        => new CharStream(selected.ToString()).IsDoubleQuote();

    #endregion

    #region Is

    [Test]
    public void Is_WhenSelectedCharMatches_ReturnsTrue()
    {
        Assert.That(new CharStream("x").Is('x'), Is.True);
    }

    [Test]
    public void Is_WhenSelectedCharDoesNotMatch_ReturnsFalse()
    {
        Assert.That(new CharStream("x").Is('y'), Is.False);
    }

    [Test]
    public void Is_OnEmptyStream_MatchesNullTerminator()
    {
        Assert.That(new CharStream("").Is('\0'), Is.True);
    }

    [Test]
    public void Is_AfterConsumingAllChars_IsReturnsFalse()
    {
        var stream = new CharStream("a");
        stream.Next();
        Assert.That(stream.Is('a'), Is.False);
    }

    #endregion

    #region IsNext

    [Test]
    public void IsNext_ReturnsWhetherFollowingCharMatches()
    {
        var stream = new CharStream("ab");

        Assert.That(stream.IsNext('b'), Is.True);
        Assert.That(stream.IsNext('a'), Is.False);
    }

    #endregion

    #region Next

    [Test]
    public void Next_WalksThroughEachCharThenReachesNullTerminator()
    {
        var stream = new CharStream("ab");

        Assert.That(stream.Is('a'), Is.True);
        stream.Next();
        Assert.That(stream.Is('b'), Is.True);
        stream.Next();
        Assert.That(stream.Is('\0'), Is.True);
    }

    [Test]
    public void Next_AtEndOfStream_DoesNotMovePastTheEnd()
    {
        var stream = new CharStream("a");

        stream.Next();
        stream.Next();
        stream.Next();

        Assert.That(stream.Is('\0'), Is.True);
    }

    [Test]
    public void Next_AcrossLineFeed_IncrementsLineNumberOnce()
    {
        var stream = new CharStream("a\nb");

        Assert.That(stream.LineNumber, Is.EqualTo(1));
        stream.Next();                          // select '\n'
        Assert.That(stream.LineNumber, Is.EqualTo(1));
        stream.Next();                          // consume the newline, select 'b'
        Assert.That(stream.Is('b'), Is.True);
        Assert.That(stream.LineNumber, Is.EqualTo(2));
    }

    [Test]
    public void Next_AcrossCarriageReturn_IncrementsLineNumberOnce()
    {
        var stream = new CharStream("a\rb");

        stream.Next();                          // Select '\r'
        stream.Next();                          // Consume the newline, select 'b'

        Assert.That(stream.Is('b'), Is.True);
        Assert.That(stream.LineNumber, Is.EqualTo(2));
    }

    [Test]
    public void Next_AcrossWindowsNewline_IncrementsLineNumberOnce()
    {
        var stream = new CharStream("a\r\nb");

        stream.Next(); // select '\r'
        stream.Next(); // consume the "\r\n" newline, select 'b'

        Assert.That(stream.Is('b'), Is.True);
        Assert.That(stream.LineNumber, Is.EqualTo(2));
    }

    #endregion

    #region NextNonWhitespace

    [Test]
    public void NextNonWhitespace_SkipsLeadingSpacesAndTabs()
    {
        var stream = new CharStream("  \tx");

        stream.NextNonWhitespace();

        Assert.That(stream.Is('x'), Is.True);
    }

    [Test]
    public void NextNonWhitespace_OnNonWhitespace_DoesNotAdvance()
    {
        var stream = new CharStream("xy");

        stream.NextNonWhitespace();

        Assert.That(stream.Is('x'), Is.True);
    }

    [Test]
    public void NextNonWhitespace_StopsAtNewline()
    {
        var stream = new CharStream("  \nx");

        stream.NextNonWhitespace();

        Assert.That(stream.HasLineEnded, Is.True);
    }

    [Test]
    public void NextNonWhitespace_AllWhitespace_ReachesNullTerminator()
    {
        var stream = new CharStream("   ");

        stream.NextNonWhitespace();

        Assert.That(stream.Is('\0'), Is.True);
    }

    #endregion

    #region LineNumber

    [Test]
    public void LineNumber_OnFreshStream_IsOne()
    {
        Assert.That(new CharStream("abc").LineNumber, Is.EqualTo(1));
    }

    [Test]
    public void LineNumber_AtEndOfStream_IsNegativeOne()
    {
        var stream = new CharStream("a");

        stream.Next();

        Assert.That(stream.LineNumber, Is.EqualTo(-1));
    }

    [Test]
    public void LineNumber_AcrossMultipleLines_CountsEachLine()
    {
        var stream = new CharStream("a\nb\nc");

        Assert.That(stream.LineNumber, Is.EqualTo(1));
        stream.Next(); // '\n'
        stream.Next(); // 'b'
        Assert.That(stream.LineNumber, Is.EqualTo(2));
        stream.Next(); // '\n'
        stream.Next(); // 'c'
        Assert.That(stream.LineNumber, Is.EqualTo(3));
    }

    #endregion

    #region IsEndOfStream

    [Test]
    public void IsEndOfStream_OnFreshNonEmptyStream_IsFalse()
    {
        Assert.That(new CharStream("a").IsEndOfStream, Is.False);
    }

    [Test]
    public void IsEndOfStream_AfterConsumingAllChars_IsTrue()
    {
        var stream = new CharStream("a");

        stream.Next();

        Assert.That(stream.IsEndOfStream, Is.True);
    }

    [Test]
    public void IsEndOfStream_OnEmptyStream_IsTrue()
    {
        Assert.That(new CharStream("").IsEndOfStream, Is.True);
    }

    [Test]
    public void IsEndOfStream_EndsWithBlankLines_IsTrue()
    {
        var stream = new CharStream("a\n\n\n");
        
        stream.Next(); // 'a'
        stream.Next(); // '\n'
        
        Assert.That(stream.IsEndOfStream, Is.True);
    }
    
    #endregion

    #region IsFirstInLine

    [Test]
    public void IsFirstInLine_AfterAdvancingWithinLine_IsFalse()
    {
        var stream = new CharStream("ab");

        stream.Next(); // select 'b'

        Assert.That(stream.IsFirstInLine, Is.False);
    }

    [Test]
    public void IsFirstInLine_OnFirstCharAfterNewline_IsTrue()
    {
        var stream = new CharStream("a\nb");

        stream.Next();
        stream.Next();

        Assert.That(stream.IsFirstInLine, Is.True);
    }

    [Test]
    public void IsFirstInLine_OnFirstCharOfStream_IsTrue()
    {
        Assert.That(new CharStream("a").IsFirstInLine, Is.True);
    }

    #endregion
}
