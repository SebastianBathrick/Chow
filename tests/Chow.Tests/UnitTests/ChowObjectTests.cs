using Chow;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;

namespace Chow.Tests.UnitTests;

[TestFixture]
public class ChowObjectTests
{
    #region Helpers

    static ChowObject CreateEmptyList()
    {
        return new(new(new SourceList()));
    }

    static ChowObject CreateEmptyDictionary()
    {
        return new(new(new SourceDict()));
    }

    static bool IsSameAtListIndex(ChowObject list, int index, ChowObject item)
    {
        return list[index] == item;
    }

    static ChowObject CreateOneElementList(string item)
    {
        var list = ChowObject.CreateList();
        list.Call("append", item);
        return list;
    }

    static ChowObject CreateDictionaryWithEntry(string key, string value)
    {
        var dictionary = ChowObject.CreateDictionary();
        dictionary[key] = value;
        return dictionary;
    }

    #endregion

    #region Properties

    [Test]
    public void None_Singleton_EqualsSelfAndNoneConstant()
    {
        var none = ChowObject.None;

        Assert.That(none == ChowObject.None, Is.True);
    }

    [Test]
    public void None_NotEqualToNullChowValueReference()
    {
        ChowObject nullObject = null!;

        Assert.That(nullObject != ChowObject.None, Is.True);
    }

    [Test]
    public void Length_EmptyList_ReturnsZero()
    {
        var list = ChowObject.CreateList();

        Assert.That(list.Length, Is.Zero);
    }

    [Test]
    public void Length_ListAfterTwoAppends_ReturnsTwo()
    {
        var list = ChowObject.CreateList();

        list.Call("append", "first");
        list.Call("append", "second");

        Assert.That(list.Length, Is.EqualTo(2));
    }

    [Test]
    public void Length_EmptyDictionary_ReturnsZero()
    {
        var dictionary = ChowObject.CreateDictionary();

        Assert.That(dictionary.Length, Is.Zero);
    }

    [Test]
    public void Length_DictionaryAfterOneEntry_ReturnsOne()
    {
        var dictionary = CreateDictionaryWithEntry("key", "@object");

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
        var dictionary = CreateDictionaryWithEntry("key", "@object");

        Assert.That(dictionary["key"] == "@object", Is.True);
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
        var dictionary = ChowObject.CreateDictionary();

        dictionary["key"] = "@object";

        Assert.That(dictionary["key"] == "@object", Is.True);
    }

    #endregion

    #region Factory Methods
    
    [Test]
    public void CreateList_NoArgs_ReturnEmptyList()
    {
        var list = ChowObject.CreateList();
        
        Assert.That(list == CreateEmptyList(), Is.True);
    }

    [Test]
    public void CreateDictionary_NoArgs_ReturnEmptyDictionary()
    {
        var dictionary = ChowObject.CreateDictionary();

        Assert.That(dictionary == CreateEmptyDictionary(), Is.True);
    }

    #endregion

    #region Attribute Methods

    [Test]
    public void GetAttribute_ListAppendMethod_ReturnsNonNoneValue()
    {
        var list = ChowObject.CreateList();

        Assert.That(list.GetAttribute("append") != ChowObject.None, Is.True);
    }

    [Test]
    public void GetAttribute_TwiceSameName_ReturnsEqualValues()
    {
        var list = ChowObject.CreateList();

        var first = list.GetAttribute("append");
        var second = list.GetAttribute("append");

        Assert.That(first == second, Is.True);
    }

    #endregion

    #region Call Self Method Methods

    [Test]
    public void Call_SingleItemListAppend_ListWithSingleItem()
    {
        var list = ChowObject.CreateList();
        var item = "item";
        
        list.Call("append", item);
        
        Assert.That(IsSameAtListIndex(list, 0, item), Is.True);
    }

    [Test]
    public void Call_DictionaryClear_EmptiesDictionary()
    {
        var dictionary = CreateDictionaryWithEntry("key", "@object");

        dictionary.Call("clear");

        Assert.That(dictionary.Length, Is.Zero);
    }

    #endregion

    #region As Method

    [Test]
    public void As_BoolTrue_ReturnsTrue()
    {
        ChowObject @object = true;

        Assert.That(@object.As<bool>(), Is.True);
    }

    [Test]
    public void As_LongValue_ReturnsSameLong()
    {
        ChowObject @object = 42L;

        Assert.That(@object.As<long>(), Is.EqualTo(42L));
    }

    [Test]
    public void As_DoubleValue_ReturnsSameDouble()
    {
        ChowObject @object = 3.5;

        Assert.That(@object.As<double>(), Is.EqualTo(3.5));
    }

