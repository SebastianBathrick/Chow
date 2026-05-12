using Chow.Interpreter;
using Chow.Interpreter.Values;
using System;

namespace Chow.Tests
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
            bool called = false;
            module["f"] = new ChowDynamic((Action)(() => { called = true; }));

            module.Run("f()");

            Assert.That(called, Is.True);
        }

        [Test]
        public void A02_NoArgFunc_ReturnValueStoredInVariable()
        {
            var module = MakeModule();
            module["f"] = new ChowDynamic((Func<ChowValue>)(() => new ChowInt(42)));

            module.Run("x = f()");

            ChowValue result = module["x"];
            Assert.That(result.As<int>(), Is.EqualTo(42));
        }

        // ------------------------------------------------------------------------------------------------------------
        // B — Single-arg delegates
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void B01_SingleArgFunc_ArgPassedCorrectly()
        {
            var module = MakeModule();
            module["f"] = new ChowDynamic((Func<ChowValue, ChowValue>)(v => new ChowInt(v.As<int>() + 1)));

            module.Run("x = f(10)");

            Assert.That(module["x"].As<int>(), Is.EqualTo(11));
        }

        [Test]
        public void B02_SingleArgAction_ArgPassedCorrectly()
        {
            var module = MakeModule();
            int received = -1;
            module["f"] = new ChowDynamic((Action<ChowValue>)(v => { received = v.As<int>(); }));

            module.Run("f(99)");

            Assert.That(received, Is.EqualTo(99));
        }

        // ------------------------------------------------------------------------------------------------------------
        // C — Multi-arg delegates
        // ------------------------------------------------------------------------------------------------------------

        [Test]
        public void C01_MultiArgFunc_ArgsInCorrectOrder()
        {
            var module = MakeModule();
            module["f"] = new ChowDynamic((Func<ChowValue[], ChowValue>)(args =>
                new ChowInt(args[0].As<int>() * 10 + args[1].As<int>())));

            module.Run("x = f(3, 7)");

            Assert.That(module["x"].As<int>(), Is.EqualTo(37));
        }

        [Test]
        public void C02_MultiArgAction_ArgsInCorrectOrder()
        {
            var module = MakeModule();
            int first = -1, second = -1;
            module["f"] = new ChowDynamic((Action<ChowValue[]>)(args =>
            {
                first = args[0].As<int>();
                second = args[1].As<int>();
            }));

            module.Run("f(1, 2)");

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
            module["f"] = new ChowDynamic(new object());

            Assert.Throws<InvalidOperationException>(() => module.Run("f()"));
        }
    }
}
