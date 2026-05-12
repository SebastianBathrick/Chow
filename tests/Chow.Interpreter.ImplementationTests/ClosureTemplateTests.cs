using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Values;
using System.Reflection;

namespace Chow.Interpreter.ImplementationTests
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
            Chunk chunk = new Chunk();
            ClosureTemplate template = new ClosureTemplate(chunk, "f", 0);

            Assert.That(template.Chunk, Is.SameAs(chunk));
        }

        [Test]
        public void Constructor_SetsName()
        {
            ClosureTemplate template = new ClosureTemplate(new Chunk(), "myFunc", 0);

            Assert.That(template.Name, Is.EqualTo("myFunc"));
        }

        [Test]
        public void Constructor_SetsParamCount_Zero()
        {
            ClosureTemplate template = new ClosureTemplate(new Chunk(), "f", 0);

            Assert.That(template.ParamCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_SetsParamCount_One()
        {
            ClosureTemplate template = new ClosureTemplate(new Chunk(), "f", 1);

            Assert.That(template.ParamCount, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_SetsParamCount_Many()
        {
            ClosureTemplate template = new ClosureTemplate(new Chunk(), "f", 7);

            Assert.That(template.ParamCount, Is.EqualTo(7));
        }

        // ============================================================================================================
        // B. Immutability
        // ============================================================================================================

        [Test]
        public void ClosureTemplate_HasNoPublicSetters()
        {
            PropertyInfo[] properties = typeof(ClosureTemplate).GetProperties();

            foreach (PropertyInfo property in properties)
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
            Chunk shared = new Chunk();
            ClosureTemplate a = new ClosureTemplate(shared, "f", 0);
            ClosureTemplate b = new ClosureTemplate(shared, "f", 0);

            Assert.That(a, Is.Not.SameAs(b));
        }
    }
}
