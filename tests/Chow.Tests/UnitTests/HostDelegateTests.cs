using Chow;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Covers .NET delegates registered in a scope and called from Chow source. Void delegates get the
/// most attention here: they return nothing, so they are the shapes that can leave the operand stack
/// unbalanced if a call does not produce a value.
/// </summary>
[TestFixture]
public class HostDelegateTests
{
    #region Helpers

    static ChowScope ScopeWith(string name, object hostDelegate)
    {
        var scope = new ChowScope();
        scope[name] = ChowObject.Create(hostDelegate);

        return scope;
    }

    #endregion

    #region Void Delegates In Loops

    // A loop keeps its iterator on the operand stack for the loop's whole lifetime, so a call that
    // produces no value would consume the iterator and strand the next iteration.
    [Test]
    public void Run_ActionWithOneParameterCalledInLoop_RunsEveryIteration()
    {
        var received = new List<object>();
        var scope = ScopeWith("log", (Action<object>)(value => received.Add(value)));

        ChowEngine.Run(
            """
            for i in [1, 2, 3]:
                log(i)
            """,
            scope);

        Assert.That(received, Is.EqualTo(new object[] { 1L, 2L, 3L }));
    }

    [Test]
    public void Run_ParameterlessActionCalledInLoop_RunsEveryIteration()
    {
        var callCount = 0;
        var scope = ScopeWith("tick", (Action)(() => callCount++));

        ChowEngine.Run(
            """
            for i in range(4):
                tick()
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(4));
    }

    [Test]
    public void Run_ChowObjectActionCalledInLoop_RunsEveryIteration()
    {
        var received = new List<string>();
        var scope = ScopeWith("log", (Action<ChowObject>)(value => received.Add(value.ToString())));

        ChowEngine.Run(
            """
            for word in ["a", "b", "c"]:
                log(word)
            """,
            scope);

        Assert.That(received, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void Run_ObjectArrayActionCalledInLoop_RunsEveryIteration()
    {
        var callCount = 0;
        var scope = ScopeWith("record", (Action<object[]>)(_ => callCount++));

        ChowEngine.Run(
            """
            for i in [1, 2, 3]:
                record(i, i)
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(3));
    }

    [Test]
    public void Run_TwoParameterActionCalledInLoop_RunsEveryIteration()
    {
        var total = 0L;
        var scope = ScopeWith(
            "add",
            (Action<object, object>)((left, right) => total += (long)left + (long)right));

        ChowEngine.Run(
            """
            for i in [1, 2, 3]:
                add(i, i)
            """,
            scope);

        Assert.That(total, Is.EqualTo(12L));
    }

    [Test]
    public void Run_VoidDelegateCalledTwicePerIteration_RunsEveryCall()
    {
        var callCount = 0;
        var scope = ScopeWith("tick", (Action)(() => callCount++));

        ChowEngine.Run(
            """
            for i in [1, 2]:
                tick()
                tick()
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(4));
    }

    // The operand stack is shared across call frames, so a void call nested inside a Chow function
    // has to stay balanced too.
    [Test]
    public void Run_VoidDelegateCalledInsideFunctionCalledFromLoop_RunsEveryIteration()
    {
        var callCount = 0;
        var scope = ScopeWith("tick", (Action)(() => callCount++));

        ChowEngine.Run(
            """
            def step():
                tick()

            for i in [1, 2, 3]:
                step()
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(3));
    }

    [Test]
    public void Run_VoidDelegateCalledInWhileLoop_RunsEveryIteration()
    {
        var callCount = 0;
        var scope = ScopeWith("tick", (Action)(() => callCount++));

        ChowEngine.Run(
            """
            i = 0
            while i < 3:
                tick()
                i = i + 1
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(3));
    }

    #endregion

    #region Delegate Results

    [Test]
    public void Run_VoidDelegateResult_IsNone()
    {
        var scope = ScopeWith("tick", (Action)(() => { }));

        Assert.That(ChowEngine.Run("tick()", scope), Is.EqualTo(ChowObject.None));
    }

    [Test]
    public void Run_VoidDelegateResultAssigned_BindsNone()
    {
        var scope = ScopeWith("tick", (Action)(() => { }));

        ChowEngine.Run("result = tick()", scope);

        Assert.That(scope["result"], Is.EqualTo(ChowObject.None));
    }

    [Test]
    public void Run_ValueReturningDelegate_ReturnsItsValue()
    {
        var scope = ScopeWith("double", (Func<object, object>)(value => (long)value * 2));

        Assert.That(ChowEngine.Run("double(21)", scope), Is.EqualTo((ChowObject)42L));
    }

    [Test]
    public void Run_ValueReturningDelegateCalledInLoop_AccumulatesEveryResult()
    {
        var scope = ScopeWith("double", (Func<object, object>)(value => (long)value * 2));

        ChowEngine.Run(
            """
            total = 0
            for i in [1, 2, 3]:
                total = total + double(i)
            """,
            scope);

        Assert.That(scope["total"], Is.EqualTo((ChowObject)12L));
    }

    #endregion
}
