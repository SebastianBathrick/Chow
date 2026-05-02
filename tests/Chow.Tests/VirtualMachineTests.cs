using Chow.Bytecode;
using Chow.Values;

namespace Chow.Tests
{
    [TestFixture]
    public class VirtualMachineTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        const int LINE = 1;

        static Chunk BuildChunk(Action<Chunk> build)
        {
            var chunk = new Chunk();
            build(chunk);
            return chunk;
        }

        static TaggedUnion Execute(Chunk chunk) => new VirtualMachine(chunk).ExecuteChunk();

        static void PushIntegerConstant(Chunk chunk, int value)
        {
            int index = chunk.AddConstant(new TaggedUnion(value));
            chunk.PushOperation(OperationCode.PushConstant, LINE, index);
        }

        static void PushFloatConstant(Chunk chunk, float value)
        {
            int index = chunk.AddConstant(new TaggedUnion(value));
            chunk.PushOperation(OperationCode.PushConstant, LINE, index);
        }

        static void AssertIntegerResult(TaggedUnion result, int expected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.IsInteger, Is.True);
                Assert.That(result.IntegerValue, Is.EqualTo(expected));
            });
        }

        static void AssertFloatResult(TaggedUnion result, float expected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.IsFloat, Is.True);
                Assert.That(result.FloatValue, Is.EqualTo(expected));
            });
        }

        // ============================================================================================================
        // A. Construction
        // ============================================================================================================

        [Test]
        public void Constructor_NullChunk_ThrowsArgumentNullException()
        {
            Assert.That(() => new VirtualMachine(null!), Throws.TypeOf<ArgumentNullException>());
        }

        // ============================================================================================================
        // B. Single-operation execution
        // ============================================================================================================

        [Test]
        public void ExecuteChunk_PushConstantOnly_ReturnsPushedConstant()
        {
            var chunk = BuildChunk(c => PushIntegerConstant(c, 42));

            TaggedUnion result = Execute(chunk);

            AssertIntegerResult(result, 42);
        }

        // ============================================================================================================
        // C. Binary arithmetic — same-type operands
        // ============================================================================================================

        [TestCase((int)OperationCode.Add, 5, 3, 8)]
        [TestCase((int)OperationCode.Subtract, 5, 3, 2)]
        [TestCase((int)OperationCode.Multiply, 4, 3, 12)]
        [TestCase((int)OperationCode.Modulus, 7, 2, 1)]
        [TestCase((int)OperationCode.Exponentiate, 2, 3, 8)]
        [TestCase((int)OperationCode.FloorDivide, 7, 2, 3)]
        public void ExecuteChunk_IntegerBinaryOperation_ReturnsIntegerResult(
            int opCode, int left, int right, int expected)
        {
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, left);
                PushIntegerConstant(c, right);
                c.PushOperation((OperationCode)opCode, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertIntegerResult(result, expected);
        }

        [TestCase((int)OperationCode.Add, 1.5f, 2.5f, 4.0f)]
        [TestCase((int)OperationCode.Subtract, 5.0f, 1.5f, 3.5f)]
        [TestCase((int)OperationCode.Multiply, 2.0f, 1.5f, 3.0f)]
        [TestCase((int)OperationCode.Divide, 5.0f, 2.0f, 2.5f)]
        public void ExecuteChunk_FloatBinaryOperation_ReturnsFloatResult(
            int opCode, float left, float right, float expected)
        {
            var chunk = BuildChunk(c =>
            {
                PushFloatConstant(c, left);
                PushFloatConstant(c, right);
                c.PushOperation((OperationCode)opCode, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, expected);
        }

        // ============================================================================================================
        // D. Type coercion (mixed integer / float operands)
        // ============================================================================================================

        [Test]
        public void ExecuteChunk_IntegerLeftFloatRightSubtract_ReturnsCoercedFloatResult()
        {
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, 5);
                PushFloatConstant(c, 1.5f);
                c.PushOperation(OperationCode.Subtract, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 3.5f);
        }

        [Test]
        public void ExecuteChunk_FloatLeftIntegerRightSubtract_ReturnsCoercedFloatResult()
        {
            var chunk = BuildChunk(c =>
            {
                PushFloatConstant(c, 5.5f);
                PushIntegerConstant(c, 2);
                c.PushOperation(OperationCode.Subtract, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 3.5f);
        }

        [Test]
        public void ExecuteChunk_IntegerDivision_ReturnsFloatResult()
        {
            // Python semantics: `/` always yields a float, even for int / int.
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, 10);
                PushIntegerConstant(c, 4);
                c.PushOperation(OperationCode.Divide, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 2.5f);
        }

        [Test]
        public void ExecuteChunk_ModulusNegativeDividend_MatchesPythonSign()
        {
            // Python: -7 % 2 == 1 (sign of divisor), not -1 (sign of dividend as in C#).
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, -7);
                PushIntegerConstant(c, 2);
                c.PushOperation(OperationCode.Modulus, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertIntegerResult(result, 1);
        }

        [Test]
        public void ExecuteChunk_FloorDivideNegativeDividend_FloorsTowardNegativeInfinity()
        {
            // Python: -7 // 2 == -4 (floor), not -3 (truncate as in C#).
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, -7);
                PushIntegerConstant(c, 2);
                c.PushOperation(OperationCode.FloorDivide, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertIntegerResult(result, -4);
        }

        [Test]
        public void ExecuteChunk_ExponentNegativeIntegerExponent_ReturnsFloatResult()
        {
            // Python: 2 ** -1 == 0.5 (float), not 0 (truncated int).
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, 2);
                PushIntegerConstant(c, -1);
                c.PushOperation(OperationCode.Exponentiate, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 0.5f);
        }

        [Test]
        public void ExecuteChunk_ExponentFloatBase_ReturnsFloatResult()
        {
            var chunk = BuildChunk(c =>
            {
                PushFloatConstant(c, 2.0f);
                PushIntegerConstant(c, 3);
                c.PushOperation(OperationCode.Exponentiate, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 8.0f);
        }

        [Test]
        public void ExecuteChunk_FloorDivideFloatOperand_ReturnsFloatResult()
        {
            var chunk = BuildChunk(c =>
            {
                PushFloatConstant(c, 7.0f);
                PushIntegerConstant(c, 2);
                c.PushOperation(OperationCode.FloorDivide, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 3.0f);
        }

        // ============================================================================================================
        // E. Negate
        // ============================================================================================================

        [Test]
        public void ExecuteChunk_NegateInteger_ReturnsNegatedInteger()
        {
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, 7);
                c.PushOperation(OperationCode.Negate, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertIntegerResult(result, -7);
        }

        [Test]
        public void ExecuteChunk_NegateFloat_ReturnsNegatedFloat()
        {
            var chunk = BuildChunk(c =>
            {
                PushFloatConstant(c, 2.5f);
                c.PushOperation(OperationCode.Negate, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, -2.5f);
        }

        // ============================================================================================================
        // F. Compound programs (stack management across multiple operations)
        // ============================================================================================================

        [Test]
        public void ExecuteChunk_LeftAssociativeSubtraction_ReturnsCorrectResult()
        {
            // Bytecode for ((10 - 4) - 2) -> 4. Reversed pop order would yield 8.
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, 10);
                PushIntegerConstant(c, 4);
                c.PushOperation(OperationCode.Subtract, LINE);
                PushIntegerConstant(c, 2);
                c.PushOperation(OperationCode.Subtract, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertIntegerResult(result, 4);
        }

        [Test]
        public void ExecuteChunk_NestedBinaryOperations_ReturnsCorrectResult()
        {
            // Bytecode for ((10 - 4) / 2) -> 3.0. Divide always yields float per Python semantics.
            var chunk = BuildChunk(c =>
            {
                PushIntegerConstant(c, 10);
                PushIntegerConstant(c, 4);
                c.PushOperation(OperationCode.Subtract, LINE);
                PushIntegerConstant(c, 2);
                c.PushOperation(OperationCode.Divide, LINE);
            });

            TaggedUnion result = Execute(chunk);

            AssertFloatResult(result, 3.0f);
        }
    }
}
