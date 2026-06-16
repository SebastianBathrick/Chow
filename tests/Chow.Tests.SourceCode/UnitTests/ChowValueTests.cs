using Chow;
using Chow.SourceData;

namespace Chow.Tests.SourceCode.UnitTests;

[TestFixture]
public class ChowValueTests
{
    #region Helpers

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
        list.Call("append", item);
        return list;
    }

    static ChowValue CreateDictionaryWithEntry(string key, string value)
    {
        var dictionary = ChowValue.CreateDictionary();
        dictionary[key] = value;
        return dictionary;
    }

    #endregion

    #region Properties

    [Test]
    public void None_Singleton_EqualsSelfAndNoneConstant()
    {
        var none = ChowValue.None;

        Assert.That(none == ChowValue.None, Is.True);
    }

    [Test]
    public void None_NotEqualToNullChowValueReference()
    {
        ChowValue nullValue = null!;

        Assert.That(nullValue != ChowValue.None, Is.True);
    }

    [Test]
    public void Length_EmptyList_ReturnsZero()
    {
        var list = ChowValue.CreateList();

        Assert.That(list.Length, Is.Zero);
    }

    [Test]
    public void Length_ListAfterTwoAppends_ReturnsTwo()
    {
        var list = ChowValue.CreateList();

        list.Call("append", "first");
        list.Call("append", "second");

        Assert.That(list.Length, Is.EqualTo(2));
    }

    [Test]
    public void Length_EmptyDictionary_ReturnsZero()
    {
        var dictionary = ChowValue.CreateDictionary();

        Assert.That(dictionary.Length, Is.Zero);
    }

    [Test]
    public void Length_DictionaryAfterOneEntry_ReturnsOne()
    {
        var dictionary = CreateDictionaryWithEntry("key", "value");

        Assert.That(dictionary.Length, Is.EqualTo(1));
    }

    [Test]
    public void IndexerGet_ListIndex_ReturnsStoredItem()
    {
        var list = CreateOneElementList("item");

        Assert.That(list[0L] == "item", Is.True);
    }

    [Test]
    public void IndexerGet_DictionaryKey_ReturnsStoredValue()
    {
        var dictionary = CreateDictionaryWithEntry("key", "value");

        Assert.That(dictionary["key"] == "value", Is.True);
    }

    [Test]
    public void SetIndexer_InsertAtValidListIndex_AssignedItemAtValidListIndex()
    {
        var list = CreateOneElementList("wrong");

        list[0L] = "correct";
        
        Assert.That(list[0L] == "correct", Is.True);
    }

    [Test]
    public void SetIndexer_DictionaryKey_AssignedValueAtKey()
    {
        var dictionary = ChowValue.CreateDictionary();

        dictionary["key"] = "value";

        Assert.That(dictionary["key"] == "value", Is.True);
    }

    #endregion

    #region Factory Methods
    
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

    #endregion

    #region Attribute Methods

    [Test]
    public void GetAttribute_ListAppendMethod_ReturnsNonNoneValue()
    {
        var list = ChowValue.CreateList();

        Assert.That(list.GetAttribute("append") != ChowValue.None, Is.True);
    }

    [Test]
    public void GetAttribute_TwiceSameName_ReturnsEqualValues()
    {
        var list = ChowValue.CreateList();

        var first = list.GetAttribute("append");
        var second = list.GetAttribute("append");

        Assert.That(first == second, Is.True);
    }

    #endregion

    #region Call Self Method Methods

    [Test]
    public void Call_SingleItemListAppend_ListWithSingleItem()
    {
        var list = ChowValue.CreateList();
        var item = "item";
        
        list.Call("append", item);
        
        Assert.That(IsSameAtListIndex(list, 0, item), Is.True);
    }

    [Test]
    public void Call_DictionaryClear_EmptiesDictionary()
    {
        var dictionary = CreateDictionaryWithEntry("key", "value");

        dictionary.Call("clear");

        Assert.That(dictionary.Length, Is.Zero);
    }

    #endregion

    #region As Method

    [Test]
    public void As_BoolTrue_ReturnsTrue()
    {
        ChowValue value = true;

        Assert.That(value.As<bool>(), Is.True);
    }

    [Test]
    public void As_LongValue_ReturnsSameLong()
    {
        ChowValue value = 42L;

        Assert.That(value.As<long>(), Is.EqualTo(42L));
    }

    [Test]
    public void As_DoubleValue_ReturnsSameDouble()
    {
        ChowValue value = 3.5;

        Assert.That(value.As<double>(), Is.EqualTo(3.5));
    }

    [Test]
    public void As_StringValue_ReturnsSameString()
    {
        ChowValue value = "hello";

        Assert.That(value.As<string>(), Is.EqualTo("hello"));
    }

    [Test]
    public void As_None_ReturnsNull()
    {
        Assert.That(ChowValue.None.As<object>(), Is.Null);
    }

    [Test]
    public void As_StringOnLong_ThrowsInvalidCastException()
    {
        ChowValue value = 42L;

        Assert.Throws<InvalidCastException>(() => value.As<string>());
    }

    #endregion

    #region Implicit Operators

    [Test]
    public void ImplicitToChowValue_Bool_RoundTripsViaConversion()
    {
        ChowValue value = true;

        Assert.That((bool)value, Is.True);
    }

    [Test]
    public void ImplicitToChowValue_Long_RoundTripsViaConversion()
    {
        ChowValue value = 42L;

        Assert.That((long)value, Is.EqualTo(42L));
    }

    [Test]
    public void ImplicitToChowValue_Double_RoundTripsViaConversion()
    {
        ChowValue value = 3.5;

        Assert.That((double)value, Is.EqualTo(3.5));
    }

    [Test]
    public void ImplicitToChowValue_String_RoundTripsViaConversion()
    {
        ChowValue value = "hello";

        Assert.That((string)value, Is.EqualTo("hello"));
    }

    [Test]
    public void ImplicitFromChowValue_Bool_MatchesExpectedScalar()
    {
        ChowValue value = true;

        bool scalar = value;

        Assert.That(scalar, Is.True);
    }

    [Test]
    public void ImplicitFromChowValue_Long_MatchesExpectedScalar()
    {
        ChowValue value = 42L;

        long scalar = value;

        Assert.That(scalar, Is.EqualTo(42L));
    }

    [Test]
    public void ImplicitFromChowValue_Double_MatchesExpectedScalar()
    {
        ChowValue value = 3.5;

        double scalar = value;

        Assert.That(scalar, Is.EqualTo(3.5));
    }

    [Test]
    public void ImplicitFromChowValue_String_MatchesExpectedScalar()
    {
        ChowValue value = "hello";

        string scalar = value;

        Assert.That(scalar, Is.EqualTo("hello"));
    }

    [Test]
    public void Equality_SameReference_ReturnsTrue()
    {
        ChowValue value = "same";
        var sameReference = value;

        Assert.That(value == sameReference, Is.True);
    }

    [Test]
    public void Equality_EqualScalars_ReturnsTrue()
    {
        ChowValue left = 42L;
        ChowValue right = 42L;

        Assert.That(left == right, Is.True);
    }

    [Test]
    public void Equality_DifferentScalars_ReturnsFalse()
    {
        ChowValue left = 42L;
        ChowValue right = 43L;

        Assert.That(left == right, Is.False);
    }

    [Test]
    public void Equality_NullReferences_ReturnsTrue()
    {
        ChowValue left = null!;
        ChowValue right = null!;

        Assert.That(left == right, Is.True);
    }

    [Test]
    public void Equality_OneNullOneValue_ReturnsFalse()
    {
        ChowValue left = null!;
        ChowValue right = 42L;

        Assert.That(left == right, Is.False);
    }

    [Test]
    public void Inequality_DifferentScalars_ReturnsTrue()
    {
        ChowValue left = 42L;
        ChowValue right = 43L;

        Assert.That(left != right, Is.True);
    }

    [Test]
    public void Equality_ChowValueBool_MatchesValue()
    {
        ChowValue trueValue = true;
        ChowValue falseValue = false;
        ChowValue longValue = 42L;

        Assert.Multiple(() =>
        {
            Assert.That(trueValue == true, Is.True);
            Assert.That(falseValue == false, Is.True);
            Assert.That(longValue == false, Is.False);
        });
    }

    [Test]
    public void Equality_ChowValueLong_MatchesValue()
    {
        ChowValue value = 42L;

        Assert.Multiple(() =>
        {
            Assert.That(value == 42L, Is.True);
            Assert.That(value != 43L, Is.True);
        });
    }

    [Test]
    public void Equality_ChowValueDouble_MatchesValue()
    {
        ChowValue doubleValue = 3.5;
        ChowValue longValue = 1L;

        Assert.Multiple(() =>
        {
            Assert.That(doubleValue == 3.5, Is.True);
            Assert.That(doubleValue != 4.5, Is.True);
            Assert.That(longValue == 1.0, Is.True);
        });
    }

    #endregion

    #region Equality Methods

    [Test]
    public void Equals_BoxedChowValue_ReturnsTrueForEqualValues()
    {
        ChowValue left = 42L;
        object right = (ChowValue)42L;

        Assert.That(left, Is.EqualTo(right));
    }

    [Test]
    public void GetHashCode_EqualValues_SameHashCode()
    {
        ChowValue left = 42L;
        ChowValue right = 42L;

        Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    }

    #endregion

    #region ToString Method

    [Test]
    public void ToString_None_ReturnsNoneLiteral()
    {
        Assert.That(ChowValue.None.ToString(), Is.EqualTo("None"));
    }

    [Test]
    public void ToString_BoolTrue_ReturnsTrueLiteral()
    {
        ChowValue value = true;

        Assert.That(value.ToString(), Is.EqualTo("True"));
    }

    [Test]
    public void ToString_Long_ReturnsInvariantString()
    {
        ChowValue value = 42L;

        Assert.That(value.ToString(), Is.EqualTo("42"));
    }

    [Test]
    public void ToString_String_ReturnsSameString()
    {
        ChowValue value = "hello";

        Assert.That(value.ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void ToString_EmptyList_MatchesSourceRepresentation()
    {
        var list = ChowValue.CreateList();

        Assert.That(list.ToString(), Is.EqualTo(CreateEmptyList().ToString()));
    }

    #endregion
}
