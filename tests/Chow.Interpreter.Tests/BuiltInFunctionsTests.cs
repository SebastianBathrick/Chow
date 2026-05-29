using System;
using System.IO;
using Chow.Interpreter.DataTypes;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.StandardLibrary;
namespace Chow.Interpreter.Tests
{
    [TestFixture]
    class BuiltInFunctionsTests
    {
        #region Helpers

        static readonly string[] AllBuiltInNames = BuiltInFunctions.InvocableObjectNames;

        TextWriter _originalOut;
        TextReader _originalIn;
        StringWriter _capturedOut;

        [SetUp]
        public void SetupTestConsole()
        {
            _originalOut = Console.Out;
            _originalIn = Console.In;
            _capturedOut = new StringWriter();
            Console.SetOut(_capturedOut);
        }

        [TearDown]
        public void TearDownTestConsole()
        {
            Console.SetOut(_originalOut);
            Console.SetIn(_originalIn);
            _capturedOut.Dispose();
        }

        static ChowModule CreateModule()
        {
            return new ChowModule();
        }

        static ChowModule CreateModuleWithoutBuiltIns()
        {
            return new ChowModule(useBuiltInFunctions: false);
        }

        #endregion

        #region A. Registration

        [Test]
        public void Registration_FreshModule_AllBuiltInsAreGlobal()
        {
            var module = CreateModule();

            Assert.Multiple(() =>
            {
                foreach (var name in AllBuiltInNames)
                {
                    Assert.That(module.IsGlobal(name), Is.True, $"'{name}' should be a global");
                }
            });
        }

        [Test]
        public void Registration_BareModule_NoBuiltInsAreGlobal()
        {
            var module = CreateModuleWithoutBuiltIns();

            Assert.Multiple(() =>
            {
                foreach (var name in AllBuiltInNames)
                {
                    Assert.That(module.IsGlobal(name), Is.False, $"'{name}' should not be a global");
                }
            });
        }

        [Test]
        public void Registration_BareModule_CallingBuiltInFromSourceThrowsUndefinedName()
        {
            var module = CreateModuleWithoutBuiltIns();
            Assert.That(() => module.Execute("print(1)"), Throws.TypeOf<UndefinedNameException>());
        }

        #endregion

        #region B. print

        [Test]
        public void Print_NoArgs_WritesBlankLine()
        {
            var module = CreateModule();
            module.Execute("print()");
            Assert.That(_capturedOut.ToString(), Is.EqualTo(Environment.NewLine));
        }

        [Test]
        public void Print_SingleString_WritesValueThenNewline()
        {
            var module = CreateModule();
            module.Execute("print(\"hello\")");
            Assert.That(_capturedOut.ToString(), Is.EqualTo("hello" + Environment.NewLine));
        }

        [Test]
        public void Print_VariadicMixedTypes_SpaceSeparatedSingleLine()
        {
            var module = CreateModule();
            module.Execute("print(1, 2.5, True, None)");
            Assert.That(_capturedOut.ToString(), Is.EqualTo("1 2.5 True None" + Environment.NewLine));
        }

        #endregion

        #region C. input

        [Test]
        public void Input_NoPrompt_ReturnsReadLine()
        {
            Console.SetIn(new StringReader("typed line\n"));
            var module = CreateModule();
            var result = module.Execute("x = input()");
            Assert.That(module["x"], Is.EqualTo("typed line"));
        }

        [Test]
        public void Input_WithPrompt_WritesPromptWithoutNewlineAndReturnsReadLine()
        {
            Console.SetIn(new StringReader("answer\n"));
            var module = CreateModule();
            module.Execute("x = input(\"q? \")");

            Assert.Multiple(() =>
            {
                Assert.That(_capturedOut.ToString(), Is.EqualTo("q? "));
                Assert.That(module["x"], Is.EqualTo("answer"));
            });
        }

        #endregion

        #region E. Numeric coercion built-ins

        [Test]
        public void Float_NoArgs_ReturnsZero()
        {
            var module = CreateModule();
            var result = module.Execute("float()");
            Assert.That(result.AsType<double>(), Is.EqualTo(0.0));
        }

        [Test]
        public void Float_FromString_ParsesValue()
        {
            var module = CreateModule();
            var result = module.Execute("float(\"1.5\")");
            Assert.That(result.AsType<double>(), Is.EqualTo(1.5));
        }

        [Test]
        public void Float_FromInt_Promotes()
        {
            var module = CreateModule();
            var result = module.Execute("float(2)");
            Assert.That(result.AsType<double>(), Is.EqualTo(2.0));
        }

        [Test]
        public void Int_NoArgs_ReturnsZero()
        {
            var module = CreateModule();
            var result = module.Execute("int()");
            Assert.That(result.AsType<long>(), Is.EqualTo(0L));
        }

        [Test]
        public void Int_FromString_ParsesValue()
        {
            var module = CreateModule();
            var result = module.Execute("int(\"3\")");
            Assert.That(result.AsType<long>(), Is.EqualTo(3L));
        }

        [Test]
        public void Int_FromFloat_Truncates()
        {
            var module = CreateModule();
            var result = module.Execute("int(2.7)");
            Assert.That(result.AsType<long>(), Is.EqualTo(2L));
        }

