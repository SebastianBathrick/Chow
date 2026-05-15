using Chow.Interpreter.Values;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class InteropCallTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static ChowModule MakeModule() => new ChowModule();

        // ------------------------------------------------------------------------------------------------------------
        // A — No-arg delegates
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void A01_NoArgAction_SideEffectOccurs()
        {
            var module = MakeModule();
            var called = false;
            module["f"] = (Action)(() => { called = true; });

            module.Execute("f()");

            Assert.That(called, Is.True);
        }

        [Test]
        public void A02_NoArgFunc_ReturnValueStoredInVariable()
        {
            var module = MakeModule();
            module["f"] = (Func<ChowValue>)(() => new ChowInt(42));

            module.Execute("x = f()");

            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(42));
        }

        // ------------------------------------------------------------------------------------------------------------
        // B — Single-arg delegates
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void B01_SingleArgFunc_ArgPassedCorrectly()
        {
            var module = MakeModule();
            module["f"] = (Func<ChowValue, ChowValue>)(v => new ChowInt(v.AsType<long>() + 1));

            module.Execute("x = f(10)");

            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(11));
        }

        [Test]
        public void B02_SingleArgAction_ArgPassedCorrectly()
        {
            var module = MakeModule();
            long received = -1;
            module["f"] = (Action<ChowValue>)(v => { received = v.AsType<long>(); });

            module.Execute("f(99)");

            Assert.That(received, Is.EqualTo(99));
        }

        // ------------------------------------------------------------------------------------------------------------
        // C — Multi-arg delegates
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void C01_MultiArgFunc_ArgsInCorrectOrder()
        {
            var module = MakeModule();
            module["f"] = (Func<ChowValue[], ChowValue>)(args =>
                new ChowInt(args[0].AsType<long>() * 10 + args[1].AsType<long>()));

            module.Execute("x = f(3, 7)");

            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(37));
        }

        [Test]
        public void C02_MultiArgAction_ArgsInCorrectOrder()
        {
            var module = MakeModule();
            long first = -1, second = -1;
            module["f"] = (Action<ChowValue[]>)(args =>
            {
                first = args[0].AsType<long>();
                second = args[1].AsType<long>();
            });

            module.Execute("f(1, 2)");

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(1));
                Assert.That(second, Is.EqualTo(2));
            });
        }

        [Test]
        public void C03_ArrayFunc_NoArgs_ReceivesEmptyArray()
        {
            var module = MakeModule();
            module["f"] = (Func<ChowValue[], ChowValue>)(args => new ChowInt(args.Length));

            module.Execute("x = f()");

            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(0));
        }

        [Test]
        public void C04_ArrayFunc_OneArg_ReceivesSingleElementArray()
        {
            var module = MakeModule();
            module["f"] = (Func<ChowValue[], ChowValue>)(args =>
                new ChowInt(args.Length * 100 + args[0].AsType<long>()));

            module.Execute("x = f(23)");

            Assert.That(module.GetGlobal("x").AsType<long>(), Is.EqualTo(123));
        }

        [Test]
        public void C05_ArrayAction_NoArgs_ReceivesEmptyArray()
        {
            var module = MakeModule();
            var receivedLength = -1;
            module["f"] = (Action<ChowValue[]>)(args => { receivedLength = args.Length; });

            module.Execute("f()");

            Assert.That(receivedLength, Is.EqualTo(0));
        }

        [Test]
        public void C06_ArrayAction_OneArg_ReceivesSingleElementArray()
        {
            var module = MakeModule();
            long received = -1;
            module["f"] = (Action<ChowValue[]>)(args => { received = args[0].AsType<long>(); });

            module.Execute("f(99)");

            Assert.That(received, Is.EqualTo(99));
        }

        [Test]
        public void C07_ExpressionStatementInsideFunction_DoesNotPolluteNestedCallStack()
        {
            var module = MakeModule();
            var seen = new List<string>();
            module["sink"] = (Action<ChowValue>)(value => seen.Add(value.ToString()));

            module.Execute(
                "def add(x, y):\n" +
                "    sink('Adding')\n" +
                "    return x + y");

            module.Execute("sink(add(1, 2))");

            Assert.That(seen, Is.EqualTo(new[] { "Adding", "3" }));
        }

        // ------------------------------------------------------------------------------------------------------------
        // D — Error cases
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void D01_NonCallableObject_ThrowsInvalidOperationException()
        {
            var module = MakeModule();
            module["f"] = new object();

            Assert.Throws<InvalidOperationException>(() => module.Execute("f()"));
        }
    }
}
