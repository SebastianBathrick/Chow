using Chow.Interpreter.Exceptions;
using Chow.SourceData;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11.0 dict key behavior.
/// </summary>
public class PythonKeyComparerTests
{
    static IEnumerable<TestCaseData> EqualKeyCases()
    {
        // Python: {1: 'a', 1.0: 'b', True: 'c'} -> {1: 'c'}
        yield return new TestCaseData(new SourceValue(1L), new SourceValue(1.0)).SetName("Int_Float");
        yield return new TestCaseData(new SourceValue(1L), new SourceValue(true)).SetName("Int_True");
        yield return new TestCaseData(new SourceValue(0L), new SourceValue(false)).SetName("Zero_False");
        yield return new TestCaseData(new SourceValue(0.0), new SourceValue(-0.0)).SetName("Zero_NegativeZero");
        yield return new TestCaseData(new SourceValue(0L), new SourceValue(-0.0)).SetName("IntZero_NegativeZero");
        yield return new TestCaseData(new SourceValue(-5L), new SourceValue(-5.0)).SetName("NegativeInt_Float");
        yield return new TestCaseData(new SourceValue(9007199254740992L), new SourceValue(9007199254740992.0))
            .SetName("TwoPow53_Float");
        yield return new TestCaseData(new SourceValue("a"), new SourceValue("a")).SetName("Str_Str");
        yield return new TestCaseData(SourceValue.None, SourceValue.None).SetName("None_None");
        yield return new TestCaseData(new SourceValue(double.NaN), new SourceValue(double.NaN)).SetName("NaN_NaN");
    }

    static IEnumerable<TestCaseData> UnequalKeyCases()
    {
        yield return new TestCaseData(new SourceValue(1L), new SourceValue(1.5)).SetName("Int_Fraction");
        yield return new TestCaseData(new SourceValue(1L), new SourceValue("1")).SetName("Int_Str");
        yield return new TestCaseData(SourceValue.None, new SourceValue(0L)).SetName("None_Zero");
        yield return new TestCaseData(new SourceValue("a"), new SourceValue("A")).SetName("Str_DifferentCase");

        // Python: 2**53 + 1 == 2.0**53 -> False, though converting the int to float would say otherwise
        yield return new TestCaseData(new SourceValue(9007199254740993L), new SourceValue(9007199254740992.0))
            .SetName("TwoPow53PlusOne_Float");

        // 2.0**63 is past long.MaxValue, so no int equals it
        yield return new TestCaseData(new SourceValue(long.MaxValue), new SourceValue(9223372036854775808.0))
            .SetName("LongMax_TwoPow63");

        yield return new TestCaseData(new SourceValue(0L), new SourceValue(double.NaN)).SetName("Zero_NaN");
    }

    // Parameters are object because SourceValue is internal and NUnit test methods must be public.
    [TestCaseSource(nameof(EqualKeyCases))]
    public void AreEqual_PythonEqualKeys_AreEqualWithSameHash(object l, object r)
    {
        var left = (SourceValue)l;
        var right = (SourceValue)r;

        Assert.Multiple(() =>
        {
            Assert.That(PythonKeyComparer.AreEqual(left, right), Is.True);
            Assert.That(PythonKeyComparer.AreEqual(right, left), Is.True);
            Assert.That(PythonKeyComparer.GetHash(left), Is.EqualTo(PythonKeyComparer.GetHash(right)));
        });
    }

    [TestCaseSource(nameof(UnequalKeyCases))]
    public void AreEqual_DifferentKeys_AreNotEqual(object l, object r)
    {
        var left = (SourceValue)l;
        var right = (SourceValue)r;

        Assert.Multiple(() =>
        {
            Assert.That(PythonKeyComparer.AreEqual(left, right), Is.False);
            Assert.That(PythonKeyComparer.AreEqual(right, left), Is.False);
        });
    }

    [Test]
    public void ValidateHashable_List_ThrowsTypeError()
    {
        // Python: {[1]: 2} -> TypeError: unhashable type: 'list'
        var list = new SourceValue(new SourceList());

        var exception = Assert.Throws<DataTypeException>(() => PythonKeyComparer.ValidateHashable(list));

        Assert.That(exception!.Message, Is.EqualTo("TypeError: unhashable type: 'list'"));
    }

    [Test]
    public void ValidateHashable_DictKeys_ThrowsTypeError()
    {
        // Python: hash({}.keys()) -> TypeError: unhashable type: 'dict_keys'
        var keys = new SourceValue(new SourceDictKeys(new SourceDict()));

        Assert.Throws<DataTypeException>(() => PythonKeyComparer.ValidateHashable(keys));
    }
}