        [Test]
        public void Int_FromBool_ReturnsOneOrZero()
        {
            var module = CreateModule();
            Assert.That(module.Execute("int(True)").AsType<long>(), Is.EqualTo(1L));
            Assert.That(module.Execute("int(False)").AsType<long>(), Is.EqualTo(0L));
        }

        [Test]
        public void Str_NoArgs_ReturnsEmpty()
        {
            var module = CreateModule();
            var result = module.Execute("str()");
            Assert.That(result.AsType<string>(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Str_FromValues_UsesChowRepresentation()
        {
            var module = CreateModule();
            Assert.That(module.Execute("str(1)").AsType<string>(), Is.EqualTo("1"));
            Assert.That(module.Execute("str(1.0)").AsType<string>(), Is.EqualTo("1.0"));
            Assert.That(module.Execute("str(True)").AsType<string>(), Is.EqualTo("True"));
            Assert.That(module.Execute("str(None)").AsType<string>(), Is.EqualTo("None"));
        }

        [Test]
        public void Bool_NoArgs_ReturnsFalse()
        {
            var module = CreateModule();
            var result = module.Execute("bool()");
            Assert.That(result.AsType<bool>(), Is.False);
        }

        [Test]
        public void Bool_FromValues_FollowsTruthiness()
        {
            var module = CreateModule();
            Assert.That(module.Execute("bool(0)").AsType<bool>(), Is.False);
            Assert.That(module.Execute("bool(1)").AsType<bool>(), Is.True);
            Assert.That(module.Execute("bool(\"\")").AsType<bool>(), Is.False);
            Assert.That(module.Execute("bool(\"x\")").AsType<bool>(), Is.True);
        }

        #endregion

        #region F. Container constructors

        [Test]
        public void List_NoArgs_ReturnsEmptyList()
        {
            var module = CreateModule();
            var result = module.Execute("list()");
            Assert.That(result.AsType<InternalList>().Count, Is.EqualTo(0));
        }

        [Test]
        public void List_FromRange_MaterializesElements()
        {
            var module = CreateModule();
            var result = module.Execute("list(range(3))").AsType<InternalList>();

            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(3));
                Assert.That(result[0].AsType<long>(), Is.EqualTo(0L));
                Assert.That(result[1].AsType<long>(), Is.EqualTo(1L));
                Assert.That(result[2].AsType<long>(), Is.EqualTo(2L));
            });
        }

        [Test]
        public void Dict_NoArgs_ReturnsEmptyDict()
        {
            var module = CreateModule();
            var result = module.Execute("dict()");
            Assert.That(result.AsType<InternalDict>().Count, Is.EqualTo(0));
        }

        [Test]
        public void Dict_FromDictLiteral_CopiesEntries()
        {
            var module = CreateModule();
            module.Execute("d = dict({\"a\": 1, \"b\": 2})\nx = d[\"a\"]\ny = d[\"b\"]");

            Assert.Multiple(() =>
            {
                Assert.That(module["x"], Is.EqualTo(1L));
                Assert.That(module["y"], Is.EqualTo(2L));
            });
        }

