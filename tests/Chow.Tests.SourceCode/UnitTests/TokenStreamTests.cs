using Chow;
using Chow.Tokens;

namespace Chow.Tests.SourceCode.UnitTests;

public class TokenStreamTests
{
    [Test]
    public void LineNumber_NewStream_ReturnsSelectedTokenLineNumber()
    {
        var stream = CreateStream(
            Token(TokenType.Name, lineNumber: 12),
            Token(TokenType.EndOfCode, lineNumber: 13));

        Assert.That(stream.LineNumber, Is.EqualTo(12));
    }

    [Test]
    public void Consume_SelectedToken_ReturnsTokenAndAdvancesStream()
    {
        var firstToken = Token(TokenType.Name, "value", 4);
        var secondToken = Token(TokenType.EndOfCode, lineNumber: 5);
        var stream = CreateStream(firstToken, secondToken);

        var consumedToken = stream.Consume();

        Assert.That(consumedToken, Is.EqualTo(firstToken));
        Assert.That(stream.LineNumber, Is.EqualTo(5));
        Assert.That(stream.IsMatch(TokenType.EndOfCode), Is.True);
    }

    [Test]
    public void Consume_LastToken_ReachesEndOfStream()
    {
        var stream = CreateStream(Token(TokenType.EndOfCode, lineNumber: 1));

        stream.Consume();

        Assert.That(stream.IsEndOfStream, Is.True);
        Assert.That(stream.IsMatch(TokenType.EndOfCode), Is.False);
        Assert.That(stream.IsNextMatch(TokenType.EndOfCode), Is.False);
    }

    [Test]
    public void ConsumeMatch_SelectedTokenMatches_ReturnsTokenAndAdvancesStream()
    {
        var firstToken = Token(TokenType.SymbolLeftParen, "(", 2);
        var stream = CreateStream(
            firstToken,
            Token(TokenType.SymbolRightParen, ")", 2));

        var consumedToken = stream.ConsumeMatch(TokenType.SymbolLeftParen);

        Assert.That(consumedToken, Is.EqualTo(firstToken));
        Assert.That(stream.IsMatch(TokenType.SymbolRightParen), Is.True);
    }

