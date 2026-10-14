using Chow;
using Chow.Interpreter.Exceptions;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Covers the errors Chow reports to the host: the shared <see cref="ChowException"/> hierarchy,
/// the <c>ErrorName: description on line N</c> message format, the line numbers the virtual machine
/// attaches to runtime errors, and the Chow errors scripts raise in place of .NET exceptions.
/// </summary>
[TestFixture]
public class ChowExceptionTests
{
    #region Line Numbers

    [Test]
    public void Run_DivideByZero_MessageHasLine()
    {
        var exception = Assert.Catch<RuntimeException>(() => ChowEngine.Run("x = 1 / 0"));

        Assert.That(exception!.LineNumber, Is.EqualTo(1));
        Assert.That(exception.Message, Is.EqualTo("ZeroDivisionError: division by zero on line 1"));
    }

    [Test]
    public void Run_ErrorInsideFunction_ReportsBodyLine()
    {
        var exception = Assert.Catch<RuntimeException>(
            () => ChowEngine.Run(
                """
                def divide(a):
                    return a / 0
                divide(1)
                """));

        Assert.That(exception!.LineNumber, Is.EqualTo(2));
    }

    // The instruction after the call belongs to line 3, so this fails if the arity check runs
    // after the caller's instruction pointer has already moved past the call.
    [Test]
    public void Run_ArityMismatch_ReportsCallLine()
    {
        var exception = Assert.Catch<DataTypeException>(
            () => ChowEngine.Run(
                """
                def f(a):
                    return a
                x = [f(1, 2),
                     3]
                """));

        Assert.That(exception!.LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void HostCallIntoFunction_ErrorInBody_ReportsBodyLine()
    {
        var scope = new ChowScope();
        ChowEngine.Run(
            """
            def divide(a):
                return a / 0
            """,
            scope);

        var exception = Assert.Catch<RuntimeException>(() => scope["divide"].CallSelf(1L));

        Assert.That(exception!.LineNumber, Is.EqualTo(2));
    }

    // An error raised directly by a host API call has no line of Chow source behind it.
    [Test]
    public void HostCall_Error_HasNullLineNumber()
    {
        var scope = new ChowScope();
        ChowEngine.Run(
            """
            class Counter:
                pass
            counter = Counter()
            """,
            scope);

        var exception = Assert.Catch<AttributeException>(() => scope["counter"].GetAttribute("missing"));

        Assert.That(exception!.LineNumber, Is.Null);
        Assert.That(exception.Message, Does.Not.Contain("on line"));
    }

    #endregion

    #region Hierarchy And Format

    [Test]
    public void CompileAndRun_AnyChowError_CatchableAsChowException()
    {
        var compileError = Assert.Catch<ChowException>(() => ChowEngine.Compile("x = "));
        var runtimeError = Assert.Catch<ChowException>(() => ChowEngine.Run("1 / 0"));

        Assert.That(compileError, Is.InstanceOf<CompileException>());
        Assert.That(compileError!.ErrorName, Is.EqualTo("SyntaxError"));
        Assert.That(runtimeError, Is.InstanceOf<RuntimeException>());
        Assert.That(runtimeError!.ErrorName, Is.EqualTo("ZeroDivisionError"));
    }

    [Test]
    public void Run_UnsupportedOperand_IsRuntimeTypeError()
    {
        var exception = Assert.Catch<RuntimeException>(() => ChowEngine.Run("1 + \"a\""));

        Assert.That(exception, Is.InstanceOf<DataTypeException>());
        Assert.That(
            exception!.Message,
            Is.EqualTo("TypeError: unsupported operand type(s) for +: 'int' and 'str' on line 1"));
    }

    [TestCase("5()", "TypeError: 'int' object is not callable on line 1")]
    [TestCase("len(5)", "TypeError: object of type 'int' has no len() on line 1")]
    public void Run_TypeError_MessageUsesPythonTypeNames(string sourceCode, string expectedMessage)
    {
        var exception = Assert.Catch<RuntimeException>(() => ChowEngine.Run(sourceCode));

        Assert.That(exception!.Message, Is.EqualTo(expectedMessage));
    }

    #endregion

    #region Script Errors

    [TestCase("[].pop()", typeof(IndexException))]
    [TestCase("[1][5]", typeof(IndexException))]
    [TestCase("[1].pop(3)", typeof(IndexException))]
    [TestCase("[1].remove(2)", typeof(ValueException))]
    [TestCase("int(\"abc\")", typeof(ValueException))]
    [TestCase("float(\"x\")", typeof(ValueException))]
    [TestCase("range(0, 1, 0)", typeof(ValueException))]
    [TestCase("min([])", typeof(ValueException))]
    [TestCase("[1, 2][::0]", typeof(ValueException))]
    [TestCase("[1].pop(0, 1)", typeof(DataTypeException))]
    [TestCase("[1].insert(\"a\", 2)", typeof(DataTypeException))]
    public void Run_ScriptError_ThrowsChowError(string sourceCode, Type expectedType)
    {
        var exception = Assert.Catch<RuntimeException>(() => ChowEngine.Run(sourceCode));

        Assert.That(exception, Is.InstanceOf(expectedType));
        Assert.That(exception!.LineNumber, Is.EqualTo(1));
    }

    [Test]
    public void Run_NonlocalReadBeforeBinding_ThrowsNameError()
    {
        var exception = Assert.Catch<UndefinedNameException>(
            () => ChowEngine.Run(
                """
                def outer():
                    def inner():
                        nonlocal x
                        return x
                    result = inner()
                    x = 1
                    return result
                outer()
                """));

        Assert.That(exception!.LineNumber, Is.EqualTo(4));
    }

    #endregion
}
