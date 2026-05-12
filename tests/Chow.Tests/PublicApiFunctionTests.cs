using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Tests
{
    [TestFixture]
    public class PublicApiFunctionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        sealed class CaptureExprHook : IExpressionStatementHook
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

        // ============================================================================================================
        // A. Indexer returns a non-null value for a function defined in source
        // ============================================================================================================

        [Test]
        public void Indexer_DefinedFunction_ReturnsNonNullValue()
        {
            (ChowModule module, CaptureExprHook _) = NewModule();
            module.Execute("def f():\n    return 1");

            ChowValue value = module["f"];

            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public void Indexer_DefinedFunction_IsNotNone()
        {
            (ChowModule module, CaptureExprHook _) = NewModule();
            module.Execute("def f():\n    return 1");

            ChowValue value = module["f"];

            Assert.That(value.IsNone, Is.False);
        }

        // ============================================================================================================
        // B. Undefined name raises ApiNameErrorException
        // ============================================================================================================

        [Test]
        public void Indexer_UndefinedName_RaisesApiNameError()
        {
            (ChowModule module, CaptureExprHook _) = NewModule();

            Assert.Throws<GetGlobalException>(() => { ChowValue _ = module["missing"]; });
        }

        [Test]
        public void Indexer_UndefinedName_AfterEmptyExecute_RaisesApiNameError()
        {
            (ChowModule module, CaptureExprHook _) = NewModule();
            module.Execute("x = 1");

            Assert.Throws<GetGlobalException>(() => { ChowValue _ = module["missing"]; });
        }

        // ============================================================================================================
        // C. Cross-module function transport
        // ============================================================================================================

        [Test]
        public void IndexerSet_CrossModule_FunctionCallable()
        {
            (ChowModule moduleA, CaptureExprHook _) = NewModule();
            (ChowModule moduleB, CaptureExprHook hookB) = NewModule();

            moduleA.Execute("def f():\n    return 99");
            moduleB["f"] = moduleA["f"];

            moduleB.Execute("f()");

            Assert.That(hookB.Values[hookB.Values.Count - 1].As<long>(), Is.EqualTo(99));
        }

        [Test]
        public void IndexerSet_CrossModule_CapturedGlobalsTravelWithFunction()
        {
            // Function defined in moduleA closes over moduleA's `x`. Even when called via moduleB,
            // it should resolve `x` via moduleA's scope (closure semantics).
            (ChowModule moduleA, CaptureExprHook _) = NewModule();
            (ChowModule moduleB, CaptureExprHook hookB) = NewModule();

            moduleA.Execute("x = 7");
            moduleA.Execute("def get():\n    return x");
            moduleB["get"] = moduleA["get"];

            moduleB.Execute("get()");

            Assert.That(hookB.Values[hookB.Values.Count - 1].As<long>(), Is.EqualTo(7));
        }

        // ============================================================================================================
        // D. Hook invocation
        // ============================================================================================================

        [Test]
        public void Hook_InvokedOncePerFunctionCallExpression()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("def f():\n    return 1");

            module.Execute("f()");
            module.Execute("f()");
            module.Execute("f()");

            Assert.That(hook.Values.Count, Is.EqualTo(3));
        }

        [Test]
        public void Hook_ReceivesReturnValue_NotFunctionItself()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("def f():\n    return 42");

            module.Execute("f()");

            ChowValue lastValue = hook.Values[hook.Values.Count - 1];
            Assert.That(lastValue.As<long>(), Is.EqualTo(42));
        }

        // ============================================================================================================
        // E. Round-trip through the indexer
        // ============================================================================================================

        [Test]
        public void IndexerRoundTrip_FunctionStillCallable()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("def f():\n    return 5");

            ChowValue fValue = module["f"];
            module["g"] = fValue;
            module.Execute("g()");

            Assert.That(hook.Values[hook.Values.Count - 1].As<long>(), Is.EqualTo(5));
        }
    }
}
