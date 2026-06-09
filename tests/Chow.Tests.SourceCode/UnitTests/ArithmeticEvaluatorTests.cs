using Chow;
using Chow.Exceptions;
using Chow.Objects;
using Chow.VM;
using Chow.VM.Utilities;

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
        AssertResult(ArithmeticEvaluator.Add, L(2), L(3), DataType.Long, 5L);
    }

    [Test]
    public void Addition_FloatInt_ReturnsFloat()
    {
        // Python: 2.0 + 3 -> 5.0
        AssertResult(ArithmeticEvaluator.Add, D(2.0), L(3), DataType.Double, 5.0);
    }

    [Test]
    public void Addition_BoolInt_ReturnsInt()
    {
        // Python: True + 1 -> 2
        AssertResult(ArithmeticEvaluator.Add, B(true), L(1), DataType.Long, 2L);
    }

    [Test]
    public void Addition_StrStr_ReturnsStr()
    {
        // Python: "a" + "b" -> "ab"
        AssertResult(ArithmeticEvaluator.Add, S("a"), S("b"), DataType.Str, "ab");
    }

    [Test]
    public void Addition_ListList_ReturnsList()
    {
        // Python: [1] + [2] -> [1, 2]
        AssertValueResult(ArithmeticEvaluator.Add, List(L(1)), List(L(2)), List(L(1), L(2)));
    }

    [Test]
    public void Addition_IntNone_RaisesTypeError()
    {
        // Python: 2 + None -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.Add, L(2), SourceValue.None),
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
        AssertResult(ArithmeticEvaluator.Subtract, L(5), L(3), DataType.Long, 2L);
    }

    [Test]
    public void Subtraction_FloatInt_ReturnsFloat()
    {
        // Python: 2.5 - 1 -> 1.5
        AssertResult(ArithmeticEvaluator.Subtract, D(2.5), L(1), DataType.Double, 1.5);
    }

    [Test]
    public void Subtraction_StrStr_RaisesTypeError()
    {
        // Python: "a" - "b" -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.Subtract, S("a"), S("b")),
            "-",
            "str",
            "str");
    }

    #endregion

    #region Multiplication  (Python: *)

    [Test]
    public void Multiplication_IntInt_ReturnsInt()
    {
        // Python: 7 * 8 -> 56
        AssertResult(ArithmeticEvaluator.Multiply, L(7), L(8), DataType.Long, 56L);
    }

    [Test]
    public void Multiplication_FloatInt_ReturnsFloat()
    {
        // Python: 2.0 * 3 -> 6.0
        AssertResult(ArithmeticEvaluator.Multiply, D(2.0), L(3), DataType.Double, 6.0);
    }

    [Test]
    public void Multiplication_StrInt_ReturnsRepeatedStr()
    {
        // Python: "ab" * 3 -> "ababab"
        AssertResult(ArithmeticEvaluator.Multiply, S("ab"), L(3), DataType.Str, "ababab");
    }

    [Test]
    public void Multiplication_IntStr_ReturnsRepeatedStr()
    {
        // Python: 3 * "ab" -> "ababab"
        AssertResult(ArithmeticEvaluator.Multiply, L(3), S("ab"), DataType.Str, "ababab");
    }

    [Test]
    public void Multiplication_StrZero_ReturnsEmptyStr()
    {
        // Python: "ab" * 0 -> ""
        AssertResult(ArithmeticEvaluator.Multiply, S("ab"), L(0), DataType.Str, string.Empty);
    }

    [Test]
    public void Multiplication_StrNegative_ReturnsEmptyStr()
    {
        // Python: "ab" * -1 -> ""
        AssertResult(ArithmeticEvaluator.Multiply, S("ab"), L(-1), DataType.Str, string.Empty);
    }

    [Test]
    public void Multiplication_BoolStr_ReturnsRepeatedStr()
    {
        // Python: True * "ab" -> "ab"
        AssertResult(ArithmeticEvaluator.Multiply, B(true), S("ab"), DataType.Str, "ab");
    }

    [Test]
    public void Multiplication_FalseStr_ReturnsEmptyStr()
    {
        // Python: False * "ab" -> ""
        AssertResult(ArithmeticEvaluator.Multiply, B(false), S("ab"), DataType.Str, string.Empty);
    }

    [Test]
    public void Multiplication_ListInt_ReturnsRepeatedList()
    {
        // Python: [1] * 3 -> [1, 1, 1]
        AssertValueResult(
            ArithmeticEvaluator.Multiply,
            List(L(1)),
            L(3),
            List(L(1), L(1), L(1)));
    }

    [Test]
    public void Multiplication_IntList_ReturnsRepeatedList()
    {
        // Python: 3 * [1] -> [1, 1, 1]
        AssertValueResult(
            ArithmeticEvaluator.Multiply,
            L(3),
            List(L(1)),
            List(L(1), L(1), L(1)));
    }

    [Test]
    public void Multiplication_ListZero_ReturnsEmptyList()
    {
        // Python: [1] * 0 -> []
        AssertValueResult(ArithmeticEvaluator.Multiply, List(L(1)), L(0), List());
    }

    [Test]
    public void Multiplication_ListNegative_ReturnsEmptyList()
    {
        // Python: [1] * -1 -> []
        AssertValueResult(ArithmeticEvaluator.Multiply, List(L(1)), L(-1), List());
    }

    [Test]
    public void Multiplication_BoolList_ReturnsRepeatedList()
    {
        // Python: True * [1] -> [1]
        AssertValueResult(ArithmeticEvaluator.Multiply, B(true), List(L(1)), List(L(1)));
    }

    [Test]
    public void Multiplication_FalseList_ReturnsEmptyList()
    {
        // Python: False * [1] -> []
        AssertValueResult(ArithmeticEvaluator.Multiply, B(false), List(L(1)), List());
    }

    [Test]
    public void Multiplication_StrFloat_RaisesTypeError()
    {
        // Python: "a" * 2.5 -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.Multiply, S("a"), D(2.5)),
            "*",
            "str",
            "float");
    }

    #endregion

    #region Division  (Python: /)

    [Test]
    public void Division_IntInt_ReturnsFloat()
    {
        // Python: 9 / 3 -> 3.0
        AssertResult(ArithmeticEvaluator.Divide, L(9), L(3), DataType.Double, 3.0);
    }

    [Test]
    public void Division_IntInt_WithRemainder_ReturnsFloat()
    {
        // Python: 7 / 2 -> 3.5
        AssertResult(ArithmeticEvaluator.Divide, L(7), L(2), DataType.Double, 3.5);
    }

    [Test]
    public void Division_ByZero_RaisesZeroDivisionError()
    {
        // Python: 1 / 0 -> ZeroDivisionError
        Assert.Throws<ZeroDivisionException>(
            () => Evaluate(ArithmeticEvaluator.Divide, L(1), L(0)));
    }

    [Test]
    public void Division_IntNone_RaisesTypeError()
    {
        // Python: 2 / None -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.Divide, L(2), SourceValue.None),
            "/",
            "int",
            "NoneType");
    }

    [Test]
    public void Division_ListInt_RaisesTypeError()
    {
        // Python: [] / 2 -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.Divide, List(), L(2)),
            "/",
            "list",
            "int");
    }

    #endregion

    #region Modulus  (Python: %)

    [Test]
    public void Modulus_PositiveOperands_ReturnsInt()
    {
        // Python: 7 % 3 -> 1
        AssertResult(ArithmeticEvaluator.Pow, L(7), L(3), DataType.Long, 1L);
    }

    [Test]
    public void Modulus_NegativeDividend_ReturnsInt()
    {
        // Python: -3 % 2 -> 1
        AssertResult(ArithmeticEvaluator.Pow, L(-3), L(2), DataType.Long, 1L);
    }

    [Test]
    public void Modulus_NegativeDivisor_ReturnsInt()
    {
        // Python: 3 % -2 -> -1
        AssertResult(ArithmeticEvaluator.Pow, L(3), L(-2), DataType.Long, -1L);
    }

    [Test]
    public void Modulus_BothNegative_ReturnsInt()
    {
        // Python: -17 % -4 -> -1
        AssertResult(ArithmeticEvaluator.Pow, L(-17), L(-4), DataType.Long, -1L);
    }

    [Test]
    public void Modulus_ByZero_RaisesZeroDivisionError()
    {
        // Python: 7 % 0 -> ZeroDivisionError
        Assert.Throws<ZeroDivisionException>(
            () => Evaluate(ArithmeticEvaluator.Pow, L(7), L(0)));
    }

    #endregion

    #region Floor Division  (Python: //)

    [Test]
    public void FloorDivision_PositiveOperands_ReturnsInt()
    {
        // Python: 17 // 4 -> 4
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(17), L(4), DataType.Long, 4L);
    }

    [Test]
    public void FloorDivision_NegativeDividend_ReturnsInt()
    {
        // Python: -17 // 4 -> -5
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(-17), L(4), DataType.Long, -5L);
    }

    [Test]
    public void FloorDivision_NegativeDivisor_ReturnsInt()
    {
        // Python: 17 // -4 -> -5
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(17), L(-4), DataType.Long, -5L);
    }

    [Test]
    public void FloorDivision_BothNegative_ReturnsInt()
    {
        // Python: -17 // -4 -> 4
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, L(-17), L(-4), DataType.Long, 4L);
    }

    [Test]
    public void FloorDivision_FloatOperands_ReturnsFloat()
    {
        // Python: 7.5 // 2.1 -> 3.0
        AssertResult(ArithmeticEvaluator.EvaluateFloorDivision, D(7.5), D(2.1), DataType.Double, 3.0);
    }

    [Test]
    public void FloorDivision_ListInt_RaisesTypeErrorWithFloorDivideOperator()
    {
        // Python: [] // 2 -> TypeError
        AssertTypeError(
            () => Evaluate(ArithmeticEvaluator.EvaluateFloorDivision, List(), L(2)),
            "//",
            "list",
            "int");
    }

    #endregion

    #region Exponentiation  (Python: **)

    [Test]
    public void Exponentiation_IntInt_ReturnsInt()
    {
        // Python: 2 ** 3 -> 8
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(2), L(3), DataType.Long, 8L);
    }

    [Test]
    public void Exponentiation_NegativeExponent_ReturnsFloat()
    {
        // Python: 2 ** -3 -> 0.125
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(2), L(-3), DataType.Double, 0.125);
    }

    [Test]
    public void Exponentiation_ZeroToZero_ReturnsInt()
    {
        // Python: 0 ** 0 -> 1
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(0), L(0), DataType.Long, 1L);
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
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(10), L(16), DataType.Long, 10_000_000_000_000_000L);
    }

    [Test]
    public void Exponentiation_FloatBase_ReturnsFloat()
    {
        // Python: 2.0 ** 3 -> 8.0
        AssertResult(ArithmeticEvaluator.EvaluateExponent, D(2.0), L(3), DataType.Double, 8.0);
    }

    [Test]
    public void Exponentiation_NegativeBase_ReturnsInt()
    {
        // Python: (-2) ** 3 -> -8
        AssertResult(ArithmeticEvaluator.EvaluateExponent, L(-2), L(3), DataType.Long, -8L);
    }

    [Test]
    public void Exponentiation_NegativeExponentLargerBase_ReturnsFloat()
    {
        // Python: 11 ** -3 -> 0.0007513148009015778
        AssertResult(
            ArithmeticEvaluator.EvaluateExponent,
            L(11),
            L(-3),
            DataType.Double,
            0.0007513148009015778);
    }

    #endregion

    #region Negation  (Python: unary -)

    [Test]
    public void Negation_True_ReturnsInt()
    {
        // Python: -True -> -1
        AssertUnaryResult(ArithmeticEvaluator.EvaluateNegation, B(true), DataType.Long, -1L);
    }

    [Test]
    public void Negation_False_ReturnsInt()
    {
        // Python: -False -> 0
        AssertUnaryResult(ArithmeticEvaluator.EvaluateNegation, B(false), DataType.Long, 0L);
    }

    [Test]
    public void Negation_Int_ReturnsInt()
    {
        // Python: -3 -> -3
        AssertUnaryResult(ArithmeticEvaluator.EvaluateNegation, L(3), DataType.Long, -3L);
    }

    [Test]
    public void Negation_Float_ReturnsFloat()
    {
        // Python: -3.5 -> -3.5
        AssertUnaryResult(ArithmeticEvaluator.EvaluateNegation, D(3.5), DataType.Double, -3.5);
    }

    [Test]
    public void Negation_Str_RaisesTypeError()
    {
        // Python: -"a" -> TypeError
        AssertUnaryTypeError(
            () => Evaluate(ArithmeticEvaluator.EvaluateNegation, S("a")),
            "-",
            "str");
    }

    #endregion

    #region Helpers

    static SourceValue L(long value) => new SourceValue(value);

    static SourceValue D(double value) => new SourceValue(value);

    static SourceValue B(bool value) => new SourceValue(value);

    static SourceValue S(string value) => new SourceValue(value);

    static SourceValue List(params SourceValue[] values)
    {
        var list = new SourceList();

        foreach (var value in values)
        {
            list.Add(value);
        }

        return new SourceValue(list);
    }

    static SourceValue Evaluate(EvaluateBinary evaluate, SourceValue left, SourceValue right)
    {
        return evaluate(right, left);
    }

    static SourceValue Evaluate(EvaluateUnary evaluate, SourceValue operand)
    {
        var value = operand;
        return evaluate(ref value);
    }

    static void AssertResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        DataType expectedDataType,
        long expectedLong)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(expectedDataType));
        Assert.That(result.AsType<long>(), Is.EqualTo(expectedLong));
    }

    static void AssertResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        DataType expectedDataType,
        double expectedDouble)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(expectedDataType));
        Assert.That(result.AsType<double>(), Is.EqualTo(expectedDouble).Within(Tolerance));
    }

    static void AssertResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        DataType expectedDataType,
        string expectedString)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(expectedDataType));
        Assert.That(result.AsType<string>(), Is.EqualTo(expectedString));
    }

    static void AssertValueResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        SourceValue expectedValue)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result, Is.EqualTo(expectedValue));
    }

    static void AssertUnaryResult(
        EvaluateUnary evaluate,
        SourceValue operand,
        DataType expectedDataType,
        long expectedLong)
    {
        var result = Evaluate(evaluate, operand);

        Assert.That(result.DataType, Is.EqualTo(expectedDataType));
        Assert.That(result.AsType<long>(), Is.EqualTo(expectedLong));
    }

    static void AssertUnaryResult(
        EvaluateUnary evaluate,
        SourceValue operand,
        DataType expectedDataType,
        double expectedDouble)
    {
        var result = Evaluate(evaluate, operand);

        Assert.That(result.DataType, Is.EqualTo(expectedDataType));
        Assert.That(result.AsType<double>(), Is.EqualTo(expectedDouble).Within(Tolerance));
    }

    static void AssertTypeError(TestDelegate action, string op, string leftType, string rightType)
    {
        var ex = Assert.Throws<DataTypeException>(action);

        Assert.That(ex.Message, Does.Contain("TypeError: unsupported operand type(s) for " + op));
        Assert.That(ex.Message, Does.Contain("'" + leftType + "'"));
        Assert.That(ex.Message, Does.Contain("'" + rightType + "'"));
    }

    static void AssertUnaryTypeError(TestDelegate action, string op, string operandType)
    {
        var ex = Assert.Throws<DataTypeException>(action);

        Assert.That(ex.Message, Does.Contain("TypeError: bad operand type for unary " + op));
        Assert.That(ex.Message, Does.Contain("'" + operandType + "'"));
    }

    delegate SourceValue EvaluateBinary(SourceValue r, SourceValue l);

    delegate SourceValue EvaluateUnary(ref SourceValue operand);

    #endregion
}