    [Test]
    public void ConsumeMatch_SelectedTokenDoesNotMatch_ThrowsSyntaxExceptionWithSelectedLineNumber()
    {
        var stream = CreateStream(Token(TokenType.Name, "name", 7));

        var exception = Assert.Throws<SyntaxException>(
            () => stream.ConsumeMatch(TokenType.SymbolLeftParen));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.LineNumber, Is.EqualTo(7));
        Assert.That(exception.Message, Is.EqualTo("SyntaxError: expected 'SymbolLeftParen'"));
        Assert.That(stream.IsMatch(TokenType.Name), Is.True);
    }

    [Test]
    public void ConsumeMatch_EndOfStream_ThrowsSyntaxExceptionWithPreviousTokenLineNumber()
    {
        var stream = CreateStream(Token(TokenType.EndOfCode, lineNumber: 9));
        stream.Consume();

        var exception = Assert.Throws<SyntaxException>(
            () => stream.ConsumeMatch(TokenType.SymbolLeftParen));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.LineNumber, Is.EqualTo(9));
        Assert.That(exception.Message, Is.EqualTo("SyntaxError: expected 'SymbolLeftParen'"));
    }

    [Test]
    public void IsMatch_SelectedTokenMatchesTarget_ReturnsTrue()
    {
        var stream = CreateStream(Token(TokenType.SymbolPlus));

        Assert.That(stream.IsMatch(TokenType.SymbolPlus), Is.True);
    }

    [Test]
    public void IsMatch_SelectedTokenDoesNotMatchTarget_ReturnsFalse()
    {
        var stream = CreateStream(Token(TokenType.SymbolPlus));

        Assert.That(stream.IsMatch(TokenType.SymbolMinus), Is.False);
    }

    [Test]
    public void IsMatch_SelectedTokenMatchesAnyTarget_ReturnsTrue()
    {
        var stream = CreateStream(Token(TokenType.SymbolPlus));

        var isMatch = stream.IsMatch(
            TokenType.SymbolMinus,
            TokenType.SymbolPlus,
            TokenType.SymbolMultiply);

        Assert.That(isMatch, Is.True);
    }

    [Test]
    public void IsMatch_SelectedTokenMatchesNoTarget_ReturnsFalse()
    {
        var stream = CreateStream(Token(TokenType.SymbolPlus));

        var isMatch = stream.IsMatch(TokenType.SymbolMinus, TokenType.SymbolMultiply);

        Assert.That(isMatch, Is.False);
    }

    [Test]
    public void IsNextMatch_NextTokenMatchesTarget_ReturnsTrueWithoutAdvancingStream()
    {
        var stream = CreateStream(
            Token(TokenType.Name, "name", 3),
            Token(TokenType.SymbolAssign, "=", 3));

        var isNextMatch = stream.IsNextMatch(TokenType.SymbolAssign);

        Assert.That(isNextMatch, Is.True);
        Assert.That(stream.IsMatch(TokenType.Name), Is.True);
    }

    [Test]
    public void IsNextMatch_NoTokenFollowsSelectedToken_ReturnsFalse()
    {
        var stream = CreateStream(Token(TokenType.Name));

        Assert.That(stream.IsNextMatch(TokenType.SymbolAssign), Is.False);
    }

    [Test]
    public void TryConsumeMatch_SelectedTokenMatchesTarget_ReturnsTrueAndOutputsToken()
    {
        var firstToken = Token(TokenType.KeywordIf, "if", 6);
        var stream = CreateStream(firstToken, Token(TokenType.EndOfCode, lineNumber: 6));

        var wasConsumed = stream.TryConsumeMatch(TokenType.KeywordIf, out var consumedToken);

        Assert.That(wasConsumed, Is.True);
        Assert.That(consumedToken, Is.EqualTo(firstToken));
        Assert.That(stream.IsMatch(TokenType.EndOfCode), Is.True);
    }

    [Test]
    public void TryConsumeMatchWithoutOutParam_SelectedTokenMatchesTarget_ReturnsTrueAndAdvancesStream()
    {
        var stream = CreateStream(
            Token(TokenType.KeywordIf, "if", 6),
            Token(TokenType.EndOfCode, lineNumber: 6));

        var wasConsumed = stream.TryConsumeMatch(TokenType.KeywordIf);

        Assert.That(wasConsumed, Is.True);
        Assert.That(stream.IsMatch(TokenType.EndOfCode), Is.True);
    }

    [Test]
    public void TryConsumeMatchWithoutOutParam_SelectedTokenDoesNotMatchTarget_ReturnsFalseAndLeavesStreamUnchanged()
    {
        var stream = CreateStream(Token(TokenType.KeywordIf, "if", 6));

        var wasConsumed = stream.TryConsumeMatch(TokenType.KeywordElse);

        Assert.That(wasConsumed, Is.False);
        Assert.That(stream.IsMatch(TokenType.KeywordIf), Is.True);
    }

    [Test]
    public void TryConsumeMatchWithoutOutParam_EndOfStream_ReturnsFalse()
    {
        var stream = CreateStream(Token(TokenType.EndOfCode, lineNumber: 1));
        stream.Consume();

        var wasConsumed = stream.TryConsumeMatch(TokenType.EndOfCode);

        Assert.That(wasConsumed, Is.False);
    }

    [Test]
    public void TryConsumeMatch_SelectedTokenDoesNotMatchTarget_ReturnsFalseAndLeavesStreamUnchanged()
    {
        var stream = CreateStream(Token(TokenType.KeywordIf, "if", 6));

        var wasConsumed = stream.TryConsumeMatch(TokenType.KeywordElse, out var consumedToken);

        Assert.That(wasConsumed, Is.False);
        Assert.That(consumedToken, Is.EqualTo(default(Token)));
        Assert.That(stream.IsMatch(TokenType.KeywordIf), Is.True);
    }

    [Test]
    public void TryConsumeMatch_EndOfStream_ReturnsFalseAndOutputsDefaultToken()
    {
        var stream = CreateStream(Token(TokenType.EndOfCode, lineNumber: 1));
        stream.Consume();

        var wasConsumed = stream.TryConsumeMatch(TokenType.EndOfCode, out var consumedToken);

        Assert.That(wasConsumed, Is.False);
        Assert.That(consumedToken, Is.EqualTo(default(Token)));
    }

    [Test]
    public void TryConsumeMatch_SelectedTokenMatchesAnyTarget_ReturnsTrueAndAdvancesStream()
    {
        var stream = CreateStream(
            Token(TokenType.SymbolMinus, "-", 8),
            Token(TokenType.LiteralInt, "1", 8, 1L));

        var wasConsumed = stream.TryConsumeMatch(
            TokenType.SymbolPlus,
            TokenType.SymbolMinus,
            TokenType.SymbolMultiply);

        Assert.That(wasConsumed, Is.True);
        Assert.That(stream.IsMatch(TokenType.LiteralInt), Is.True);
    }

    [Test]
    public void TryConsumeMatch_SelectedTokenMatchesNoTarget_ReturnsFalseAndLeavesStreamUnchanged()
    {
        var stream = CreateStream(Token(TokenType.SymbolMinus, "-", 8));

        var wasConsumed = stream.TryConsumeMatch(TokenType.SymbolPlus, TokenType.SymbolMultiply);

        Assert.That(wasConsumed, Is.False);
        Assert.That(stream.IsMatch(TokenType.SymbolMinus), Is.True);
    }

    [Test]
    public void TryConsumeMatch_AnyTargetAtEndOfStream_ReturnsFalse()
    {
        var stream = CreateStream(Token(TokenType.EndOfCode, lineNumber: 1));
        stream.Consume();

        var wasConsumed = stream.TryConsumeMatch(TokenType.EndOfCode, TokenType.EmptyToken);

        Assert.That(wasConsumed, Is.False);
    }

    static TokenStream CreateStream(params Token[] tokens)
    {
        return new TokenStream(tokens.ToList());
    }

    static Token Token(
        TokenType type,
        string lexeme = "",
        int lineNumber = 1,
        object? literal = null)
    {
        return new Token(type, lexeme, lineNumber, literal);
    }
}
