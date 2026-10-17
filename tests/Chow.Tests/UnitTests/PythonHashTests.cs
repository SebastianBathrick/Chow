using Chow.SourceData;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Expected values are CPython 3.11.0's hash() run with PYTHONHASHSEED=0 (64-bit). Strings are given
/// as code points so this file stays ASCII.
/// </summary>
public class PythonHashTests
{
    static IEnumerable<TestCaseData> StringCases()
    {
        yield return new TestCaseData(new int[0], 0L).SetName("Str_Empty");
        yield return new TestCaseData(new[] { 0x61 }, 4644417185603328019L).SetName("Str_OneAscii");
        yield return new TestCaseData(new[] { 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67 }, 7904145750247929094L).SetName("Str_SevenBytes");
        yield return new TestCaseData(new[] { 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68 }, 4574395652268504554L).SetName("Str_EightBytes");
        yield return new TestCaseData(new[] { 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69 }, -532774252720507163L).SetName("Str_NineBytes");
        yield return new TestCaseData(new[] { 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78 }, 4771951516191823429L).SetName("Str_SeventeenBytes");
        yield return new TestCaseData(new[] { 0x63, 0x61, 0x66, 0xe9 }, 137524001917817222L).SetName("Str_Latin1");
        yield return new TestCaseData(new[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, -2129700217430894956L).SetName("Str_Latin1Only");
        yield return new TestCaseData(new[] { 0x4e2d, 0x6587 }, 501833946989431548L).SetName("Str_Bmp");
        yield return new TestCaseData(new[] { 0x100 }, 75343234424780393L).SetName("Str_BmpLowest");
        yield return new TestCaseData(new[] { 0xd800 }, 5926611903945376553L).SetName("Str_LoneSurrogate");
        yield return new TestCaseData(new[] { 0x1f600 }, -3536540696076613844L).SetName("Str_Astral");
        yield return new TestCaseData(new[] { 0x61, 0x1f600, 0x62 }, -7541021549821082252L).SetName("Str_AstralMixed");
        yield return new TestCaseData(new[] { 0x68, 0x65, 0x6c, 0x6c, 0x6f, 0x5f, 0x77, 0x6f, 0x72, 0x6c, 0x64 }, -5624712218341157593L).SetName("Str_Identifier");
        yield return new TestCaseData(new[] { 0x61, 0x20, 0x62, 0x20, 0x63 }, 4986143910718902681L).SetName("Str_Spaces");
    }

    static IEnumerable<TestCaseData> IntCases()
    {
        yield return new TestCaseData(0L, 0L).SetName("Int_Zero");
        yield return new TestCaseData(1L, 1L).SetName("Int_One");
        yield return new TestCaseData(-1L, -2L).SetName("Int_MinusOne");
        yield return new TestCaseData(-2L, -2L).SetName("Int_MinusTwo");
        yield return new TestCaseData(2305843009213693950L, 2305843009213693950L).SetName("Int_ModulusMinusOne");
        yield return new TestCaseData(2305843009213693951L, 0L).SetName("Int_Modulus");
        yield return new TestCaseData(2305843009213693952L, 1L).SetName("Int_ModulusPlusOne");
        yield return new TestCaseData(4611686018427387904L, 2L).SetName("Int_TwoPow62");
        yield return new TestCaseData(-4611686018427387904L, -2L).SetName("Int_MinusTwoPow62");
        yield return new TestCaseData(long.MaxValue, 3L).SetName("Int_LongMax");
        yield return new TestCaseData(long.MinValue, -4L).SetName("Int_LongMin");
        yield return new TestCaseData(12345678901234567L, 12345678901234567L).SetName("Int_Large");
        yield return new TestCaseData(-2305843009213693951L, 0L).SetName("Int_NegativeModulus");
    }

    // Floats are given as their IEEE bits so every value is exact.
    static IEnumerable<TestCaseData> FloatCases()
    {
        yield return new TestCaseData(0L, 0L).SetName("Float_Zero");
        yield return new TestCaseData(long.MinValue, 0L).SetName("Float_NegativeZero");
        yield return new TestCaseData(4602678819172646912L, 1152921504606846976L).SetName("Float_Half");
        yield return new TestCaseData(4609434218613702656L, 1152921504606846977L).SetName("Float_OneAndHalf");
        yield return new TestCaseData(-4611123068473966592L, -576460752303423490L).SetName("Float_Negative");
        yield return new TestCaseData(4591870180066957722L, 230584300921369408L).SetName("Float_OneTenth");
        yield return new TestCaseData(4599676419421066581L, 768614336404564608L).SetName("Float_Third");
        yield return new TestCaseData(9094988921128908188L, 1224995262755759164L).SetName("Float_Huge");
        yield return new TestCaseData(-9104749988965452967L, -482449582752280463L).SetName("Float_Tiny");
        yield return new TestCaseData(1L, 16777216L).SetName("Float_Subnormal");
        yield return new TestCaseData(9218868437227405311L, 2234066890152476671L).SetName("Float_Max");
        yield return new TestCaseData(4881901996069617664L, 1L).SetName("Float_TwoPow61");
        yield return new TestCaseData(4886405595696988160L, 2L).SetName("Float_TwoPow62");
        yield return new TestCaseData(9218868437227405312L, 314159L).SetName("Float_Infinity");
        yield return new TestCaseData(-4503599627370496L, -314159L).SetName("Float_NegativeInfinity");
    }

    [TestCaseSource(nameof(StringCases))]
    public void Compute_String_MatchesCPython(int[] codePoints, long expected)
    {
        var value = string.Concat(codePoints.Select(cp => cp is >= 0xd800 and <= 0xdfff
            ? ((char)cp).ToString()
            : char.ConvertFromUtf32(cp)));

        Assert.That(PythonHash.Compute(new SourceValue(value)), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(IntCases))]
    public void Compute_Int_MatchesCPython(long value, long expected)
    {
        Assert.That(PythonHash.Compute(new SourceValue(value)), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(FloatCases))]
    public void Compute_Float_MatchesCPython(long bits, long expected)
    {
        Assert.That(PythonHash.Compute(new SourceValue(BitConverter.Int64BitsToDouble(bits))), Is.EqualTo(expected));
    }

    [Test]
    public void Compute_Bools_MatchCPython()
    {
        // Python: hash(True) -> 1, hash(False) -> 0
        Assert.Multiple(() =>
        {
            Assert.That(PythonHash.Compute(new SourceValue(true)), Is.EqualTo(1L));
            Assert.That(PythonHash.Compute(new SourceValue(false)), Is.EqualTo(0L));
        });
    }

    [Test]
    public void Compute_FrozenSets_MatchCPython()
    {
        Assert.Multiple(() =>
        {
            // Python: hash(frozenset())
            Assert.That(FrozenSetHash(), Is.EqualTo(133146708735736L));

            // Python: hash(frozenset([1, 2, 3]))
            Assert.That(FrozenSetHash(1L, 2L, 3L), Is.EqualTo(-272375401224217160L));

            // Python: hash(frozenset([1, 'a', 2.5, 7]))
            Assert.That(FrozenSetHash(1L, "a", 2.5, 7L), Is.EqualTo(5446967699369502833L));

            // Python: hash(frozenset(['x', 'y', 'zz']))
            Assert.That(FrozenSetHash("x", "y", "zz"), Is.EqualTo(-1351251946556640442L));

            // Python: hash(frozenset(range(50)))
            Assert.That(FrozenSetHash(Enumerable.Range(0, 50).Select(i => (object)(long)i).ToArray()),
                Is.EqualTo(-1309786936372247613L));
        });
    }

    static long FrozenSetHash(params object[] items)
    {
        var set = new SourceSet(isFrozen: true);

        foreach (var item in items)
        {
            set.Add(new SourceValue(item));
        }

        return PythonHash.Compute(new SourceValue(set));
    }
}
