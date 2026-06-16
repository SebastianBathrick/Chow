using Chow;
using Chow.SourceData;
using Chow.VM;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11.0 subprocess (2026-06-05).
/// </summary>
public class LogicEvaluatorTests
{
    #region Not  (Python: not)

    [Test]
    public void Not_True_ReturnsFalse()
    {
        // Python: not True -> False
        AssertBoolResult(LogicEvaluator.EvaluateNot, B(true), false);
    }

    [Test]
    public void Not_False_ReturnsTrue()
    {
        // Python: not False -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, B(false), true);
    }

    [Test]
    public void Not_ZeroInt_ReturnsTrue()
    {
        // Python: not 0 -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, L(0), true);
    }

    [Test]
    public void Not_NonZeroInt_ReturnsFalse()
    {
        // Python: not 1 -> False
        AssertBoolResult(LogicEvaluator.EvaluateNot, L(1), false);
    }

    [Test]
    public void Not_ZeroFloat_ReturnsTrue()
    {
        // Python: not 0.0 -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, D(0.0), true);
    }

    [Test]
    public void Not_NonZeroFloat_ReturnsFalse()
    {
        // Python: not 1.0 -> False
        AssertBoolResult(LogicEvaluator.EvaluateNot, D(1.0), false);
    }

    [Test]
    public void Not_EmptyString_ReturnsTrue()
    {
        // Python: not "" -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, S(string.Empty), true);
    }

    [Test]
    public void Not_NonEmptyString_ReturnsFalse()
    {
        // Python: not "x" -> False
        AssertBoolResult(LogicEvaluator.EvaluateNot, S("x"), false);
    }

    [Test]
    public void Not_EmptyList_ReturnsTrue()
    {
        // Python: not [] -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, List(), true);
    }

    [Test]
    public void Not_NonEmptyList_ReturnsFalse()
    {
        // Python: not [1] -> False
        AssertBoolResult(LogicEvaluator.EvaluateNot, List(L(1)), false);
    }

    [Test]
    public void Not_EmptyDict_ReturnsTrue()
    {
        // Python: not {} -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, Dict(), true);
    }

    [Test]
    public void Not_NonEmptyDict_ReturnsFalse()
    {
        // Python: not {'a': 1} -> False
        AssertBoolResult(LogicEvaluator.EvaluateNot, Dict(S("a"), L(1)), false);
    }

    [Test]
    public void Not_None_ReturnsTrue()
    {
        // Python: not None -> True
        AssertBoolResult(LogicEvaluator.EvaluateNot, SourceValue.None, true);
    }

    #endregion

    #region And  (Python: and)

    [Test]
    public void And_FalsyLeft_ReturnsLeft()
    {
        // Python: 0 and "rhs" -> 0
        AssertValueResult(LogicEvaluator.EvaluateAnd, L(0), S("rhs"), L(0));
    }

    [Test]
    public void And_TruthyLeft_ReturnsRight()
    {
        // Python: 1 and "rhs" -> "rhs"
        AssertValueResult(LogicEvaluator.EvaluateAnd, L(1), S("rhs"), S("rhs"));
    }

    [Test]
    public void And_EmptyStringLeft_ReturnsLeft()
    {
        // Python: "" and 3 -> ""
        AssertValueResult(LogicEvaluator.EvaluateAnd, S(string.Empty), L(3), S(string.Empty));
    }

    [Test]
    public void And_NonEmptyStringLeft_ReturnsRight()
    {
        // Python: "x" and 3 -> 3
        AssertValueResult(LogicEvaluator.EvaluateAnd, S("x"), L(3), L(3));
    }

    [Test]
    public void And_NoneLeft_ReturnsLeft()
    {
        // Python: None and 3 -> None
        AssertValueResult(LogicEvaluator.EvaluateAnd, SourceValue.None, L(3), SourceValue.None);
    }

