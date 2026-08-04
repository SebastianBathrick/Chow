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

    #region Chow Object Delegates

    // Taking ChowObject means the delegate is converted by an implicit operator at the call site,
    // so reaching this helper at all is what proves direct assignment works for that shape.
    static ChowScope ScopeWithOperatorDelegate(string name, ChowObject hostDelegate)
    {
        var scope = new ChowScope();
        scope[name] = hostDelegate;

        return scope;
    }

    [Test]
    public void Run_FuncOfChowObject_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate("answer", (Func<ChowObject>)(() => 42L));

        Assert.That(ChowEngine.Run("answer()", scope), Is.EqualTo((ChowObject)42L));
    }

    [Test]
    public void Run_FuncOfChowObjectReturningNull_ReturnsNone()
    {
        var scope = ScopeWithOperatorDelegate("nothing", (Func<ChowObject>)(() => null!));

        Assert.That(ChowEngine.Run("nothing()", scope), Is.EqualTo(ChowObject.None));
    }

    [Test]
    public void Run_ActionOfChowObject_ReceivesArgument()
    {
        var received = new List<string>();
        var scope = ScopeWithOperatorDelegate(
            "log",
            (Action<ChowObject>)(value => received.Add(value.ToString())));

        ChowEngine.Run("log(\"hello\")", scope);

        Assert.That(received, Is.EqualTo(new[] { "hello" }));
    }

    [Test]
    public void Run_ActionOfChowObject_ReturnsNone()
    {
        var scope = ScopeWithOperatorDelegate("log", (Action<ChowObject>)(_ => { }));

        Assert.That(ChowEngine.Run("log(1)", scope), Is.EqualTo(ChowObject.None));
    }

    // A void delegate is the shape that used to unbalance the operand stack inside a loop.
    [Test]
    public void Run_ActionOfChowObjectCalledInLoop_RunsEveryIteration()
    {
        var received = new List<string>();
        var scope = ScopeWithOperatorDelegate(
            "log",
            (Action<ChowObject>)(value => received.Add(value.ToString())));

        ChowEngine.Run(
            """
            for i in [1, 2, 3]:
                log(i)
            """,
            scope);

        Assert.That(received, Is.EqualTo(new[] { "1", "2", "3" }));
    }

    [Test]
    public void Run_ActionOfChowObjectArray_ReceivesEveryArgument()
    {
        var received = new List<string>();
        var scope = ScopeWithOperatorDelegate(
            "record",
            (Action<ChowObject[]>)(values =>
            {
                foreach (var value in values)
                {
                    received.Add(value.ToString());
                }
            }));

        ChowEngine.Run("record(1, \"two\", 3.5)", scope);

        Assert.That(received, Is.EqualTo(new[] { "1", "two", "3.5" }));
    }

    [Test]
    public void Run_ActionOfChowObjectArrayCalledInLoop_RunsEveryIteration()
    {
        var callCount = 0;
        var scope = ScopeWithOperatorDelegate("record", (Action<ChowObject[]>)(_ => callCount++));

        ChowEngine.Run(
            """
            for i in [1, 2, 3]:
                record(i, i)
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(3));
    }

    [Test]
    public void Run_FuncOfChowObjectArray_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "total",
            (Func<ChowObject[], ChowObject>)(values =>
            {
                var sum = 0L;

                foreach (var value in values)
                {
                    sum += value;
                }

                return sum;
            }));

        Assert.That(ChowEngine.Run("total(1, 2, 3)", scope), Is.EqualTo((ChowObject)6L));
    }

    [Test]
    public void Run_FuncOfChowObjectArrayReturningNull_ReturnsNone()
    {
        var scope = ScopeWithOperatorDelegate("nothing", (Func<ChowObject[], ChowObject>)(_ => null!));

        Assert.That(ChowEngine.Run("nothing(1)", scope), Is.EqualTo(ChowObject.None));
    }

    // Chow objects reach the delegate as Chow objects, so a list argument stays usable as one.
    [Test]
    public void Run_ChowObjectDelegate_ReceivesChowTypesNotHostTypes()
    {
        ChowObject? received = null;
        var scope = ScopeWithOperatorDelegate("capture", (Action<ChowObject>)(value => received = value));

        ChowEngine.Run("capture([1, 2, 3])", scope);

        Assert.Multiple(() =>
        {
            Assert.That(received!.IsList, Is.True);
            Assert.That(received!.Length, Is.EqualTo(3));
        });
    }

    // The object-based conversions have to keep resolving to their own operators now that the Chow
    // object ones sit beside them.
    [Test]
    public void Run_ObjectDelegatesAlongsideChowObjectOnes_StillResolveToObjectOverloads()
    {
        var scope = new ChowScope();
        scope["fromObject"] = (Func<object>)(() => 1L);
        scope["fromChow"] = (Func<ChowObject>)(() => 2L);

        Assert.That(ChowEngine.Run("fromObject() + fromChow()", scope), Is.EqualTo((ChowObject)3L));
    }

    #endregion

    #region Fixed Arity Delegates

    [Test]
    public void Run_ParameterlessAction_RunsAndReturnsNone()
    {
        var called = false;
        var scope = ScopeWithOperatorDelegate("tick", (Action)(() => called = true));

        var result = ChowEngine.Run("tick()", scope);

        Assert.Multiple(() =>
        {
            Assert.That(called, Is.True);
            Assert.That(result, Is.EqualTo(ChowObject.None));
        });
    }

    [Test]
    public void Run_FuncOfOneObject_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate("double", (Func<object, object>)(a => (long)a * 2));

        Assert.That(ChowEngine.Run("double(21)", scope), Is.EqualTo((ChowObject)42L));
    }

    [Test]
    public void Run_FuncOfTwoObjects_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Func<object, object, object>)((a, b) => (long)a + (long)b));

        Assert.That(ChowEngine.Run("add(2, 3)", scope), Is.EqualTo((ChowObject)5L));
    }

    [Test]
    public void Run_FuncOfThreeObjects_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Func<object, object, object, object>)((a, b, c) => (long)a + (long)b + (long)c));

        Assert.That(ChowEngine.Run("add(1, 2, 3)", scope), Is.EqualTo((ChowObject)6L));
    }

    [Test]
    public void Run_ActionOfTwoObjects_ReceivesBothArguments()
    {
        var total = 0L;
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Action<object, object>)((a, b) => total = (long)a + (long)b));

        ChowEngine.Run("add(4, 5)", scope);

        Assert.That(total, Is.EqualTo(9L));
    }

    [Test]
    public void Run_ActionOfThreeObjects_ReceivesEveryArgument()
    {
        var total = 0L;
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Action<object, object, object>)((a, b, c) => total = (long)a + (long)b + (long)c));

        ChowEngine.Run("add(1, 2, 3)", scope);

        Assert.That(total, Is.EqualTo(6L));
    }

    [Test]
    public void Run_FuncOfOneChowObject_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "double",
            (Func<ChowObject, ChowObject>)(a => (long)a * 2));

        Assert.That(ChowEngine.Run("double(21)", scope), Is.EqualTo((ChowObject)42L));
    }

    [Test]
    public void Run_FuncOfTwoChowObjects_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Func<ChowObject, ChowObject, ChowObject>)((a, b) => (long)a + (long)b));

        Assert.That(ChowEngine.Run("add(2, 3)", scope), Is.EqualTo((ChowObject)5L));
    }

    [Test]
    public void Run_FuncOfThreeChowObjects_ReturnsItsValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Func<ChowObject, ChowObject, ChowObject, ChowObject>)(
                (a, b, c) => (long)a + (long)b + (long)c));

        Assert.That(ChowEngine.Run("add(1, 2, 3)", scope), Is.EqualTo((ChowObject)6L));
    }

    [Test]
    public void Run_ActionOfTwoChowObjects_ReceivesBothArguments()
    {
        var received = new List<string>();
        var scope = ScopeWithOperatorDelegate(
            "log",
            (Action<ChowObject, ChowObject>)((a, b) =>
            {
                received.Add(a.ToString());
                received.Add(b.ToString());
            }));

        ChowEngine.Run("log(\"a\", \"b\")", scope);

        Assert.That(received, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void Run_ActionOfThreeChowObjects_ReceivesEveryArgument()
    {
        var total = 0L;
        var scope = ScopeWithOperatorDelegate(
            "add",
            (Action<ChowObject, ChowObject, ChowObject>)((a, b, c) => total = a + b + c));

        ChowEngine.Run("add(1, 2, 3)", scope);

        Assert.That(total, Is.EqualTo(6L));
    }

    [Test]
    public void Run_FixedArityVoidDelegateCalledInLoop_RunsEveryIteration()
    {
        var callCount = 0;
        var scope = ScopeWithOperatorDelegate(
            "record",
            (Action<ChowObject, ChowObject>)((_, _) => callCount++));

        ChowEngine.Run(
            """
            for i in [1, 2, 3]:
                record(i, i)
            """,
            scope);

        Assert.That(callCount, Is.EqualTo(3));
    }

    // A delegate declaring more parameters than the call supplies still runs; the missing Chow
    // object arrives as None rather than raising.
    [Test]
    public void Run_FewerArgumentsThanParameters_PassesNoneForTheMissingOne()
    {
        ChowObject? second = null;
        var scope = ScopeWithOperatorDelegate(
            "log",
            (Action<ChowObject, ChowObject>)((_, b) => second = b));

        ChowEngine.Run("log(1)", scope);

        Assert.That(second, Is.EqualTo(ChowObject.None));
    }

    // A delegate declared over host objects may still hand back a Chow object, which has to be
    // unwrapped rather than stored as an opaque host value.
    [Test]
    public void Run_ObjectDelegateReturningChowObject_ProducesUsableChowValue()
    {
        var scope = ScopeWithOperatorDelegate(
            "makeList",
            (Func<object, object>)(_ => ChowObject.CreateList()));

        Assert.That(ChowEngine.Run("len(makeList(1))", scope), Is.EqualTo((ChowObject)0L));
    }

    #endregion
}
