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

    #region Host Callbacks

    // Like countdown, but every recursive call goes through the host, which calls back into Chow.
    const string HostCountdownSourceCode = """
                                           def countdown(n):
                                               if n == 0:
                                                   return 0
                                               return host_countdown(n - 1)
                                           """;

    // Exposes host_countdown, which calls the scope's countdown function from the host.
    static ChowScope CreateHostCountdownScope(ChowRunSettings settings)
    {
        var scope = new ChowScope();
        scope["host_countdown"] = ChowObject.Create(
            (Func<ChowObject, ChowObject>)(n => scope["countdown"].CallSelf(n)));
        ChowEngine.Run(HostCountdownSourceCode, scope, settings);

        return scope;
    }

    [Test]
    public void HostCallbackRecursion_AtLimit_Succeeds()
    {
        var scope = CreateHostCountdownScope(WithDepth(10));

        Assert.That(ChowEngine.Run("countdown(9)", scope, WithDepth(10)), Is.EqualTo((ChowObject)0L));
    }

    // Each host call into Chow runs on a new call stack, which must keep counting the frames of the
    // call stacks waiting on the host.
    [Test]
    public void HostCallbackRecursion_PastLimit_ThrowsRecursionErrorOnCallLine()
    {
        var scope = CreateHostCountdownScope(WithDepth(10));

        var exception = Assert.Throws<RecursionException>(
            () => ChowEngine.Run("countdown(10)", scope, WithDepth(10)));

        Assert.That(exception!.LineNumber, Is.EqualTo(4));
    }

    // Every level nests a new Processor on the C# stack, which may run out before the limit is
    // reached. Either way it must be a RecursionError rather than a stack overflow ending the process.
    [TestCase(ChowRunSettings.DefaultMaxRecursionDepth)]
    [TestCase(ChowRunSettings.NoRecursionLimit)]
    public void HostCallbackInfiniteRecursion_ThrowsRecursionError(int maxRecursionDepth)
    {
        var scope = new ChowScope();
        scope["host_call"] = ChowObject.Create((Func<ChowObject>)(() => scope["f"].CallSelf()));

        var exception = Assert.Throws<RecursionException>(
            () => ChowEngine.Run(
                """
                def f():
                    return host_call()
                f()
                """,
                scope,
                WithDepth(maxRecursionDepth)));

        Assert.That(exception!.Message, Does.StartWith("RecursionError: maximum recursion depth"));
    }

    // A host callback that runs a whole program, rather than calling a function, is guarded too.
    [Test]
    public void HostCallbackRunningProgram_InfiniteRecursion_ThrowsRecursionError()
    {
        var scope = new ChowScope();
        var program = ChowEngine.Compile("host_run()");
        scope["host_run"] = ChowObject.Create((Action)(() => ChowEngine.Run(program, scope)));

        Assert.Throws<RecursionException>(() => ChowEngine.Run(program, scope));
    }

    // Host calls made one after another, rather than nested, must not add up.
    [Test]
    public void HostCallbacksInSequence_DoNotAccumulateDepth()
    {
        var scope = CreateHostCountdownScope(WithDepth(10));

        Assert.DoesNotThrow(
            () => ChowEngine.Run(
                """
                def repeat():
                    for i in range(100):
                        countdown(5)
                repeat()
                """,
                scope,
                WithDepth(10)));
    }

    [Test]
    public void HostCallbackRecursionError_LaterRunsStartFromZeroDepth()
    {
        var scope = CreateHostCountdownScope(WithDepth(10));
        Assert.Catch<RecursionException>(() => ChowEngine.Run("countdown(10)", scope, WithDepth(10)));

        Assert.Multiple(() =>
        {
            Assert.That(ChowEngine.Run("countdown(9)", scope, WithDepth(10)), Is.EqualTo((ChowObject)0L));
            Assert.That(ChowEngine.Run(CountdownFrom(999)), Is.EqualTo((ChowObject)0L));
        });
    }

    #endregion
}
