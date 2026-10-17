using Chow.SourceData;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Expected values verified against Python 3.11.0.
/// </summary>
public class SourceIteratorTests
{
    [TestCase("")]
    [TestCase("abc")]
    public void TypeName_AsciiString_IsStrAsciiIterator(string value)
    {
        // Python: type(iter('abc')).__name__ -> 'str_ascii_iterator'
        Assert.That(new SourceStringIterator(value).TypeName, Is.EqualTo("str_ascii_iterator"));
    }

    [Test]
    public void TypeName_NonAsciiString_IsStrIterator()
    {
        // Python: type(iter('a' + chr(0xe9))).__name__ -> 'str_iterator' (built from a code point to keep this file ASCII)
        var value = "a" + char.ConvertFromUtf32(0xe9);

        Assert.That(new SourceStringIterator(value).TypeName, Is.EqualTo("str_iterator"));
    }

    [Test]
    public void TryMoveNext_RangeEndingNearLongMaxValue_StopsWithoutOverflow()
    {
        // Python: list(range(2**63 - 4, 2**63 - 1, 2)) -> [2**63 - 4, 2**63 - 2]. Stepping past the
        // last value would overflow, so the iterator must count values rather than compare bounds.
        var range = new SourceRange(long.MaxValue - 3, long.MaxValue, 2);

        Assert.Multiple(() =>
        {
            Assert.That(Drain(new SourceRangeIterator(range, isReversed: false)),
                Is.EqualTo(new[] { long.MaxValue - 3, long.MaxValue - 1 }));
            Assert.That(Drain(new SourceRangeIterator(range, isReversed: true)),
                Is.EqualTo(new[] { long.MaxValue - 1, long.MaxValue - 3 }));
        });
    }

    static List<long> Drain(SourceIterator iterator)
    {
        var values = new List<long>();

        while (iterator.TryMoveNext(out var value))
        {
            values.Add(value.ToLong());
        }

        return values;
    }
}
