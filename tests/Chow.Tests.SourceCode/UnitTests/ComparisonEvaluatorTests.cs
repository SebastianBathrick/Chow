using Chow;
using Chow.SourceData;
using Chow.VM;

namespace Chow.Tests.SourceCode.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11.0 subprocess (2026-06-05).
/// </summary>
public class ComparisonEvaluatorTests
{
    #region Equality  (Python: ==)

    [Test]
    public void Equality_IntInt_ReturnsTrue()
    {
        // Python: 1 == 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, L(1), L(1), true);
    }

    [Test]
    public void Equality_IntFloat_ReturnsTrue()
    {
        // Python: 1 == 1.0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, L(1), D(1.0), true);
    }

    [Test]
    public void Equality_BoolInt_ReturnsTrue()
    {
        // Python: True == 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, B(true), L(1), true);
    }

    [Test]
    public void Equality_FalseIntZero_ReturnsTrue()
    {
        // Python: False == 0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, B(false), L(0), true);
    }

    [Test]
    public void Equality_StrStr_ReturnsTrue()
    {
        // Python: "a" == "a" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, S("a"), S("a"), true);
    }

    [Test]
    public void Equality_NoneNone_ReturnsTrue()
    {
        // Python: None == None -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, SourceValue.None, SourceValue.None, true);
    }

    [Test]
    public void Equality_IntStr_ReturnsFalse()
    {
        // Python: 1 == "1" -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, L(1), S("1"), false);
    }

    [Test]
    public void Equality_NoneInt_ReturnsFalse()
    {
        // Python: None == 0 -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, SourceValue.None, L(0), false);
    }

    [Test]
    public void Equality_ListListSameElements_ReturnsTrue()
    {
        // Python: [1, 2] == [1, 2] -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, List(L(1), L(2)), List(L(1), L(2)), true);
    }

    [Test]
    public void Equality_ListListDifferentElements_ReturnsFalse()
    {
        // Python: [1, 2] == [1, 3] -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, List(L(1), L(2)), List(L(1), L(3)), false);
    }

    [Test]
    public void Equality_DictDictSameEntries_ReturnsTrue()
    {
        // Python: {'a': 1} == {'a': 1} -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, Dict(S("a"), L(1)), Dict(S("a"), L(1)), true);
    }

    [Test]
    public void Equality_DictDictDifferentEntries_ReturnsFalse()
    {
        // Python: {'a': 1} == {'a': 2} -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateEqual, Dict(S("a"), L(1)), Dict(S("a"), L(2)), false);
    }

    #endregion

    #region Not Equal  (Python: !=)

    [Test]
    public void NotEqual_IntInt_ReturnsTrue()
    {
        // Python: 1 != 2 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateNotEqual, L(1), L(2), true);
    }

    [Test]
    public void NotEqual_IntFloat_ReturnsFalse()
    {
        // Python: 1 != 1.0 -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateNotEqual, L(1), D(1.0), false);
    }

    [Test]
    public void NotEqual_BoolInt_ReturnsTrue()
    {
        // Python: True != 0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateNotEqual, B(true), L(0), true);
    }

    [Test]
    public void NotEqual_StrStr_ReturnsTrue()
    {
        // Python: "a" != "b" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateNotEqual, S("a"), S("b"), true);
    }

    [Test]
    public void NotEqual_NoneNone_ReturnsFalse()
    {
        // Python: None != None -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateNotEqual, SourceValue.None, SourceValue.None, false);
    }

    [Test]
    public void NotEqual_IntStr_ReturnsTrue()
    {
        // Python: 1 != "1" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateNotEqual, L(1), S("1"), true);
    }

    #endregion

    #region Less Than  (Python: <)

    [Test]
    public void Less_IntInt_ReturnsTrue()
    {
        // Python: 1 < 2 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLess, L(1), L(2), true);
    }

    [Test]
    public void Less_IntFloat_ReturnsTrue()
    {
        // Python: 1 < 2.0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLess, L(1), D(2.0), true);
    }

    [Test]
    public void Less_BoolInt_ReturnsTrue()
    {
        // Python: True < 2 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLess, B(true), L(2), true);
    }

    [Test]
    public void Less_FalseTrue_ReturnsTrue()
    {
        // Python: False < True -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLess, B(false), B(true), true);
    }

    [Test]
    public void Less_StrStrAscending_ReturnsTrue()
    {
        // Python: "a" < "b" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLess, S("a"), S("b"), true);
    }

    [Test]
    public void Less_StrStrDescending_ReturnsFalse()
    {
        // Python: "b" < "a" -> False
        AssertBoolResult(ComparisonEvaluator.EvaluateLess, S("b"), S("a"), false);
    }

    [Test]
    public void Less_IntNone_RaisesTypeError()
    {
        // Python: 1 < None -> TypeError
        AssertTypeError(
            () => Evaluate(ComparisonEvaluator.EvaluateLess, L(1), SourceValue.None),
            "<",
            "int",
            "NoneType");
    }

