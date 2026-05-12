using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class LocalScopeTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static TaggedUnion Int(long value) => new TaggedUnion(value);

        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_WithModuleParent_ParentOrNullReturnsModule()
        {
            var module = new ModuleScope();
            var local = new LocalScope(module);

            Assert.That(local.ParentOrNull, Is.SameAs(module));
        }

        [Test]
        public void Constructor_WithLocalParent_ParentOrNullReturnsParentLocal()
        {
            var module = new ModuleScope();
            var outer = new LocalScope(module);
            var inner = new LocalScope(outer);

            Assert.That(inner.ParentOrNull, Is.SameAs(outer));
        }

        [Test]
        public void Constructor_NewInstance_NoVariablesDefined()
        {
            var local = new LocalScope(new ModuleScope());

            Assert.That(local.IsVariableDefined("x"), Is.False);
        }

        // ============================================================================================================
        // B. Local-only writes (Python local-by-default)
        // ============================================================================================================

        [Test]
        public void AssignVariableValue_DoesNotMutateParent()
        {
            var module = new ModuleScope();
            module.AssignVariableValue("x", Int(1));
            var local = new LocalScope(module);

            local.AssignVariableValue("x", Int(2));

            Assert.Multiple(() =>
            {
                Assert.That(local.GetVariableValue("x"), Is.EqualTo(Int(2)));
                Assert.That(module.GetVariableValue("x"), Is.EqualTo(Int(1)));
            });
        }

        [Test]
        public void IsVariableDefined_LocalOnly_DoesNotConsultParent()
        {
            // LocalScope.IsVariableDefined is non-recursive; chain walking lives in CallStack.
            var module = new ModuleScope();
            module.AssignVariableValue("x", Int(1));
            var local = new LocalScope(module);

            Assert.That(local.IsVariableDefined("x"), Is.False);
        }

        // ============================================================================================================
        // C. No copy ctor (snapshot semantics removed in favor of ref-sharing)
        // ============================================================================================================

        [Test]
        public void LocalScope_HasNoCopyConstructor()
        {
            // Compile-time check: this test exists to flag any reintroduction of a copy ctor.
            // If a copy ctor returns, this test still passes but the design intent is documented.
            var ctors = typeof(LocalScope).GetConstructors();
            var hasCopyCtor = false;
            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(LocalScope))
                {
                    hasCopyCtor = true;
                    break;
                }
            }

            Assert.That(hasCopyCtor, Is.False);
        }
    }
}
