using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Stack;
using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class CallStackClosureTests
    {
        static TaggedUnion Int(long v) => new TaggedUnion(v);

        static (CallStack stack, ModuleScope module) NewStack()
        {
            var module = new ModuleScope();
            var stack = new CallStack(new Chunk(), module);
            return (stack, module);
        }

        static Closure NewClosure(IScope enclosing, int paramCount = 0)
        {
            return new Closure(new Chunk(), enclosing, "f", paramCount);
        }

        // ============================================================================================================
        // A. Module-level invariants
        // ============================================================================================================

        [Test]
        public void NewStack_IsModuleLevel()
        {
            (var stack, var _) = NewStack();

            Assert.That(stack.IsModuleLevel, Is.True);
        }

        [Test]
        public void NewStack_CurrentScope_IsModuleScope()
        {
            (var stack, var module) = NewStack();

            Assert.That(stack.CurrentScope, Is.SameAs(module));
        }

        // ============================================================================================================
        // B. EnterFunctionCall mechanics
        // ============================================================================================================

        [Test]
        public void EnterFunctionCall_FlipsIsModuleLevel()
        {
            (var stack, var module) = NewStack();

            stack.EnterFunctionCall(NewClosure(module));

            Assert.That(stack.IsModuleLevel, Is.False);
        }

        [Test]
        public void EnterFunctionCall_SwitchesCurrentChunkToClosureChunk()
        {
            (var stack, var module) = NewStack();
            var closure = NewClosure(module);

            stack.EnterFunctionCall(closure);

            Assert.That(stack.CurrentChunk, Is.SameAs(closure.Chunk));
        }

        [Test]
        public void EnterFunctionCall_NewFrameScope_IsLocalScope()
        {
            (var stack, var module) = NewStack();

            stack.EnterFunctionCall(NewClosure(module));

            Assert.That(stack.CurrentScope, Is.InstanceOf<LocalScope>());
        }

        [Test]
        public void EnterFunctionCall_NewLocalScope_ParentIsClosureEnclosing()
        {
            (var stack, var module) = NewStack();
            var closure = NewClosure(module);

            stack.EnterFunctionCall(closure);

            Assert.That(stack.CurrentScope.ParentOrNull, Is.SameAs(closure.Enclosing));
        }

        [Test]
        public void EnterFunctionCall_NestedClosure_ParentIsOuterLocalScope()
        {
            (var stack, var module) = NewStack();

            // Simulate outer function call
            stack.EnterFunctionCall(NewClosure(module));
            var outerLocal = stack.CurrentScope;

            // Inner closure was defined inside the outer call, so it captures outerLocal.
            stack.EnterFunctionCall(NewClosure(outerLocal));

            Assert.That(stack.CurrentScope.ParentOrNull, Is.SameAs(outerLocal));
        }

        // ============================================================================================================
        // C. Write isolation
        // ============================================================================================================

        [Test]
        public void Assign_InsideCall_LandsInLocalScope_NotModule()
        {
            (var stack, var module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));

            stack.AssignVariableValue("x", Int(99));

            Assert.Multiple(() =>
            {
                Assert.That(stack.CurrentScope.IsVariableDefined("x"), Is.True);
                Assert.That(module.IsVariableDefined("x"), Is.False);
            });
        }

        // ============================================================================================================
        // D. Lookup walks the closure chain
        // ============================================================================================================

        [Test]
        public void Lookup_InsideCall_FindsCapturedModuleGlobal()
        {
            (var stack, var module) = NewStack();
            module.AssignVariableValue("g", Int(7));
            stack.EnterFunctionCall(NewClosure(module));

            Assert.Multiple(() =>
            {
                Assert.That(stack.IsVariableDefined("g"), Is.True);
                Assert.That(stack.GetVariableValue("g"), Is.EqualTo(Int(7)));
            });
        }

        [Test]
        public void Lookup_InsideNestedCall_WalksLocalThenEnclosingThenModule()
        {
            (var stack, var module) = NewStack();
            module.AssignVariableValue("globalVar", Int(1));

            stack.EnterFunctionCall(NewClosure(module));
            stack.AssignVariableValue("outerLocal", Int(2));
            var outerLocal = stack.CurrentScope;

            stack.EnterFunctionCall(NewClosure(outerLocal));
            stack.AssignVariableValue("innerLocal", Int(3));

            Assert.Multiple(() =>
            {
                Assert.That(stack.GetVariableValue("innerLocal"), Is.EqualTo(Int(3)));
                Assert.That(stack.GetVariableValue("outerLocal"), Is.EqualTo(Int(2)));
                Assert.That(stack.GetVariableValue("globalVar"), Is.EqualTo(Int(1)));
            });
        }

        // ============================================================================================================
        // E. Recursion isolation
        // ============================================================================================================

        [Test]
        public void TwoNestedEntries_OfSameClosure_HaveDistinctLocalScopes()
        {
            (var stack, var module) = NewStack();
            var closure = NewClosure(module);

            stack.EnterFunctionCall(closure);
            var first = stack.CurrentScope;

            stack.EnterFunctionCall(closure);
            var second = stack.CurrentScope;

            Assert.That(first, Is.Not.SameAs(second));
        }

        // ============================================================================================================
        // F. ExitFunctionCall mechanics
        // ============================================================================================================

        [Test]
        public void ExitFunctionCall_RestoresIsModuleLevel()
        {
            (var stack, var module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));

            stack.ExitFunctionCall();

            Assert.That(stack.IsModuleLevel, Is.True);
        }

        [Test]
        public void ExitFunctionCall_RestoresCurrentScope_ToModule()
        {
            (var stack, var module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));

            stack.ExitFunctionCall();

            Assert.That(stack.CurrentScope, Is.SameAs(module));
        }

        [Test]
        public void ExitFunctionCall_LocalNames_NoLongerVisible()
        {
            (var stack, var module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));
            stack.AssignVariableValue("x", Int(5));

            stack.ExitFunctionCall();

            Assert.That(stack.IsVariableDefined("x"), Is.False);
        }
    }
}