    [Test]
    public void And_NonEmptyListLeft_ReturnsRight()
    {
        // Python: [1] and "rhs" -> "rhs"
        AssertValueResult(LogicEvaluator.EvaluateAnd, List(L(1)), S("rhs"), S("rhs"));
    }

    #endregion

    #region Or  (Python: or)

    [Test]
    public void Or_FalsyLeft_ReturnsRight()
    {
        // Python: 0 or "rhs" -> "rhs"
        AssertValueResult(LogicEvaluator.EvaluateOr, L(0), S("rhs"), S("rhs"));
    }

    [Test]
    public void Or_TruthyLeft_ReturnsLeft()
    {
        // Python: 1 or "rhs" -> 1
        AssertValueResult(LogicEvaluator.EvaluateOr, L(1), S("rhs"), L(1));
    }

    [Test]
    public void Or_EmptyStringLeft_ReturnsRight()
    {
        // Python: "" or 3 -> 3
        AssertValueResult(LogicEvaluator.EvaluateOr, S(string.Empty), L(3), L(3));
    }

    [Test]
    public void Or_NonEmptyStringLeft_ReturnsLeft()
    {
        // Python: "x" or 3 -> "x"
        AssertValueResult(LogicEvaluator.EvaluateOr, S("x"), L(3), S("x"));
    }

    [Test]
    public void Or_NoneLeft_ReturnsRight()
    {
        // Python: None or 3 -> 3
        AssertValueResult(LogicEvaluator.EvaluateOr, SourceValue.None, L(3), L(3));
    }

    [Test]
    public void Or_EmptyListLeft_ReturnsRight()
    {
        // Python: [] or "rhs" -> "rhs"
        AssertValueResult(LogicEvaluator.EvaluateOr, List(), S("rhs"), S("rhs"));
    }

    #endregion

    #region Union  (Python: dict | dict)

    [Test]
    public void Union_DisjointDicts_MergesEntries()
    {
        // Python: {'a': 1} | {'b': 2} -> {'a': 1, 'b': 2}
        AssertDictResult(
            LogicEvaluator.EvaluateUnion,
            Dict(S("a"), L(1)),
            Dict(S("b"), L(2)),
            DictOf((S("a"), L(1)), (S("b"), L(2))));
    }

    [Test]
    public void Union_ConflictingKeys_RightOperandWins()
    {
        // Python: {'a': 1} | {'a': 9} -> {'a': 9}
        AssertDictResult(
            LogicEvaluator.EvaluateUnion,
            Dict(S("a"), L(1)),
            Dict(S("a"), L(9)),
            Dict(S("a"), L(9)));
    }

    [Test]
    public void Union_EmptyDicts_ReturnsEmptyDict()
    {
        // Python: {} | {} -> {}
        AssertDictResult(LogicEvaluator.EvaluateUnion, Dict(), Dict(), Dict());
    }

    [Test]
    public void Union_DictInt_RaisesTypeError()
    {
        // Python: {'a': 1} | 1 -> TypeError
        AssertTypeError(
            () => Evaluate(LogicEvaluator.EvaluateUnion, Dict(S("a"), L(1)), L(1)),
            "|",
            "dict",
            "int");
    }

    [Test]
    public void Union_IntDict_RaisesTypeError()
    {
        // Python: 1 | {'a': 1} -> TypeError
        AssertTypeError(
            () => Evaluate(LogicEvaluator.EvaluateUnion, L(1), Dict(S("a"), L(1))),
            "|",
            "int",
            "dict");
    }

    [Test]
    public void Union_ListList_RaisesTypeError()
    {
        // Python: [1] | [2] -> TypeError
        AssertTypeError(
            () => Evaluate(LogicEvaluator.EvaluateUnion, List(L(1)), List(L(2))),
            "|",
            "list",
            "list");
    }

    [Test]
    public void Union_IntInt_RaisesTypeError()
    {
        // Chow does not implement integer bitwise-or yet, so `|` between ints is unsupported.
        AssertTypeError(
            () => Evaluate(LogicEvaluator.EvaluateUnion, L(1), L(2)),
            "|",
            "int",
            "int");
    }

