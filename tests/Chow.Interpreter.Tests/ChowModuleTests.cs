using System.Collections.Generic;
using System.Linq;
using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter.Tests
{
    [TestFixture]
    class ChowModuleTests
    {

        #region Helpers

        static IEnumerable<string> AllBuiltInNames => BuiltIns.AllTypes.Select(BuiltIns.NameOf);

        #endregion

        #region Indexer

        [Test]
        public void Indexer_GetUndefinedName_ThrowsGlobalAccessException()
        {
            var module = new ChowModule();
            Assert.That(() => module["nope"], Throws.TypeOf<GlobalAccessException>()
                .With.Property(nameof(GlobalAccessException.Name)).EqualTo("nope"));
        }

        [Test]
        public void Indexer_SetThenGetInt_ReturnsValue()
        {
            var module = new ChowModule();
            module["x"] = 42;
            Assert.That(module["x"], Is.EqualTo(42L));
        }

        [Test]
        public void Indexer_SetThenGetDouble_ReturnsValue()
        {
            var module = new ChowModule();
            module["x"] = 3.14;
            Assert.That(module["x"], Is.EqualTo(3.14));
        }

        [Test]
        public void Indexer_SetThenGetString_ReturnsValue()
        {
            var module = new ChowModule();
            module["x"] = "hello";
            Assert.That(module["x"], Is.EqualTo("hello"));
        }

        [Test]
        public void Indexer_SetThenGetBool_ReturnsValue()
        {
            var module = new ChowModule();
            module["x"] = true;
            Assert.That(module["x"], Is.EqualTo(true));
        }

        [Test]
        public void Indexer_SetNullValue_ThrowsArgumentNullException()
        {
            var module = new ChowModule();
            Assert.That(() => module["x"] = null!, Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void Indexer_SetExistingName_OverwritesPreviousValue()
        {
            var module = new ChowModule();
            module["x"] = 1;
            module["x"] = 2;
            Assert.That(module["x"], Is.EqualTo(2L));
        }

        #endregion

        #region Constructor / Built-Ins

        [TestCaseSource(nameof(AllBuiltInNames))]
        public void Constructor_BuiltIn_IsAccessibleViaIndexer(string name)
        {
            var module = new ChowModule();
            Assert.That(() => module[name], Throws.Nothing);
        }

        #endregion

        #region Execute - Input Handling

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\n\n")]
        [TestCase("\t")]
        public void Execute_TriviallyEmptySource_IsNoOp(string? source)
        {
            var module = new ChowModule();
            Assert.That(() => module.Execute(source), Throws.Nothing);
        }

        #endregion

        #region Execute - Global Scope Semantics

        [Test]
        public void Execute_AssignsVariable_AccessibleViaIndexer()
        {
            var module = new ChowModule();
            module.Execute("x = 5");
            Assert.That(module["x"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_TwoCalls_PreserveGlobalScope()
        {
            var module = new ChowModule();
            module.Execute("x = 5");
            module.Execute("y = x + 1");
            Assert.That(module["y"], Is.EqualTo(6L));
        }

        [Test]
        public void Indexer_SetThenExecuteReads_HostValueVisibleToChow()
        {
            var module = new ChowModule();
            module["x"] = 10;
            module.Execute("y = x");
            Assert.That(module["y"], Is.EqualTo(10L));
        }

        #endregion
    }
}
