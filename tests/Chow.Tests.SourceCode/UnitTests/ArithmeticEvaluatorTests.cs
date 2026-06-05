using Chow;
using Chow.DataTypes;
using Chow.Expressions;
using Chow.Exceptions;

namespace Chow.Tests.SourceCode.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11 REPL (2026-06-05).
/// </summary>
public class ArithmeticEvaluatorTests
{
    const double Tolerance = 1e-12;

    #region Addition  (Python: +)

    [Test]
    public void Addition_IntInt_ReturnsInt()
    {
        // Python: 2 + 3 -> 5
        AssertResult(ArithmeticEvaluator.EvaluateAddition, L(2), L(3), Tag.Long, 5L);
    }

    [Test]
    public void Addition_FloatInt_ReturnsFloat()
    {
        // Python: 2.0 + 3 -> 5.0
        AssertResult(ArithmeticEvaluator.EvaluateAddition, D(2.0), L(3), Tag.Double, 5.0);
    }

    [Test]
    public void Addition_BoolInt_ReturnsInt()
    {
        // Python: True + 1 -> 2
        AssertResult(ArithmeticEvaluator.EvaluateAddition, B(true), L(1), Tag.Long, 2L);
    }

    [Test]
    public void Addition_IntNone_RaisesTypeError()
    {
        // Python: 2 + None -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.EvaluateAddition, L(2), TaggedUnion.None),
            "+",
            "int",
            "NoneType");
    }

    #endregion

    #region Subtraction  (Python: -)

    [Test]
    public void Subtraction_IntInt_ReturnsInt()
    {
        // Python: 5 - 3 -> 2
        AssertResult(ArithmeticEvaluator.EvaluateSubtraction, L(5), L(3), Tag.Long, 2L);
    }

    [Test]
    public void Subtraction_FloatInt_ReturnsFloat()
    {
        // Python: 2.5 - 1 -> 1.5
        AssertResult(ArithmeticEvaluator.EvaluateSubtraction, D(2.5), L(1), Tag.Double, 1.5);
    }

    #endregion

    #region Multiplication  (Python: *)

    [Test]
    public void Multiplication_IntInt_ReturnsInt()
    {
        // Python: 7 * 8 -> 56
        AssertResult(ArithmeticEvaluator.EvaluateMultiplication, L(7), L(8), Tag.Long, 56L);
    }

    [Test]
    public void Multiplication_FloatInt_ReturnsFloat()
    {
        // Python: 2.0 * 3 -> 6.0
        AssertResult(ArithmeticEvaluator.EvaluateMultiplication, D(2.0), L(3), Tag.Double, 6.0);
    }

    #endregion

    #region Division  (Python: /)

    [Test]
    public void Division_IntInt_ReturnsFloat()
    {
        // Python: 9 / 3 -> 3.0
        AssertResult(ArithmeticEvaluator.EvaluateDivision, L(9), L(3), Tag.Double, 3.0);
    }

    [Test]
    public void Division_IntInt_WithRemainder_ReturnsFloat()
    {
        // Python: 7 / 2 -> 3.5
        AssertResult(ArithmeticEvaluator.EvaluateDivision, L(7), L(2), Tag.Double, 3.5);
    }

    [Test]
    public void Division_ByZero_RaisesZeroDivisionError()
    {
        // Python: 1 / 0 -> ZeroDivisionError
        Assert.Throws<ZeroDivisionException>(
            () => Evaluate(ArithmeticEvaluator.EvaluateDivision, L(1), L(0)));
    }

    [Test]
    public void Division_IntNone_RaisesTypeError()
    {
        // Python: 2 / None -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.EvaluateDivision, L(2), TaggedUnion.None),
            "/",
            "int",
            "NoneType");
    }

    #endregion

    #region Modulus  (Python: %)

    [Test]
    public void Modulus_PositiveOperands_ReturnsInt()
    {
        // Python: 7 % 3 -> 1
        AssertResult(ArithmeticEvaluator.EvaluateModulus, L(7), L(3), Tag.Long, 1L);
    }

    [Test]
    public void Modulus_NegativeDividend_ReturnsInt()
    {
        // Python: -3 % 2 -> 1
        AssertResult(ArithmeticEvaluator.EvaluateModulus, L(-3), L(2), Tag.Long, 1L);
    }

    [Test]
    public void Modulus_NegativeDivisor_ReturnsInt()
    {
        // Python: 3 % -2 -> -1
        AssertResult(ArithmeticEvaluator.EvaluateModulus, L(3), L(-2), Tag.Long, -1L);
    }

    [Test]
    public void Modulus_BothNegative_ReturnsInt()
    {
        // Python: -17 % -4 -> -1
        AssertResult(ArithmeticEvaluator.EvaluateModulus, L(-17), L(-4), Tag.Long, -1L);
    }

    [Test]
    public void Modulus_ByZero_RaisesZeroDivisionError()
    {
        // Python: 7 % 0 -> ZeroDivisionError
        Assert.Throws<ZeroDivisionException>(
            () => Evaluate(ArithmeticEvaluator.EvaluateModulus, L(7), L(0)));
    }

    #endregion

    #region Floor Division  (Python: //)

    [Test]
    public void FloorDivision_PositiveOperands_ReturnsInt()
    {
        // Python: 17 // 4 -> 4
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(17), L(4), Tag.Long, 4L);
    }

    [Test]
    public void FloorDivision_NegativeDividend_ReturnsInt()
    {
        // Python: -17 // 4 -> -5
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(-17), L(4), Tag.Long, -5L);
    }

    [Test]
    public void FloorDivision_NegativeDivisor_ReturnsInt()
    {
        // Python: 17 // -4 -> -5
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(17), L(-4), Tag.Long, -5L);
    }

    [Test]
    public void FloorDivision_BothNegative_ReturnsInt()
    {
        // Python: -17 // -4 -> 4
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(-17), L(-4), Tag.Long, 4L);
    }

    [Test]
    public void FloorDivision_FloatOperands_ReturnsFloat()
    {
        // Python: 7.5 // 2.1 -> 3.0
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, D(7.5), D(2.1), Tag.Double, 3.0);
    }

    #endregion

    #region Exponentiation  (Python: **)

    [Test]
    public void Exponentiation_IntInt_ReturnsInt()
    {
        // Python: 2 ** 3 -> 8
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(2), L(3), Tag.Long, 8L);
    }

    [Test]
    public void Exponentiation_NegativeExponent_ReturnsFloat()
    {
        // Python: 2 ** -3 -> 0.125
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(2), L(-3), Tag.Double, 0.125);
    }

    [Test]
    public void Exponentiation_ZeroToZero_ReturnsInt()
    {
        // Python: 0 ** 0 -> 1
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(0), L(0), Tag.Long, 1L);
    }

    [Test]
    public void Exponentiation_ZeroToNegative_RaisesZeroDivisionError()
    {
        // Python: 0 ** -1 -> ZeroDivisionError
        Assert.Throws<ZeroDivisionException>(
            () => Evaluate(ArithmeticEvaluator.EvaluateExponent, L(0), L(-1)));
    }

    [Test]
    public void Exponentiation_LargeIntExponent_ReturnsExactInt()
    {
        // Python: 10 ** 16 -> 10000000000000000
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(10), L(16), Tag.Long, 10_000_000_000_000_000L);
    }

    [Test]
    public void Exponentiation_FloatBase_ReturnsFloat()
    {
        // Python: 2.0 ** 3 -> 8.0
        AssertResult(ArithmeticEvaluator.EvaluateExponent, D(2.0), L(3), Tag.Double, 8.0);
    }

    [Test]
    public void Exponentiation_NegativeBase_ReturnsInt()
    {
        // Python: (-2) ** 3 -> -8
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(-2), L(3), Tag.Long, -8L);
    }

    [Test]
    public void Exponentiation_NegativeExponentLargerBase_ReturnsFloat()
    {
        // Python: 11 ** -3 -> 0.0007513148009015778
        AssertResult(
            ArithmeticEvaluator.EvaluateExponent,
            L(11),
            L(-3),
            Tag.Double,
            0.0007513148009015778);
    }

    #endregion

    #region Helpers

    static TaggedUnion L(long value) => new TaggedUnion(value);

    static TaggedUnion D(double value) => new TaggedUnion(value);

    static TaggedUnion B(bool value) => new TaggedUnion(value);

    static TaggedUnion Evaluate(EvaluateBinary evaluate, TaggedUnion left, TaggedUnion right)
    {
        var l = left;
        var r = right;
        return evaluate(ref l, ref r);
    }

    static void AssertResult(
        EvaluateBinary evaluate,
        TaggedUnion left,
        TaggedUnion right,
        Tag expectedTag,
        long expectedLong)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.Tag, Is.EqualTo(expectedTag));
        Assert.That(result.AsType<long>(), Is.EqualTo(expectedLong));
    }

    static void AssertResult(
        EvaluateBinary evaluate,
        TaggedUnion left,
        TaggedUnion right,
        Tag expectedTag,
        double expectedDouble)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.Tag, Is.EqualTo(expectedTag));
        Assert.That(result.AsType<double>(), Is.EqualTo(expectedDouble).Within(Tolerance));
    }

    static void AssertTypeError(TestDelegate action, string op, string leftType, string rightType)
    {
        var ex = Assert.Throws<TypeException>(action);

        Assert.That(ex.Message, Does.Contain("TypeError: unsupported operand type(s) for " + op));
        Assert.That(ex.Message, Does.Contain("'" + leftType + "'"));
        Assert.That(ex.Message, Does.Contain("'" + rightType + "'"));
    }

    delegate TaggedUnion EvaluateBinary(ref TaggedUnion left, ref TaggedUnion right);

    #endregion
}
