using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.ImplementationTests
{
    [TestFixture]
    public class ListExecutionTests
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

        static ChowList LastList(CaptureExprHook hook) => (ChowList)Last(hook);

        // ============================================================================================================
        // A. Literals
        // ============================================================================================================

        [Test]
        public void EmptyListLiteral_ProducesZeroElementList()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[]");
            Assert.That(LastList(hook).Count, Is.EqualTo(0));
        }

        [Test]
        public void ListLiteral_PreservesElementOrder()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2, 3]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<int>(), Is.EqualTo(1));
            Assert.That(list[1].As<int>(), Is.EqualTo(2));
            Assert.That(list[2].As<int>(), Is.EqualTo(3));
        }

        [Test]
        public void NestedListLiteral_ParsesAndExecutes()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[[1, 2], [3]]");
            ChowList outer = LastList(hook);
            Assert.That(outer.Count, Is.EqualTo(2));
            ChowList inner0 = (ChowList)outer[0];
            Assert.That(inner0.Count, Is.EqualTo(2));
            Assert.That(inner0[0].As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // B. Index read
        // ============================================================================================================

        [Test]
        public void Subscript_PositiveIndex_ReadsElement()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[10, 20, 30][1]");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(20));
        }

        [Test]
        public void Subscript_NegativeIndex_WrapsFromEnd()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[10, 20, 30][-1]");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(30));
        }

        [Test]
        public void Subscript_OutOfRange_Throws()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Execute("[1, 2][5]"), Throws.TypeOf<System.IndexOutOfRangeException>());
        }

        // ============================================================================================================
        // C. Subscript assign
        // ============================================================================================================

        [Test]
        public void SubscriptAssign_PositiveIndex_Mutates()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na[0] = 9\na");
            ChowList list = LastList(hook);
            Assert.That(list[0].As<int>(), Is.EqualTo(9));
            Assert.That(list[1].As<int>(), Is.EqualTo(2));
        }

        [Test]
        public void SubscriptAssign_NegativeIndex_Mutates()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na[-1] = 9\na");
            ChowList list = LastList(hook);
            Assert.That(list[2].As<int>(), Is.EqualTo(9));
        }

        // ============================================================================================================
        // D. Slicing
        // ============================================================================================================

        [Test]
        public void Slice_StartStop_ReturnsRange()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[10, 20, 30, 40][1:3]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].As<int>(), Is.EqualTo(20));
            Assert.That(list[1].As<int>(), Is.EqualTo(30));
        }

        [Test]
        public void Slice_FullColon_ReturnsCopy()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2, 3][:]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Slice_NegativeStep_ReversesList()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2, 3][::-1]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<int>(), Is.EqualTo(3));
            Assert.That(list[2].As<int>(), Is.EqualTo(1));
        }

        [Test]
        public void Slice_StepTwo_SkipsElements()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[0, 1, 2, 3, 4][::2]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<int>(), Is.EqualTo(0));
            Assert.That(list[1].As<int>(), Is.EqualTo(2));
            Assert.That(list[2].As<int>(), Is.EqualTo(4));
        }

        // ============================================================================================================
        // E. Methods (bound-method via delegate)
        // ============================================================================================================

        [Test]
        public void MethodCall_Append_MutatesList()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("a = [1]\na.append(2)\na");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[1].As<int>(), Is.EqualTo(2));
        }

        [Test]
        public void MethodCall_PopNoArg_RemovesAndReturnsLast()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na.pop()");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(3));
        }

        [Test]
        public void MethodCall_Reverse_InPlace()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na.reverse()\na");
            ChowList list = LastList(hook);
            Assert.That(list[0].As<int>(), Is.EqualTo(3));
            Assert.That(list[2].As<int>(), Is.EqualTo(1));
        }

        [Test]
        public void BoundMethod_StoredInVariable_StillBoundToOriginalList()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("a = [1]\nf = a.append\nf(2)\nf(3)\na");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[1].As<int>(), Is.EqualTo(2));
            Assert.That(list[2].As<int>(), Is.EqualTo(3));
        }

        // ============================================================================================================
        // F. Attribute errors
        // ============================================================================================================

        [Test]
        public void Attribute_Unknown_ThrowsAttributeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Execute("[1].fake"), Throws.TypeOf<ChowAttributeErrorException>());
        }

        [Test]
        public void AttributeAssign_OnList_ThrowsAttributeError()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Execute("a = [1]\na.x = 1"), Throws.TypeOf<ChowAttributeErrorException>());
        }

        // ============================================================================================================
        // G. Operators
        // ============================================================================================================

        [Test]
        public void Concat_TwoLists_ProducesJoined()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1] + [2, 3]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<int>(), Is.EqualTo(1));
            Assert.That(list[2].As<int>(), Is.EqualTo(3));
        }

        [Test]
        public void Repeat_ListTimesInt_RepeatsN()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[0] * 3");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Repeat_IntTimesList_AlsoRepeats()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("3 * [0]");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Repeat_NegativeCount_ProducesEmptyList()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2] * -1");
            ChowList list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        // ============================================================================================================
        // H. Equality
        // ============================================================================================================

        [Test]
        public void Equality_EqualLists_True()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2] == [1, 2]");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void Equality_DifferentElements_False()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2] == [1, 3]");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }

        [Test]
        public void Equality_NestedLists_Recursive()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, [2]] == [1, [2]]");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        // ============================================================================================================
        // I. Truthiness
        // ============================================================================================================

        [Test]
        public void Truthiness_EmptyList_IsFalsy()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("if []:\n    1\nelse:\n    2");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(2));
        }

        [Test]
        public void Truthiness_NonEmptyList_IsTruthy()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("if [0]:\n    1\nelse:\n    2");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // J. Repr
        // ============================================================================================================

        [Test]
        public void Repr_IntList_FormatsWithBracketsAndCommaSpace()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[1, 2, 3]");
            Assert.That(Last(hook).ToString(), Is.EqualTo("[1, 2, 3]"));
        }

        [Test]
        public void Repr_EmptyList_FormatsAsBrackets()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            module.Execute("[]");
            Assert.That(Last(hook).ToString(), Is.EqualTo("[]"));
        }

        // ============================================================================================================
        // K. API surface
        // ============================================================================================================

        [Test]
        public void Api_HostAssignsChowList_ReadableFromSource()
        {
            (ChowModule module, CaptureExprHook hook) = NewModule();
            ChowList list = new ChowList();
            list.Internal.Add(new Chow.Interpreter.Values.Internal.TaggedUnion(42));
            module["x"] = list;
            module.Execute("x[0]");
            Assert.That(Last(hook).As<int>(), Is.EqualTo(42));
        }

        [Test]
        public void Api_SourceCreatesList_ReadableViaHost()
        {
            ChowModule module = new ChowModule();
            module.Execute("x = [1, 2, 3]");
            ChowList list = (ChowList)module["x"];
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[1].As<int>(), Is.EqualTo(2));
        }

        // ============================================================================================================
        // L. Slice assignment is rejected at compile time
        // ============================================================================================================

        [Test]
        public void SliceAssign_ThrowsNotImplemented()
        {
            (ChowModule module, _) = NewModule();
            Assert.That(() => module.Execute("a = [1, 2]\na[0:1] = [9]"), Throws.TypeOf<System.NotImplementedException>());
        }
    }
}
