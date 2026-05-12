using Chow.Interpreter.Compilation;
using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values.Internal;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
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
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.ScanTokens();
            Parser parser = new Parser(tokens);
            Node root = parser.BuildTree();
            Compiler compiler = new Compiler(root);
            return compiler.CompileRoot();
        }

        static TaggedUnion Execute(Chunk chunk, ModuleScope scope = null)
        {
            VirtualMachine vm = new VirtualMachine(chunk, scope, null!);
            vm.EvaluateChunk();
            return vm.ValStackTop;
        }

        static TaggedUnion ExecuteSource(string source, ModuleScope scope = null)
        {
            return Execute(Compile(source), scope);
        }

        // ============================================================================================================
        // A. MakeClosure produces a Closure carrying captured scope
        // ============================================================================================================

        [Test]
        public void MakeClosure_ProducesClosure_WithCapturedScope()
        {
            // Hand-build a chunk: PushConstant(template) + MakeClosure
            Chunk module = new Chunk();
            ClosureTemplate template = new ClosureTemplate(new Chunk(), "f", 0);
            int idx = module.RegisterConstant(new TaggedUnion((object)template));
            module.AddInstruction(OperationCode.PushConstant, LINE, idx);
            module.AddInstruction(OperationCode.MakeClosure, LINE);

            ModuleScope scope = new ModuleScope();
            VirtualMachine vm = new VirtualMachine(module, scope, null!);
            vm.EvaluateChunk();

            TaggedUnion top = vm.ValStackTop;
            Assert.Multiple(() =>
            {
                Assert.That(top.Tag, Is.EqualTo(Tag.Object));
                Assert.That(top.ObjectValue, Is.InstanceOf<Closure>());
                Closure closure = (Closure)top.ObjectValue;
                Assert.That(closure.Chunk, Is.SameAs(template.Chunk));
                Assert.That(closure.Enclosing, Is.SameAs(scope));
                Assert.That(closure.Name, Is.EqualTo("f"));
                Assert.That(closure.ParamCount, Is.EqualTo(0));
            });
        }

        // ============================================================================================================
        // B. End-to-end Call + ReturnValue produce the function's return value
        // ============================================================================================================

        [Test]
        public void Call_OnClosure_ProducesReturnValue()
        {
            ModuleScope scope = new ModuleScope();
            ExecuteSource("def f():\n    return 7\nresult = f()", scope);

            Assert.That(scope.GetVariableValue("result").IntegerValue, Is.EqualTo(7));
        }

        [Test]
        public void Call_AfterCall_CallerIPResumesAtNextInstr()
        {
            // After the call, the trailing expression `1 + 2` must execute. If caller IP didn't advance, it loops.
            // Result on the value stack is whatever the last expression statement produced; PopExprStmntResult
            // discards `f()`'s result. The last surviving stack top is the `1 + 2` result (discarded too), so we
            // rely on no infinite loop and on completion.
            TaggedUnion _ = ExecuteSource("def f():\n    return 1\nf()\n1 + 2");

            // If we got here, IP advancement after Call works.
            Assert.Pass();
        }

        // ============================================================================================================
        // C. Arity mismatch
        // ============================================================================================================

        [Test]
        public void Call_ArityMismatch_RaisesTypeError_WithFunctionName()
        {
            Chunk chunk = Compile("def myFunc(a):\n    return a\nmyFunc(1, 2)");

            ChowTypeErrorException ex = Assert.Throws<ChowTypeErrorException>(() => Execute(chunk));

            Assert.That(ex.Message, Does.Contain("myFunc"));
        }

        [Test]
        public void Call_TooFewArgs_RaisesTypeError()
        {
            Chunk chunk = Compile("def f(a, b):\n    return a\nf(1)");

            Assert.Throws<ChowTypeErrorException>(() => Execute(chunk));
        }

        // ============================================================================================================
        // D. Interop dispatch still works through the Call op (regression)
        // ============================================================================================================

        [Test]
        public void Call_OnInteropDelegate_StillExecutes()
        {
            ChowModule module = new ChowModule();
            int captured = 0;
            module["bump"] = new Chow.Interpreter.Values.ChowDynamic((System.Action)(() => { captured = 1; }));

            module.Execute("bump()");

            Assert.That(captured, Is.EqualTo(1));
        }

        // ============================================================================================================
        // E. Deep recursion does not blow the C# stack (proves iterative dispatch)
        // ============================================================================================================

        [Test]
        public void DeepRecursion_200Levels_DoesNotStackOverflow()
        {
            string source =
                "def deep(n):\n" +
                "    if n == 0:\n" +
                "        return 0\n" +
                "    return deep(n - 1) + 1\n" +
                "result = deep(200)";

            ModuleScope scope = new ModuleScope();
            ExecuteSource(source, scope);

            Assert.That(scope.GetVariableValue("result").IntegerValue, Is.EqualTo(200));
        }

        // ============================================================================================================
        // F. ReturnValue properly pops the frame so module-level resumes
        // ============================================================================================================

        [Test]
        public void Return_PopsFrame_ModuleResumesAfterCall()
        {
            // The post-call statement `x = 99` must execute; verify the module scope has `x = 99`.
            ModuleScope scope = new ModuleScope();
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
            ModuleScope scope = new ModuleScope();
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
