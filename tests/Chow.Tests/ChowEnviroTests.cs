using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Values.Internal;

namespace Chow.Tests
{
    [TestFixture]
    public class ChowEnviroTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static LocalScope NewEnviro() => new LocalScope();

        static TaggedUnion Int(int value) => new TaggedUnion(value);

        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_NewInstance_IsTopLevel()
        {
            var env = NewEnviro();

            Assert.That(env.IsOutermostDepth, Is.True);
        }

        [Test]
        public void Constructor_NewInstance_NoVariablesDefined()
        {
            var env = NewEnviro();

            Assert.That(env.IsVariableDefined("x"), Is.False);
        }

        // ============================================================================================================
        // B. Assign + Get (top-level scope)
        // ============================================================================================================

        [Test]
        public void AssignVariableValue_NewName_StoresValue()
        {
            var env = NewEnviro();

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
            var env = NewEnviro();

            env.AssignVariableValue("x", Int(1));
            env.AssignVariableValue("x", Int(2));

            Assert.That(env.GetVariableValue("x"), Is.EqualTo(Int(2)));
        }

        [Test]
        public void IsVariableDefined_AfterAssign_ReturnsTrue()
        {
            var env = NewEnviro();

            env.AssignVariableValue("x", Int(0));

            Assert.That(env.IsVariableDefined("x"), Is.True);
        }

        [Test]
        public void IsVariableDefined_BeforeAssign_ReturnsFalse()
        {
            var env = NewEnviro();

            Assert.That(env.IsVariableDefined("anything"), Is.False);
        }

        // ============================================================================================================
        // C. Scope enter / exit
        // ============================================================================================================

        [Test]
        public void EnterScope_AfterCall_NoLongerTopLevel()
        {
            var env = NewEnviro();

            env.EnterNestedScope();

            Assert.That(env.IsOutermostDepth, Is.False);
        }

        [Test]
        public void ExitScope_AfterMatchingEnter_RestoresTopLevel()
        {
            var env = NewEnviro();

            env.EnterNestedScope();
            env.ExitNestedScope();

            Assert.That(env.IsOutermostDepth, Is.True);
        }

        [Test]
        public void ExitScope_RemovesScopeLocalVariables()
        {
            var env = NewEnviro();

            env.EnterNestedScope();
            env.AssignVariableValue("x", Int(1));
            env.ExitNestedScope();

            Assert.That(env.IsVariableDefined("x"), Is.False);
        }

        [Test]
        public void ExitScope_PreservesOuterVariables()
        {
            var env = NewEnviro();

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
            var env = NewEnviro();

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
            var env = NewEnviro();

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
            var env = NewEnviro();

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
            var env = NewEnviro();

            env.AssignVariableValue("x", Int(1));
            env.EnterNestedScope();
            env.AssignVariableValue("x", Int(2));

            Assert.That(env.GetVariableValue("x"), Is.EqualTo(Int(2)));
        }

        [Test]
        public void AssignVariableValue_InnerScopeRebindsExistingName_PersistsAfterExit()
        {
            // Python block-scope: `x = 1; if cond: x = 2` -> x is 2 after the block.
            var env = NewEnviro();

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
