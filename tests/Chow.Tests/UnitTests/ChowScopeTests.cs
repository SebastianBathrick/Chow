using Chow;

namespace Chow.Tests.UnitTests;

[TestFixture]
public class ChowScopeTests
{
    [Test]
    public void Indexer_SetThenGet_ReturnsStoredValue()
    {
        var scope = new ChowScope();

        scope["x"] = 5L;

        Assert.That(scope["x"] == 5L, Is.True);
    }

    [Test]
    public void Indexer_SeededVariable_VisibleToExecute()
    {
        var scope = new ChowScope();
        scope["x"] = 5L;

        ChowScope result = ChowEngine.Execute("x + 1", scope);

        Assert.That(result.ExpressionResult == 6L, Is.True);
    }

    [Test]
    public void ExpressionResult_AfterExecute_HoldsLastExpressionValue()
    {
        ChowScope result = ChowEngine.Execute("40 + 2", new ChowScope());

        Assert.That(result.ExpressionResult == 42L, Is.True);
    }

    [Test]
    public void Length_FreshScope_ReturnsZero()
    {
        var scope = new ChowScope();

        Assert.That(scope.Length, Is.Zero);
    }

    [Test]
    public void Length_AfterTwoAssignments_ReturnsTwo()
    {
        var scope = new ChowScope();

        scope["a"] = 1L;
        scope["b"] = 2L;

        Assert.That(scope.Length, Is.EqualTo(2));
    }

    [Test]
    public void ImplicitConversions_RoundTripThroughChowValue_PreservesState()
    {
        var scope = new ChowScope();
        scope["x"] = 7L;

        ChowValue asValue = scope;
        ChowScope backToScope = asValue;

        Assert.That(backToScope["x"] == 7L, Is.True);
    }
}
