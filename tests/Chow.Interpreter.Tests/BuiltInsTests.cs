using Chow.Interpreter.Values;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class BuiltInsTests
    {
        [TestCase("print")]
        [TestCase("input")]
        [TestCase("float")]
        [TestCase("str")]
        [TestCase("int")]
        [TestCase("bool")]
        [TestCase("list")]
        [TestCase("dict")]
        [TestCase("len")]
        [TestCase("type")]
        [TestCase("abs")]
        [TestCase("round")]
        [TestCase("min")]
        [TestCase("max")]
        public void ImportBuiltIns_DefinesSharedBuiltIn(string name)
        {
            var module = new ChowModule();

            module.ImportBuiltIns();

            Assert.That(module.ContainsGlobal(name), Is.True);
        }

        [Test]
        public void ImportBuiltIns_Type_ReturnsPythonStyleTypeName()
        {
            var module = new ChowModule();
            module.ImportBuiltIns();

            module.Execute("__result = type(1)");

            var result = (ChowStr)module.GetGlobal("__result");
            Assert.That(result.Value, Is.EqualTo("int"));
        }

        [Test]
        public void ImportBuiltIns_Len_ReturnsCollectionLength()
        {
            var module = new ChowModule();
            module.ImportBuiltIns();

            module.Execute("__result = len([1, 2, 3])");

            Assert.That(module.GetGlobal("__result").AsType<long>(), Is.EqualTo(3));
        }

        [Test]
        public void ImportBuiltIns_ListWithNoArgs_ReturnsEmptyList()
        {
            var module = new ChowModule();
            module.ImportBuiltIns();

            module.Execute("__result = list()");

            var result = (ChowList)module.GetGlobal("__result");
            Assert.That(result.Count, Is.EqualTo(0));
        }
    }
}
