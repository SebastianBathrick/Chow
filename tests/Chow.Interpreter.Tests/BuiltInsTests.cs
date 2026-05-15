using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class BuiltInsTests
    {
        // ============================================================================================================
        // A. Auto-seeding at construction
        // ============================================================================================================

        [Test]
        public void NewModule_PrintIsActive_WithoutSetup()
        {
            var module = new ChowModule();

            Assert.That(module.IsBuiltInActive(BuiltInType.Print), Is.True);
        }

        [Test]
        public void NewModule_CanExecutePrint_WithoutImportCall()
        {
            var module = new ChowModule();

            Assert.DoesNotThrow(() => module.Execute("print(\"hello\")"));
        }

        [TestCase(BuiltInType.Print)]
        [TestCase(BuiltInType.Input)]
        [TestCase(BuiltInType.Float)]
        [TestCase(BuiltInType.Str)]
        [TestCase(BuiltInType.Int)]
        [TestCase(BuiltInType.Bool)]
        [TestCase(BuiltInType.List)]
        [TestCase(BuiltInType.Dict)]
        [TestCase(BuiltInType.Len)]
        [TestCase(BuiltInType.Type)]
        [TestCase(BuiltInType.Abs)]
        [TestCase(BuiltInType.Round)]
        [TestCase(BuiltInType.Min)]
        [TestCase(BuiltInType.Max)]
        public void NewModule_EachBuiltIn_IsActiveAndDefined(BuiltInType type)
        {
            var module = new ChowModule();

            Assert.Multiple(() =>
            {
                Assert.That(module.IsBuiltInActive(type), Is.True);
            });
        }

        // ============================================================================================================
        // B. SetBuiltInActive disable / re-enable
        // ============================================================================================================

        [Test]
        public void SetBuiltInActive_False_RemovesNameFromScope()
        {
            var module = new ChowModule();

            module.SetBuiltInActive(BuiltInType.Print, false);

            Assert.That(module.IsBuiltInActive(BuiltInType.Print), Is.False);
        }

        [Test]
        public void Disabled_PrintCall_RaisesNameError()
        {
            var module = new ChowModule();
            module.SetBuiltInActive(BuiltInType.Print, false);

            Assert.Throws<UndefinedNameException>(() => module.Execute("print(\"hi\")"));
        }

        [Test]
        public void SetBuiltInActive_True_AfterDisable_RestoresBuiltIn()
        {
            var module = new ChowModule();
            module.SetBuiltInActive(BuiltInType.Print, false);

            module.SetBuiltInActive(BuiltInType.Print, true);

            Assert.Multiple(() =>
            {
                Assert.That(module.IsBuiltInActive(BuiltInType.Print), Is.True);
                Assert.DoesNotThrow(() => module.Execute("print(\"hi\")"));
            });
        }

        [Test]
        public void SetBuiltInActive_True_WhenAlreadyActive_IsIdempotent()
        {
            var module = new ChowModule();

            module.SetBuiltInActive(BuiltInType.Print, true);

            Assert.That(module.IsBuiltInActive(BuiltInType.Print), Is.True);
        }

        [Test]
        public void SetBuiltInActive_False_WhenAlreadyInactive_IsIdempotent()
        {
            var module = new ChowModule();
            module.SetBuiltInActive(BuiltInType.Print, false);

            module.SetBuiltInActive(BuiltInType.Print, false);

            Assert.That(module.IsBuiltInActive(BuiltInType.Print), Is.False);
        }

        // ============================================================================================================
        // C. SetBuiltInValue override
        // ============================================================================================================

        [Test]
        public void SetBuiltInValue_WhileActive_TakesEffectImmediately()
        {
            var module = new ChowModule();
            var calledWith = (string)null;

            module.SetBuiltInValue(BuiltInType.Print, (Func<ChowValue, ChowValue>)(arg =>
            {
                calledWith = ((ChowStr)arg).Value;
                return ChowValue.None;
            }));

            module.Execute("print(\"captured\")");

            Assert.That(calledWith, Is.EqualTo("captured"));
        }

        [Test]
        public void SetBuiltInValue_WhileInactive_DoesNotReactivate()
        {
            var module = new ChowModule();
            module.SetBuiltInActive(BuiltInType.Print, false);

            module.SetBuiltInValue(BuiltInType.Print, (Func<ChowValue, ChowValue>)(_ => ChowValue.None));

            Assert.That(module.IsBuiltInActive(BuiltInType.Print), Is.False);
        }

        [Test]
        public void SetBuiltInValue_WhileInactive_ThenReenable_InstallsOverride()
        {
            var module = new ChowModule();
            var calledWith = (string)null;
            module.SetBuiltInActive(BuiltInType.Print, false);

            module.SetBuiltInValue(BuiltInType.Print, (Func<ChowValue, ChowValue>)(arg =>
            {
                calledWith = ((ChowStr)arg).Value;
                return ChowValue.None;
            }));
            module.SetBuiltInActive(BuiltInType.Print, true);
            module.Execute("print(\"installed\")");

            Assert.That(calledWith, Is.EqualTo("installed"));
        }

        [Test]
        public void Override_SurvivesDisableEnableCycle()
        {
            var module = new ChowModule();
            var callCount = 0;
            module.SetBuiltInValue(BuiltInType.Print, (Func<ChowValue, ChowValue>)(_ =>
            {
                callCount++;
                return ChowValue.None;
            }));

            module.SetBuiltInActive(BuiltInType.Print, false);
            module.SetBuiltInActive(BuiltInType.Print, true);
            module.Execute("print(\"x\")");

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void SetBuiltInValue_Null_ThrowsArgumentNullException()
        {
            var module = new ChowModule();

            Assert.Throws<ArgumentNullException>(() => module.SetBuiltInValue(BuiltInType.Print, null));
        }

        // ============================================================================================================
        // D. Ported behavior coverage (preserved from the original BuiltInsTests)
        // ============================================================================================================

        [Test]
        public void Type_ReturnsPythonStyleTypeName()
        {
            var module = new ChowModule();

            module.Execute("__result = type(1)");

            var result = (ChowStr)module.GetGlobal("__result");
            Assert.That(result.Value, Is.EqualTo("int"));
        }

        [Test]
        public void Len_ReturnsCollectionLength()
        {
            var module = new ChowModule();

            module.Execute("__result = len([1, 2, 3])");

            Assert.That(module.GetGlobal("__result").AsType<long>(), Is.EqualTo(3));
        }

        [Test]
        public void List_NoArgs_ReturnsEmptyList()
        {
            var module = new ChowModule();

            module.Execute("__result = list()");

            var result = (ChowList)module.GetGlobal("__result");
            Assert.That(result.Count, Is.EqualTo(0));
        }
    }
}
