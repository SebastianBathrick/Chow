using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class WhileExecutionTests
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

        // NOTE: Sources here intentionally omit a trailing '\n' after the final non-block statement.
        // The existing top-level parser loop misreads `<non-block stmnt>\n<EOF>` as "expected
        // statement at EOF" because the loop's EOF check is not refreshed after consuming the
        // trailing newline. Avoiding that pattern keeps these tests focused on while-loop behavior.

        // ============================================================================================================
        // A. Basic counter
        // ============================================================================================================

        [Test]
        public void While_CounterLoop_RunsConditionTimes()
        {
            (var module, _) = NewModule();
            module.Execute("x = 0\nwhile x < 5:\n    x = x + 1");
            Assert.That(module.GetGlobal("x").As<long>(), Is.EqualTo(5));
        }

        [Test]
        public void While_ConditionFalseAtEntry_BodyNeverRuns()
        {
            (var module, _) = NewModule();
            module.Execute("x = 10\nwhile x < 5:\n    x = x + 1");
            Assert.That(module.GetGlobal("x").As<long>(), Is.EqualTo(10));
        }

        // ============================================================================================================
        // B. break
        // ============================================================================================================

        [Test]
        public void Break_ExitsLoopEarly()
        {
            (var module, _) = NewModule();
            var src =
                "x = 0\n" +
                "while True:\n" +
                "    x = x + 1\n" +
                "    if x == 3:\n" +
                "        break";
            module.Execute(src);
            Assert.That(module.GetGlobal("x").As<long>(), Is.EqualTo(3));
        }

        [Test]
        public void Break_OutsideLoop_ThrowsAtCompileTime()
        {
            (var module, _) = NewModule();
            Assert.Throws<ParserEx>(() => module.Execute("break"));
        }

        // ============================================================================================================
        // C. continue
        // ============================================================================================================

        [Test]
        public void Continue_SkipsRemainderOfBody_ConditionReevaluated()
        {
            (var module, _) = NewModule();
            var src =
                "x = 0\n" +
                "total = 0\n" +
                "while x < 5:\n" +
                "    x = x + 1\n" +
                "    if x == 3:\n" +
                "        continue\n" +
                "    total = total + x";
            module.Execute(src);
            // 1 + 2 + 4 + 5 = 12 (x == 3 skipped)
            Assert.That(module.GetGlobal("total").As<long>(), Is.EqualTo(12));
        }

        [Test]
        public void Continue_OutsideLoop_ThrowsAtCompileTime()
        {
            (var module, _) = NewModule();
            Assert.Throws<ParserEx>(() => module.Execute("continue"));
        }

        // ============================================================================================================
        // D. Nesting
        // ============================================================================================================

        [Test]
        public void NestedWhile_InnerBreak_OnlyExitsInner()
        {
            (var module, _) = NewModule();
            var src =
                "i = 0\n" +
                "total = 0\n" +
                "while i < 3:\n" +
                "    j = 0\n" +
                "    while j < 10:\n" +
                "        if j == 2:\n" +
                "            break\n" +
                "        total = total + 1\n" +
                "        j = j + 1\n" +
                "    i = i + 1";
            module.Execute(src);
            // inner adds 2 per outer iter; outer runs 3 times -> 6
            Assert.Multiple(() =>
            {
                Assert.That(module.GetGlobal("i").As<long>(), Is.EqualTo(3));
                Assert.That(module.GetGlobal("total").As<long>(), Is.EqualTo(6));
            });
        }

        // ============================================================================================================
        // E. Module-scope visibility
        // ============================================================================================================

        [Test]
        public void While_LoopVariable_VisibleAfterLoopExits()
        {
            (var module, _) = NewModule();
            module.Execute("i = 0\nwhile i < 4:\n    i = i + 1");
            Assert.That(module.GetGlobal("i").As<long>(), Is.EqualTo(4));
        }

        // ============================================================================================================
        // F. Expression values can be captured inside loop body
        // ============================================================================================================

        [Test]
        public void While_AssignmentInBody_CapturesEachIteration()
        {
            (var module, var _) = NewModule();
            module.Execute("i = 0\nvalues = []\nwhile i < 3:\n    values.append(i)\n    i = i + 1");

            var values = (ChowList)module.GetGlobal("values");

            Assert.Multiple(() =>
            {
                Assert.That(values.Count, Is.EqualTo(3));
                Assert.That(values[0].As<long>(), Is.EqualTo(0));
                Assert.That(values[1].As<long>(), Is.EqualTo(1));
                Assert.That(values[2].As<long>(), Is.EqualTo(2));
            });
        }
    }
}