    #endregion

    #region Truthiness Helpers

    [Test]
    public void IsTruthy_TruthyValues_ReturnsTrue()
    {
        AssertIsTruthy(B(true), true);
        AssertIsTruthy(L(1), true);
        AssertIsTruthy(D(1.0), true);
        AssertIsTruthy(S("x"), true);
        AssertIsTruthy(List(L(1)), true);
        AssertIsTruthy(Dict(S("a"), L(1)), true);
    }

    [Test]
    public void IsTruthy_FalsyValues_ReturnsFalse()
    {
        AssertIsTruthy(SourceValue.None, false);
        AssertIsTruthy(B(false), false);
        AssertIsTruthy(L(0), false);
        AssertIsTruthy(D(0.0), false);
        AssertIsTruthy(S(string.Empty), false);
        AssertIsTruthy(List(), false);
        AssertIsTruthy(Dict(), false);
    }

    [Test]
    public void ShortCircuitHelpers_ReturnPythonLogicalPredicates()
    {
        var falsy = L(0);
        var truthy = L(1);

        Assert.That(LogicEvaluator.ShouldShortCircuitAnd(ref falsy), Is.True);
        Assert.That(LogicEvaluator.ShouldShortCircuitAnd(ref truthy), Is.False);
        Assert.That(LogicEvaluator.ShouldShortCircuitOr(ref falsy), Is.False);
        Assert.That(LogicEvaluator.ShouldShortCircuitOr(ref truthy), Is.True);
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

    static SourceValue Dict()
    {
        return new SourceValue(new SourceDictionary());
    }

    static SourceValue Dict(SourceValue key, SourceValue value)
    {
        var dict = new SourceDictionary();
        dict.Add(key, value);
        return new SourceValue(dict);
    }

    static SourceValue DictOf(params (SourceValue Key, SourceValue Value)[] entries)
    {
        var dict = new SourceDictionary();

        foreach (var entry in entries)
        {
            dict.Add(entry.Key, entry.Value);
        }

        return new SourceValue(dict);
    }

    static SourceValue Evaluate(EvaluateUnary evaluate, SourceValue operand)
    {
        return evaluate(ref operand);
    }

    static SourceValue Evaluate(EvaluateBinary evaluate, SourceValue left, SourceValue right)
    {
        return evaluate(ref right, ref left);
    }

    static void AssertBoolResult(EvaluateUnary evaluate, SourceValue operand, bool expectedBool)
    {
        var result = Evaluate(evaluate, operand);

        Assert.That(result.DataType, Is.EqualTo(DataType.Bool));
        Assert.That(result.ToBool(), Is.EqualTo(expectedBool));
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

    static void AssertDictResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        SourceValue expectedValue)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(DataType.Dict));
        Assert.That(
            ComparisonEvaluator.EvaluateEqual(ref result, ref expectedValue).ToBool(),
            Is.True,
            $"Expected {expectedValue}, but was {result}");
    }

    static void AssertTypeError(TestDelegate action, string op, string leftType, string rightType)
    {
        var ex = Assert.Throws<DataTypeException>(action);

        Assert.That(ex.Message, Does.Contain("TypeError: unsupported operand type(s) for " + op));
        Assert.That(ex.Message, Does.Contain("'" + leftType + "'"));
        Assert.That(ex.Message, Does.Contain("'" + rightType + "'"));
    }

    static void AssertIsTruthy(SourceValue operand, bool expectedValue)
    {
        var value = operand;
        Assert.That(LogicEvaluator.IsTruthy(ref value), Is.EqualTo(expectedValue));
    }

    delegate SourceValue EvaluateUnary(ref SourceValue operand);

    delegate SourceValue EvaluateBinary(ref SourceValue r, ref SourceValue l);

    #endregion
}
