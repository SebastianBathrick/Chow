using System;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class ForExecutionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static ChowModule NewModule()
        {
            return new ChowModule();
        }

        // ============================================================================================================
        // A. range() iteration
        // ============================================================================================================

        [TestCase("for i in range(3):\n    values.append(i)", new long[] { 0, 1, 2 })]
        [TestCase("for i in range(2, 5):\n    values.append(i)", new long[] { 2, 3, 4 })]
        [TestCase("for i in range(2, 9, 2):\n    values.append(i)", new long[] { 2, 4, 6, 8 })]
        [TestCase("for i in range(5, 0, -1):\n    values.append(i)", new long[] { 5, 4, 3, 2, 1 })]
        [TestCase("for i in range(0):\n    values.append(i)", new long[] { })]
        public void For_RangeIteration_YieldsExpectedValues(string body, long[] expected)
        {
            var module = NewModule();
            module.Execute("values = []\n" + body);

            var values = (ChowList)module.GetGlobal("values");

            Assert.That(values.Count, Is.EqualTo(expected.Length));

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(values[i].AsType<long>(), Is.EqualTo(expected[i]));
            }
        }

        // ============================================================================================================
        // B. Iterating containers
        // ============================================================================================================

        [Test]
        public void For_ListIteration_VisitsEachElementInOrder()
        {
            var module = NewModule();
            module.Execute(
                "total = 0\n" +
                "for x in [10, 20, 30]:\n" +
                "    total = total + x");

            Assert.That(module.GetGlobal("total").AsType<long>(), Is.EqualTo(60));
        }

        [Test]
        public void For_StringIteration_YieldsOneCharStringsInOrder()
        {
            var module = NewModule();
            module.Execute(
                "chars = []\n" +
                "for c in \"abc\":\n" +
                "    chars.append(c)");

            var chars = (ChowList)module.GetGlobal("chars");

            Assert.Multiple(() =>
            {
                Assert.That(chars.Count, Is.EqualTo(3));
                Assert.That(((ChowStr)chars[0]).Value, Is.EqualTo("a"));
                Assert.That(((ChowStr)chars[1]).Value, Is.EqualTo("b"));
                Assert.That(((ChowStr)chars[2]).Value, Is.EqualTo("c"));
            });
        }

        // ============================================================================================================
        // C. break / continue / else
        // ============================================================================================================

        [Test]
        public void For_Break_ExitsLoopAndSkipsElseBlock()
        {
            var module = NewModule();
            module.Execute(
                "values = []\n" +
                "ran_else = False\n" +
                "for i in range(5):\n" +
                "    if i == 2:\n" +
                "        break\n" +
                "    values.append(i)\n" +
                "else:\n" +
                "    ran_else = True");

            var values = (ChowList)module.GetGlobal("values");

            Assert.Multiple(() =>
            {
                Assert.That(values.Count, Is.EqualTo(2));
                Assert.That(values[0].AsType<long>(), Is.EqualTo(0));
                Assert.That(values[1].AsType<long>(), Is.EqualTo(1));
                Assert.That(module.GetGlobal("ran_else").AsType<bool>(), Is.False);
            });
        }

        [Test]
        public void For_Continue_SkipsBodyAndElseStillRuns()
        {
            var module = NewModule();
            module.Execute(
                "values = []\n" +
                "ran_else = False\n" +
                "for i in range(4):\n" +
                "    if i == 2:\n" +
                "        continue\n" +
                "    values.append(i)\n" +
                "else:\n" +
                "    ran_else = True");

            var values = (ChowList)module.GetGlobal("values");

            Assert.Multiple(() =>
            {
                Assert.That(values.Count, Is.EqualTo(3));
                Assert.That(values[0].AsType<long>(), Is.EqualTo(0));
                Assert.That(values[1].AsType<long>(), Is.EqualTo(1));
                Assert.That(values[2].AsType<long>(), Is.EqualTo(3));
                Assert.That(module.GetGlobal("ran_else").AsType<bool>(), Is.True);
            });
        }

        [Test]
        public void For_NaturalExhaustion_RunsElseBlock()
        {
            var module = NewModule();
            module.Execute(
                "ran_else = False\n" +
                "for i in range(3):\n" +
                "    n = 0\n" +
                "else:\n" +
                "    ran_else = True");

            Assert.That(module.GetGlobal("ran_else").AsType<bool>(), Is.True);
        }

        // ============================================================================================================
        // D. Loop variable visibility (Python leak semantics)
        // ============================================================================================================

        [Test]
        public void For_LoopVariable_VisibleAfterLoopExits()
        {
            var module = NewModule();
            module.Execute("for i in range(3):\n    n = 0");

            Assert.That(module.GetGlobal("i").AsType<long>(), Is.EqualTo(2));
        }

        // ============================================================================================================
        // E. Errors
        // ============================================================================================================

        [Test]
        public void For_NonIterableTarget_ThrowsTypeException()
        {
            var module = NewModule();
            Assert.Throws<TypeException>(() => module.Execute("for x in 5:\n    n = 0"));
        }

        [Test]
        public void For_RangeStepZero_ThrowsAtCall()
        {
            var module = NewModule();
            Assert.Throws<InvalidOperationException>(() => module.Execute("for x in range(0, 5, 0):\n    n = 0"));
        }
    }
}