    [Test]
    public void Less_StrInt_RaisesTypeError()
    {
        // Python: "a" < 1 -> TypeError
        AssertTypeError(
            () => Evaluate(ComparisonEvaluator.EvaluateLess, S("a"), L(1)),
            "<",
            "str",
            "int");
    }

    [Test]
    public void Less_NoneNone_RaisesTypeError()
    {
        // Python: None < None -> TypeError
        AssertTypeError(
            () => Evaluate(ComparisonEvaluator.EvaluateLess, SourceValue.None, SourceValue.None),
            "<",
            "NoneType",
            "NoneType");
    }

    #endregion

    #region Less Or Equal  (Python: <=)

    [Test]
    public void LessEqual_IntInt_ReturnsTrue()
    {
        // Python: 1 <= 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLessEqual, L(1), L(1), true);
    }

    [Test]
    public void LessEqual_IntFloat_ReturnsTrue()
    {
        // Python: 1 <= 2.0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLessEqual, L(1), D(2.0), true);
    }

    [Test]
    public void LessEqual_StrStr_ReturnsTrue()
    {
        // Python: "a" <= "a" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLessEqual, S("a"), S("a"), true);
    }

    [Test]
    public void LessEqual_BoolInt_ReturnsTrue()
    {
        // Python: True <= 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateLessEqual, B(true), L(1), true);
    }

    [Test]
    public void LessEqual_NoneInt_RaisesTypeError()
    {
        // Python: None <= 1 -> TypeError
        AssertTypeError(
            () => Evaluate(ComparisonEvaluator.EvaluateLessEqual, SourceValue.None, L(1)),
            "<=",
            "NoneType",
            "int");
    }

    #endregion

    #region Greater Than  (Python: >)

    [Test]
    public void Greater_IntInt_ReturnsTrue()
    {
        // Python: 2 > 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreater, L(2), L(1), true);
    }

    [Test]
    public void Greater_FloatInt_ReturnsTrue()
    {
        // Python: 2.0 > 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreater, D(2.0), L(1), true);
    }

    [Test]
    public void Greater_BoolInt_ReturnsTrue()
    {
        // Python: True > 0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreater, B(true), L(0), true);
    }

    [Test]
    public void Greater_StrStr_ReturnsTrue()
    {
        // Python: "b" > "a" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreater, S("b"), S("a"), true);
    }

    [Test]
    public void Greater_IntNone_RaisesTypeError()
    {
        // Python: 1 > None -> TypeError
        AssertTypeError(
            () => Evaluate(ComparisonEvaluator.EvaluateGreater, L(1), SourceValue.None),
            ">",
            "int",
            "NoneType");
    }

    #endregion

    #region Greater Or Equal  (Python: >=)

    [Test]
    public void GreaterEqual_IntInt_ReturnsTrue()
    {
        // Python: 1 >= 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreaterEqual, L(1), L(1), true);
    }

    [Test]
    public void GreaterEqual_FloatInt_ReturnsTrue()
    {
        // Python: 2.0 >= 1 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreaterEqual, D(2.0), L(1), true);
    }

    [Test]
    public void GreaterEqual_StrStr_ReturnsTrue()
    {
        // Python: "a" >= "a" -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreaterEqual, S("a"), S("a"), true);
    }

    [Test]
    public void GreaterEqual_BoolInt_ReturnsTrue()
    {
        // Python: True >= 0 -> True
        AssertBoolResult(ComparisonEvaluator.EvaluateGreaterEqual, B(true), L(0), true);
    }

    [Test]
    public void GreaterEqual_NoneInt_RaisesTypeError()
    {
        // Python: None >= 0 -> TypeError
        AssertTypeError(
            () => Evaluate(ComparisonEvaluator.EvaluateGreaterEqual, SourceValue.None, L(0)),
            ">=",
            "NoneType",
            "int");
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
            list.AppendItem(value);
        }

        return new SourceValue(list);
    }

    static SourceValue Dict(SourceValue key, SourceValue value)
    {
        var dict = new SourceDictionary();
        dict.Add(key, value);
        return new SourceValue(dict);
    }

    static SourceValue Evaluate(EvaluateBinary evaluate, SourceValue left, SourceValue right)
    {
        return evaluate(ref right, ref left);
    }

    static void AssertBoolResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        bool expectedBool)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(DataType.Bool));
        Assert.That(result.ToBool(), Is.EqualTo(expectedBool));
    }

    static void AssertTypeError(TestDelegate action, string op, string leftType, string rightType)
    {
        var ex = Assert.Throws<DataTypeException>(action);

        Assert.That(ex.Message, Does.Contain("TypeError: unsupported operand type(s) for " + op));
        Assert.That(ex.Message, Does.Contain("'" + leftType + "'"));
        Assert.That(ex.Message, Does.Contain("'" + rightType + "'"));
    }

    delegate SourceValue EvaluateBinary(ref SourceValue r, ref SourceValue l);

    #endregion
}
