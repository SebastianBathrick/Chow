using Chow;
using Chow.DataTypes;
using Chow.Expressions;

namespace Chow.Tests.SourceCode.UnitTests;

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
        AssertBoolResult(LogicEvaluator.EvaluateNot, TaggedUnion.None, true);
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
        AssertValueResult(LogicEvaluator.EvaluateAnd, TaggedUnion.None, L(3), TaggedUnion.None);
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
        AssertValueResult(LogicEvaluator.EvaluateOr, TaggedUnion.None, L(3), L(3));
    }

    [Test]
    public void Or_EmptyListLeft_ReturnsRight()
    {
        // Python: [] or "rhs" -> "rhs"
        AssertValueResult(LogicEvaluator.EvaluateOr, List(), S("rhs"), S("rhs"));
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
        AssertIsTruthy(TaggedUnion.None, false);
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

    static TaggedUnion L(long value) => new TaggedUnion(value);

    static TaggedUnion D(double value) => new TaggedUnion(value);

    static TaggedUnion B(bool value) => new TaggedUnion(value);

    static TaggedUnion S(string value) => new TaggedUnion(value);

    static TaggedUnion List(params TaggedUnion[] values)
    {
        var list = new SourceList();

        foreach (var value in values)
        {
            list.Add(value);
        }

        return new TaggedUnion(list);
    }

    static TaggedUnion Dict()
    {
        return new TaggedUnion(new SourceDictionary());
    }

    static TaggedUnion Dict(TaggedUnion key, TaggedUnion value)
    {
        var dict = new SourceDictionary();
        dict.Add(key, value);
        return new TaggedUnion(dict);
    }

    static TaggedUnion Evaluate(EvaluateUnary evaluate, TaggedUnion operand)
    {
        var value = operand;
        return evaluate(ref value);
    }

    static TaggedUnion Evaluate(EvaluateBinary evaluate, TaggedUnion left, TaggedUnion right)
    {
        var l = left;
        var r = right;
        return evaluate(ref l, ref r);
    }

    static void AssertBoolResult(EvaluateUnary evaluate, TaggedUnion operand, bool expectedBool)
    {
        var result = Evaluate(evaluate, operand);

        Assert.That(result.DataType, Is.EqualTo(DataType.Bool));
        Assert.That(result.AsType<bool>(), Is.EqualTo(expectedBool));
    }

    static void AssertValueResult(
        EvaluateBinary evaluate,
        TaggedUnion left,
        TaggedUnion right,
        TaggedUnion expectedValue)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result, Is.EqualTo(expectedValue));
    }

    static void AssertIsTruthy(TaggedUnion operand, bool expectedValue)
    {
        var value = operand;
        Assert.That(LogicEvaluator.IsTruthy(ref value), Is.EqualTo(expectedValue));
    }

    delegate TaggedUnion EvaluateUnary(ref TaggedUnion operand);

    delegate TaggedUnion EvaluateBinary(ref TaggedUnion left, ref TaggedUnion right);

    #endregion
}
