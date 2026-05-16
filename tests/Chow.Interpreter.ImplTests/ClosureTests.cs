using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;
namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class ClosureTests
    {
        static ChowValue Int(long v)
        {
            return new ChowValue(v);
        }

        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_SetsChunk()
        {
            var chunk = new Chunk();
            var module = new Scope();
            var closure = new Closure(chunk, module, "f", 0);

            Assert.That(closure.Chunk, Is.SameAs(chunk));
        }

        [Test]
        public void Constructor_SetsName()
        {
            var closure = new Closure(new Chunk(), new Scope(), "myFunc", 0);

            Assert.That(closure.Name, Is.EqualTo("myFunc"));
        }

        [Test]
        public void Constructor_SetsParamCount()
        {
            var closure = new Closure(new Chunk(), new Scope(), "f", 3);

            Assert.That(closure.ParamCount, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_SetsEnclosing()
        {
            var module = new Scope();
            var closure = new Closure(new Chunk(), module, "f", 0);

            Assert.That(closure.Enclosing, Is.SameAs(module));
        }

        // ============================================================================================================
        // B. Capture-by-reference semantics
        // ============================================================================================================

        [Test]
        public void Enclosing_IsSameRef_NotCopy_WithModuleScope()
        {
            var module = new Scope();
            var closure = new Closure(new Chunk(), module, "f", 0);

            Assert.That(closure.Enclosing, Is.SameAs(module));
        }

        [Test]
        public void Enclosing_IsSameRef_WithLocalScope()
        {
            var local = new Scope(new Scope());
            var closure = new Closure(new Chunk(), local, "f", 0);

            Assert.That(closure.Enclosing, Is.SameAs(local));
        }

        [Test]
        public void Mutation_OfCapturedScope_IsVisibleViaClosure()
        {
            var module = new Scope();
            var closure = new Closure(new Chunk(), module, "f", 0);

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
            var module = new Scope();
            module.AssignVariableValue("x", Int(1));
            var closure = new Closure(new Chunk(), module, "f", 0);

            module.AssignVariableValue("x", Int(2));

            Assert.That(closure.Enclosing.GetVariableValue("x"), Is.EqualTo(Int(2)));
        }

        // ============================================================================================================
        // C. Identity
        // ============================================================================================================

        [Test]
        public void TwoClosures_SameInputs_AreDistinctRefs()
        {
            var sharedChunk = new Chunk();
            var sharedScope = new Scope();

            var a = new Closure(sharedChunk, sharedScope, "f", 0);
            var b = new Closure(sharedChunk, sharedScope, "f", 0);

            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void TwoClosures_DifferentScopes_HaveDistinctEnclosings()
        {
            var sharedChunk = new Chunk();
            var scopeA = new Scope();
            var scopeB = new Scope();

            var a = new Closure(sharedChunk, scopeA, "f", 0);
            var b = new Closure(sharedChunk, scopeB, "f", 0);

            Assert.That(a.Enclosing, Is.Not.SameAs(b.Enclosing));
        }
    }
}
