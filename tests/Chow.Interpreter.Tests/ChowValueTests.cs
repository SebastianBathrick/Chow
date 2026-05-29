using Chow.Interpreter.DataTypes;
using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter.Tests
{
    [TestFixture]
    class ChowValueTests
    {

        #region Helpers

        static InternalList ListOf(params ChowValue[] elements)
        {
            var list = new InternalList();

            foreach (var element in elements)
            {
                list.Add(element);
            }

            return list;
        }

        static InternalDict DictOf(params (ChowValue key, ChowValue value)[] pairs)
        {
            var dict = new InternalDict();

            foreach (var pair in pairs)
            {
                dict.Add(pair.key, pair.value);
            }

            return dict;
        }

        #endregion

        #region Zero division

        [Test]
        public void Divide_IntByZero_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(1L);
            var right = new ChowValue(0L);
            Assert.That(() => left.CreateQuotient(right), Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Divide_FloatByZero_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(1.0);
            var right = new ChowValue(0.0);
            Assert.That(() => left.CreateQuotient(right), Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Divide_BoolByFalse_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(true);
            var right = new ChowValue(false);
            Assert.That(() => left.CreateQuotient(right), Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Modulus_IntByZero_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(5L);
            var right = new ChowValue(0L);
            Assert.That(() => left.CreateModulus(right), Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Modulus_FloatByZero_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(5.0);
            var right = new ChowValue(0.0);
            Assert.That(() => left.CreateModulus(right), Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void FloorDivide_IntByZero_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(5L);
            var right = new ChowValue(0L);
            Assert.That(() => left.CreateFloorQuotient(right), Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void FloorDivide_FloatByZero_ThrowsZeroDivisionException()
        {
            var left = new ChowValue(5.0);
            var right = new ChowValue(0.0);
            Assert.That(() => left.CreateFloorQuotient(right), Throws.TypeOf<ZeroDivisionException>());
        }

        #endregion

        #region Integer precision

        [Test]
        public void Power_LargeIntegerExponent_StaysExact()
        {
            // 3^39 is not exactly representable as a double, so the old (long)Math.Pow path lost precision.
            var left = new ChowValue(3L);
            var right = new ChowValue(39L);
            var result = left.CreatePower(right);
            Assert.That(result.AsType<long>(), Is.EqualTo(4052555153018976267L));
        }

        [Test]
        public void Power_NegativeExponent_PromotesToFloat()
        {
            var left = new ChowValue(2L);
            var right = new ChowValue(-3L);
            var result = left.CreatePower(right);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsOfType<double>(), Is.True);
                Assert.That(result.AsType<double>(), Is.EqualTo(0.125));
            });
        }

        [TestCase(-7L, 2L, -4L)]
        [TestCase(7L, -2L, -4L)]
        [TestCase(-7L, -2L, 3L)]
        [TestCase(7L, 2L, 3L)]
        public void FloorDivide_SignedOperands_FloorsTowardNegativeInfinity(long a, long b, long expected)
        {
            var result = new ChowValue(a).CreateFloorQuotient(new ChowValue(b));
            Assert.That(result.AsType<long>(), Is.EqualTo(expected));
        }

        [Test]
        public void FloorDivide_LargeOperands_StaysExact()
        {
            // For long.MaxValue / 3, the old double-detour overflowed (double can't represent long.MaxValue
            // exactly, and (long)Math.Floor would wrap). Exact integer floor stays sound.
            var left = new ChowValue(long.MaxValue);
            var right = new ChowValue(3L);
            var result = left.CreateFloorQuotient(right);
            Assert.That(result.AsType<long>(), Is.EqualTo(long.MaxValue / 3L));
        }

        #endregion

        #region Equality

        [Test]
        public void IsNotEqualTo_CrossType_ReturnsTrue()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new ChowValue(1L).IsNotEqualTo(new ChowValue("1")), Is.True);
                Assert.That(ChowValue.None.IsNotEqualTo(new ChowValue(false)), Is.True);
                Assert.That(new ChowValue(ListOf(new ChowValue(1L))).IsNotEqualTo(new ChowValue(1L)), Is.True);
            });
        }

        [Test]
        public void IsNotEqualTo_SameTypeUnequal_ReturnsTrue()
        {
            var listA = new ChowValue(ListOf(new ChowValue(1L), new ChowValue(2L)));
            var listB = new ChowValue(ListOf(new ChowValue(1L), new ChowValue(3L)));
            Assert.Multiple(() =>
            {
                Assert.That(new ChowValue(1L).IsNotEqualTo(new ChowValue(2L)), Is.True);
                Assert.That(new ChowValue("a").IsNotEqualTo(new ChowValue("b")), Is.True);
                Assert.That(listA.IsNotEqualTo(listB), Is.True);
            });
        }

        [Test]
        public void IsNotEqualTo_SameTypeEqual_ReturnsFalse()
        {
            var listA = new ChowValue(ListOf(new ChowValue(1L), new ChowValue(2L)));
            var listB = new ChowValue(ListOf(new ChowValue(1L), new ChowValue(2L)));
            Assert.Multiple(() =>
            {
                Assert.That(new ChowValue(1L).IsNotEqualTo(new ChowValue(1L)), Is.False);
                Assert.That(new ChowValue(1L).IsNotEqualTo(new ChowValue(1.0)), Is.False);
                Assert.That(listA.IsNotEqualTo(listB), Is.False);
            });
        }

        [Test]
        public void IsEqualTo_NumericPromotion_ReturnsTrue()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new ChowValue(1L).IsTypeAgnosticEqualTo(new ChowValue(1.0)), Is.True);
                Assert.That(new ChowValue(true).IsTypeAgnosticEqualTo(new ChowValue(1L)), Is.True);
            });
        }

        #endregion

        #region Bool coercion

        [Test]
        public void Multiply_BoolByString_RepeatsCorrectly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new ChowValue(true).CreateProduct(new ChowValue("ab")).AsType<string>(), Is.EqualTo("ab"));
                Assert.That(new ChowValue(false).CreateProduct(new ChowValue("ab")).AsType<string>(), Is.EqualTo(string.Empty));
                Assert.That(new ChowValue("ab").CreateProduct(new ChowValue(true)).AsType<string>(), Is.EqualTo("ab"));
            });
        }

        [Test]
        public void Multiply_BoolByList_RepeatsCorrectly()
        {
            var source = ListOf(new ChowValue(1L), new ChowValue(2L));
            var expected = ListOf(new ChowValue(1L), new ChowValue(2L));

            var repeatedRight = new ChowValue(source).CreateProduct(new ChowValue(true)).AsType<InternalList>();
            var emptied = new ChowValue(false).CreateProduct(new ChowValue(source)).AsType<InternalList>();

            Assert.Multiple(() =>
            {
                Assert.That(InternalList.ElementsEqual(repeatedRight, expected), Is.True);
                Assert.That(emptied.Count, Is.EqualTo(0));
            });
        }

        #endregion

        #region AsType<int>

        [Test]
        public void AsType_Int_FromIntValue_ReturnsTruncated()
        {
            Assert.That(new ChowValue(5L).AsType<int>(), Is.EqualTo(5));
        }

        [Test]
        public void AsType_Int_FromBoolValue_ReturnsZeroOrOne()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new ChowValue(true).AsType<int>(), Is.EqualTo(1));
                Assert.That(new ChowValue(false).AsType<int>(), Is.EqualTo(0));
            });
        }

        [Test]
        public void AsType_Long_FromIntValue_StillWorks()
        {
            Assert.That(new ChowValue(5L).AsType<long>(), Is.EqualTo(5L));
        }

        #endregion

        #region ChowValue(object) narrowing

        [Test]
        public void Ctor_ObjectHoldingString_LandsInStrTag()
        {
            var cv = new ChowValue((object)"hi");
            Assert.Multiple(() =>
            {
                Assert.That(cv.IsOfType<string>(), Is.True);
                Assert.That(cv.AsType<string>(), Is.EqualTo("hi"));
            });
        }

        [Test]
        public void Ctor_ObjectHoldingLong_LandsInIntTag()
        {
            var cv = new ChowValue((object)5L);
            Assert.Multiple(() =>
            {
                Assert.That(cv.IsOfType<long>(), Is.True);
                Assert.That(cv.AsType<long>(), Is.EqualTo(5L));
            });
        }

        [Test]
        public void Ctor_ObjectHoldingBoxedInt_LandsInIntTag()
        {
            var cv = new ChowValue((object)5);
            Assert.Multiple(() =>
            {
                Assert.That(cv.IsOfType<long>(), Is.True);
                Assert.That(cv.AsType<long>(), Is.EqualTo(5L));
            });
        }

        [Test]
        public void Ctor_ObjectHoldingBool_LandsInBoolTag()
        {
            var cv = new ChowValue((object)true);
            Assert.Multiple(() =>
            {
                Assert.That(cv.IsOfType<bool>(), Is.True);
                Assert.That(cv.AsType<bool>(), Is.True);
            });
        }

        [Test]
        public void Ctor_ObjectHoldingDouble_LandsInFloatTag()
        {
            var cv = new ChowValue((object)1.5);
            Assert.Multiple(() =>
            {
                Assert.That(cv.IsOfType<double>(), Is.True);
                Assert.That(cv.AsType<double>(), Is.EqualTo(1.5));
            });
        }

        [Test]
        public void Ctor_ObjectHoldingInternalList_LandsInListTag()
        {
            var list = ListOf(new ChowValue(1L));
            var cv = new ChowValue((object)list);
            Assert.That(cv.IsOfType<InternalList>(), Is.True);
        }

        [Test]
        public void Ctor_ObjectHoldingInternalDict_LandsInDictTag()
        {
            var dict = DictOf((new ChowValue(1L), new ChowValue(2L)));
            var cv = new ChowValue((object)dict);
            Assert.That(cv.IsOfType<InternalDict>(), Is.True);
        }

        [Test]
        public void Ctor_ObjectHoldingInternalRange_LandsInRangeTag()
        {
            var range = new InternalRange(0L, 5L, 1L);
            var cv = new ChowValue((object)range);
            Assert.That(cv.IsOfType<InternalRange>(), Is.True);
        }

        [Test]
        public void Ctor_ObjectHoldingFuncDelegate_LandsInObjectTag()
        {
            // Interop wraps (e.g. Compiler.cs:243, ChowModule.cs:20) intentionally cast delegates and
            // ClosureTemplate/IChowIterator to object so they land under Tag.Object — preserve that.
            Func<ChowValue[], ChowValue> del = args => ChowValue.None;
            var cv = new ChowValue(del);
            Assert.That(cv.IsOfType<Func<ChowValue[], ChowValue>>(), Is.True);
        }

        [Test]
        public void Ctor_ObjectNull_ThrowsArgumentNullException()
        {
            Assert.That(() => new ChowValue((object)null), Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region Container hashing

        [Test]
        public void GetHashCode_EqualLists_ReturnsSameHash()
        {
            var a = new ChowValue(ListOf(new ChowValue(1L), new ChowValue(2L), new ChowValue(3L)));
            var b = new ChowValue(ListOf(new ChowValue(1L), new ChowValue(2L), new ChowValue(3L)));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void GetHashCode_EqualDictsDifferentInsertionOrder_ReturnsSameHash()
        {
            var a = new ChowValue(DictOf(
                (new ChowValue(1L), new ChowValue(2L)),
                (new ChowValue(3L), new ChowValue(4L))));
            var b = new ChowValue(DictOf(
                (new ChowValue(3L), new ChowValue(4L)),
                (new ChowValue(1L), new ChowValue(2L))));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void GetHashCode_DifferentLists_Differ()
        {
            // Trivial single-element lists with distinct content should not collide via _dataType fallback.
            var a = new ChowValue(ListOf(new ChowValue(1L)));
            var b = new ChowValue(ListOf(new ChowValue(2L)));
            Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        #endregion

        #region Str null guard

        [Test]
        public void Ctor_StringNull_ThrowsArgumentNullException()
        {
            Assert.That(() => new ChowValue((string)null), Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region None

        [Test]
        public void None_AsTypeObject_ReturnsNull()
        {
            Assert.That(ChowValue.None.AsType<object>(), Is.Null);
        }

        [Test]
        public void None_IsOfTypeObject_ReturnsFalse()
        {
            Assert.That(ChowValue.None.IsOfType<object>(), Is.False);
        }

        [Test]
        public void None_IsOfTypeLong_ReturnsFalse()
        {
            Assert.That(ChowValue.None.IsOfType<long>(), Is.False);
        }

        [Test]
        public void None_EqualsNone_ReturnsTrue()
        {
            Assert.That(ChowValue.None.Equals(ChowValue.None), Is.True);
        }

        [Test]
        public void None_EqualsZero_ReturnsFalse()
        {
            Assert.That(ChowValue.None.Equals(new ChowValue(0L)), Is.False);
        }

        [Test]
        public void None_ToString_ReturnsNoneLiteral()
        {
            Assert.That(ChowValue.None.ToString(), Is.EqualTo("None"));
        }

        #endregion

    }
}
