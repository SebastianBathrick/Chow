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

        dictionary["key"] = "value";

        Assert.That(dictionary.Length, Is.EqualTo(1));
    }

    #endregion

    #region Indexer

    [Test]
    public void IndexerGet_ExistingKey_ReturnsStoredValue()
    {
        var dictionary = CreateDictionaryWithEntry("key", "value");

        Assert.That(dictionary["key"] == "value", Is.True);
    }

    [Test]
    public void IndexerSet_NewKey_StoresValue()
    {
        var dictionary = new ChowDictionary();

        dictionary["key"] = "value";

        Assert.Multiple(() =>
        {
            Assert.That(dictionary["key"] == "value", Is.True);
            Assert.That(dictionary.Length, Is.EqualTo(1));
        });
    }

    #endregion

    #region Get

    [Test]
    public void Get_ExistingKey_ReturnsValue()
    {
        // Python: {"key": "value"}.get("key") -> "value"
        var dictionary = CreateDictionaryWithEntry("key", "value");

        Assert.That(dictionary.Get("key") == "value", Is.True);
    }

    [Test]
    public void Get_MissingKey_ReturnsNone()
    {
        // Python: {}.get("missing") -> None
        var dictionary = new ChowDictionary();

        Assert.That(dictionary.Get("missing") == ChowValue.None, Is.True);
    }

    [Test]
    public void Get_MissingKeyWithDefault_ReturnsDefault()
    {
        // Python: {}.get("missing", 0) -> 0
        var dictionary = new ChowDictionary();

        Assert.That(dictionary.Get("missing", 0L) == 0L, Is.True);
    }

    #endregion

    #region Pop

    [Test]
    public void Pop_ExistingKey_RemovesEntryAndReturnsValue()
    {
        // Python: {"key": "value"}.pop("key") -> "value"
        var dictionary = CreateDictionaryWithEntry("key", "value");

        var popped = dictionary.Pop("key");

        Assert.Multiple(() =>
        {
            Assert.That(popped == "value", Is.True);
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

    [Test]
    public void Pop_MissingKeyWithDefault_ReturnsDefaultWithoutRemoving()
    {
        // Python: {}.pop("missing", "default") -> "default"
        var dictionary = new ChowDictionary();

        var popped = dictionary.Pop("missing", "default");

        Assert.Multiple(() =>
        {
            Assert.That(popped == "default", Is.True);
            Assert.That(dictionary.Length, Is.Zero);
        });
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

    #region SetDefault

    [Test]
    public void SetDefault_NewKey_InsertsAndReturnsDefault()
    {
        // Python: {}.setdefault("key") -> None
        var dictionary = new ChowDictionary();

        var result = dictionary.SetDefault("key");

        Assert.Multiple(() =>
        {
            Assert.That(result == ChowValue.None, Is.True);
            Assert.That(dictionary["key"] == ChowValue.None, Is.True);
            Assert.That(dictionary.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void SetDefault_ExistingKey_ReturnsExistingWithoutOverwrite()
    {
        // Python: {"key": "existing"}.setdefault("key", "default") -> "existing"
        var dictionary = CreateDictionaryWithEntry("key", "existing");

        var result = dictionary.SetDefault("key", "default");

        Assert.Multiple(() =>
        {
            Assert.That(result == "existing", Is.True);
            Assert.That(dictionary["key"] == "existing", Is.True);
            Assert.That(dictionary.Length, Is.EqualTo(1));
        });
    }

    #endregion

    #region Clear

    [Test]
    public void Clear_PopulatedDictionary_EmptiesDictionary()
    {
        var dictionary = CreateDictionaryWithEntry("key", "value");

        dictionary.Clear();

        Assert.That(dictionary.Length, Is.Zero);
    }

    #endregion

    #region Implicit Operators and ToString

    [Test]
    public void ImplicitToChowValue_RoundTripsSameBackingObject()
    {
        var dictionary = new ChowDictionary();

        ChowValue value = dictionary;
        value["key"] = "value";

        Assert.That(dictionary["key"] == "value", Is.True);
    }

    [Test]
    public void ImplicitFromChowValue_WrapsExistingDictionary()
    {
        ChowValue value = ChowValue.CreateDictionary();
        value["key"] = "value";

        ChowDictionary dictionary = value;

        Assert.That(dictionary["key"] == "value", Is.True);
    }

    [Test]
    public void ToString_EmptyDictionary_MatchesChowValueRepresentation()
    {
        var dictionary = new ChowDictionary();

        Assert.That(dictionary.ToString(), Is.EqualTo(ChowValue.CreateDictionary().ToString()));
    }

    #endregion
}
