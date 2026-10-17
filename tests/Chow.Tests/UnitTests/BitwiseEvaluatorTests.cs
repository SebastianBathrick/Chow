using Chow;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11.0.
/// </summary>
public class BitwiseEvaluatorTests
{
    #region Or  (Python: |)

    [Test]
    public void Or_Ints_ReturnsBitwiseOr()
    {
        // Python: 1 | 2 -> 3; -8 | 3 -> -5
        AssertLongResult(BitwiseEvaluator.Or, L(1), L(2), 3);
        AssertLongResult(BitwiseEvaluator.Or, L(-8), L(3), -5);
    }

    [Test]
    public void Or_DisjointDicts_MergesEntries()
    {
        // Python: {'a': 1} | {'b': 2} -> {'a': 1, 'b': 2}
        AssertDictResult(
            BitwiseEvaluator.Or,
            Dict(S("a"), L(1)),
            Dict(S("b"), L(2)),
            DictOf((S("a"), L(1)), (S("b"), L(2))));
    }

    [Test]
    public void Or_ConflictingKeys_RightOperandWins()
    {
        // Python: {'a': 1} | {'a': 9} -> {'a': 9}
        AssertDictResult(
            BitwiseEvaluator.Or,
            Dict(S("a"), L(1)),
            Dict(S("a"), L(9)),
            Dict(S("a"), L(9)));
    }

    [Test]
    public void Or_EmptyDicts_ReturnsEmptyDict()
    {
        // Python: {} | {} -> {}
        AssertDictResult(BitwiseEvaluator.Or, Dict(), Dict(), Dict());
    }

    [Test]
    public void Or_DictInt_RaisesTypeError()
    {
        // Python: {'a': 1} | 1 -> TypeError
        AssertTypeError(() => Evaluate(BitwiseEvaluator.Or, Dict(S("a"), L(1)), L(1)), "|", "dict", "int");
    }

    [Test]
    public void Or_IntDict_RaisesTypeError()
    {
        // Python: 1 | {'a': 1} -> TypeError
        AssertTypeError(() => Evaluate(BitwiseEvaluator.Or, L(1), Dict(S("a"), L(1))), "|", "int", "dict");
    }

    [Test]
    public void Or_ListList_RaisesTypeError()
    {
        // Python: [1] | [2] -> TypeError
        AssertTypeError(() => Evaluate(BitwiseEvaluator.Or, List(L(1)), List(L(2))), "|", "list", "list");
    }

    [Test]
    public void Or_DictSet_RaisesTypeError()
    {
        // Python: {} | set() -> TypeError
        AssertTypeError(() => Evaluate(BitwiseEvaluator.Or, Dict(), Set()), "|", "dict", "set");
    }

    [Test]
    public void Or_Sets_ResultTakesLeftOperandsType()
    {
        // Python: {2} | frozenset([1]) -> {1, 2}; frozenset([1]) | {2} -> frozenset({1, 2})
        AssertSetResult(BitwiseEvaluator.Or, Set(L(2)), FrozenSet(L(1)), DataType.Set, "{1, 2}");
        AssertSetResult(BitwiseEvaluator.Or, FrozenSet(L(1)), Set(L(2)), DataType.FrozenSet, "frozenset({1, 2})");
    }

    #endregion

    #region And  (Python: &)

    [Test]
    public void And_Ints_ReturnsBitwiseAnd()
    {
        // Python: 6 & 3 -> 2; -1 & 3 -> 3
        AssertLongResult(BitwiseEvaluator.And, L(6), L(3), 2);
        AssertLongResult(BitwiseEvaluator.And, L(-1), L(3), 3);
        AssertLongResult(BitwiseEvaluator.And, L(long.MaxValue), L(-1), long.MaxValue);
    }

    [Test]
    public void And_Sets_IteratesTheSmallerOperand()
    {
        // Python: {1, 2} & {1.0, 3} -> {1.0}; {1} & {1.0, 2} -> {1}
        AssertSetResult(BitwiseEvaluator.And, Set(L(1), L(2)), Set(D(1.0), L(3)), DataType.Set, "{1.0}");
        AssertSetResult(BitwiseEvaluator.And, Set(L(1)), Set(D(1.0), L(2)), DataType.Set, "{1}");
    }

    [Test]
    public void And_SetList_RaisesTypeError()
    {
        // Python: {1} & [1] -> TypeError; operators need sets on both sides, unlike the methods.
        AssertTypeError(() => Evaluate(BitwiseEvaluator.And, Set(L(1)), List(L(1))), "&", "set", "list");
    }

    #endregion

    #region Xor  (Python: ^)

