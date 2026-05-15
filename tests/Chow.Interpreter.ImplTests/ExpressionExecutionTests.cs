using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class ExpressionExecutionTests
    {
        static TaggedUnion ExecuteStackTop(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var parser = new Parser(tokens);
            var root = parser.BuildTree();
            var compiler = new Compiler(root);
            var chunk = compiler.CompileRoot();
            var vm = new VirtualMachine(chunk, new GlobalScope());

            vm.EvaluateChunk();

            return vm.ValStackTop;
        }

        static ChowValue ExecuteResult(string expression)
        {
            var module = new ChowModule();
            module.Execute("__result = " + expression);
            return module.GetGlobal("__result");
        }

        [Test]
        public void ExpressionStatement_DiscardsStackResult()
        {
            var result = ExecuteStackTop("1 + 2");

            Assert.That(result.Tag, Is.EqualTo(Tag.None));
        }

        [Test]
        public void And_LeftFalsy_ReturnsLeftOperand()
        {
            var result = ExecuteResult("0 and 5");

            Assert.That(result.AsType<long>(), Is.EqualTo(0));
        }

        [Test]
        public void And_LeftTruthy_ReturnsRightOperand()
        {
            var result = ExecuteResult("2 and 5");

            Assert.That(result.AsType<long>(), Is.EqualTo(5));
        }

        [Test]
        public void Or_LeftTruthy_ReturnsLeftOperand()
        {
            var result = ExecuteResult("2 or 5");

            Assert.That(result.AsType<long>(), Is.EqualTo(2));
        }

        [Test]
        public void Or_LeftFalsy_ReturnsRightOperand()
        {
            var result = ExecuteResult("0 or 5");

            Assert.That(result.AsType<long>(), Is.EqualTo(5));
        }

        [Test]
        public void And_LeftFalsy_ShortCircuitsRightOperand()
        {
            var module = new ChowModule();
            module["fail"] = (Func<ChowValue>)(() => throw new InvalidOperationException("should not be called"));

            module.Execute("__result = 0 and fail()");

            Assert.That(module.GetGlobal("__result").AsType<long>(), Is.EqualTo(0));
        }

        [Test]
        public void Or_LeftTruthy_ShortCircuitsRightOperand()
        {
            var module = new ChowModule();
            module["fail"] = (Func<ChowValue>)(() => throw new InvalidOperationException("should not be called"));

            module.Execute("__result = 1 or fail()");

            Assert.That(module.GetGlobal("__result").AsType<long>(), Is.EqualTo(1));
        }

        [Test]
        public void ChainedComparison_EvaluatesAsAndLoweredComparisons()
        {
            var result = ExecuteResult("1 < 2 < 3");

            Assert.That(result.AsType<bool>(), Is.True);
        }

        [Test]
        public void ChainedComparison_EvaluatesMiddleExpressionPerComparison()
        {
            var module = new ChowModule();
            var calls = 0;
            module["middle"] = (Func<ChowValue>)(() =>
            {
                calls++;
                return new ChowInt(2);
            });

            module.Execute("__result = 1 < middle() < 3");

            Assert.Multiple(() =>
            {
                Assert.That(module.GetGlobal("__result").AsType<bool>(), Is.True);
                Assert.That(calls, Is.EqualTo(2));
            });
        }
    }
}
