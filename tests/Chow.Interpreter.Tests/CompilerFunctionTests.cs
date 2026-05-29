using Chow.Interpreter.Bytecode;
using Chow.Interpreter.DataTypes;
namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class CompilerFunctionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static Chunk Compile(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var parser = new Parser(tokens);
            var root = parser.BuildTree();
            var compiler = new Compiler(root);
            return compiler.CompileRoot();
        }

        static List<Instruction> Instructions(Chunk chunk)
        {
            var result = new List<Instruction>();

            for (var i = 0; i < chunk.InstructionCount; i++)
            {
                result.Add(chunk[i]);
            }

            return result;
        }

        static ClosureTemplate FindFirstTemplate(Chunk chunk)
        {
            for (var i = 0; i < chunk.InstructionCount; i++)
            {
                var op = chunk[i];

                if (op.Code != OperationCode.PushConstant)
                {
                    continue;
                }

                var constant = chunk.ReadConstant(op.Operand);

                if (constant.IsOfType<ClosureTemplate>())
                {
                    return constant.AsType<ClosureTemplate>();
                }
            }

            return null;
        }

        // ============================================================================================================
        // A. def emits ClosureTemplate -> PushNewClosureFromTemplate -> PopAndAssignToVariable
        // ============================================================================================================

        [Test]
        public void Def_EmitsPushTemplate_MakeClosure_Assign_InOrder()
        {
            var chunk = Compile("def f():\n    return 1");

            var ops = Instructions(chunk);

            // Expect last three of module chunk: PushConstant(template), PushNewClosureFromTemplate, PopAndAssignToVariable(f)
            Assert.That(ops.Count, Is.GreaterThanOrEqualTo(3));
            var n = ops.Count;

            Assert.Multiple(() =>
            {
                Assert.That(ops[n - 3].Code, Is.EqualTo(OperationCode.PushConstant));
                Assert.That(ops[n - 2].Code, Is.EqualTo(OperationCode.PushNewClosureFromTemplate));
                Assert.That(ops[n - 1].Code, Is.EqualTo(OperationCode.PopAndAssignToVariable));
            });
        }

        [Test]
        public void Def_TemplateConstant_HasFunctionMetadata()
        {
            var chunk = Compile("def myFunc(a, b, c):\n    return a");

            var template = FindFirstTemplate(chunk);

            Assert.That(template, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(template.Name, Is.EqualTo("myFunc"));
                Assert.That(template.ParamCount, Is.EqualTo(3));
            });
        }

        [Test]
        public void Def_AssignOperand_BindsToFunctionName()
        {
            var chunk = Compile("def myFunc():\n    return 1");

            var ops = Instructions(chunk);
            var assignOp = ops[ops.Count - 1];

            Assert.That(chunk.ReadVariableName(assignOp.Operand), Is.EqualTo("myFunc"));
        }

        // ============================================================================================================
        // B. Function body has implicit None + PushReturnValue tail
        // ============================================================================================================

        [Test]
        public void FuncBody_NoExplicitReturn_EndsWithImplicitNoneReturn()
        {
            var chunk = Compile("def f():\n    x = 1");
            var body = FindFirstTemplate(chunk).Chunk;
            var ops = Instructions(body);

            var n = ops.Count;
            Assert.That(n, Is.GreaterThanOrEqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(ops[n - 2].Code, Is.EqualTo(OperationCode.PushConstant));
                Assert.That(ops[n - 1].Code, Is.EqualTo(OperationCode.PushReturnValue));

                var tailConst = body.ReadConstant(ops[n - 2].Operand);
                Assert.That(tailConst.DataType, Is.EqualTo(DataType.None));
            });
        }

        [Test]
        public void FuncBody_WithExplicitReturn_StillHasImplicitTail()
        {
            var chunk = Compile("def f():\n    return 1");
            var body = FindFirstTemplate(chunk).Chunk;
            var ops = Instructions(body);

            // Last two ops are always the implicit None + PushReturnValue tail.
            var n = ops.Count;
            Assert.Multiple(() =>
            {
                Assert.That(ops[n - 1].Code, Is.EqualTo(OperationCode.PushReturnValue));
                Assert.That(ops[n - 2].Code, Is.EqualTo(OperationCode.PushConstant));

                var tailConst = body.ReadConstant(ops[n - 2].Operand);
                Assert.That(tailConst.DataType, Is.EqualTo(DataType.None));
            });
        }

        // ============================================================================================================
        // C. Bare return emits PushConstant(None) + PushReturnValue
        // ============================================================================================================

        [Test]
        public void BareReturn_EmitsPushNoneThenReturnValue()
        {
            var chunk = Compile("def f():\n    return");
            var body = FindFirstTemplate(chunk).Chunk;
            var ops = Instructions(body);

            // Body has the block-wrapped bare return plus the implicit-None tail; both produce
            // a PushConstant(None) + PushReturnValue pair. Two ReturnValues confirms the bare return
            // is not optimized away.
            var returnValueCount = 0;
            var pushNoneBeforeReturnCount = 0;

            for (var i = 0; i < ops.Count; i++)
            {
                if (ops[i].Code != OperationCode.PushReturnValue)
                {
                    continue;
                }

                returnValueCount++;

                Assert.That(i, Is.GreaterThan(0), "PushReturnValue cannot be the first op");
                var prev = ops[i - 1];

                if (prev.Code == OperationCode.PushConstant && body.ReadConstant(prev.Operand).DataType == DataType.None)
                {
                    pushNoneBeforeReturnCount++;
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(returnValueCount, Is.EqualTo(2));
                Assert.That(pushNoneBeforeReturnCount, Is.EqualTo(2));
            });
        }

        // ============================================================================================================
        // D. Params bound in reverse order at start of body
        // ============================================================================================================

        [Test]
        public void Params_BoundInReverseOrder_AtStartOfBody()
        {
            var chunk = Compile("def f(a, b, c):\n    return a");
            var body = FindFirstTemplate(chunk).Chunk;
            var ops = Instructions(body);

            Assert.That(ops.Count, Is.GreaterThanOrEqualTo(3));
            Assert.Multiple(() =>
            {
                // Reverse order: c then b then a
                Assert.That(ops[0].Code, Is.EqualTo(OperationCode.PopAndAssignToVariable));
                Assert.That(body.ReadVariableName(ops[0].Operand), Is.EqualTo("c"));

                Assert.That(ops[1].Code, Is.EqualTo(OperationCode.PopAndAssignToVariable));
                Assert.That(body.ReadVariableName(ops[1].Operand), Is.EqualTo("b"));

                Assert.That(ops[2].Code, Is.EqualTo(OperationCode.PopAndAssignToVariable));
                Assert.That(body.ReadVariableName(ops[2].Operand), Is.EqualTo("a"));
            });
        }

        // ============================================================================================================
        // E. CallFunction site emit
        // ============================================================================================================

        [Test]
        public void Call_EmitsPushNameThenArgsThenCallWithArgCount()
        {
            var chunk = Compile("def add(a, b):\n    return a + b\nadd(3, 4)");

            // The call site is at the end of the module chunk; find it.
            var ops = Instructions(chunk);

            // Scan for the CallFunction op; verify operand and preceding ops.
            var callIdx = -1;

            for (var i = 0; i < ops.Count; i++)
            {
                if (ops[i].Code == OperationCode.CallFunction)
                {
                    callIdx = i;
                    break;
                }
            }

            Assert.That(callIdx, Is.GreaterThan(0));
            Assert.Multiple(() =>
            {
                Assert.That(ops[callIdx].Operand, Is.EqualTo(2));

                // PushVariableValue(add) sits before the two arg evaluations.
                Assert.That(ops[callIdx - 3].Code, Is.EqualTo(OperationCode.PushVariableValue));
                Assert.That(chunk.ReadVariableName(ops[callIdx - 3].Operand), Is.EqualTo("add"));
            });
        }

        [Test]
        public void CallNoArgs_EmitsCallWithZeroOperand()
        {
            var chunk = Compile("def f():\n    return 1\nf()");

            var ops = Instructions(chunk);
            var callIdx = -1;

            for (var i = 0; i < ops.Count; i++)
            {
                if (ops[i].Code == OperationCode.CallFunction)
                {
                    callIdx = i;
                    break;
                }
            }

            Assert.That(callIdx, Is.GreaterThan(-1));
            Assert.That(ops[callIdx].Operand, Is.EqualTo(0));
        }

        // ============================================================================================================
        // F. Nested defs produce independent chunks
        // ============================================================================================================

        [Test]
        public void NestedDef_OuterChunk_ContainsInnerMakeClosure()
        {
            var source =
                "def outer():\n" +
                "    def inner():\n" +
                "        return 1\n" +
                "    return inner()";

            var module = Compile(source);
            var outerTemplate = FindFirstTemplate(module);
            var outerBody = outerTemplate.Chunk;

            // Inner def must appear inside outer's body as a ClosureTemplate constant.
            var innerTemplate = FindFirstTemplate(outerBody);

            Assert.That(innerTemplate, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(innerTemplate.Name, Is.EqualTo("inner"));
                Assert.That(outerTemplate.Chunk, Is.Not.SameAs(innerTemplate.Chunk));
                Assert.That(outerTemplate.Chunk, Is.Not.SameAs(module));
            });
        }
    }
}
