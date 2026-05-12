using Chow.Interpreter;
using Chow.Interpreter.Values;
using System;

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

            Assert.That(module.GetGlobal("x").As<long>(), Is.EqualTo(42));
        }

        // ------------------------------------------------------------------------------------------------------------
        // B — Single-arg delegates
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void B01_SingleArgFunc_ArgPassedCorrectly()
        {
            var module = MakeModule();
            module["f"] = (Func<ChowValue, ChowValue>)(v => new ChowInt(v.As<long>() + 1));

            module.Execute("x = f(10)");

            Assert.That(module.GetGlobal("x").As<long>(), Is.EqualTo(11));
        }

        [Test]
        public void B02_SingleArgAction_ArgPassedCorrectly()
        {
            var module = MakeModule();
            long received = -1;
            module["f"] = (Action<ChowValue>)(v => { received = v.As<long>(); });

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
                new ChowInt(args[0].As<long>() * 10 + args[1].As<long>()));

            module.Execute("x = f(3, 7)");

            Assert.That(module.GetGlobal("x").As<long>(), Is.EqualTo(37));
        }

        [Test]
        public void C02_MultiArgAction_ArgsInCorrectOrder()
        {
            var module = MakeModule();
            long first = -1, second = -1;
            module["f"] = (Action<ChowValue[]>)(args =>
            {
                first = args[0].As<long>();
                second = args[1].As<long>();
            });

            module.Execute("f(1, 2)");

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(1));
                Assert.That(second, Is.EqualTo(2));
            });
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
