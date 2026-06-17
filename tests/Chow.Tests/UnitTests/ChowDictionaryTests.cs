using Chow.VM;

namespace Chow.Tests.UnitTests;

[TestFixture]
public class ChowDictionaryTests
{
    #region Helpers

    static ChowDictionary CreateDictionaryWithEntry(string key, string value)
    {
        var dictionary = new ChowDictionary();
        dictionary[key] = value;
        return dictionary;
    }

    #endregion

    #region Construction and Length

    [Test]
    public void Constructor_NoArgs_ReturnsEmptyDictionary()
    {
        var dictionary = new ChowDictionary();

        Assert.That(dictionary.Length, Is.Zero);
    }

    [Test]
    public void Length_AfterIndexerSet_ReturnsOne()
    {
        var dictionary = new ChowDictionary();

        dictionary["key"] = "@object";

        Assert.That(dictionary.Length, Is.EqualTo(1));
    }

    #endregion

    #region Indexer

    [Test]
    public void IndexerGet_ExistingKey_ReturnsStoredValue()
    {
        var dictionary = CreateDictionaryWithEntry("key", "@object");

        Assert.That(dictionary["key"] == "@object", Is.True);
    }

    [Test]
    public void IndexerSet_NewKey_StoresValue()
    {
        var dictionary = new ChowDictionary();

        dictionary["key"] = "@object";

        Assert.Multiple(() =>
        {
            Assert.That(dictionary["key"] == "@object", Is.True);
            Assert.That(dictionary.Length, Is.EqualTo(1));
        });
    }

    #endregion

    #region Get

    [Test]
    public void Get_ExistingKey_ReturnsValue()
    {
        // Python: {"key": "@object"}.get("key") -> "@object"
        var dictionary = CreateDictionaryWithEntry("key", "@object");

        Assert.That(dictionary.Get("key") == "@object", Is.True);
    }

    [Test]
    public void Get_MissingKey_ReturnsNone()
    {
        // Python: {}.get("missing") -> None
        var dictionary = new ChowDictionary();

        Assert.That(dictionary.Get("missing") == ChowObject.None, Is.True);
    }

    #endregion

    #region Pop

    [Test]
    public void Pop_ExistingKey_RemovesEntryAndReturnsValue()
    {
        // Python: {"key": "@object"}.pop("key") -> "@object"
        var dictionary = CreateDictionaryWithEntry("key", "@object");

        var popped = dictionary.Pop("key");

        Assert.Multiple(() =>
        {
            Assert.That(popped == "@object", Is.True);
            Assert.That(dictionary.Length, Is.Zero);
        });
    }

    [Test]
    public void Pop_MissingKey_ThrowsKeyError()
    {
        // Python: {}.pop("missing") -> KeyError
        var dictionary = new ChowDictionary();

        Assert.Throws<SubscriptException>(() => dictionary.Pop("missing"));
    }

    #endregion

    #region Update

    [Test]
    public void Update_OtherDictionary_MergesEntries()
    {
        // Python: {"left": "old"}.update({"right": "new"}) -> None
        var target = CreateDictionaryWithEntry("left", "old");
        var other = CreateDictionaryWithEntry("right", "new");

        target.Update(other);

        Assert.Multiple(() =>
        {
            Assert.That(target["left"] == "old", Is.True);
            Assert.That(target["right"] == "new", Is.True);
            Assert.That(target.Length, Is.EqualTo(2));
        });
    }

    #endregion

    #region Clear

    [Test]
    public void Clear_PopulatedDictionary_EmptiesDictionary()
    {
        var dictionary = CreateDictionaryWithEntry("key", "@object");

        dictionary.Clear();

        Assert.That(dictionary.Length, Is.Zero);
    }

    #endregion

    #region Implicit Operators and ToString

    [Test]
    public void ImplicitToChowValue_RoundTripsSameBackingObject()
    {
        var dictionary = new ChowDictionary();

        ChowObject @object = dictionary;
        @object["key"] = "@object";

        Assert.That(dictionary["key"] == "@object", Is.True);
    }

    [Test]
    public void ImplicitFromChowValue_WrapsExistingDictionary()
    {
        ChowObject @object = ChowObject.CreateDictionary();
        @object["key"] = "@object";

        ChowDictionary dictionary = @object;

        Assert.That(dictionary["key"] == "@object", Is.True);
    }

    [Test]
    public void ToString_EmptyDictionary_MatchesChowValueRepresentation()
    {
        var dictionary = new ChowDictionary();

        Assert.That(dictionary.ToString(), Is.EqualTo(ChowObject.CreateDictionary().ToString()));
    }

    #endregion
}
