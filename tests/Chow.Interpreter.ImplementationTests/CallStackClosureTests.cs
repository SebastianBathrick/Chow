using Chow.Interpreter.Compilation;
using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Values.Internal;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class CallStackClosureTests
    {
        static TaggedUnion Int(long v) => new TaggedUnion(v);

        static (CallStack stack, ModuleScope module) NewStack()
        {
            ModuleScope module = new ModuleScope();
            CallStack stack = new CallStack(new Chunk(), module);
            return (stack, module);
        }

        static Closure NewClosure(Scope enclosing, int paramCount = 0)
        {
            return new Closure(new Chunk(), enclosing, "f", paramCount);
        }

        // ============================================================================================================
        // A. Module-level invariants
        // ============================================================================================================

        [Test]
        public void NewStack_IsModuleLevel()
        {
            (CallStack stack, ModuleScope _) = NewStack();

            Assert.That(stack.IsModuleLevel, Is.True);
        }

        [Test]
        public void NewStack_CurrentScope_IsModuleScope()
        {
            (CallStack stack, ModuleScope module) = NewStack();

            Assert.That(stack.CurrentScope, Is.SameAs(module));
        }

        // ============================================================================================================
        // B. EnterFunctionCall mechanics
        // ============================================================================================================

        [Test]
        public void EnterFunctionCall_FlipsIsModuleLevel()
        {
            (CallStack stack, ModuleScope module) = NewStack();

            stack.EnterFunctionCall(NewClosure(module));

            Assert.That(stack.IsModuleLevel, Is.False);
        }

        [Test]
        public void EnterFunctionCall_SwitchesCurrentChunkToClosureChunk()
        {
            (CallStack stack, ModuleScope module) = NewStack();
            Closure closure = NewClosure(module);

            stack.EnterFunctionCall(closure);

            Assert.That(stack.CurrentChunk, Is.SameAs(closure.Chunk));
        }

        [Test]
        public void EnterFunctionCall_NewFrameScope_IsLocalScope()
        {
            (CallStack stack, ModuleScope module) = NewStack();

            stack.EnterFunctionCall(NewClosure(module));

            Assert.That(stack.CurrentScope, Is.InstanceOf<LocalScope>());
        }

        [Test]
        public void EnterFunctionCall_NewLocalScope_ParentIsClosureEnclosing()
        {
            (CallStack stack, ModuleScope module) = NewStack();
            Closure closure = NewClosure(module);

            stack.EnterFunctionCall(closure);

            Assert.That(stack.CurrentScope.ParentOrNull, Is.SameAs(closure.Enclosing));
        }

        [Test]
        public void EnterFunctionCall_NestedClosure_ParentIsOuterLocalScope()
        {
            (CallStack stack, ModuleScope module) = NewStack();

            // Simulate outer function call
            stack.EnterFunctionCall(NewClosure(module));
            Scope outerLocal = stack.CurrentScope;

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
            (CallStack stack, ModuleScope module) = NewStack();
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
            (CallStack stack, ModuleScope module) = NewStack();
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
            (CallStack stack, ModuleScope module) = NewStack();
            module.AssignVariableValue("globalVar", Int(1));

            stack.EnterFunctionCall(NewClosure(module));
            stack.AssignVariableValue("outerLocal", Int(2));
            Scope outerLocal = stack.CurrentScope;

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
            (CallStack stack, ModuleScope module) = NewStack();
            Closure closure = NewClosure(module);

            stack.EnterFunctionCall(closure);
            Scope first = stack.CurrentScope;

            stack.EnterFunctionCall(closure);
            Scope second = stack.CurrentScope;

            Assert.That(first, Is.Not.SameAs(second));
        }

        // ============================================================================================================
        // F. ExitFunctionCall mechanics
        // ============================================================================================================

        [Test]
        public void ExitFunctionCall_RestoresIsModuleLevel()
        {
            (CallStack stack, ModuleScope module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));

            stack.ExitFunctionCall();

            Assert.That(stack.IsModuleLevel, Is.True);
        }

        [Test]
        public void ExitFunctionCall_RestoresCurrentScope_ToModule()
        {
            (CallStack stack, ModuleScope module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));

            stack.ExitFunctionCall();

            Assert.That(stack.CurrentScope, Is.SameAs(module));
        }

        [Test]
        public void ExitFunctionCall_LocalNames_NoLongerVisible()
        {
            (CallStack stack, ModuleScope module) = NewStack();
            stack.EnterFunctionCall(NewClosure(module));
            stack.AssignVariableValue("x", Int(5));

            stack.ExitFunctionCall();

            Assert.That(stack.IsVariableDefined("x"), Is.False);
        }
    }
}