    [Test]
    public void As_StringValue_ReturnsSameString()
    {
        ChowObject @object = "hello";

        Assert.That(@object.As<string>(), Is.EqualTo("hello"));
    }

    [Test]
    public void As_None_ReturnsNull()
    {
        Assert.That(ChowObject.None.As<object>(), Is.Null);
    }

    [Test]
    public void As_StringOnLong_ThrowsInvalidCastException()
    {
        ChowObject @object = 42L;

        Assert.Throws<InvalidCastException>(() => @object.As<string>());
    }

    #endregion

    #region Implicit Operators

    [Test]
    public void ImplicitToChowValue_Bool_RoundTripsViaConversion()
    {
        ChowObject @object = true;

        Assert.That((bool)@object, Is.True);
    }

    [Test]
    public void ImplicitToChowValue_Long_RoundTripsViaConversion()
    {
        ChowObject @object = 42L;

        Assert.That((long)@object, Is.EqualTo(42L));
    }

    [Test]
    public void ImplicitToChowValue_Double_RoundTripsViaConversion()
    {
        ChowObject @object = 3.5;

        Assert.That((double)@object, Is.EqualTo(3.5));
    }

    [Test]
    public void ImplicitToChowValue_String_RoundTripsViaConversion()
    {
        ChowObject @object = "hello";

        Assert.That(@object.ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void ImplicitFromChowValue_Bool_MatchesExpectedScalar()
    {
        ChowObject @object = true;

        bool scalar = @object;

        Assert.That(scalar, Is.True);
    }

    [Test]
    public void ImplicitFromChowValue_Long_MatchesExpectedScalar()
    {
        ChowObject @object = 42L;

        long scalar = @object;

        Assert.That(scalar, Is.EqualTo(42L));
    }

    [Test]
    public void ImplicitFromChowValue_Double_MatchesExpectedScalar()
    {
        ChowObject @object = 3.5;

        double scalar = @object;

        Assert.That(scalar, Is.EqualTo(3.5));
    }

    [Test]
    public void ImplicitFromChowValue_String_MatchesExpectedScalar()
    {
        ChowObject @object = "hello";

        string scalar = @object.As<string>();

        Assert.That(scalar, Is.EqualTo("hello"));
    }

    [Test]
    public void Equality_SameReference_ReturnsTrue()
    {
        ChowObject @object = "same";
        var sameReference = @object;

        Assert.That(@object == sameReference, Is.True);
    }

    [Test]
    public void Equality_EqualScalars_ReturnsTrue()
    {
        ChowObject left = 42L;
        ChowObject right = 42L;

        Assert.That(left == right, Is.True);
    }

    [Test]
    public void Equality_DifferentScalars_ReturnsFalse()
    {
        ChowObject left = 42L;
        ChowObject right = 43L;

        Assert.That(left == right, Is.False);
    }

    [Test]
    public void Equality_NullReferences_ReturnsTrue()
    {
        ChowObject left = null!;
        ChowObject right = null!;

        Assert.That(left == right, Is.True);
    }

    [Test]
    public void Equality_OneNullOneValue_ReturnsFalse()
    {
        ChowObject left = null!;
        ChowObject right = 42L;

        Assert.That(left == right, Is.False);
    }

    [Test]
    public void Inequality_DifferentScalars_ReturnsTrue()
    {
        ChowObject left = 42L;
        ChowObject right = 43L;

        Assert.That(left != right, Is.True);
    }

    [Test]
    public void Equality_ChowValueBool_MatchesValue()
    {
        ChowObject trueObject = true;
        ChowObject falseObject = false;
        ChowObject longObject = 42L;

        Assert.Multiple(() =>
        {
            Assert.That(trueObject == true, Is.True);
            Assert.That(falseObject == false, Is.True);
            Assert.That(longObject == false, Is.False);
        });
    }

    [Test]
    public void Equality_ChowValueLong_MatchesValue()
    {
        ChowObject @object = 42L;

        Assert.Multiple(() =>
        {
            Assert.That(@object == 42L, Is.True);
            Assert.That(@object != 43L, Is.True);
        });
    }

    [Test]
    public void Equality_ChowValueDouble_MatchesValue()
    {
        ChowObject doubleObject = 3.5;
        ChowObject longObject = 1L;

        Assert.Multiple(() =>
        {
            Assert.That(doubleObject == 3.5, Is.True);
            Assert.That(doubleObject != 4.5, Is.True);
            Assert.That(longObject == 1.0, Is.True);
        });
    }

    #endregion

    #region Equality Methods

    [Test]
    public void Equals_BoxedChowValue_ReturnsTrueForEqualValues()
    {
        ChowObject left = 42L;
        object right = (ChowObject)42L;

        Assert.That(left, Is.EqualTo(right));
    }

    [Test]
    public void GetHashCode_EqualValues_SameHashCode()
    {
        ChowObject left = 42L;
        ChowObject right = 42L;

        Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    }

    #endregion

    #region ToString Method

    [Test]
    public void ToString_None_ReturnsNoneLiteral()
    {
        Assert.That(ChowObject.None.ToString(), Is.EqualTo("None"));
    }

    [Test]
    public void ToString_BoolTrue_ReturnsTrueLiteral()
    {
        ChowObject @object = true;

        Assert.That(@object.ToString(), Is.EqualTo("True"));
    }

    [Test]
    public void ToString_Long_ReturnsInvariantString()
    {
        ChowObject @object = 42L;

        Assert.That(@object.ToString(), Is.EqualTo("42"));
    }

    [Test]
    public void ToString_String_ReturnsSameString()
    {
        ChowObject @object = "hello";

        Assert.That(@object.ToString(), Is.EqualTo("hello"));
    }

    [Test]
    public void ToString_EmptyList_MatchesSourceRepresentation()
    {
        var list = ChowObject.CreateList();

        Assert.That(list.ToString(), Is.EqualTo(CreateEmptyList().ToString()));
    }

    #endregion

    #region Calling Chow-Defined Methods

    // Runs source in a fresh scope and hands back one of the variables it defined.
    static ChowObject RunAndGet(string sourceCode, string variableName)
    {
        var scope = new ChowScope();
        ChowEngine.Run(sourceCode, scope);

        return scope[variableName];
    }

    static ChowObject CreateCounterInstance()
    {
        return RunAndGet(
            """
            class Counter:
                def __init__(self, start):
                    self.value = start

                def read(self):
                    return self.value

                def bump(self, amount):
                    self.value = self.value + amount

                def bump_twice(self, amount):
                    self.bump(amount)
                    self.bump(amount)

            counter = Counter(5)
            """,
            "counter");
    }

    [Test]
    public void Call_MethodWithArgument_ReturnsResult()
    {
        var instance = RunAndGet(
            """
            class Scaler:
                def __init__(self, factor):
                    self.factor = factor

                def scale(self, value):
                    return self.factor * value

            scaler = Scaler(3)
            """,
            "scaler");

        Assert.That(instance.Call("scale", 4L), Is.EqualTo((ChowObject)12L));
    }

    [Test]
    public void Call_MethodWithNoArguments_ReturnsResult()
    {
        var counter = CreateCounterInstance();

        Assert.That(counter.Call("read"), Is.EqualTo((ChowObject)5L));
    }

    [Test]
    public void Call_MethodWithoutReturn_ReturnsNone()
    {
        var counter = CreateCounterInstance();

        Assert.That(counter.Call("bump", 1L), Is.EqualTo(ChowObject.None));
    }

    [Test]
    public void Call_MutatingMethod_UpdatesInstanceState()
    {
        var counter = CreateCounterInstance();

        counter.Call("bump", 3L);

        Assert.That(counter.GetAttribute("value"), Is.EqualTo((ChowObject)8L));
    }

    // The receiver has to survive a call the method makes back through self.
    [Test]
    public void Call_MethodCallingAnotherMethod_AppliesBothCalls()
    {
        var counter = CreateCounterInstance();

        counter.Call("bump_twice", 10L);

        Assert.That(counter.Call("read"), Is.EqualTo((ChowObject)25L));
    }

    // A host call has no surrounding frame, so the module scope is recovered from the closure.
    [Test]
    public void Call_MethodUsingGlobal_ResolvesAgainstModuleScope()
    {
        var scope = new ChowScope();

        ChowEngine.Run(
            """
            tally = 0

            class Recorder:
                def record(self, amount):
                    global tally
                    tally = tally + amount
                    return tally

            recorder = Recorder()
            """,
            scope);

        ChowObject recorder = scope["recorder"];
        recorder.Call("record", 4L);

        Assert.That(recorder.Call("record", 6L), Is.EqualTo((ChowObject)10L));
        Assert.That(scope["tally"], Is.EqualTo((ChowObject)10L));
    }

    [Test]
    public void Call_MethodWithWrongArgumentCount_ThrowsDataTypeException()
    {
        var counter = CreateCounterInstance();

        Assert.Throws<DataTypeException>(() => counter.Call("read", 1L));
    }

    [Test]
    public void Call_UndefinedMethodName_ThrowsAttributeException()
    {
        var counter = CreateCounterInstance();

        Assert.Throws<AttributeException>(() => counter.Call("missing"));
    }

    #endregion
}
