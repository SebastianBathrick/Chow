namespace Chow.Tests.UnitTests;

[TestFixture]
public class ChowListTests
{
    #region Helpers

    static ChowList CreateListWithItems(params string[] items)
    {
        var list = new ChowList();

        foreach (var item in items)
        {
            list.Append(item);
        }

        return list;
    }

    #endregion

    #region Construction and Length

    [Test]
    public void Constructor_NoArgs_ReturnsEmptyList()
    {
        var list = new ChowList();

        Assert.That(list.Length, Is.Zero);
    }

    [Test]
    public void Length_AfterTwoAppends_ReturnsTwo()
    {
        var list = new ChowList();

        list.Append("first");
        list.Append("second");

        Assert.That(list.Length, Is.EqualTo(2));
    }

    #endregion

    #region Indexer

    [Test]
    public void IndexerGet_ValidIndex_ReturnsStoredItem()
    {
        var list = CreateListWithItems("item");

        Assert.That(list[0] == "item", Is.True);
    }

    [Test]
    public void IndexerSet_ValidIndex_UpdatesItem()
    {
        var list = CreateListWithItems("wrong");

        list[0] = "correct";

        Assert.That(list[0] == "correct", Is.True);
    }

    #endregion

    #region Mutation Methods

    [Test]
    public void Append_SingleItem_ItemAtIndexZero()
    {
        // Python: [].append("item") mutates the list
        var list = new ChowList();

        list.Append("item");

        Assert.Multiple(() =>
        {
            Assert.That(list[0] == "item", Is.True);
            Assert.That(list.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void Insert_AtMiddleIndex_ShiftsExistingItems()
    {
        // Python: ["a", "c"].insert(1, "b") -> ["a", "b", "c"]
        var list = CreateListWithItems("a", "c");

        list.Insert(1L, "b");

        Assert.Multiple(() =>
        {
            Assert.That(list[0] == "a", Is.True);
            Assert.That(list[1] == "b", Is.True);
            Assert.That(list[2] == "c", Is.True);
            Assert.That(list.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void Pop_ValidIndex_RemovesAndReturnsItem()
    {
        // Python: ["a", "b"].pop(0) -> "a"
        var list = CreateListWithItems("a", "b");

        var popped = list.Pop(0L);

        Assert.Multiple(() =>
        {
            Assert.That(popped == "a", Is.True);
            Assert.That(list[0] == "b", Is.True);
            Assert.That(list.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void Remove_ExistingValue_RemovesFirstMatch()
    {
        // Python: ["a", "b", "a"].remove("a") -> ["b", "a"]
        var list = CreateListWithItems("a", "b", "a");

        list.Remove("a");

        Assert.Multiple(() =>
        {
            Assert.That(list[0] == "b", Is.True);
            Assert.That(list[1] == "a", Is.True);
            Assert.That(list.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void Reverse_NonEmptyList_ReordersInPlace()
    {
        // Python: ["a", "b"].reverse() -> None
        var list = CreateListWithItems("a", "b");

        var result = list.Reverse();

        Assert.Multiple(() =>
        {
            Assert.That(result == ChowValue.None, Is.True);
            Assert.That(list[0] == "b", Is.True);
            Assert.That(list[1] == "a", Is.True);
        });
    }

    [Test]
    public void Clear_PopulatedList_EmptiesList()
    {
        var list = CreateListWithItems("item");

        list.Clear();

        Assert.That(list.Length, Is.Zero);
    }

    #endregion

    #region Error Paths

    [Test]
    public void Pop_OutOfRangeIndex_Throws()
    {
        // Python: ["item"].pop(1) -> IndexError
        var list = CreateListWithItems("item");

        Assert.Throws<IndexOutOfRangeException>(() => list.Pop(1L));
    }

    [Test]
    public void Remove_ValueNotInList_Throws()
    {
        // Python: ["item"].remove("missing") -> ValueError
        var list = CreateListWithItems("item");

        Assert.Throws<ArgumentException>(() => list.Remove("missing"));
    }

    #endregion

    #region Implicit Operators and ToString

    [Test]
    public void ImplicitToChowValue_RoundTripsSameBackingObject()
    {
        var list = new ChowList();

        ChowValue value = list;
        value.Call("append", "item");

        Assert.That(list[0] == "item", Is.True);
    }

    [Test]
    public void ImplicitFromChowValue_WrapsExistingList()
    {
        ChowValue value = ChowValue.CreateList();
        value.Call("append", "item");

        ChowList list = value;

        Assert.That(list[0] == "item", Is.True);
    }

    [Test]
    public void ToString_EmptyList_MatchesChowValueRepresentation()
    {
        var list = new ChowList();

        Assert.That(list.ToString(), Is.EqualTo(ChowValue.CreateList().ToString()));
    }

    #endregion
}
