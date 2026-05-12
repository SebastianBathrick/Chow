using Chow.Interpreter;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class WhileExecutionTests
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
            (ChowModule module, _) = NewModule();
            module.Execute("x = 0\nwhile x < 5:\n    x = x + 1");
            Assert.That(module["x"].As<long>(), Is.EqualTo(5));
        }

        [Test]
        public void While_ConditionFalseAtEntry_BodyNeverRuns()
        {
            (ChowModule module, _) = NewModule();
            module.Execute("x = 10\nwhile x < 5:\n    x = x + 1");
            Assert.That(module["x"].As<long>(), Is.EqualTo(10));
        }

        // ============================================================================================================
        // B. break
        // ============================================================================================================

        [Test]
        public void Break_ExitsLoopEarly()
        {
            (ChowModule module, _) = NewModule();
            string src =
                "x = 0\n" +
                "while True:\n" +
                "    x = x + 1\n" +
                "    if x == 3:\n" +
                "        break";
            module.Execute(src);
            Assert.That(module["x"].As<long>(), Is.EqualTo(3));
        }

        [Test]
        public void Break_OutsideLoop_ThrowsAtCompileTime()
        {
            (ChowModule module, _) = NewModule();
            Assert.Throws<ParserEx>(() => module.Execute("break"));
        }

        // ============================================================================================================
        // C. continue
        // ============================================================================================================

        [Test]
        public void Continue_SkipsRemainderOfBody_ConditionReevaluated()
        {
            (ChowModule module, _) = NewModule();
            string src =
                "x = 0\n" +
                "total = 0\n" +
                "while x < 5:\n" +
                "    x = x + 1\n" +
                "    if x == 3:\n" +
                "        continue\n" +
                "    total = total + x";
            module.Execute(src);
            // 1 + 2 + 4 + 5 = 12 (x == 3 skipped)
            Assert.That(module["total"].As<long>(), Is.EqualTo(12));
        }

        [Test]
        public void Continue_OutsideLoop_ThrowsAtCompileTime()
        {
            (ChowModule module, _) = NewModule();
            Assert.Throws<ParserEx>(() => module.Execute("continue"));
        }

        // ============================================================================================================
        // D. Nesting
        // ============================================================================================================

        [Test]
        public void NestedWhile_InnerBreak_OnlyExitsInner()
        {
            (ChowModule module, _) = NewModule();
            string src =
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
                Assert.That(module["i"].As<long>(), Is.EqualTo(3));
                Assert.That(module["total"].As<long>(), Is.EqualTo(6));
            });
        }

        // ============================================================================================================
        // E. Module-scope visibility
        // ============================================================================================================

        [Test]
        public void While_LoopVariable_VisibleAfterLoopExits()
        {
            (ChowModule module, _) = NewModule();
            module.Execute("i = 0\nwhile i < 4:\n    i = i + 1");
            Assert.That(module["i"].As<long>(), Is.EqualTo(4));
        }

        // ============================================================================================================
        // F. Expression-statement hook still fires inside loop body
        // ============================================================================================================

        [Test]
        public void While_ExprStmntInBody_HookFiresEachIteration()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("i = 0\nwhile i < 3:\n    i\n    i = i + 1");
            Assert.That(hook.Values.Count, Is.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(hook.Values[0].As<long>(), Is.EqualTo(0));
                Assert.That(hook.Values[1].As<long>(), Is.EqualTo(1));
                Assert.That(hook.Values[2].As<long>(), Is.EqualTo(2));
            });
        }
    }
}
