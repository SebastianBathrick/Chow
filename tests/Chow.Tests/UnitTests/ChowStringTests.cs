using Chow;

namespace Chow.Tests.UnitTests;

[TestFixture]
public class ChowStringTests
{
    [Test]
    public void ImplicitFromHostString_RoundTripsBackToHostString()
    {
        ChowString chowString = "hello";

        string hostString = chowString;

        Assert.That(hostString, Is.EqualTo("hello"));
    }

    [Test]
    public void ImplicitToChowObject_PreservesValue()
    {
        ChowString chowString = "hello";

        ChowObject chowObject = chowString;

        Assert.That(chowObject.ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void ImplicitFromChowObject_PreservesValue()
    {
        ChowObject chowObject = "hello";

        ChowString chowString = chowObject;

        Assert.That(chowString.ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void Length_MatchesCharacterCount()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.Length, Is.EqualTo(5));
    }

    [Test]
    public void Indexer_ReturnsSingleCharacterString()
    {
        ChowString chowString = "hello";

        Assert.That(chowString[1].ToString(), Is.EqualTo("e"));
    }

    [Test]
    public void IsString_IsTrue()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.IsString, Is.True);
    }

    [Test]
    public void IsString_IsFalseForList()
    {
        var list = new ChowList();

        Assert.That(list.IsString, Is.False);
    }

    [Test]
    public void IsString_IsTrueForStringTypedChowObject()
    {
        ChowObject chowObject = "hello";

        Assert.That(chowObject.IsString, Is.True);
    }

    [Test]
    public void Contains_FindsSubstring()
    {
        ChowString chowString = "hello world";

        Assert.That(chowString.Contains("o w"), Is.True);
    }

    [Test]
    public void StartsWith_MatchesPrefix()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.StartsWith("hel"), Is.True);
    }

    [Test]
    public void EndsWith_MatchesSuffix()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.EndsWith("llo"), Is.True);
    }

    [Test]
    public void IndexOf_ReturnsPosition()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.IndexOf("ll"), Is.EqualTo(2));
    }

    [Test]
    public void IndexOf_ReturnsNegativeOneWhenAbsent()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.IndexOf("z"), Is.EqualTo(-1));
    }

    [Test]
    public void Substring_ReturnsRequestedSlice()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.Substring(1, 3).ToString(), Is.EqualTo("ell"));
    }

    [Test]
    public void ToUpper_UppercasesValue()
    {
        ChowString chowString = "hello";

        Assert.That(chowString.ToUpper().ToString(), Is.EqualTo("HELLO"));
    }

    [Test]
    public void ToLower_LowercasesValue()
    {
        ChowString chowString = "HELLO";

        Assert.That(chowString.ToLower().ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void PassedAsCallArgument_DoesNotThrow()
    {
        // Exercises ApiConverter.GetWrappedChowObject for ChowString via a method call that takes
        // an IChowObject-derived argument.
        var list = ChowObject.CreateList();
        ChowString item = "hello";

        Assert.DoesNotThrow(() => list.Call("append", item));
        Assert.That(list[0L].ToString(), Is.EqualTo("hello"));
    }
}
