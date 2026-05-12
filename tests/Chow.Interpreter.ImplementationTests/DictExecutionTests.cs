using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class DictExecutionTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        sealed class CaptureExprHook : IExprStatementHook
        {
            public List<ChowValue> Values { get; } = new List<ChowValue>();

            public void Invoke(ChowValue value)
            {
                Values.Add(value);
            }
        }

        static (ChowModule module, CaptureExprHook hook) NewModule()
        {
            ChowModule module = new ChowModule();
            CaptureExprHook hook = new CaptureExprHook();
            module.AddHook(hook);
            return (module, hook);
        }

        static ChowValue Last(CaptureExprHook hook) => hook.Values[hook.Values.Count - 1];

        static ChowDict LastDict(CaptureExprHook hook) => (ChowDict)Last(hook);

        // ============================================================================================================
        // A. Literals
        // ============================================================================================================

        [Test]
        public void EmptyDictLiteral_ProducesZeroEntryDict()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{}");
            Assert.That(LastDict(hook).Count, Is.EqualTo(0));
        }

        [Test]
        public void DictLiteral_SinglePair_StoresValue()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'}[1]");
            Assert.That(Last(hook).ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void DictLiteral_PreservesInsertionOrder()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{3: 'a', 1: 'b', 2: 'c'}");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{3: 'a', 1: 'b', 2: 'c'}"));
        }

        [Test]
        public void DictLiteral_MixedKeyTypes_Allowed()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'i', 'k': 's', None: 'n'}");
            Assert.That(LastDict(hook).Count, Is.EqualTo(3));
        }

        [Test]
        public void DictLiteral_NestedDict_ParsesAndExecutes()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: {2: 'inner'}}");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: {2: 'inner'}}"));
        }

        [Test]
        public void DictLiteral_ContainingList_ParsesAndExecutes()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: [10, 20]}");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: [10, 20]}"));
        }

        [Test]
        public void ListLiteral_ContainingDict_ParsesAndExecutes()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("[{1: 'a'}, {2: 'b'}]");
            Assert.That(Last(hook).ToString(), Is.EqualTo("[{1: 'a'}, {2: 'b'}]"));
        }

        // ============================================================================================================
        // B. Subscript read
        // ============================================================================================================

        [Test]
        public void Subscript_ExistingKey_ReadsValue()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 10, 2: 20}[2]");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(20));
        }

        [Test]
        public void Subscript_MissingKey_ThrowsKeyError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("{1: 10}[99]"), Throws.TypeOf<ChowKeyErrorException>());
        }

        [Test]
        public void Subscript_UnhashableKey_ThrowsTypeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("{1: 10}[[1]]"), Throws.TypeOf<ChowTypeErrorException>());
        }

        // ============================================================================================================
        // C. Subscript assign
        // ============================================================================================================

        [Test]
        public void SubscriptAssign_NewKey_AppendsInInsertionOrder()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a'}\nd[2] = 'b'\nd");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'a', 2: 'b'}"));
        }

        [Test]
        public void SubscriptAssign_ExistingKey_OverwritesAndPreservesPosition()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a', 2: 'b'}\nd[1] = 'z'\nd");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'z', 2: 'b'}"));
        }

        // ============================================================================================================
        // D. Methods
        // ============================================================================================================

        [Test]
        public void MethodCall_GetPresent_ReturnsValue()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'}.get(1)");
            Assert.That(Last(hook).ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void MethodCall_GetMissingNoDefault_ReturnsNone()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'}.get(99)");
            Assert.That(Last(hook).IsNone, Is.True);
        }

        [Test]
        public void MethodCall_GetMissingWithDefault_ReturnsDefault()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'}.get(99, 'fallback')");
            Assert.That(Last(hook).ToString(), Is.EqualTo("fallback"));
        }

        [Test]
        public void MethodCall_Clear_EmptiesDict()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a', 2: 'b'}\nd.clear()\nd");
            Assert.That(LastDict(hook).Count, Is.EqualTo(0));
        }

        [Test]
        public void MethodCall_PopPresent_RemovesAndReturnsValue()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a', 2: 'b'}\nd.pop(1)");
            Assert.That(Last(hook).ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void MethodCall_PopMissingNoDefault_ThrowsKeyError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("{1: 'a'}.pop(99)"), Throws.TypeOf<ChowKeyErrorException>());
        }

        [Test]
        public void MethodCall_PopMissingWithDefault_ReturnsDefault()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'}.pop(99, 'fallback')");
            Assert.That(Last(hook).ToString(), Is.EqualTo("fallback"));
        }

        [Test]
        public void MethodCall_Update_MergesRightWinsAndAppendsNewKeys()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a', 2: 'b'}\nd.update({2: 'z', 3: 'c'})\nd");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'a', 2: 'z', 3: 'c'}"));
        }

        [Test]
        public void MethodCall_SetDefaultPresent_ReturnsExisting()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a'}\nd.setdefault(1, 'z')");
            Assert.That(Last(hook).ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void MethodCall_SetDefaultMissing_InsertsAndReturnsDefault()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {1: 'a'}\nd.setdefault(2, 'b')\nd");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'a', 2: 'b'}"));
        }

        [Test]
        public void BoundMethod_StoredInVariable_StillBoundToOriginalDict()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("d = {}\nf = d.setdefault\nf(1, 'a')\nf(2, 'b')\nd");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'a', 2: 'b'}"));
        }

        // ============================================================================================================
        // E. Attribute errors
        // ============================================================================================================

        [Test]
        public void Attribute_Unknown_ThrowsAttributeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("{}.fake"), Throws.TypeOf<ChowAttributeErrorException>());
        }

        [Test]
        public void AttributeAssign_OnDict_ThrowsAttributeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("d = {}\nd.x = 1"), Throws.TypeOf<ChowAttributeErrorException>());
        }

        // ============================================================================================================
        // F. | merge operator
        // ============================================================================================================

        [Test]
        public void Merge_TwoDicts_RightWinsAndPreservesOrder()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a', 2: 'b'} | {2: 'z', 3: 'c'}");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'a', 2: 'z', 3: 'c'}"));
        }

        [Test]
        public void Merge_NonDictOperand_Throws()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("{1: 'a'} | 5"), Throws.InstanceOf<System.Exception>());
        }

        // ============================================================================================================
        // G. Equality
        // ============================================================================================================

        [Test]
        public void Equality_EqualDicts_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a', 2: 'b'} == {2: 'b', 1: 'a'}");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void Equality_DifferentValues_False()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'} == {1: 'b'}");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }

        [Test]
        public void Equality_NestedDicts_Recursive()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: {2: 'x'}} == {1: {2: 'x'}}");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void Inequality_DifferentDicts_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a'} != {1: 'b'}");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        // ============================================================================================================
        // H. Truthiness
        // ============================================================================================================

        [Test]
        public void Truthiness_EmptyDict_IsFalsy()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("if {}:\n    1\nelse:\n    2");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(2));
        }

        [Test]
        public void Truthiness_NonEmptyDict_IsTruthy()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("if {1: 'a'}:\n    1\nelse:\n    2");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // I. in / not in
        // ============================================================================================================

        [Test]
        public void In_DictPresentKey_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("1 in {1: 'a'}");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void In_DictAbsentKey_False()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("99 in {1: 'a'}");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }

        [Test]
        public void NotIn_DictAbsentKey_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("99 not in {1: 'a'}");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void In_ListPresentElement_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("2 in [1, 2, 3]");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void In_ListAbsentElement_False()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("9 in [1, 2, 3]");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }

        [Test]
        public void NotIn_ListAbsentElement_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("9 not in [1, 2, 3]");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void In_UnhashableKeyAgainstDict_ThrowsTypeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("[1] in {1: 'a'}"), Throws.TypeOf<ChowTypeErrorException>());
        }

        [Test]
        public void In_NonIterableRightOperand_ThrowsTypeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Run("1 in 5"), Throws.TypeOf<ChowTypeErrorException>());
        }

        // ============================================================================================================
        // J. Repr
        // ============================================================================================================

        [Test]
        public void Repr_EmptyDict_FormatsAsBraces()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{}");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{}"));
        }

        [Test]
        public void Repr_StringValues_SingleQuoted()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Run("{1: 'a', 2: 'b'}");
            Assert.That(Last(hook).ToString(), Is.EqualTo("{1: 'a', 2: 'b'}"));
        }

        // ============================================================================================================
        // K. API surface
        // ============================================================================================================

        [Test]
        public void Api_HostAssignsChowDict_ReadableFromSource()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            ChowDict dict = new ChowDict();
            dict.Internal.Add(
                new Chow.Interpreter.Values.Internal.TaggedUnion(1),
                new Chow.Interpreter.Values.Internal.TaggedUnion(42));
            module["x"] = dict;
            module.Run("x[1]");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(42));
        }

        [Test]
        public void Api_SourceCreatesDict_ReadableViaHost()
        {
            ChowModule module = new ChowModule();
            module.Run("x = {1: 'a', 2: 'b'}");
            ChowDict dict = (ChowDict)module["x"];
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict[new ChowInt(1)].ToString(), Is.EqualTo("a"));
        }
    }
}
