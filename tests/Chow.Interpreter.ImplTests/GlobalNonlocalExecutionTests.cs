using Chow.Interpreter.Values;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class GlobalNonlocalExecutionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        sealed class CaptureExprHook
        {
            readonly ChowModule _module;

            public CaptureExprHook(ChowModule module)
            {
                _module = module;
            }

            public ChowValue Last => _module.GetGlobal("__result");
        }

        static (ChowModule module, CaptureExprHook hook) NewModule()
        {
            var module = new ChowModule();
            var hook = new CaptureExprHook(module);
            return (module, hook);
        }

        // ============================================================================================================
        // A. global: write from inside a function rebinds the module variable
        // ============================================================================================================

        [Test]
        public void Global_AssignmentInFunction_RebindsModuleVariable()
        {
            (var module, _) = NewModule();

            var src =
                "x = 0\n" +
                "def f():\n" +
                "    global x\n" +
                "    x = 99\n" +
                "f()";

            module.Execute(src);

            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(99));
        }

        [Test]
        public void Global_AssignmentInFunction_CreatesModuleVariableIfMissing()
        {
            (var module, _) = NewModule();

            var src =
                "def f():\n" +
                "    global y\n" +
                "    y = 7\n" +
                "f()";

            module.Execute(src);

            Assert.That(module.ContainsGlobal("y"), Is.True);
            Assert.That(module.GetGlobal("y").AsType<long>(), Is.EqualTo(7));
        }

        [Test]
        public void Global_ReadInFunction_ReadsModuleVariable()
        {
            (var module, var hook) = NewModule();

            var src =
                "x = 42\n" +
                "def f():\n" +
                "    global x\n" +
                "    return x\n" +
                "__result = f()";

            module.Execute(src);

            Assert.That(hook.Last.AsType<long>(), Is.EqualTo(42));
        }

        // ============================================================================================================
        // B. nonlocal: write from inner function rebinds enclosing function's variable, not module's
        // ============================================================================================================

        [Test]
        public void Nonlocal_AssignmentInInner_RebindsOuterVariable_AndLeavesModuleUntouched()
        {
            (var module, var hook) = NewModule();

            var src =
                "x = 0\n" +
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        nonlocal x\n" +
                "        x = 99\n" +
                "    inner()\n" +
                "    return x\n" +
                "__result = outer()";

            module.Execute(src);

            Assert.Multiple(() =>
            {
                Assert.That(hook.Last.AsType<long>(), Is.EqualTo(99));
                Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(0));
            });
        }

        [Test]
        public void Nonlocal_ReadInInner_ReadsOuterVariable()
        {
            (var module, var hook) = NewModule();

            var src =
                "def outer():\n" +
                "    x = 5\n" +
                "    def inner():\n" +
                "        nonlocal x\n" +
                "        return x\n" +
                "    return inner()\n" +
                "__result = outer()";

            module.Execute(src);

            Assert.That(hook.Last.AsType<long>(), Is.EqualTo(5));
        }

        // ============================================================================================================
        // C. Nested functions: nonlocal targets the nearest enclosing function that binds the name
        // ============================================================================================================

        [Test]
        public void Nonlocal_NestedThreeDeep_TargetsNearestBindingScope()
        {
            (var module, var hook) = NewModule();

            // outermost binds x = 1; middle does NOT bind x; innermost declares nonlocal x.
            // The nearest enclosing function-scope binding is outermost's, so the write lands there.
            var src =
                "def outermost():\n" +
                "    x = 1\n" +
                "    def middle():\n" +
                "        def innermost():\n" +
                "            nonlocal x\n" +
                "            x = 100\n" +
                "        innermost()\n" +
                "    middle()\n" +
                "    return x\n" +
                "__result = outermost()";

            module.Execute(src);

            Assert.That(hook.Last.AsType<long>(), Is.EqualTo(100));
        }

        [Test]
        public void Nonlocal_NestedTwoDeep_TargetsImmediateOuterWhenItBinds()
        {
            (var module, var hook) = NewModule();

            var src =
                "def outermost():\n" +
                "    x = 1\n" +
                "    def middle():\n" +
                "        x = 50\n" +
                "        def innermost():\n" +
                "            nonlocal x\n" +
                "            x = 100\n" +
                "        innermost()\n" +
                "        return x\n" +
                "    middle_val = middle()\n" +
                "    return [middle_val, x]\n" +
                "__result = outermost()";

            module.Execute(src);

            var list = (ChowList)hook.Last;
            Assert.Multiple(() =>
            {
                Assert.That(list[0].AsType<long>(), Is.EqualTo(100));
                Assert.That(list[1].AsType<long>(), Is.EqualTo(1));
            });
        }

        // ============================================================================================================
        // D. def binding under `global foo` writes to module scope
        // ============================================================================================================

        [Test]
        public void Global_DefBinding_BindsFunctionToModuleScope()
        {
            (var module, var hook) = NewModule();

            var src =
                "def outer():\n" +
                "    global helper\n" +
                "    def helper():\n" +
                "        return 7\n" +
                "outer()\n" +
                "__result = helper()";

            module.Execute(src);

            Assert.Multiple(() =>
            {
                Assert.That(module.ContainsGlobal("helper"), Is.True);
                Assert.That(hook.Last.AsType<long>(), Is.EqualTo(7));
            });
        }

        // ============================================================================================================
        // E. Regression: reading without declaring still walks LEGB
        // ============================================================================================================

        [Test]
        public void Read_WithoutDeclaration_WalksLEGB_FromInnermostToModule()
        {
            (var module, var hook) = NewModule();

            var src =
                "n = 1\n" +
                "def outer():\n" +
                "    n = 10\n" +
                "    def inner():\n" +
                "        return n\n" +
                "    return inner()\n" +
                "__result = outer()";

            module.Execute(src);

            // Inner reads `n` without declaring; LEGB resolves to outer's 10, not module's 1.
            Assert.That(hook.Last.AsType<long>(), Is.EqualTo(10));
        }

        [Test]
        public void Read_NoEnclosingBinding_FallsThroughToModule()
        {
            (var module, var hook) = NewModule();

            var src =
                "n = 1\n" +
                "def outer():\n" +
                "    def inner():\n" +
                "        return n\n" +
                "    return inner()\n" +
                "__result = outer()";

            module.Execute(src);

            Assert.That(hook.Last.AsType<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // F. Local-by-default still applies when no declaration is present
        // ============================================================================================================

        [Test]
        public void Function_LocalAssign_DoesNotRebindModuleVariable()
        {
            (var module, _) = NewModule();

            var src =
                "x = 0\n" +
                "def f():\n" +
                "    x = 99\n" +
                "f()";

            module.Execute(src);

            // Without `global x`, the assignment in f is local-by-default; module's x is untouched.
            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(0));
        }
    }
}
