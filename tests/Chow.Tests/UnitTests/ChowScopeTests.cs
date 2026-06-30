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

        ChowObject result = ChowEngine.Run("x + 1", scope);

        Assert.That(result == 6L, Is.True);
    }

    [Test]
    public void ExpressionResult_AfterExecute_HoldsLastExpressionValue()
    {
        ChowObject result = ChowEngine.Run("40 + 2", new ChowScope());

        Assert.That(result == 42L, Is.True);
    }

    // A fresh scope is seeded with the internal "expr_result" entry that holds the value of the
    // last evaluated expression, so its baseline Length is 1.
    const int SeededEntryCount = 1;

    [Test]
    public void Length_FreshScope_ReturnsSeededEntryCount()
    {
        var scope = new ChowScope();

        Assert.That(scope.Length, Is.EqualTo(0));
    }

    [Test]
    public void Length_AfterTwoAssignments_ReturnsTwoPlusSeededEntries()
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

        ChowObject asObject = scope;
        ChowScope backToScope = asObject;

        Assert.That(backToScope["x"] == 7L, Is.True);
    }
}
