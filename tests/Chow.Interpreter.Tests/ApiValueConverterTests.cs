using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;

namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class ApiValueConverterTests
    {
        // ============================================================================================================
        // A. ToTaggedUnion (ChowValue -> TaggedUnion)
        // ============================================================================================================

        [Test]
        public void ToTaggedUnion_NullValue_ThrowsArgumentNullException()
        {
            Assert.That(() => ChowValueConverter.ToTaggedUnion(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ToTaggedUnion_ChowNone_ReturnsNoneTaggedUnion()
        {
            var result = ChowValueConverter.ToTaggedUnion(ChowValue.None);

            Assert.That(result.Tag, Is.EqualTo(Tag.None));
        }

        [Test]
        public void ToTaggedUnion_ChowInt_ReturnsIntTaggedUnion()
        {
            var result = ChowValueConverter.ToTaggedUnion(new ChowInt(42));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsInt, Is.True);
                Assert.That(result.IntegerValue, Is.EqualTo(42));
            });
        }

        [Test]
        public void ToTaggedUnion_ChowFloat_ReturnsFloatTaggedUnionPreservingValue()
        {
            // Regression: TaggedUnion(float) ctor previously clobbered _float with default.
            var result = ChowValueConverter.ToTaggedUnion(new ChowFloat(2.5f));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsFloat, Is.True);
                Assert.That(result.FloatValue, Is.EqualTo(2.5f));
            });
        }

        [Test]
        public void ToTaggedUnion_ChowBoolTrue_ReturnsBooleanTaggedUnion()
        {
            var result = ChowValueConverter.ToTaggedUnion(new ChowBool(true));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsBoolean, Is.True);
                Assert.That(result.BooleanValue, Is.True);
            });
        }

        [Test]
        public void ToTaggedUnion_ChowBoolFalse_ReturnsBooleanTaggedUnion()
        {
            var result = ChowValueConverter.ToTaggedUnion(new ChowBool(false));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsBoolean, Is.True);
                Assert.That(result.BooleanValue, Is.False);
            });
        }

        [Test]
        public void ToTaggedUnion_ChowStr_ReturnsStrTaggedUnion()
        {
            var result = ChowValueConverter.ToTaggedUnion(new ChowStr("hello"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsString, Is.True);
                Assert.That(result.StringValue, Is.EqualTo("hello"));
            });
        }

        [Test]
        public void ToTaggedUnion_ChowDynamic_WrapsUnderlyingObjectAsObjectTag()
        {
            var payload = new object();

            var result = ChowValueConverter.ToTaggedUnion(new ChowDynamic(payload));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsObject, Is.True);
                Assert.That(result.ObjectValue, Is.SameAs(payload));
            });
        }

        // ============================================================================================================
        // B. ToApiClassObj (TaggedUnion -> ChowValue)
        // ============================================================================================================

        [Test]
        public void ToApiClassObj_NoneTag_ReturnsChowValueNone()
        {
            var result = ChowValueConverter.ToChowValue(TaggedUnion.None);

            Assert.That(result, Is.SameAs(ChowValue.None));
        }

        [Test]
        public void ToApiClassObj_IntTag_ReturnsChowIntWithSameValue()
        {
            var result = ChowValueConverter.ToChowValue(new TaggedUnion(7));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.TypeOf<ChowInt>());
                Assert.That(result.As<long>(), Is.EqualTo(7));
            });
        }

        [Test]
        public void ToApiClassObj_FloatTag_ReturnsChowFloatWithSameValue()
        {
            var result = ChowValueConverter.ToChowValue(new TaggedUnion(3.25f));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.TypeOf<ChowFloat>());
                Assert.That(result.As<double>(), Is.EqualTo(3.25));
            });
        }

        [Test]
        public void ToApiClassObj_BoolTrueTag_ReturnsChowBoolWithSameValue()
        {
            var result = ChowValueConverter.ToChowValue(new TaggedUnion(true));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.TypeOf<ChowBool>());
                Assert.That(result.As<bool>(), Is.True);
            });
        }

        [Test]
        public void ToApiClassObj_BoolFalseTag_ReturnsChowBoolWithSameValue()
        {
            var result = ChowValueConverter.ToChowValue(new TaggedUnion(false));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.TypeOf<ChowBool>());
                Assert.That(result.As<bool>(), Is.False);
            });
        }

        [Test]
        public void ToApiClassObj_StrTag_ReturnsChowStr()
        {
            var result = ChowValueConverter.ToChowValue(new TaggedUnion("hello"));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.TypeOf<ChowStr>());
                Assert.That(((ChowStr)result).Value, Is.EqualTo("hello"));
            });
        }

        [Test]
        public void ToApiClassObj_ObjectTag_ReturnsChowDynamicWrappingSameRef()
        {
            var payload = new object();

            var result = ChowValueConverter.ToChowValue(new TaggedUnion(payload));

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.TypeOf<ChowDynamic>());
                Assert.That(((ChowDynamic)result).Value, Is.SameAs(payload));
            });
        }

        // ============================================================================================================
        // C. Round-trip
        // ============================================================================================================

        [Test]
        public void RoundTrip_Int_PreservesValue()
        {
            var roundTripped = ChowValueConverter.ToChowValue(
                ChowValueConverter.ToTaggedUnion(new ChowInt(123)));

            Assert.That(roundTripped.As<long>(), Is.EqualTo(123));
        }

        [Test]
        public void RoundTrip_Float_PreservesValue()
        {
            var roundTripped = ChowValueConverter.ToChowValue(
                ChowValueConverter.ToTaggedUnion(new ChowFloat(2.5)));

            Assert.That(roundTripped.As<double>(), Is.EqualTo(2.5));
        }

        [Test]
        public void RoundTrip_BoolTrue_PreservesValue()
        {
            var roundTripped = ChowValueConverter.ToChowValue(
                ChowValueConverter.ToTaggedUnion(new ChowBool(true)));

            Assert.That(roundTripped.As<bool>(), Is.True);
        }

        [Test]
        public void RoundTrip_None_PreservesIdentity()
        {
            var roundTripped = ChowValueConverter.ToChowValue(
                ChowValueConverter.ToTaggedUnion(ChowValue.None));

            Assert.That(roundTripped, Is.SameAs(ChowValue.None));
        }

        [Test]
        public void RoundTrip_Dynamic_PreservesUnderlyingRef()
        {
            var payload = new object();

            var roundTripped = ChowValueConverter.ToChowValue(
                ChowValueConverter.ToTaggedUnion(new ChowDynamic(payload)));

            Assert.Multiple(() =>
            {
                Assert.That(roundTripped, Is.TypeOf<ChowDynamic>());
                Assert.That(((ChowDynamic)roundTripped).Value, Is.SameAs(payload));
            });
        }

        [Test]
        public void RoundTrip_Str_PreservesValue()
        {
            var roundTripped = ChowValueConverter.ToChowValue(
                ChowValueConverter.ToTaggedUnion(new ChowStr("hello")));

            Assert.Multiple(() =>
            {
                Assert.That(roundTripped, Is.TypeOf<ChowStr>());
                Assert.That(((ChowStr)roundTripped).Value, Is.EqualTo("hello"));
            });
        }
    }
}
