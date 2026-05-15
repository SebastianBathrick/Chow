using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class PublicApiFunctionTests
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
        // A. Indexer returns a non-null value for a function defined in source
        // ============================================================================================================

        [Test]
        public void Indexer_DefinedFunction_ReturnsNonNullValue()
        {
            (var module, var _) = NewModule();
            module.Execute("def f():\n    return 1");

            var value = module.GetGlobal("f");

            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public void Indexer_DefinedFunction_IsNotNone()
        {
            (var module, var _) = NewModule();
            module.Execute("def f():\n    return 1");

            var value = module.GetGlobal("f");

            Assert.That(value.IsNone, Is.False);
        }

        // ============================================================================================================
        // B. Undefined name raises ApiNameErrorException
        // ============================================================================================================

        [Test]
        public void Indexer_UndefinedName_RaisesApiNameError()
        {
            (var module, var _) = NewModule();

            Assert.Throws<GlobalAccessException>(() => { var _ = module["missing"]; });
        }

        [Test]
        public void Indexer_UndefinedName_AfterEmptyExecute_RaisesApiNameError()
        {
            (var module, var _) = NewModule();
            module.Execute("x = 1");

            Assert.Throws<GlobalAccessException>(() => { var _ = module["missing"]; });
        }

        // ============================================================================================================
        // C. Cross-module function transport
        // ============================================================================================================

        [Test]
        public void IndexerSet_CrossModule_FunctionCallable()
        {
            (var moduleA, var _) = NewModule();
            (var moduleB, var hookB) = NewModule();

            moduleA.Execute("def f():\n    return 99");
            moduleB["f"] = moduleA["f"];

            moduleB.Execute("__result = f()");

            Assert.That(Last(hookB).As<long>(), Is.EqualTo(99));
        }

        [Test]
        public void IndexerSet_CrossModule_CapturedGlobalsTravelWithFunction()
        {
            // Function defined in moduleA closes over moduleA's `x`. Even when called via moduleB,
            // it should resolve `x` via moduleA's scope (closure semantics).
            (var moduleA, var _) = NewModule();
            (var moduleB, var hookB) = NewModule();

            moduleA.Execute("x = 7");
            moduleA.Execute("def get():\n    return x");
            moduleB["get"] = moduleA["get"];

            moduleB.Execute("__result = get()");

            Assert.That(Last(hookB).As<long>(), Is.EqualTo(7));
        }

        // ============================================================================================================
        // D. Function call expression values
        // ============================================================================================================

        [Test]
        public void FunctionCallExpression_CanBeAssignedRepeatedly()
        {
            (var module, var hook) = NewModule();
            module.Execute("def f():\n    return 1");

            module.Execute("__result = f()");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));

            module.Execute("__result = f()");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));

            module.Execute("__result = f()");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }

        [Test]
        public void FunctionCallExpression_AssignsReturnValue_NotFunctionItself()
        {
            (var module, var hook) = NewModule();
            module.Execute("def f():\n    return 42");

            module.Execute("__result = f()");

            var lastValue = Last(hook);
            Assert.That(lastValue.As<long>(), Is.EqualTo(42));
        }

        [Test]
        public void CallFunction_DefinedFunction_ReturnsValue()
        {
            (var module, var _) = NewModule();
            module.Execute("def add(a, b):\n    return a + b");

            var result = module.CallFunction("add", new ChowInt(2), new ChowInt(3));

            Assert.That(result.As<long>(), Is.EqualTo(5));
        }

        [Test]
        public void CallFunction_DefinedFunction_AcceptsHostArguments()
        {
            (var module, var _) = NewModule();
            module.Execute("def add(a, b):\n    return a + b");

            var result = module.CallFunction("add", 2, 3L);

            Assert.That(result.As<long>(), Is.EqualTo(5));
        }

        // ============================================================================================================
        // E. Round-trip through the indexer
        // ============================================================================================================

        [Test]
        public void IndexerRoundTrip_FunctionStillCallable()
        {
            (var module, var hook) = NewModule();
            module.Execute("def f():\n    return 5");

            var fValue = module.GetGlobal("f");
            module["g"] = fValue;
            module.Execute("__result = g()");

            Assert.That(Last(hook).As<long>(), Is.EqualTo(5));
        }
    }
}
