using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class ClosureTests
    {
        static TaggedUnion Int(long v) => new TaggedUnion(v);

        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_SetsChunk()
        {
            Chunk chunk = new Chunk();
            ModuleScope module = new ModuleScope();
            Closure closure = new Closure(chunk, module, "f", 0);

            Assert.That(closure.Chunk, Is.SameAs(chunk));
        }

        [Test]
        public void Constructor_SetsName()
        {
            Closure closure = new Closure(new Chunk(), new ModuleScope(), "myFunc", 0);

            Assert.That(closure.Name, Is.EqualTo("myFunc"));
        }

        [Test]
        public void Constructor_SetsParamCount()
        {
            Closure closure = new Closure(new Chunk(), new ModuleScope(), "f", 3);

            Assert.That(closure.ParamCount, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_SetsEnclosing()
        {
            ModuleScope module = new ModuleScope();
            Closure closure = new Closure(new Chunk(), module, "f", 0);

            Assert.That(closure.Enclosing, Is.SameAs(module));
        }

        // ============================================================================================================
        // B. Capture-by-reference semantics
        // ============================================================================================================

        [Test]
        public void Enclosing_IsSameRef_NotCopy_WithModuleScope()
        {
            ModuleScope module = new ModuleScope();
            Closure closure = new Closure(new Chunk(), module, "f", 0);

            Assert.That(closure.Enclosing, Is.SameAs(module));
        }

        [Test]
        public void Enclosing_IsSameRef_WithLocalScope()
        {
            LocalScope local = new LocalScope(new ModuleScope());
            Closure closure = new Closure(new Chunk(), local, "f", 0);

            Assert.That(closure.Enclosing, Is.SameAs(local));
        }

        [Test]
        public void Mutation_OfCapturedScope_IsVisibleViaClosure()
        {
            ModuleScope module = new ModuleScope();
            Closure closure = new Closure(new Chunk(), module, "f", 0);

            module.AssignVariableValue("x", Int(42));

            Assert.Multiple(() =>
            {
                Assert.That(closure.Enclosing.IsVariableDefined("x"), Is.True);
                Assert.That(closure.Enclosing.GetVariableValue("x"), Is.EqualTo(Int(42)));
            });
        }

        [Test]
        public void Rebinding_CapturedName_IsVisibleViaClosure()
        {
            ModuleScope module = new ModuleScope();
            module.AssignVariableValue("x", Int(1));
            Closure closure = new Closure(new Chunk(), module, "f", 0);

            module.AssignVariableValue("x", Int(2));

            Assert.That(closure.Enclosing.GetVariableValue("x"), Is.EqualTo(Int(2)));
        }

        // ============================================================================================================
        // C. Identity
        // ============================================================================================================

        [Test]
        public void TwoClosures_SameInputs_AreDistinctRefs()
        {
            Chunk sharedChunk = new Chunk();
            ModuleScope sharedScope = new ModuleScope();

            Closure a = new Closure(sharedChunk, sharedScope, "f", 0);
            Closure b = new Closure(sharedChunk, sharedScope, "f", 0);

            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void TwoClosures_DifferentScopes_HaveDistinctEnclosings()
        {
            Chunk sharedChunk = new Chunk();
            ModuleScope scopeA = new ModuleScope();
            ModuleScope scopeB = new ModuleScope();

            Closure a = new Closure(sharedChunk, scopeA, "f", 0);
            Closure b = new Closure(sharedChunk, scopeB, "f", 0);

            Assert.That(a.Enclosing, Is.Not.SameAs(b.Enclosing));
        }
    }
}
