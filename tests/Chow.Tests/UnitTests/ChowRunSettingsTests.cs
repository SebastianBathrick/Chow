using Chow;
using Chow.Interpreter.Exceptions;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Covers <see cref="ChowRunSettings"/>: its default values, the recursion limit it sets on a run,
/// and how a scope keeps its built-ins choice and latest recursion limit across runs.
/// </summary>
[TestFixture]
public class ChowRunSettingsTests
{
    // countdown(n) runs n + 1 nested calls before returning.
    const string CountdownSourceCode = """
                                       def countdown(n):
                                           if n == 0:
                                               return 0
                                           return countdown(n - 1)
                                       """;

    static ChowRunSettings WithDepth(int maxRecursionDepth)
    {
        return new ChowRunSettings().WithMaxRecursionDepth(maxRecursionDepth);
    }

    static string CountdownFrom(long n)
    {
        return $"{CountdownSourceCode}\ncountdown({n})";
    }

    #region Values

    [Test]
    public void Default_IncludesBuiltInsAndHasDefaultDepth()
    {
        var settings = default(ChowRunSettings);

        Assert.Multiple(() =>
        {
            Assert.That(settings.ExcludeBuiltIns, Is.False);
            Assert.That(settings.MaxRecursionDepth, Is.EqualTo(ChowRunSettings.DefaultMaxRecursionDepth));
            Assert.That(ChowRunSettings.DefaultMaxRecursionDepth, Is.EqualTo(1000));
        });
    }

    [Test]
    public void WithMaxRecursionDepth_Zero_MeansNoLimit()
    {
        Assert.That(WithDepth(0).MaxRecursionDepth, Is.EqualTo(ChowRunSettings.NoRecursionLimit));
    }

    [Test]
    public void WithMaxRecursionDepth_Negative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WithDepth(-1));
    }

    [Test]
    public void WithMethods_KeepOtherSettings()
    {
        var settings = WithDepth(5).WithExcludeBuiltIns();

        Assert.Multiple(() =>
        {
            Assert.That(settings.MaxRecursionDepth, Is.EqualTo(5));
            Assert.That(settings.ExcludeBuiltIns, Is.True);
            Assert.That(settings.WithMaxRecursionDepth(7).ExcludeBuiltIns, Is.True);
        });
    }

    #endregion

    #region Recursion Limit

    [Test]
    public void Run_InfiniteRecursion_ThrowsRecursionErrorOnCallLine()
    {
        var exception = Assert.Throws<RecursionException>(
            () => ChowEngine.Run(
                """
                def f():
                    return f()
                f()
                """));

        Assert.That(
            exception!.Message,
            Is.EqualTo("RecursionError: maximum recursion depth of 1000 exceeded on line 2"));
    }

    [Test]
    public void Run_RecursionAtLimit_Succeeds()
    {
        Assert.That(ChowEngine.Run(CountdownFrom(49), settings: WithDepth(50)), Is.EqualTo((ChowObject)0L));
    }

    [Test]
    public void Run_RecursionPastLimit_ThrowsRecursionError()
    {
        var exception = Assert.Throws<RecursionException>(
            () => ChowEngine.Run(CountdownFrom(50), settings: WithDepth(50)));

        Assert.That(exception!.Message, Does.StartWith("RecursionError: maximum recursion depth of 50 exceeded"));
    }

    [Test]
    public void Run_NoRecursionLimit_RecursesPastDefaultLimit()
    {
        var settings = WithDepth(ChowRunSettings.NoRecursionLimit);

        Assert.That(ChowEngine.Run(CountdownFrom(5000), settings: settings), Is.EqualTo((ChowObject)0L));
    }

    [Test]
    public void Run_RecursionThroughBoundMethod_CountsTowardLimit()
    {
        const string sourceCode = """
                                  class Node:
                                      def countdown(self, n):
                                          if n == 0:
                                              return 0
                                          return self.countdown(n - 1)
                                  node = Node()
                                  """;

        Assert.Multiple(() =>
        {
            Assert.That(
                ChowEngine.Run($"{sourceCode}\nnode.countdown(9)", settings: WithDepth(10)),
                Is.EqualTo((ChowObject)0L));
            Assert.Throws<RecursionException>(
                () => ChowEngine.Run($"{sourceCode}\nnode.countdown(10)", settings: WithDepth(10)));
        });
    }

    #endregion

    #region Scope

    [Test]
    public void Run_ScopeFirstRunWithBuiltIns_RerunExcludingThemThrowsArgumentException()
    {
        var scope = new ChowScope();
        ChowEngine.Run("x = 1", scope);

        Assert.Throws<ArgumentException>(
            () => ChowEngine.Run("x = 2", scope, new ChowRunSettings().WithExcludeBuiltIns()));
    }

    [Test]
    public void Run_ScopeFirstRunExcludingBuiltIns_RerunWithThemThrowsArgumentException()
    {
        var scope = new ChowScope();
        ChowEngine.Run("x = 1", scope, new ChowRunSettings().WithExcludeBuiltIns());

        Assert.Throws<ArgumentException>(() => ChowEngine.Run("x = 2", scope));
    }

    // The rejected run must not execute, so the scope keeps its earlier value.
    [Test]
    public void Run_ScopeBuiltInsMismatch_LeavesScopeUnchanged()
    {
        var scope = new ChowScope();
        ChowEngine.Run("x = 1", scope);

        Assert.Catch<ArgumentException>(
            () => ChowEngine.Run("x = 2", scope, new ChowRunSettings().WithExcludeBuiltIns()));
        Assert.That(scope["x"], Is.EqualTo((ChowObject)1L));
    }

    [Test]
    public void Run_ScopeRerunExcludingBuiltIns_BuiltInsStayUndefined()
    {
        var scope = new ChowScope();
        var settings = new ChowRunSettings().WithExcludeBuiltIns();
        ChowEngine.Run("x = 1", scope, settings);

        Assert.Throws<UndefinedNameException>(() => ChowEngine.Run("len([x])", scope, settings));
    }

    [Test]
    public void Run_ScopeRerunWithNewDepth_UsesNewDepth()
    {
        var scope = new ChowScope();
        ChowEngine.Run(CountdownSourceCode, scope, WithDepth(100));

        Assert.Throws<RecursionException>(() => ChowEngine.Run("countdown(50)", scope, WithDepth(10)));
    }

    // Source code is compiled before the scope is set up, so a run that fails to compile must not
    // decide the scope's built-ins choice.
    [Test]
    public void Run_ScopeFirstRunFailsToCompile_DoesNotLockSettings()
    {
        var scope = new ChowScope();

        Assert.Catch<CompileException>(
            () => ChowEngine.Run("x = ", scope, new ChowRunSettings().WithExcludeBuiltIns()));
        Assert.That(ChowEngine.Run("len([1])", scope), Is.EqualTo((ChowObject)1L));
    }

    [Test]
    public void HostCallIntoFunction_UsesScopeRecursionLimit()
    {
        var scope = new ChowScope();
        ChowEngine.Run(CountdownSourceCode, scope, WithDepth(10));
        var countdown = scope["countdown"];

        Assert.Multiple(() =>
        {
            Assert.That(countdown.CallSelf(9L), Is.EqualTo((ChowObject)0L));
            Assert.Throws<RecursionException>(() => countdown.CallSelf(10L));
        });
    }

    #endregion
}
