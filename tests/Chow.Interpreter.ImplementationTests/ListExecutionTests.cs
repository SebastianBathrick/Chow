using Chow.Interpreter;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.State.Values;
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

        sealed class CaptureExprHook : IExpressionStatementHook
        {
            public List<ChowValue> Values { get; } = new List<ChowValue>();

            public void Invoke(object value = null)
            {
                Values.Add((ChowValue)value);
            }
        }

        static (ChowModule module, CaptureExprHook hook) NewModule()
        {
            var module = new ChowModule();
            var hook = new CaptureExprHook();
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
            (var module, var hook) = NewModule();
            module.Execute("[]");
            Assert.That(LastList(hook).Count, Is.EqualTo(0));
        }

        [Test]
        public void ListLiteral_PreservesElementOrder()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2, 3]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<long>(), Is.EqualTo(1));
            Assert.That(list[1].As<long>(), Is.EqualTo(2));
            Assert.That(list[2].As<long>(), Is.EqualTo(3));
        }

        [Test]
        public void NestedListLiteral_ParsesAndExecutes()
        {
            (var module, var hook) = NewModule();
            module.Execute("[[1, 2], [3]]");
            var outer = LastList(hook);
            Assert.That(outer.Count, Is.EqualTo(2));
            var inner0 = (ChowList)outer[0];
            Assert.That(inner0.Count, Is.EqualTo(2));
            Assert.That(inner0[0].As<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // B. Index read
        // ============================================================================================================

        [Test]
        public void Subscript_PositiveIndex_ReadsElement()
        {
            (var module, var hook) = NewModule();
            module.Execute("[10, 20, 30][1]");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(20));
        }

        [Test]
        public void Subscript_NegativeIndex_WrapsFromEnd()
        {
            (var module, var hook) = NewModule();
            module.Execute("[10, 20, 30][-1]");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(30));
        }

        [Test]
        public void Subscript_OutOfRange_Throws()
        {
            (var module, _) = NewModule();
            Assert.That(() => module.Execute("[1, 2][5]"), Throws.TypeOf<System.IndexOutOfRangeException>());
        }

        // ============================================================================================================
        // C. Subscript assign
        // ============================================================================================================

        [Test]
        public void SubscriptAssign_PositiveIndex_Mutates()
        {
            (var module, var hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na[0] = 9\na");
            var list = LastList(hook);
            Assert.That(list[0].As<long>(), Is.EqualTo(9));
            Assert.That(list[1].As<long>(), Is.EqualTo(2));
        }

        [Test]
        public void SubscriptAssign_NegativeIndex_Mutates()
        {
            (var module, var hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na[-1] = 9\na");
            var list = LastList(hook);
            Assert.That(list[2].As<long>(), Is.EqualTo(9));
        }

        // ============================================================================================================
        // D. Slicing
        // ============================================================================================================

        [Test]
        public void Slice_StartStop_ReturnsRange()
        {
            (var module, var hook) = NewModule();
            module.Execute("[10, 20, 30, 40][1:3]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].As<long>(), Is.EqualTo(20));
            Assert.That(list[1].As<long>(), Is.EqualTo(30));
        }

        [Test]
        public void Slice_FullColon_ReturnsCopy()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2, 3][:]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Slice_NegativeStep_ReversesList()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2, 3][::-1]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<long>(), Is.EqualTo(3));
            Assert.That(list[2].As<long>(), Is.EqualTo(1));
        }

        [Test]
        public void Slice_StepTwo_SkipsElements()
        {
            (var module, var hook) = NewModule();
            module.Execute("[0, 1, 2, 3, 4][::2]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<long>(), Is.EqualTo(0));
            Assert.That(list[1].As<long>(), Is.EqualTo(2));
            Assert.That(list[2].As<long>(), Is.EqualTo(4));
        }

        // ============================================================================================================
        // E. Methods (bound-method via delegate)
        // ============================================================================================================

        [Test]
        public void MethodCall_Append_MutatesList()
        {
            (var module, var hook) = NewModule();
            module.Execute("a = [1]\na.append(2)\na");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[1].As<long>(), Is.EqualTo(2));
        }

        [Test]
        public void MethodCall_PopNoArg_RemovesAndReturnsLast()
        {
            (var module, var hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na.pop()");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(3));
        }

        [Test]
        public void MethodCall_Reverse_InPlace()
        {
            (var module, var hook) = NewModule();
            module.Execute("a = [1, 2, 3]\na.reverse()\na");
            var list = LastList(hook);
            Assert.That(list[0].As<long>(), Is.EqualTo(3));
            Assert.That(list[2].As<long>(), Is.EqualTo(1));
        }

        [Test]
        public void BoundMethod_StoredInVariable_StillBoundToOriginalList()
        {
            (var module, var hook) = NewModule();
            module.Execute("a = [1]\nf = a.append\nf(2)\nf(3)\na");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[1].As<long>(), Is.EqualTo(2));
            Assert.That(list[2].As<long>(), Is.EqualTo(3));
        }

        // ============================================================================================================
        // F. Attribute errors
        // ============================================================================================================

        [Test]
        public void Attribute_Unknown_ThrowsAttributeError()
        {
            (var module, _) = NewModule();
            Assert.That(() => module.Execute("[1].fake"), Throws.TypeOf<Exceptions.AttributeException>());
        }

        [Test]
        public void AttributeAssign_OnList_ThrowsAttributeError()
        {
            (var module, _) = NewModule();
            Assert.That(() => module.Execute("a = [1]\na.x = 1"), Throws.TypeOf<Exceptions.AttributeException>());
        }

        // ============================================================================================================
        // G. Operators
        // ============================================================================================================

        [Test]
        public void Concat_TwoLists_ProducesJoined()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1] + [2, 3]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].As<long>(), Is.EqualTo(1));
            Assert.That(list[2].As<long>(), Is.EqualTo(3));
        }

        [Test]
        public void Repeat_ListTimesInt_RepeatsN()
        {
            (var module, var hook) = NewModule();
            module.Execute("[0] * 3");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Repeat_IntTimesList_AlsoRepeats()
        {
            (var module, var hook) = NewModule();
            module.Execute("3 * [0]");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Repeat_NegativeCount_ProducesEmptyList()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2] * -1");
            var list = LastList(hook);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        // ============================================================================================================
        // H. Equality
        // ============================================================================================================

        [Test]
        public void Equality_EqualLists_True()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2] == [1, 2]");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        [Test]
        public void Equality_DifferentElements_False()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2] == [1, 3]");
            Assert.That(Last(hook).As<bool>(), Is.False);
        }

        [Test]
        public void Equality_NestedLists_Recursive()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, [2]] == [1, [2]]");
            Assert.That(Last(hook).As<bool>(), Is.True);
        }

        // ============================================================================================================
        // I. Truthiness
        // ============================================================================================================

        [Test]
        public void Truthiness_EmptyList_IsFalsy()
        {
            (var module, var hook) = NewModule();
            module.Execute("if []:\n    1\nelse:\n    2");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(2));
        }

        [Test]
        public void Truthiness_NonEmptyList_IsTruthy()
        {
            (var module, var hook) = NewModule();
            module.Execute("if [0]:\n    1\nelse:\n    2");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(1));
        }

        // ============================================================================================================
        // J. Repr
        // ============================================================================================================

        [Test]
        public void Repr_IntList_FormatsWithBracketsAndCommaSpace()
        {
            (var module, var hook) = NewModule();
            module.Execute("[1, 2, 3]");
            Assert.That(Last(hook).ToString(), Is.EqualTo("[1, 2, 3]"));
        }

        [Test]
        public void Repr_EmptyList_FormatsAsBrackets()
        {
            (var module, var hook) = NewModule();
            module.Execute("[]");
            Assert.That(Last(hook).ToString(), Is.EqualTo("[]"));
        }

        // ============================================================================================================
        // K. API surface
        // ============================================================================================================

        [Test]
        public void Api_HostAssignsChowList_ReadableFromSource()
        {
            (var module, var hook) = NewModule();
            var list = new ChowList();
            list.Internal.Add(new TaggedUnion(42));
            module["x"] = list;
            module.Execute("x[0]");
            Assert.That(Last(hook).As<long>(), Is.EqualTo(42));
        }

        [Test]
        public void Api_SourceCreatesList_ReadableViaHost()
        {
            var module = new ChowModule();
            module.Execute("x = [1, 2, 3]");
            var list = (ChowList)module.GetGlobal("x");
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[1].As<long>(), Is.EqualTo(2));
        }

        // ============================================================================================================
        // L. Slice assignment is rejected at compile time
        // ============================================================================================================

        [Test]
        public void SliceAssign_ThrowsNotImplemented()
        {
            (var module, _) = NewModule();
            Assert.That(() => module.Execute("a = [1, 2]\na[0:1] = [9]"), Throws.TypeOf<System.NotImplementedException>());
        }
    }
}
