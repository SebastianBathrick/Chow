using Chow.Interpreter.Exceptions;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    internal class DataTypeConversionMapTests
    {
        [TestCase(ExpressionOperator.Add, DataType.Bool, DataType.Bool, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Add, DataType.Bool, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Add, DataType.Int, DataType.Bool, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Add, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Add, DataType.Int, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Add, DataType.Float, DataType.Int, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Add, DataType.Float, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Add, DataType.Bool, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Subtract, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Subtract, DataType.Float, DataType.Int, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Multiply, DataType.Bool, DataType.Bool, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Multiply, DataType.Float, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Modulus, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Modulus, DataType.Int, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.FloorDivide, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.FloorDivide, DataType.Float, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Exponentiate, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Exponentiate, DataType.Int, DataType.Float, ConversionCase.PromoteToFloat)]
        public void GetLeftRightConversionCase_ArithmeticCombos_ReturnsExpectedPromotion(
            ExpressionOperator op, DataType left, DataType right, ConversionCase expected)
        {
            Assert.That(DataTypeConversionMap.GetLeftRightConversionCase(op, left, right), Is.EqualTo(expected));
        }

        [TestCase(DataType.Bool, DataType.Bool)]
        [TestCase(DataType.Int, DataType.Int)]
        [TestCase(DataType.Int, DataType.Float)]
        [TestCase(DataType.Float, DataType.Int)]
        [TestCase(DataType.Float, DataType.Float)]
        [TestCase(DataType.Bool, DataType.Float)]
        public void GetLeftRightConversionCase_Divide_AlwaysPromotesToFloat(DataType left, DataType right)
        {
            Assert.That(
                DataTypeConversionMap.GetLeftRightConversionCase(ExpressionOperator.Divide, left, right),
                Is.EqualTo(ConversionCase.PromoteToFloat));
        }

        [TestCase(ExpressionOperator.Less, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Greater, DataType.Int, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.LessEqual, DataType.Bool, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.GreaterEqual, DataType.Float, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Less, DataType.Str, DataType.Str, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.GreaterEqual, DataType.Str, DataType.Str, ConversionCase.NoConversion)]
        public void GetLeftRightConversionCase_Comparisons_ReturnsExpected(
            ExpressionOperator op, DataType left, DataType right, ConversionCase expected)
        {
            Assert.That(DataTypeConversionMap.GetLeftRightConversionCase(op, left, right), Is.EqualTo(expected));
        }

        [TestCase(ExpressionOperator.Add, DataType.Str, DataType.Str)]
        [TestCase(ExpressionOperator.Add, DataType.List, DataType.List)]
        [TestCase(ExpressionOperator.Multiply, DataType.List, DataType.Int)]
        [TestCase(ExpressionOperator.Multiply, DataType.Int, DataType.List)]
        [TestCase(ExpressionOperator.Multiply, DataType.Str, DataType.Int)]
        [TestCase(ExpressionOperator.Multiply, DataType.Int, DataType.Str)]
        [TestCase(ExpressionOperator.BinaryOr, DataType.Dict, DataType.Dict)]
        public void GetLeftRightConversionCase_ContainerCarveOuts_ReturnsNoConversion(
            ExpressionOperator op, DataType left, DataType right)
        {
            Assert.That(
                DataTypeConversionMap.GetLeftRightConversionCase(op, left, right),
                Is.EqualTo(ConversionCase.NoConversion));
        }

        [TestCase(ExpressionOperator.And, DataType.Bool, DataType.Int)]
        [TestCase(ExpressionOperator.And, DataType.List, DataType.Dict)]
        [TestCase(ExpressionOperator.Or, DataType.Str, DataType.None)]
        [TestCase(ExpressionOperator.Or, DataType.Float, DataType.Range)]
        public void GetLeftRightConversionCase_AndOr_AnyOperands_Throws(
            ExpressionOperator op, DataType left, DataType right)
        {
            // And/Or short-circuit at compile time (Compiler.CompileShortCircuit emits jump opcodes),
            // so they never reach this lookup. If queried defensively, throwing TypeException is the
            // documented response.
            Assert.That(
                () => DataTypeConversionMap.GetLeftRightConversionCase(op, left, right),
                Throws.TypeOf<TypeException>());
        }

        [TestCase(ExpressionOperator.Equal, DataType.Int, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Equal, DataType.Int, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Equal, DataType.Int, DataType.Str, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Equal, DataType.List, DataType.List, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Equal, DataType.None, DataType.None, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.NotEqual, DataType.List, DataType.None, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.NotEqual, DataType.Dict, DataType.Dict, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.NotEqual, DataType.Float, DataType.Bool, ConversionCase.PromoteToFloat)]
        public void GetLeftRightConversionCase_Equality_ReturnsExpected(
            ExpressionOperator op, DataType left, DataType right, ConversionCase expected)
        {
            Assert.That(DataTypeConversionMap.GetLeftRightConversionCase(op, left, right), Is.EqualTo(expected));
        }

        [TestCase(ExpressionOperator.In, DataType.Int, DataType.List)]
        [TestCase(ExpressionOperator.In, DataType.Str, DataType.Dict)]
        [TestCase(ExpressionOperator.In, DataType.Int, DataType.Range)]
        [TestCase(ExpressionOperator.In, DataType.Str, DataType.Str)]
        [TestCase(ExpressionOperator.NotIn, DataType.None, DataType.List)]
        [TestCase(ExpressionOperator.NotIn, DataType.Float, DataType.Dict)]
        public void GetLeftRightConversionCase_Membership_ReturnsNoConversion(
            ExpressionOperator op, DataType left, DataType right)
        {
            Assert.That(
                DataTypeConversionMap.GetLeftRightConversionCase(op, left, right),
                Is.EqualTo(ConversionCase.NoConversion));
        }

        [TestCase(ExpressionOperator.Add, DataType.Int, DataType.List)]
        [TestCase(ExpressionOperator.Add, DataType.Str, DataType.Int)]
        [TestCase(ExpressionOperator.Less, DataType.Int, DataType.Str)]
        [TestCase(ExpressionOperator.Less, DataType.List, DataType.List)]
        [TestCase(ExpressionOperator.Subtract, DataType.Str, DataType.Str)]
        [TestCase(ExpressionOperator.Multiply, DataType.Str, DataType.Str)]
        [TestCase(ExpressionOperator.BinaryOr, DataType.Int, DataType.Int)]
        [TestCase(ExpressionOperator.In, DataType.Int, DataType.Int)]
        [TestCase(ExpressionOperator.In, DataType.Str, DataType.Int)]
        public void GetLeftRightConversionCase_UnsupportedCombo_ThrowsTypeException(
            ExpressionOperator op, DataType left, DataType right)
        {
            Assert.That(
                () => DataTypeConversionMap.GetLeftRightConversionCase(op, left, right),
                Throws.TypeOf<TypeException>());
        }

        [TestCase(ExpressionOperator.Negate, DataType.Bool, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Negate, DataType.Int, ConversionCase.PromoteToInt)]
        [TestCase(ExpressionOperator.Negate, DataType.Float, ConversionCase.PromoteToFloat)]
        [TestCase(ExpressionOperator.Not, DataType.None, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Bool, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Int, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Float, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Str, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.List, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Dict, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Range, ConversionCase.NoConversion)]
        [TestCase(ExpressionOperator.Not, DataType.Object, ConversionCase.NoConversion)]
        public void GetOperandConversionCase_UnaryOps_ReturnsExpected(
            ExpressionOperator op, DataType operand, ConversionCase expected)
        {
            Assert.That(DataTypeConversionMap.GetOperandConversionCase(op, operand), Is.EqualTo(expected));
        }

        [TestCase(ExpressionOperator.Negate, DataType.Str)]
        [TestCase(ExpressionOperator.Negate, DataType.List)]
        [TestCase(ExpressionOperator.Negate, DataType.Dict)]
        [TestCase(ExpressionOperator.Negate, DataType.None)]
        [TestCase(ExpressionOperator.Negate, DataType.Range)]
        [TestCase(ExpressionOperator.Negate, DataType.Object)]
        public void GetOperandConversionCase_UnsupportedOperand_ThrowsTypeException(
            ExpressionOperator op, DataType operand)
        {
            Assert.That(
                () => DataTypeConversionMap.GetOperandConversionCase(op, operand),
                Throws.TypeOf<TypeException>());
        }

        [Test]
        public void GetLeftRightConversionCase_UnsupportedCombo_MessageMentionsUnsupportedOperandType()
        {
            Assert.That(
                () => DataTypeConversionMap.GetLeftRightConversionCase(
                    ExpressionOperator.Add, DataType.Int, DataType.List),
                Throws.TypeOf<TypeException>().With.Message.Contains("unsupported operand type"));
        }
    }
}
