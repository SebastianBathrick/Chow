using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class FunctionExecutionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        sealed class CaptureExprHook : IExprStatementHook
        {
            public List<ChowValue> Values { get; } = new List<ChowValue>();

            public void Invoke(ChowValue value)
            {
                Values.Add(value);
            }
        }

        static (ChowModule module, CaptureExprHook hook) NewModule()
        {
            ChowModule module = new ChowModule();
            CaptureExprHook hook = new CaptureExprHook();
            module.AddHook(hook);
            return (module, hook);
        }

        static ChowValue Last(CaptureExprHook hook)
        {
            return hook.Values[hook.Values.Count - 1];
        }

        // ============================================================================================================
        // A. Top-level def + simple call
        // ============================================================================================================

        [Test]
        public void Def_NoArgs_ReturnsLiteral()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("def f():\n    return 1");
            module.Execute("f()");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // B. Positional args
        // ============================================================================================================

        [Test]
        public void Def_TwoArgs_ReturnsSum()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("def add(a, b):\n    return a + b");
            module.Execute("add(3, 4)");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(7));
        }

        // ============================================================================================================
        // C. Recursion
        // ============================================================================================================

        [Test]
        public void Def_Recursive_Factorial()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def fact(n):\n" +
                "    if n == 0:\n" +
                "        return 1\n" +
                "    return n * fact(n - 1)";

            module.Execute(defSource);
            module.Execute("fact(5)");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(120));
        }

        // ============================================================================================================
        // D. Implicit return None when control falls off the body
        // ============================================================================================================

        [Test]
        public void Def_NoReturn_ReturnsNone()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("def noop():\n    x = 1");
            module.Execute("noop()");

            Assert.That(Last(hook).IsNone, Is.True);
        }

        // ============================================================================================================
        // E. Bare return -> None
        // ============================================================================================================

        [Test]
        public void Def_BareReturn_ReturnsNone()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def early(n):\n" +
                "    if n < 0:\n" +
                "        return\n" +
                "    return n";

            module.Execute(defSource);
            module.Execute("early(-1)");

            Assert.That(Last(hook).IsNone, Is.True);
        }

        [Test]
        public void Def_BareReturn_FallThroughReturnsValue()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def early(n):\n" +
                "    if n < 0:\n" +
                "        return\n" +
                "    return n";

            module.Execute(defSource);
            module.Execute("early(5)");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(5));
        }

        // ============================================================================================================
        // F. Closure sees later module-global mutation
        // ============================================================================================================

        [Test]
        public void Closure_OverGlobal_SeesLatestValue()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("x = 10");
            module.Execute("def get():\n    return x");
            module.Execute("x = 20");
            module.Execute("get()");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(20));
        }

        // ============================================================================================================
        // G. Nested def captures outer locals
        // ============================================================================================================

        [Test]
        public void NestedDef_CapturesOuterLocal()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        return x\n" +
                "    return inner()";

            module.Execute(defSource);
            module.Execute("outer()");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // H. Closure outlives the call that created it
        // ============================================================================================================

        [Test]
        public void Closure_OutlivesOuterCall()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def make():\n" +
                "    x = 5\n" +
                "    def f():\n" +
                "        return x\n" +
                "    return f";

            module.Execute(defSource);
            module.Execute("g = make()");
            module.Execute("g()");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(5));
        }

        // ============================================================================================================
        // I. Each recursive call has its own locals
        // ============================================================================================================

        [Test]
        public void Recursion_LocalsAreIsolatedPerCall()
        {
            // Counts down from 3 to 0; each frame's `n` must not be clobbered by deeper frames.
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def countdown(n):\n" +
                "    if n == 0:\n" +
                "        return 0\n" +
                "    x = countdown(n - 1)\n" +
                "    return n + x";

            module.Execute(defSource);
            module.Execute("countdown(3)");

            // 3 + (2 + (1 + 0)) = 6
            Assert.That(Last(hook).As<int>(), Is.EqualTo(6));
        }

        // ============================================================================================================
        // J. Arity mismatch raises TypeErrorException
        // ============================================================================================================

        [Test]
        public void Call_ArityMismatch_RaisesTypeError()
        {
            (ChowModule module, CaptureExprHook _) = NewModule();

            module.Execute("def f(a):\n    return a");

            Assert.Throws<TypeErrorException>(() => module.Execute("f(1, 2)"));
        }

        // ============================================================================================================
        // K. State persists across Execute calls
        // ============================================================================================================

        [Test]
        public void Execute_TwoCalls_FunctionDefinedInFirstUsableInSecond()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("def square(n):\n    return n * n");
            module.Execute("square(7)");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(49));
        }

        // ============================================================================================================
        // L. Cross-module function transport via the indexer
        // ============================================================================================================

        [Test]
        public void Function_TransportedAcrossModules_StillCallable()
        {
            (ChowModule moduleA, CaptureExprHook _) = NewModule();
            (ChowModule moduleB, CaptureExprHook hookB) = NewModule();

            moduleA.Execute("def f():\n    return 42");

            ChowValue fValue = moduleA["f"];
            moduleB["f"] = fValue;

            moduleB.Execute("f()");

            Assert.That(Last(hookB).As<int>(), Is.EqualTo(42));
        }

        // ============================================================================================================
        // M. Multi-level closure capture (three nested defs)
        // ============================================================================================================

        [Test]
        public void NestedDef_ThreeLevels_DeepestSeesOutermost()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            string defSource =
                "def a():\n" +
                "    x = 1\n" +
                "    def b():\n" +
                "        def c():\n" +
                "            return x\n" +
                "        return c()\n" +
                "    return b()";

            module.Execute(defSource);
            module.Execute("a()");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // P. Function as first-class value (assignment + call through new name)
        // ============================================================================================================

        [Test]
        public void Function_AssignedToNewName_CallableThroughAlias()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("def f():\n    return 1");
            module.Execute("g = f");
            module.Execute("g()");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // Q. Function passed as an argument to another function
        // ============================================================================================================

        [Test]
        public void Function_PassedAsArg_InvokedByCallee()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();

            module.Execute("def apply(fn):\n    return fn()");
            module.Execute("def one():\n    return 1");
            module.Execute("apply(one)");

            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }
    }
}
