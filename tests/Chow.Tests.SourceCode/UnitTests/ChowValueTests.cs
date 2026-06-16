using Chow;
using Chow.SourceData;

namespace Chow.Tests.SourceCode.UnitTests;

[TestFixture]
public class ChowValueTests
{
    static ChowValue CreateEmptyList()
    {
        return new(new(new SourceList()));
    }

    static ChowValue CreateEmptyDictionary()
    {
        return new(new(new SourceDictionary()));
    }

    static bool IsSameAtListIndex(ChowValue list, int index, ChowValue item)
    {
        return list[index] == item;
    }

    static ChowValue CreateOneElementList(string item)
    {
        var list = ChowValue.CreateList();
        list.Call("append", new ChowValue(item));
        return list;
    }
    
    [Test]
    public void CreateList_NoArgs_ReturnEmptyList()
    {
        var list = ChowValue.CreateList();
        
        Assert.That(list == CreateEmptyList(), Is.True);
    }

    [Test]
    public void CreateDictionary_NoArgs_ReturnEmptyDictionary()
    {
        var dictionary = ChowValue.CreateDictionary();

        Assert.That(dictionary == CreateEmptyDictionary(), Is.True);
    }

    [Test]
    public void Call_SingleItemListAppend_ListWithSingleItem()
    {
        var list = ChowValue.CreateList();
        var item = "item";
        
        list.Call("append", item);
        
        Assert.That(IsSameAtListIndex(list, 0, item), Is.True);
    }

    [Test]
    public void SetIndexer_InsertAtValidListIndex_AssignedItemAtValidListIndex()
    {
        var list = CreateOneElementList("wrong");

        list[0] = "correct";
        
        Assert.That(list[0] == "correct", Is.True);
    }
}
