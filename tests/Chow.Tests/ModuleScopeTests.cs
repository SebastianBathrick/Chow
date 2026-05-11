using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Values.Internal;

namespace Chow.Tests
{
    [TestFixture]
    public class ModuleScopeTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static ModuleScope NewModule() => new ModuleScope();

        static TaggedUnion Int(int value) => new TaggedUnion(value);

        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_NewInstance_IsOutermostDepth()
        {
            var env = NewModule();

            Assert.That(env.IsOutermostDepth, Is.True);
        }

        [Test]
        public void Constructor_NewInstance_NoVariablesDefined()
        {
            var env = NewModule();

            Assert.That(env.IsVariableDefined("x"), Is.False);
        }

        [Test]
        public void Constructor_NewInstance_HasNoParent()
        {
            var env = NewModule();

            Assert.That(env.ParentOrNull, Is.Null);
        }

        // ============================================================================================================
        // B. Assign + Get (top-level scope)
        // ============================================================================================================

        [Test]
        public void AssignVariableValue_NewName_StoresValue()
        {
            var env = NewModule();

            env.AssignVariableValue("x", Int(5));

            Assert.Multiple(() =>
            {
                Assert.That(env.IsVariableDefined("x"), Is.True);
                Assert.That(env.GetVariableValue("x"), Is.EqualTo(Int(5)));
            });
        }

        [Test]
        public void AssignVariableValue_ExistingName_OverwritesValue()
        {
            var env = NewModule();

            env.AssignVariableValue("x", Int(1));
            env.AssignVariableValue("x", Int(2));

            Assert.That(env.GetVariableValue("x"), Is.EqualTo(Int(2)));
        }

        [Test]
        public void IsVariableDefined_AfterAssign_ReturnsTrue()
        {
            var env = NewModule();

            env.AssignVariableValue("x", Int(0));

            Assert.That(env.IsVariableDefined("x"), Is.True);
        }

        [Test]
        public void IsVariableDefined_BeforeAssign_ReturnsFalse()
        {
            var env = NewModule();

            Assert.That(env.IsVariableDefined("anything"), Is.False);
        }

        // ============================================================================================================
        // C. Scope enter / exit
        // ============================================================================================================

        [Test]
        public void EnterScope_AfterCall_NoLongerOutermostDepth()
        {
            var env = NewModule();

            env.EnterNestedScope();

            Assert.That(env.IsOutermostDepth, Is.False);
        }

        [Test]
        public void ExitScope_AfterMatchingEnter_RestoresOutermostDepth()
        {
            var env = NewModule();

            env.EnterNestedScope();
            env.ExitNestedScope();

            Assert.That(env.IsOutermostDepth, Is.True);
        }

        [Test]
        public void ExitScope_RemovesScopeLocalVariables()
        {
            var env = NewModule();

            env.EnterNestedScope();
            env.AssignVariableValue("x", Int(1));
            env.ExitNestedScope();

            Assert.That(env.IsVariableDefined("x"), Is.False);
        }

        [Test]
        public void ExitScope_PreservesOuterVariables()
        {
            var env = NewModule();

            env.AssignVariableValue("outer", Int(7));
            env.EnterNestedScope();
            env.AssignVariableValue("inner", Int(9));
            env.ExitNestedScope();

            Assert.Multiple(() =>
            {
                Assert.That(env.IsVariableDefined("outer"), Is.True);
                Assert.That(env.GetVariableValue("outer"), Is.EqualTo(Int(7)));
                Assert.That(env.IsVariableDefined("inner"), Is.False);
            });
        }

        [Test]
        public void ExitScope_MultipleVariablesInScope_RemovesAll()
        {
            var env = NewModule();

            env.EnterNestedScope();
            env.AssignVariableValue("a", Int(1));
            env.AssignVariableValue("b", Int(2));
            env.AssignVariableValue("c", Int(3));
            env.ExitNestedScope();

            Assert.Multiple(() =>
            {
                Assert.That(env.IsVariableDefined("a"), Is.False);
                Assert.That(env.IsVariableDefined("b"), Is.False);
                Assert.That(env.IsVariableDefined("c"), Is.False);
            });
        }

        [Test]
        public void EnterExit_NestedScopes_TracksDepth()
        {
            var env = NewModule();

            env.EnterNestedScope();
            env.EnterNestedScope();
            Assert.That(env.IsOutermostDepth, Is.False);

            env.ExitNestedScope();
            Assert.That(env.IsOutermostDepth, Is.False);

            env.ExitNestedScope();
            Assert.That(env.IsOutermostDepth, Is.True);
        }

        [Test]
        public void ExitScope_NestedScope_RemovesInnermostVariablesOnly()
        {
            var env = NewModule();

            env.EnterNestedScope();
            env.AssignVariableValue("mid", Int(5));
            env.EnterNestedScope();
            env.AssignVariableValue("inner", Int(9));
            env.ExitNestedScope();

            Assert.Multiple(() =>
            {
                Assert.That(env.IsVariableDefined("mid"), Is.True);
                Assert.That(env.GetVariableValue("mid"), Is.EqualTo(Int(5)));
                Assert.That(env.IsVariableDefined("inner"), Is.False);
            });
        }

        // ============================================================================================================
        // D. Block-scope rebinding (Python semantics: inner write to existing name persists past scope exit)
        // ============================================================================================================

        [Test]
        public void AssignVariableValue_InnerScopeRebindsExistingName_InnerScopeSeesNewValue()
        {
            var env = NewModule();

            env.AssignVariableValue("x", Int(1));
            env.EnterNestedScope();
            env.AssignVariableValue("x", Int(2));

            Assert.That(env.GetVariableValue("x"), Is.EqualTo(Int(2)));
        }

        [Test]
        public void AssignVariableValue_InnerScopeRebindsExistingName_PersistsAfterExit()
        {
            // Python block-scope: `x = 1; if cond: x = 2` -> x is 2 after the block.
            var env = NewModule();

            env.AssignVariableValue("x", Int(1));
            env.EnterNestedScope();
            env.AssignVariableValue("x", Int(2));
            env.ExitNestedScope();

            Assert.Multiple(() =>
            {
                Assert.That(env.IsVariableDefined("x"), Is.True);
                Assert.That(env.GetVariableValue("x"), Is.EqualTo(Int(2)));
            });
        }
    }
}
