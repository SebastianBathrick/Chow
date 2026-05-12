using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class FunctionExecutionTests
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

        static ChowValue Last(CaptureExprHook hook)
        {
            return hook.Last;
        }

        // ============================================================================================================
        // A. Top-level def + simple call
        // ============================================================================================================

        [Test]
        public void Def_NoArgs_ReturnsLiteral()
        {
            (var module, var hook) = NewModule();

            module.Execute("def f():\n    return 1");
            module.Execute("__result = f()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // B. Positional args
        // ============================================================================================================

        [Test]
        public void Def_TwoArgs_ReturnsSum()
        {
            (var module, var hook) = NewModule();

            module.Execute("def add(a, b):\n    return a + b");
            module.Execute("__result = add(3, 4)");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(7));
        }

        // ============================================================================================================
        // C. Recursion
        // ============================================================================================================

        [Test]
        public void Def_Recursive_Factorial()
        {
            (var module, var hook) = NewModule();

            var defSource =
                "def fact(n):\n" +
                "    if n == 0:\n" +
                "        return 1\n" +
                "    return n * fact(n - 1)";

            module.Execute(defSource);
            module.Execute("__result = fact(5)");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(120));
        }

        // ============================================================================================================
        // D. Implicit return None when control falls off the body
        // ============================================================================================================

        [Test]
        public void Def_NoReturn_ReturnsNone()
        {
            (var module, var hook) = NewModule();

            module.Execute("def noop():\n    x = 1");
            module.Execute("__result = noop()");

            Assert.That(Last(hook).IsNone, Is.True);
        }

        // ============================================================================================================
        // E. Bare return -> None
        // ============================================================================================================

        [Test]
        public void Def_BareReturn_ReturnsNone()
        {
            (var module, var hook) = NewModule();

            var defSource =
                "def early(n):\n" +
                "    if n < 0:\n" +
                "        return\n" +
                "    return n";

            module.Execute(defSource);
            module.Execute("__result = early(-1)");

            Assert.That(Last(hook).IsNone, Is.True);
        }

        [Test]
        public void Def_BareReturn_FallThroughReturnsValue()
        {
            (var module, var hook) = NewModule();

            var defSource =
                "def early(n):\n" +
                "    if n < 0:\n" +
                "        return\n" +
                "    return n";

            module.Execute(defSource);
            module.Execute("__result = early(5)");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(5));
        }

        // ============================================================================================================
        // F. Closure sees later module-global mutation
        // ============================================================================================================

        [Test]
        public void Closure_OverGlobal_SeesLatestValue()
        {
            (var module, var hook) = NewModule();

            module.Execute("x = 10");
            module.Execute("def get():\n    return x");
            module.Execute("x = 20");
            module.Execute("__result = get()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(20));
        }

        // ============================================================================================================
        // G. Nested def captures outer locals
        // ============================================================================================================

        [Test]
        public void NestedDef_CapturesOuterLocal()
        {
            (var module, var hook) = NewModule();

            var defSource =
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        return x\n" +
                "    return inner()";

            module.Execute(defSource);
            module.Execute("__result = outer()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // H. Closure outlives the call that created it
        // ============================================================================================================

        [Test]
        public void Closure_OutlivesOuterCall()
        {
            (var module, var hook) = NewModule();

            var defSource =
                "def make():\n" +
                "    x = 5\n" +
                "    def f():\n" +
                "        return x\n" +
                "    return f";

            module.Execute(defSource);
            module.Execute("g = make()");
            module.Execute("__result = g()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(5));
        }

        // ============================================================================================================
        // I. Each recursive call has its own locals
        // ============================================================================================================

        [Test]
        public void Recursion_LocalsAreIsolatedPerCall()
        {
            // Counts down from 3 to 0; each frame's `n` must not be clobbered by deeper frames.
            (var module, var hook) = NewModule();

            var defSource =
                "def countdown(n):\n" +
                "    if n == 0:\n" +
                "        return 0\n" +
                "    x = countdown(n - 1)\n" +
                "    return n + x";

            module.Execute(defSource);
            module.Execute("__result = countdown(3)");

            // 3 + (2 + (1 + 0)) = 6
            Assert.That(Last(hook).As<long>(), Is.EqualTo(6));
        }

        // ============================================================================================================
        // J. Arity mismatch raises TypeErrorException
        // ============================================================================================================

        [Test]
        public void Call_ArityMismatch_RaisesTypeError()
        {
            (var module, var _) = NewModule();

            module.Execute("def f(a):\n    return a");

            Assert.Throws<TypeException>(() => module.Execute("f(1, 2)"));
        }

        // ============================================================================================================
        // K. State persists across Execute calls
        // ============================================================================================================

        [Test]
        public void Execute_TwoCalls_FunctionDefinedInFirstUsableInSecond()
        {
            (var module, var hook) = NewModule();

            module.Execute("def square(n):\n    return n * n");
            module.Execute("__result = square(7)");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(49));
        }

        // ============================================================================================================
        // L. Cross-module function transport via the indexer
        // ============================================================================================================

        [Test]
        public void Function_TransportedAcrossModules_StillCallable()
        {
            (var moduleA, var _) = NewModule();
            (var moduleB, var hookB) = NewModule();

            moduleA.Execute("def f():\n    return 42");

            var fValue = moduleA.GetGlobal("f");
            moduleB["f"] = fValue;

            moduleB.Execute("__result = f()");

            Assert.That(Last(hookB).As<long>(), Is.EqualTo(42));
        }

        // ============================================================================================================
        // M. Multi-level closure capture (three nested defs)
        // ============================================================================================================

        [Test]
        public void NestedDef_ThreeLevels_DeepestSeesOutermost()
        {
            (var module, var hook) = NewModule();

            var defSource =
                "def a():\n" +
                "    x = 1\n" +
                "    def b():\n" +
                "        def c():\n" +
                "            return x\n" +
                "        return c()\n" +
                "    return b()";

            module.Execute(defSource);
            module.Execute("__result = a()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // P. Function as first-class value (assignment + call through new name)
        // ============================================================================================================

        [Test]
        public void Function_AssignedToNewName_CallableThroughAlias()
        {
            (var module, var hook) = NewModule();

            module.Execute("def f():\n    return 1");
            module.Execute("g = f");
            module.Execute("__result = g()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // Q. Function passed as an argument to another function
        // ============================================================================================================

        [Test]
        public void Function_PassedAsArg_InvokedByCallee()
        {
            (var module, var hook) = NewModule();

            module.Execute("def apply(fn):\n    return fn()");
            module.Execute("def one():\n    return 1");
            module.Execute("__result = apply(one)");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }
    }
}
