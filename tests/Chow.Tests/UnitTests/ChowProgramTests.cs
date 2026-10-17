using Chow;
using Chow.Interpreter.Exceptions;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Covers compiling source code once through <see cref="ChowEngine.Compile"/> and running the
/// resulting <see cref="ChowProgram"/> many times, plus the compile errors <c>Compile</c> raises.
/// </summary>
[TestFixture]
public class ChowProgramTests
{
    #region Running Programs

    [Test]
    public void Run_CompiledProgram_ReturnsLastExpressionValue()
    {
        var program = ChowEngine.Compile("40 + 2");

        ChowObject result = ChowEngine.Run(program);

        Assert.That(result == 42L, Is.True);
    }

    // List literals are built fresh on every run rather than stored in the constant pool, so one
    // run's mutations must not be visible to the next.
    [Test]
    public void Run_SameProgramTwice_DoesNotShareListLiteral()
    {
        var program = ChowEngine.Compile(
            """
            x = []
            x.append(1)
            len(x)
            """);

        ChowObject first = ChowEngine.Run(program, new ChowScope());
        ChowObject second = ChowEngine.Run(program, new ChowScope());

        Assert.That(first == 1L, Is.True);
        Assert.That(second == 1L, Is.True);
    }

    [Test]
    public void Run_SameProgramDifferentScopes_ReadsEachScope()
    {
        var program = ChowEngine.Compile("n * 10");
        var firstScope = new ChowScope();
        var secondScope = new ChowScope();
        firstScope["n"] = 2L;
        secondScope["n"] = 5L;

        ChowObject first = ChowEngine.Run(program, firstScope);
        ChowObject second = ChowEngine.Run(program, secondScope);

        Assert.That(first == 20L, Is.True);
        Assert.That(second == 50L, Is.True);
    }

    [Test]
    public void Run_SameProgramSharedScope_PersistsState()
    {
        var program = ChowEngine.Compile("count = count + 1");
        var scope = new ChowScope();
        scope["count"] = 1L;

        ChowEngine.Run(program, scope);
        ChowEngine.Run(program, scope);

        Assert.That(scope["count"] == 3L, Is.True);
    }

    [Test]
    public void Run_CompiledProgram_WritesExpressionResultToScope()
    {
        var program = ChowEngine.Compile("40 + 2");
        var scope = new ChowScope();

        ChowEngine.Run(program, scope);

        Assert.That(scope["expr_result"] == 42L, Is.True);
    }

    [Test]
    public void Run_ProgramDefiningFunction_FunctionCallableFromHost()
    {
        var program = ChowEngine.Compile(
            """
            def double(a):
                return a * 2
            """);
        var scope = new ChowScope();

        ChowEngine.Run(program, scope);
        ChowObject result = scope["double"].CallSelf(21L);

        Assert.That(result == 42L, Is.True);
    }

    [Test]
    public void Run_RuntimeError_ThrowsOnRunNotCompile()
    {
        var program = ChowEngine.Compile("1 / 0");

        Assert.Throws<ZeroDivisionException>(() => ChowEngine.Run(program));
    }

    [Test]
    public void Run_NullProgram_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ChowEngine.Run((ChowProgram)null!));
    }

    [Test]
    public void Run_UseBuiltInsFalse_BuiltInUndefined()
    {
        var program = ChowEngine.Compile("len([1])");

        Assert.Throws<UndefinedNameException>(() => ChowEngine.Run(program, useBuiltIns: false));
    }

    // Source code is compiled before the scope is set up, so a compile error must not leave the
    // built-in functions (or anything else) behind in the host's scope.
    [Test]
    public void Run_SourceWithCompileError_LeavesScopeUnchanged()
    {
        var scope = new ChowScope();

        Assert.Catch<CompileException>(() => ChowEngine.Run("x = ", scope));

        Assert.That(scope.Length, Is.EqualTo(0));
    }

    #endregion

    #region Compile Errors

    [TestCase("x = \"abc", typeof(ScannerException))]
    [TestCase("x = ", typeof(SyntaxException))]
    [TestCase("nonlocal x", typeof(SemanticException))]
    [TestCase("99999999999999999999", typeof(ScannerException))]
    [TestCase("a = [1, 2]\na[0:1] = [3]", typeof(SyntaxException))]
    [TestCase("x = None\nx is", typeof(SyntaxException))]
    [TestCase("x = None\nx is not", typeof(SyntaxException))]
    public void Compile_InvalidSource_ThrowsCompileException(string sourceCode, Type expectedType)
    {
        var exception = Assert.Catch<CompileException>(() => ChowEngine.Compile(sourceCode));

        Assert.That(exception, Is.InstanceOf(expectedType));
    }

    [Test]
    public void Compile_IntLiteralOutOfRange_ReportsLiteralAndLine()
    {
        var exception = Assert.Catch<CompileException>(
            () => ChowEngine.Compile("x = 1\ny = 99999999999999999999"));

        Assert.That(exception!.LineNumber, Is.EqualTo(2));
        Assert.That(
            exception.Message,
            Is.EqualTo("SyntaxError: int literal out of range: 99999999999999999999 on line 2"));
    }

    [Test]
    public void Compile_BreakOutsideLoop_HasPlainMessage()
    {
        var exception = Assert.Catch<CompileException>(() => ChowEngine.Compile("break"));

        Assert.That(exception!.LineNumber, Is.EqualTo(1));
        Assert.That(exception.Message, Is.EqualTo("SyntaxError: 'break' outside loop on line 1"));
    }

    [Test]
    public void Compile_ContinueOutsideLoop_HasPlainMessage()
    {
        var exception = Assert.Catch<CompileException>(() => ChowEngine.Compile("x = 1\ncontinue"));

        Assert.That(exception!.LineNumber, Is.EqualTo(2));
        Assert.That(exception.Message, Is.EqualTo("SyntaxError: 'continue' not properly in loop on line 2"));
    }

    #endregion
}