    [Test]
    public void Xor_Ints_ReturnsBitwiseXor()
    {
        // Python: 6 ^ 3 -> 5; -5 ^ 3 -> -8
        AssertLongResult(BitwiseEvaluator.Xor, L(6), L(3), 5);
        AssertLongResult(BitwiseEvaluator.Xor, L(-5), L(3), -8);
    }

    [Test]
    public void Xor_Sets_ReturnsSymmetricDifference()
    {
        // Python: {1, 2, 3} ^ {2, 5} -> {1, 3, 5}
        AssertSetResult(BitwiseEvaluator.Xor, Set(L(1), L(2), L(3)), Set(L(2), L(5)), DataType.Set, "{1, 3, 5}");
    }

    #endregion

    #region Bool Operands

    [Test]
    public void Bools_BothBool_ReturnBool()
    {
        // Python: True & False -> False; True | False -> True; True ^ True -> False
        AssertBoolResult(BitwiseEvaluator.And, B(true), B(false), false);
        AssertBoolResult(BitwiseEvaluator.Or, B(true), B(false), true);
        AssertBoolResult(BitwiseEvaluator.Xor, B(true), B(true), false);
    }

    [Test]
    public void Bools_MixedWithInt_ReturnInt()
    {
        // Python: True & 1 -> 1; True | 2 -> 3; 1 ^ True -> 0
        AssertLongResult(BitwiseEvaluator.And, B(true), L(1), 1);
        AssertLongResult(BitwiseEvaluator.Or, B(true), L(2), 3);
        AssertLongResult(BitwiseEvaluator.Xor, L(1), B(true), 0);
    }

    #endregion

    #region Unsupported Operands

    [Test]
    public void FloatOperand_RaisesTypeError()
    {
        // Python: 1.5 & 1 -> TypeError: unsupported operand type(s) for &: 'float' and 'int'
        AssertTypeError(() => Evaluate(BitwiseEvaluator.And, D(1.5), L(1)), "&", "float", "int");
    }

    [Test]
    public void StrOperands_RaiseTypeError()
    {
        // Python: 'a' ^ 'b' -> TypeError
        AssertTypeError(() => Evaluate(BitwiseEvaluator.Xor, S("a"), S("b")), "^", "str", "str");
    }

    [Test]
    public void NoneOperand_RaisesTypeError()
    {
        // Python: 1 | None -> TypeError: unsupported operand type(s) for |: 'int' and 'NoneType'
        AssertTypeError(() => Evaluate(BitwiseEvaluator.Or, L(1), SourceValue.None), "|", "int", "NoneType");
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

    static SourceValue Set(params SourceValue[] values) => SetOf(isFrozen: false, values);

    static SourceValue FrozenSet(params SourceValue[] values) => SetOf(isFrozen: true, values);

    static SourceValue SetOf(bool isFrozen, SourceValue[] values)
    {
        var set = new SourceSet(isFrozen);

        foreach (var value in values)
        {
            set.Add(value);
        }

        return new SourceValue(set);
    }

    static SourceValue Dict()
    {
        return new SourceValue(new SourceDict());
    }

    static SourceValue Dict(SourceValue key, SourceValue value)
    {
        var dict = new SourceDict();
        dict.Add(key, value);
        return new SourceValue(dict);
    }

    static SourceValue DictOf(params (SourceValue Key, SourceValue Value)[] entries)
    {
        var dict = new SourceDict();

        foreach (var entry in entries)
        {
            dict.Add(entry.Key, entry.Value);
        }

        return new SourceValue(dict);
    }

    static SourceValue Evaluate(EvaluateBinary evaluate, SourceValue left, SourceValue right)
    {
        return evaluate(ref right, ref left);
    }

    static void AssertLongResult(EvaluateBinary evaluate, SourceValue left, SourceValue right, long expected)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(DataType.Long));
        Assert.That(result.ToLong(), Is.EqualTo(expected));
    }

    static void AssertBoolResult(EvaluateBinary evaluate, SourceValue left, SourceValue right, bool expected)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(DataType.Bool));
        Assert.That(result.ToBool(), Is.EqualTo(expected));
    }

    static void AssertSetResult(
        EvaluateBinary evaluate,
        SourceValue left,
        SourceValue right,
        DataType expectedType,
        string expectedRepresentation)
    {
        var result = Evaluate(evaluate, left, right);

        Assert.That(result.DataType, Is.EqualTo(expectedType));
        Assert.That(result.ToRepresentation(), Is.EqualTo(expectedRepresentation));
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

    delegate SourceValue EvaluateBinary(ref SourceValue r, ref SourceValue l);

    #endregion
}
