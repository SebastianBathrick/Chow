using Chow.Code;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11.0 repr().
/// </summary>
public class StringRepresentationTests
{
    [TestCase("abc", "'abc'")]
    [TestCase("", "''")]
    [TestCase("it's", "\"it's\"")]
    [TestCase("say \"hi\"", "'say \"hi\"'")]
    [TestCase("both ' and \"", "'both \\' and \"'")]
    [TestCase("a\tb\nc\rd", "'a\\tb\\nc\\rd'")]
    [TestCase("back\\slash", "'back\\\\slash'")]
    public void Format_AsciiString_MatchesPythonRepr(string value, string expected)
    {
        Assert.That(StringRepresentation.Format(value), Is.EqualTo(expected));
    }

    // Built from code points so this source file stays plain ASCII.
    static IEnumerable<TestCaseData> NonAsciiCases()
    {
        // Control characters
        yield return new TestCaseData(FromCodePoints(0x01, 0x7f), "'\\x01\\x7f'");

        // Latin-1 control and no-break space
        yield return new TestCaseData(FromCodePoints(0x85, 0xa0), "'\\x85\\xa0'");

        // Printable non-ASCII stays as-is
        yield return new TestCaseData(FromCodePoints(0x63, 0xe9), "'" + FromCodePoints(0x63, 0xe9) + "'");

        // Zero-width space (Cf), line separator (Zl), private use (Co)
        yield return new TestCaseData(FromCodePoints(0x200b, 0x2028, 0xe000), "'\\u200b\\u2028\\ue000'");

        // Lone surrogate
        yield return new TestCaseData(((char)0xd800).ToString(), "'\\ud800'");

        // Printable astral character (emoji) stays as-is
        yield return new TestCaseData(FromCodePoints(0x1f600), "'" + FromCodePoints(0x1f600) + "'");

        // Unprintable astral character (language tag, Cf)
        yield return new TestCaseData(FromCodePoints(0xe0001), "'\\U000e0001'");
    }

    [TestCaseSource(nameof(NonAsciiCases))]
    public void Format_NonAsciiString_MatchesPythonRepr(string value, string expected)
    {
        Assert.That(StringRepresentation.Format(value), Is.EqualTo(expected));
    }

    static string FromCodePoints(params int[] codePoints)
    {
        return string.Concat(codePoints.Select(char.ConvertFromUtf32));
    }
}
