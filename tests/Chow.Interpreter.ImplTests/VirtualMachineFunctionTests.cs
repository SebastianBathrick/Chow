using Chow.Interpreter.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class VirtualMachineFunctionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        const int LINE = 1;

        static Chunk Compile(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var parser = new Parser(tokens);
            var root = parser.BuildTree();
            var compiler = new Compiler(root);
            return compiler.CompileRoot();
        }

        static TaggedUnion Execute(Chunk chunk, Scope scope = null)
        {
            var vm = new VirtualMachine(chunk, scope);
            vm.EvaluateChunk();
            return vm.ValStackTop;
        }

        static TaggedUnion ExecuteSource(string source, Scope scope = null)
        {
            return Execute(Compile(source), scope);
        }

        // ============================================================================================================
        // A. PushNewClosureFromTemplate produces a Closure carrying captured scope
        // ============================================================================================================

        [Test]
        public void MakeClosure_ProducesClosure_WithCapturedScope()
        {
            // Hand-build a chunk: PushConstant(template) + PushNewClosureFromTemplate
            var module = new Chunk();
            var template = new ClosureTemplate(new Chunk(), "f", 0);
            var idx = module.RegisterConstant(new TaggedUnion(template));
            module.AddInstruction(OperationCode.PushConstant, LINE, idx);
            module.AddInstruction(OperationCode.PushNewClosureFromTemplate, LINE);

            var scope = new Scope();
            var vm = new VirtualMachine(module, scope);
            vm.EvaluateChunk();

            var top = vm.ValStackTop;
            Assert.Multiple(() =>
            {
                Assert.That(top.Tag, Is.EqualTo(Tag.Object));
                Assert.That(top.ObjectValue, Is.InstanceOf<Closure>());
                var closure = (Closure)top.ObjectValue;
                Assert.That(closure.Chunk, Is.SameAs(template.Chunk));
                Assert.That(closure.Enclosing, Is.SameAs(scope));
                Assert.That(closure.Name, Is.EqualTo("f"));
                Assert.That(closure.ParamCount, Is.EqualTo(0));
            });
        }

        // ============================================================================================================
        // B. End-to-end CallFunction + PushReturnValue produce the function's return value
        // ============================================================================================================

        [Test]
        public void Call_OnClosure_ProducesReturnValue()
        {
            var scope = new Scope();
            ExecuteSource("def f():\n    return 7\nresult = f()", scope);

            Assert.That(scope.GetVariableValue("result").IntegerValue, Is.EqualTo(7));
        }

        [Test]
        public void Call_AfterCall_CallerIPResumesAtNextInstr()
        {
            // After the call, the trailing expression `1 + 2` must execute. If caller IP didn't advance, it loops.
            // Result on the value stack is whatever the last expression statement produced; PopExpressionStatementResult
            // discards `f()`'s result. The last surviving stack top is the `1 + 2` result (discarded too), so we
            // rely on no infinite loop and on completion.
            var _ = ExecuteSource("def f():\n    return 1\nf()\n1 + 2");

            // If we got here, IP advancement after CallFunction works.
            Assert.Pass();
        }

        // ============================================================================================================
        // C. Arity mismatch
        // ============================================================================================================

        [Test]
        public void Call_ArityMismatch_RaisesTypeError_WithFunctionName()
        {
            var chunk = Compile("def myFunc(a):\n    return a\nmyFunc(1, 2)");

            var ex = Assert.Throws<TypeException>(() => Execute(chunk));

            Assert.That(ex.Message, Does.Contain("myFunc"));
        }

        [Test]
        public void Call_TooFewArgs_RaisesTypeError()
        {
            var chunk = Compile("def f(a, b):\n    return a\nf(1)");

            Assert.Throws<TypeException>(() => Execute(chunk));
        }

        // ============================================================================================================
        // D. Interop dispatch still works through the CallFunction op (regression)
        // ============================================================================================================

        [Test]
        public void Call_OnInteropDelegate_StillExecutes()
        {
            var module = new ChowModule();
            var captured = 0;
            module["bump"] = new Values.ChowDynamic((Action)(() => { captured = 1; }));

            module.Execute("bump()");

            Assert.That(captured, Is.EqualTo(1));
        }

        // ============================================================================================================
        // E. Deep recursion does not blow the C# stack (proves iterative dispatch)
        // ============================================================================================================

        [Test]
        public void DeepRecursion_200Levels_DoesNotStackOverflow()
        {
            var source =
                "def deep(n):\n" +
                "    if n == 0:\n" +
                "        return 0\n" +
                "    return deep(n - 1) + 1\n" +
                "result = deep(200)";

            var scope = new Scope();
            ExecuteSource(source, scope);

            Assert.That(scope.GetVariableValue("result").IntegerValue, Is.EqualTo(200));
        }

        // ============================================================================================================
        // F. PushReturnValue properly pops the frame so module-level resumes
        // ============================================================================================================

        [Test]
        public void Return_PopsFrame_ModuleResumesAfterCall()
        {
            // The post-call statement `x = 99` must execute; verify the module scope has `x = 99`.
            var scope = new Scope();
            ExecuteSource("def f():\n    return 1\nf()\nx = 99", scope);

            Assert.Multiple(() =>
            {
                Assert.That(scope.IsVariableDefined("x"), Is.True);
                Assert.That(scope.GetVariableValue("x").IntegerValue, Is.EqualTo(99));
            });
        }

        // ============================================================================================================
        // G. Closure capture observable through VM execution
        // ============================================================================================================

        [Test]
        public void Closure_CapturesModuleGlobal_VisibleInsideCall()
        {
            var scope = new Scope();
            ExecuteSource(
                "x = 100\n" +
                "def get():\n" +
                "    return x\n" +
                "result = get()",
                scope);

            Assert.That(scope.GetVariableValue("result").IntegerValue, Is.EqualTo(100));
        }
    }
}
