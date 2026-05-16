using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Values;
namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class ClosureTemplateTests
    {
        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_SetsChunk()
        {
            var chunk = new Chunk();
            var template = new ClosureTemplate(chunk, "f", 0);

            Assert.That(template.Chunk, Is.SameAs(chunk));
        }

        [Test]
        public void Constructor_SetsName()
        {
            var template = new ClosureTemplate(new Chunk(), "myFunc", 0);

            Assert.That(template.Name, Is.EqualTo("myFunc"));
        }

        [Test]
        public void Constructor_SetsParamCount_Zero()
        {
            var template = new ClosureTemplate(new Chunk(), "f", 0);

            Assert.That(template.ParamCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_SetsParamCount_One()
        {
            var template = new ClosureTemplate(new Chunk(), "f", 1);

            Assert.That(template.ParamCount, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_SetsParamCount_Many()
        {
            var template = new ClosureTemplate(new Chunk(), "f", 7);

            Assert.That(template.ParamCount, Is.EqualTo(7));
        }

        // ============================================================================================================
        // B. Immutability
        // ============================================================================================================

        [Test]
        public void ClosureTemplate_HasNoPublicSetters()
        {
            var properties = typeof(ClosureTemplate).GetProperties();

            foreach (var property in properties)
            {
                Assert.That(property.GetSetMethod(), Is.Null, $"Property {property.Name} should be read-only");
            }
        }

        // ============================================================================================================
        // C. Identity
        // ============================================================================================================

        [Test]
        public void TwoInstances_SameInputs_AreDistinctRefs()
        {
            var shared = new Chunk();
            var a = new ClosureTemplate(shared, "f", 0);
            var b = new ClosureTemplate(shared, "f", 0);

            Assert.That(a, Is.Not.SameAs(b));
        }
    }
}
