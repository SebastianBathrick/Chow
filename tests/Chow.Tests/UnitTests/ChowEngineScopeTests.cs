using Chow;
using Chow.SourceData;

namespace Chow.Tests.UnitTests;

[TestFixture]
public class ChowEngineScopeTests
{
    static ChowValue CreateScope()
    {
        return (ChowValue)ChowValueFactory.CreateScope();
    }

    static ChowValue ExpressionResult(ChowValue scope)
    {
        return scope.GetAttribute(SourceObjectConsts.ScopeExpressionResultAttribute);
    }

    [Test]
    public void Execute_WithScope_PersistsStateAcrossCalls()
    {
        var scope = CreateScope();

        ChowEngine.Execute("x = 5", scope);
        var result = ChowEngine.Execute("x + 1", scope);

        Assert.That(ExpressionResult(result) == 6L, Is.True);
    }

    [Test]
    public void Execute_WithScope_ReturnedValueCarriesLastExpressionResult()
    {
        var scope = CreateScope();

        var result = ChowEngine.Execute("40 + 2", scope);

        Assert.That(ExpressionResult(result) == 42L, Is.True);
    }

    [Test]
    public void Execute_WithScope_ReturnedScopeCanContinueAccumulatedState()
    {
        var scope = CreateScope();

        ChowEngine.Execute("total = 1", scope);
        var afterFirst = ChowEngine.Execute("total = total + 2\ntotal", scope);
        var afterSecond = ChowEngine.Execute("total + 3", afterFirst);

        Assert.That(ExpressionResult(afterSecond) == 6L, Is.True);
    }

    [Test]
    public void Execute_WithBuiltIns_BuiltInIsAvailable()
    {
        var scope = CreateScope();

        var result = ChowEngine.Execute("len([1, 2, 3])", scope, useBuiltIns: true);

        Assert.That(ExpressionResult(result) == 3L, Is.True);
    }

    [Test]
    public void Execute_WithoutBuiltIns_BuiltInIsUndefined()
    {
        var scope = CreateScope();

        Assert.That(() => ChowEngine.Execute("len([1, 2, 3])", scope, useBuiltIns: false),
            Throws.Exception);
    }

    [Test]
    public void Execute_NullScope_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ChowEngine.Execute("1", (ChowValue)null!));
    }

    [Test]
    public void Execute_NonScopeValue_ThrowsArgumentException()
    {
        ChowValue notScope = 42L;

        Assert.Throws<ArgumentException>(() => ChowEngine.Execute("1", notScope));
    }
}
