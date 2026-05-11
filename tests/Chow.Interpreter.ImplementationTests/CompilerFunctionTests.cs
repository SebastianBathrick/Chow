using Chow.Interpreter.Compilation;
using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values.Internal;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class CompilerFunctionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static Chunk Compile(string source)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.ScanTokens();
            Parser parser = new Parser(tokens);
            Node root = parser.BuildTree();
            Compiler compiler = new Compiler(root);
            return compiler.CompileRoot();
        }

        static List<Instruction> Instructions(Chunk chunk)
        {
            List<Instruction> result = new List<Instruction>();
            for (int i = 0; i < chunk.InstructionCount; i++)
            {
                result.Add(chunk[i]);
            }
            return result;
        }

        static ClosureTemplate FindFirstTemplate(Chunk chunk)
        {
            for (int i = 0; i < chunk.InstructionCount; i++)
            {
                Instruction op = chunk[i];
                if (op.Code != OperationCode.PushConstant)
                {
                    continue;
                }
                TaggedUnion constant = chunk.ReadConstant(op.Operand);
                if (constant.Tag == Tag.Object && constant.ObjectValue is ClosureTemplate template)
                {
                    return template;
                }
            }
            return null;
        }

        // ============================================================================================================
        // A. def emits ClosureTemplate -> MakeClosure -> AssignOrDeclareVariable
        // ============================================================================================================

        [Test]
        public void Def_EmitsPushTemplate_MakeClosure_Assign_InOrder()
        {
            Chunk chunk = Compile("def f():\n    return 1");

            List<Instruction> ops = Instructions(chunk);

            // Expect last three of module chunk: PushConstant(template), MakeClosure, AssignOrDeclareVariable(f)
            Assert.That(ops.Count, Is.GreaterThanOrEqualTo(3));
            int n = ops.Count;

            Assert.Multiple(() =>
            {
                Assert.That(ops[n - 3].Code, Is.EqualTo(OperationCode.PushConstant));
                Assert.That(ops[n - 2].Code, Is.EqualTo(OperationCode.MakeClosure));
                Assert.That(ops[n - 1].Code, Is.EqualTo(OperationCode.AssignOrDeclareVariable));
            });
        }

        [Test]
        public void Def_TemplateConstant_HasFunctionMetadata()
        {
            Chunk chunk = Compile("def myFunc(a, b, c):\n    return a");

            ClosureTemplate template = FindFirstTemplate(chunk);

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
            Chunk chunk = Compile("def myFunc():\n    return 1");

            List<Instruction> ops = Instructions(chunk);
            Instruction assignOp = ops[ops.Count - 1];

            Assert.That(chunk.ReadVariableName(assignOp.Operand), Is.EqualTo("myFunc"));
        }

        // ============================================================================================================
        // B. Function body has implicit None + ReturnValue tail
        // ============================================================================================================

        [Test]
        public void FuncBody_NoExplicitReturn_EndsWithImplicitNoneReturn()
        {
            Chunk chunk = Compile("def f():\n    x = 1");
            Chunk body = FindFirstTemplate(chunk).Chunk;
            List<Instruction> ops = Instructions(body);

            int n = ops.Count;
            Assert.That(n, Is.GreaterThanOrEqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(ops[n - 2].Code, Is.EqualTo(OperationCode.PushConstant));
                Assert.That(ops[n - 1].Code, Is.EqualTo(OperationCode.ReturnValue));

                TaggedUnion tailConst = body.ReadConstant(ops[n - 2].Operand);
                Assert.That(tailConst.Tag, Is.EqualTo(Tag.None));
            });
        }

        [Test]
        public void FuncBody_WithExplicitReturn_StillHasImplicitTail()
        {
            Chunk chunk = Compile("def f():\n    return 1");
            Chunk body = FindFirstTemplate(chunk).Chunk;
            List<Instruction> ops = Instructions(body);

            // Last two ops are always the implicit None + ReturnValue tail.
            int n = ops.Count;
            Assert.Multiple(() =>
            {
                Assert.That(ops[n - 1].Code, Is.EqualTo(OperationCode.ReturnValue));
                Assert.That(ops[n - 2].Code, Is.EqualTo(OperationCode.PushConstant));

                TaggedUnion tailConst = body.ReadConstant(ops[n - 2].Operand);
                Assert.That(tailConst.Tag, Is.EqualTo(Tag.None));
            });
        }

        // ============================================================================================================
        // C. Bare return emits PushConstant(None) + ReturnValue
        // ============================================================================================================

        [Test]
        public void BareReturn_EmitsPushNoneThenReturnValue()
        {
            Chunk chunk = Compile("def f():\n    return");
            Chunk body = FindFirstTemplate(chunk).Chunk;
            List<Instruction> ops = Instructions(body);

            // Body has the block-wrapped bare return plus the implicit-None tail; both produce
            // a PushConstant(None) + ReturnValue pair. Two ReturnValues confirms the bare return
            // is not optimized away.
            int returnValueCount = 0;
            int pushNoneBeforeReturnCount = 0;
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].Code != OperationCode.ReturnValue)
                {
                    continue;
                }
                returnValueCount++;

                Assert.That(i, Is.GreaterThan(0), "ReturnValue cannot be the first op");
                Instruction prev = ops[i - 1];
                if (prev.Code == OperationCode.PushConstant && body.ReadConstant(prev.Operand).Tag == Tag.None)
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
            Chunk chunk = Compile("def f(a, b, c):\n    return a");
            Chunk body = FindFirstTemplate(chunk).Chunk;
            List<Instruction> ops = Instructions(body);

            Assert.That(ops.Count, Is.GreaterThanOrEqualTo(3));
            Assert.Multiple(() =>
            {
                // Reverse order: c then b then a
                Assert.That(ops[0].Code, Is.EqualTo(OperationCode.AssignOrDeclareVariable));
                Assert.That(body.ReadVariableName(ops[0].Operand), Is.EqualTo("c"));

                Assert.That(ops[1].Code, Is.EqualTo(OperationCode.AssignOrDeclareVariable));
                Assert.That(body.ReadVariableName(ops[1].Operand), Is.EqualTo("b"));

                Assert.That(ops[2].Code, Is.EqualTo(OperationCode.AssignOrDeclareVariable));
                Assert.That(body.ReadVariableName(ops[2].Operand), Is.EqualTo("a"));
            });
        }

        // ============================================================================================================
        // E. Call site emit
        // ============================================================================================================

        [Test]
        public void Call_EmitsPushNameThenArgsThenCallWithArgCount()
        {
            Chunk chunk = Compile("def add(a, b):\n    return a + b\nadd(3, 4)");

            // The call site is at the end of the module chunk; find it.
            List<Instruction> ops = Instructions(chunk);

            // Scan for the Call op; verify operand and preceding ops.
            int callIdx = -1;
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].Code == OperationCode.Call)
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
            Chunk chunk = Compile("def f():\n    return 1\nf()");

            List<Instruction> ops = Instructions(chunk);
            int callIdx = -1;
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].Code == OperationCode.Call)
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
            string source =
                "def outer():\n" +
                "    def inner():\n" +
                "        return 1\n" +
                "    return inner()";

            Chunk module = Compile(source);
            ClosureTemplate outerTemplate = FindFirstTemplate(module);
            Chunk outerBody = outerTemplate.Chunk;

            // Inner def must appear inside outer's body as a ClosureTemplate constant.
            ClosureTemplate innerTemplate = FindFirstTemplate(outerBody);

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