        [Test]
        public void Dict_FromNonMapping_ThrowsTypeException()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("dict([1, 2])"), Throws.TypeOf<TypeException>());
        }

        #endregion

        #region G. len

        [Test]
        public void Len_OnString_ReturnsCharacterCount()
        {
            var module = CreateModule();
            Assert.That(module.Execute("len(\"hello\")").AsType<long>(), Is.EqualTo(5L));
        }

        [Test]
        public void Len_OnList_ReturnsElementCount()
        {
            var module = CreateModule();
            Assert.That(module.Execute("len([1, 2, 3])").AsType<long>(), Is.EqualTo(3L));
        }

        [Test]
        public void Len_OnDict_ReturnsEntryCount()
        {
            var module = CreateModule();
            Assert.That(module.Execute("len({\"a\": 1, \"b\": 2})").AsType<long>(), Is.EqualTo(2L));
        }

        [Test]
        public void Len_OnRange_ReturnsStepCount()
        {
            var module = CreateModule();
            Assert.That(module.Execute("len(range(5))").AsType<long>(), Is.EqualTo(5L));
        }

        [Test]
        public void Len_OnUnsupportedType_ThrowsTypeException()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("len(1)"), Throws.TypeOf<TypeException>());
        }

        #endregion

        #region H. abs

        [Test]
        public void Abs_OnNegativeInt_ReturnsPositive()
        {
            var module = CreateModule();
            Assert.That(module.Execute("abs(-5)").AsType<long>(), Is.EqualTo(5L));
        }

        [Test]
        public void Abs_OnNegativeFloat_ReturnsPositive()
        {
            var module = CreateModule();
            Assert.That(module.Execute("abs(-2.5)").AsType<double>(), Is.EqualTo(2.5));
        }

        [Test]
        public void Abs_OnBool_ReturnsOneOrZero()
        {
            var module = CreateModule();
            Assert.That(module.Execute("abs(True)").AsType<long>(), Is.EqualTo(1L));
            Assert.That(module.Execute("abs(False)").AsType<long>(), Is.EqualTo(0L));
        }

        [Test]
        public void Abs_OnString_ThrowsTypeException()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("abs(\"x\")"), Throws.TypeOf<TypeException>());
        }

        #endregion

        #region I. round

        [Test]
        public void Round_HalfToEven_RoundsDownAtTwoPointFive()
        {
            var module = CreateModule();
            Assert.That(module.Execute("round(2.5)").AsType<long>(), Is.EqualTo(2L));
        }

        [Test]
        public void Round_HalfToEven_RoundsUpAtThreePointFive()
        {
            var module = CreateModule();
            Assert.That(module.Execute("round(3.5)").AsType<long>(), Is.EqualTo(4L));
        }

        [Test]
        public void Round_WithDigits_ReturnsFloat()
        {
            // Use a cleanly representable input: 2.345 cannot be exactly represented as a double,
            // so round(2.345, 2) returns 2.35 (matches CPython). 3.14159 → 3.14 is a clean check.
            var module = CreateModule();
            Assert.That(module.Execute("round(3.14159, 2)").AsType<double>(), Is.EqualTo(3.14).Within(1e-9));
        }

        [Test]
        public void Round_OneArgForm_ReturnsInt()
        {
            var module = CreateModule();
            Assert.That(module.Execute("round(1)").AsType<long>(), Is.EqualTo(1L));
        }

        #endregion

        #region J. min / max

        [Test]
        public void Min_VariadicInts_ReturnsSmallest()
        {
            var module = CreateModule();
            Assert.That(module.Execute("min(3, 1, 2)").AsType<long>(), Is.EqualTo(1L));
        }

        [Test]
        public void Min_FromList_ReturnsSmallestElement()
        {
            var module = CreateModule();
            Assert.That(module.Execute("min([3, 1, 2])").AsType<long>(), Is.EqualTo(1L));
        }

        [Test]
        public void Min_EmptyList_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("min([])"), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Max_VariadicInts_ReturnsLargest()
        {
            var module = CreateModule();
            Assert.That(module.Execute("max(3, 1, 2)").AsType<long>(), Is.EqualTo(3L));
        }

        [Test]
        public void Max_FromList_ReturnsLargestElement()
        {
            var module = CreateModule();
            Assert.That(module.Execute("max([3, 1, 2])").AsType<long>(), Is.EqualTo(3L));
        }

        [Test]
        public void Max_OnStrings_UsesLexicographicOrder()
        {
            var module = CreateModule();
            Assert.That(module.Execute("max(\"a\", \"b\", \"ab\")").AsType<string>(), Is.EqualTo("b"));
        }

        #endregion

        #region K. range

        [Test]
        public void Range_OneArg_ProducesZeroToStopExclusive()
        {
            var module = CreateModule();
            var range = module.Execute("range(3)").AsType<InternalRange>();
            Assert.That(range.Count, Is.EqualTo(3));
        }

        [Test]
        public void Range_TwoArgs_HonorsStart()
        {
            var module = CreateModule();
            var list = module.Execute("list(range(2, 5))").AsType<InternalList>();

            Assert.Multiple(() =>
            {
                Assert.That(list.Count, Is.EqualTo(3));
                Assert.That(list[0].AsType<long>(), Is.EqualTo(2L));
                Assert.That(list[2].AsType<long>(), Is.EqualTo(4L));
            });
        }

        [Test]
        public void Range_ThreeArgs_HonorsStep()
        {
            var module = CreateModule();
            var list = module.Execute("list(range(0, 10, 3))").AsType<InternalList>();

            Assert.Multiple(() =>
            {
                Assert.That(list.Count, Is.EqualTo(4));
                Assert.That(list[0].AsType<long>(), Is.EqualTo(0L));
                Assert.That(list[3].AsType<long>(), Is.EqualTo(9L));
            });
        }

        [Test]
        public void Range_NegativeStep_CountsDown()
        {
            var module = CreateModule();
            var list = module.Execute("list(range(5, 0, -1))").AsType<InternalList>();

            Assert.Multiple(() =>
            {
                Assert.That(list.Count, Is.EqualTo(5));
                Assert.That(list[0].AsType<long>(), Is.EqualTo(5L));
                Assert.That(list[4].AsType<long>(), Is.EqualTo(1L));
            });
        }

        [Test]
        public void Range_NonIntegerArg_ThrowsTypeException()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("range(1.5)"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Range_ZeroStep_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("range(0, 5, 0)"), Throws.TypeOf<InvalidOperationException>());
        }

        #endregion

        #region L. Argument-count validation

        [Test]
        public void ArgCount_LenWithZeroArgs_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("len()"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void ArgCount_LenWithTwoArgs_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("len(1, 2)"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void ArgCount_AbsWithZeroArgs_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("abs()"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void ArgCount_RangeWithZeroArgs_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("range()"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void ArgCount_RoundWithThreeArgs_Throws()
        {
            var module = CreateModule();
            Assert.That(() => module.Execute("round(1, 2, 3)"), Throws.TypeOf<TypeException>());
        }

        #endregion
    }
}
