using Chow.Interpreter.DataTypes;
using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter.Tests
{
    [TestFixture]
    class ChowModuleTests
    {

        #region Helpers

        // TODO: Get rid of this. I have no idea why this is here.
        static ChowModule NewModule()
        {
            return new ChowModule();
        }

        #endregion

        #region Indexer - Basic Get/Set

        [Test]
        public void Indexer_GetUndefinedName_ThrowsGlobalAccessException()
        {
            var module = NewModule();
            Assert.That(() => module["nope"], Throws.TypeOf<GlobalAccessException>()
                .With.Property(nameof(GlobalAccessException.Name)).EqualTo("nope"));
        }

        [Test]
        public void Indexer_GetUndefinedName_NamePropertyExposesMissingName()
        {
            var module = NewModule();

            try
            {
                _ = module["missing_thing"];
                Assert.Fail();
            }
            catch (GlobalAccessException ex)
            {
                Assert.That(ex.Name, Is.EqualTo("missing_thing"));
            }
        }

        [Test]
        public void Indexer_SetThenGetInt_ReturnsValue()
        {
            var module = NewModule();
            module["x"] = 42;
            Assert.That(module["x"], Is.EqualTo(42L));
        }

        [Test]
        public void Indexer_SetThenGetLong_ReturnsValue()
        {
            var module = NewModule();
            module["x"] = 9999999999L;
            Assert.That(module["x"], Is.EqualTo(9999999999L));
        }

        [Test]
        public void Indexer_SetThenGetDouble_ReturnsValue()
        {
            var module = NewModule();
            module["x"] = 3.14;
            Assert.That(module["x"], Is.EqualTo(3.14));
        }

        [Test]
        public void Indexer_SetThenGetString_ReturnsValue()
        {
            var module = NewModule();
            module["x"] = "hello";
            Assert.That(module["x"], Is.EqualTo("hello"));
        }

        [Test]
        public void Indexer_SetThenGetBool_ReturnsValue()
        {
            var module = NewModule();
            module["x"] = true;
            Assert.That(module["x"], Is.EqualTo(true));
        }

        [Test]
        public void Indexer_SetThenGetBoolFalse_ReturnsValue()
        {
            var module = NewModule();
            module["x"] = false;
            Assert.That(module["x"], Is.EqualTo(false));
        }

        [Test]
        public void Indexer_SetThenGetZero_ReturnsZero()
        {
            var module = NewModule();
            module["x"] = 0;
            Assert.That(module["x"], Is.EqualTo(0L));
        }

        [Test]
        public void Indexer_SetThenGetEmptyString_ReturnsEmptyString()
        {
            var module = NewModule();
            module["x"] = "";
            Assert.That(module["x"], Is.EqualTo(""));
        }

        [Test]
        public void Indexer_SetThenGetLongMaxValue_RoundTrips()
        {
            var module = NewModule();
            module["x"] = long.MaxValue;
            Assert.That(module["x"], Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void Indexer_SetThenGetLongMinValue_RoundTrips()
        {
            var module = NewModule();
            module["x"] = long.MinValue;
            Assert.That(module["x"], Is.EqualTo(long.MinValue));
        }

        [Test]
        public void Indexer_SetThenGetDoubleNaN_RoundTrips()
        {
            var module = NewModule();
            module["x"] = double.NaN;
            Assert.That(double.IsNaN((double)module["x"]!), Is.True);
        }

        [Test]
        public void Indexer_SetThenGetDoublePositiveInfinity_RoundTrips()
        {
            var module = NewModule();
            module["x"] = double.PositiveInfinity;
            Assert.That(module["x"], Is.EqualTo(double.PositiveInfinity));
        }

        [Test]
        public void Indexer_SetThenGetDoubleNegativeInfinity_RoundTrips()
        {
            var module = NewModule();
            module["x"] = double.NegativeInfinity;
            Assert.That(module["x"], Is.EqualTo(double.NegativeInfinity));
        }

        [Test]
        public void Indexer_SetNullValue_ThrowsArgumentNullException()
        {
            var module = NewModule();
            Assert.That(() => module["x"] = null!, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Indexer_SetExistingName_OverwritesPreviousValue()
        {
            var module = NewModule();
            module["x"] = 1;
            module["x"] = 2;
            Assert.That(module["x"], Is.EqualTo(2L));
        }

        [Test]
        public void Indexer_SetTwiceWithDifferentTypes_LastTypeWins()
        {
            var module = NewModule();
            module["x"] = 1;
            module["x"] = "now a string";
            Assert.That(module["x"], Is.EqualTo("now a string"));
        }

        [Test]
        public void Indexer_SetIntReadAsLong_PromotesToLong()
        {
            var module = NewModule();
            module["x"] = 5;
            Assert.That(module["x"], Is.TypeOf<long>());
        }

        [Test]
        public void Indexer_CaseSensitive_LowercaseAndUppercaseAreDistinct()
        {
            var module = NewModule();
            module["x"] = 1;
            module["X"] = 2;
            Assert.Multiple(() =>
            {
                Assert.That(module["x"], Is.EqualTo(1L));
                Assert.That(module["X"], Is.EqualTo(2L));
            });
        }

        [Test]
        public void Indexer_SetWithVeryLongName_CanBeReadBack()
        {
            var module = NewModule();
            var name = new string('a', 1000);
            module[name] = 7;
            Assert.That(module[name], Is.EqualTo(7L));
        }

        [Test]
        public void Indexer_ShadowingBuiltIn_HostValueWinsOnRead()
        {
            var module = NewModule();
            module["print"] = 42;
            Assert.That(module["print"], Is.EqualTo(42L));
        }

        #endregion

        #region Constructor / Built-Ins

        [Test]
        public void Constructor_BuiltInPrint_IsSeeded()
        {
            Assert.That(() => NewModule()["print"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInInput_IsSeeded()
        {
            Assert.That(() => NewModule()["input"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInClear_IsSeeded()
        {
            Assert.That(() => NewModule()["clear"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInFloat_IsSeeded()
        {
            Assert.That(() => NewModule()["float"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInStr_IsSeeded()
        {
            Assert.That(() => NewModule()["str"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInInt_IsSeeded()
        {
            Assert.That(() => NewModule()["int"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInBool_IsSeeded()
        {
            Assert.That(() => NewModule()["bool"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInList_IsSeeded()
        {
            Assert.That(() => NewModule()["list"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInDict_IsSeeded()
        {
            Assert.That(() => NewModule()["dict"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInLen_IsSeeded()
        {
            Assert.That(() => NewModule()["len"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInAbs_IsSeeded()
        {
            Assert.That(() => NewModule()["abs"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInRound_IsSeeded()
        {
            Assert.That(() => NewModule()["round"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInMin_IsSeeded()
        {
            Assert.That(() => NewModule()["min"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInMax_IsSeeded()
        {
            Assert.That(() => NewModule()["max"], Throws.Nothing);
        }

        [Test]
        public void Constructor_BuiltInRange_IsSeeded()
        {
            Assert.That(() => NewModule()["range"], Throws.Nothing);
        }

        #endregion

        #region Execute - Input Handling

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\n\n")]
        [TestCase("\t")]
        public void Execute_TriviallyEmptySource_IsNoOp(string? source)
        {
            var module = NewModule();
            Assert.That(() => module.Execute(source), Throws.Nothing);
        }

        [Test]
        public void Execute_OnlyComment_IsNoOp()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("# comment only"), Throws.Nothing);
        }

        [Test]
        public void Execute_MultipleBlankLines_IsNoOp()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("\n\n\n\n"), Throws.Nothing);
        }

        #endregion

        #region Execute - Arithmetic

        [Test]
        public void Execute_IntAddition_ProducesSum()
        {
            var module = NewModule();
            module.Execute("r = 2 + 3");
            Assert.That(module["r"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_IntSubtraction_ProducesDifference()
        {
            var module = NewModule();
            module.Execute("r = 10 - 3");
            Assert.That(module["r"], Is.EqualTo(7L));
        }

        [Test]
        public void Execute_IntMultiplication_ProducesProduct()
        {
            var module = NewModule();
            module.Execute("r = 4 * 5");
            Assert.That(module["r"], Is.EqualTo(20L));
        }

        [Test]
        public void Execute_IntTrueDivision_ProducesFloat()
        {
            var module = NewModule();
            module.Execute("r = 10 / 4");
            Assert.That(module["r"], Is.EqualTo(2.5));
        }

        [Test]
        public void Execute_IntFloorDivision_ProducesInt()
        {
            var module = NewModule();
            module.Execute("r = 10 // 3");
            Assert.That(module["r"], Is.EqualTo(3L));
        }

        [Test]
        public void Execute_IntModulus_ProducesRemainder()
        {
            var module = NewModule();
            module.Execute("r = 10 % 3");
            Assert.That(module["r"], Is.EqualTo(1L));
        }

        [Test]
        public void Execute_NegativeModulus_FloorsTowardNegativeInfinity()
        {
            var module = NewModule();
            module.Execute("r = -7 % 3");
            Assert.That(module["r"], Is.EqualTo(2L));
        }

        [Test]
        public void Execute_IntExponent_ProducesPower()
        {
            var module = NewModule();
            module.Execute("r = 2 ** 8");
            Assert.That(module["r"], Is.EqualTo(256L));
        }

        [Test]
        public void Execute_NegativeExponent_PromotesToFloat()
        {
            var module = NewModule();
            module.Execute("r = 2 ** -1");
            Assert.That(module["r"], Is.EqualTo(0.5));
        }

        [Test]
        public void Execute_IntFloatPromotion_ProducesFloat()
        {
            var module = NewModule();
            module.Execute("r = 1 + 2.0");
            Assert.That(module["r"], Is.EqualTo(3.0));
        }

        [Test]
        public void Execute_UnaryNegation_NegatesValue()
        {
            var module = NewModule();
            module.Execute("r = -5");
            Assert.That(module["r"], Is.EqualTo(-5L));
        }

        [Test]
        public void Execute_DoubleNegation_RestoresValue()
        {
            var module = NewModule();
            module.Execute("r = --5");
            Assert.That(module["r"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_StringConcat_ProducesConcatenated()
        {
            var module = NewModule();
            module.Execute("r = \"hello \" + \"world\"");
            Assert.That(module["r"], Is.EqualTo("hello world"));
        }

        [Test]
        public void Execute_StringRepeat_ProducesRepeated()
        {
            var module = NewModule();
            module.Execute("r = \"ab\" * 3");
            Assert.That(module["r"], Is.EqualTo("ababab"));
        }

        [Test]
        public void Execute_OperatorPrecedence_MultiplyBeforeAdd()
        {
            var module = NewModule();
            module.Execute("r = 2 + 3 * 4");
            Assert.That(module["r"], Is.EqualTo(14L));
        }

        [Test]
        public void Execute_Parentheses_OverridePrecedence()
        {
            var module = NewModule();
            module.Execute("r = (2 + 3) * 4");
            Assert.That(module["r"], Is.EqualTo(20L));
        }

        #endregion

        #region Execute - Comparison

        [Test]
        public void Execute_IntEqual_ProducesTrue()
        {
            var module = NewModule();
            module.Execute("r = 5 == 5");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_IntNotEqual_ProducesTrue()
        {
            var module = NewModule();
            module.Execute("r = 5 != 6");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_IntLess_ProducesTrue()
        {
            var module = NewModule();
            module.Execute("r = 3 < 5");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_IntGreater_ProducesTrue()
        {
            var module = NewModule();
            module.Execute("r = 5 > 3");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_IntLessOrEqual_BoundaryReturnsTrue()
        {
            var module = NewModule();
            module.Execute("r = 5 <= 5");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_IntGreaterOrEqual_BoundaryReturnsTrue()
        {
            var module = NewModule();
            module.Execute("r = 5 >= 5");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_FloatComparison_Works()
        {
            var module = NewModule();
            module.Execute("r = 1.5 < 2.5");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_StringEquality_Works()
        {
            var module = NewModule();
            module.Execute("r = \"abc\" == \"abc\"");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_CrossTypeIntFloatEqual_PromotesAndCompares()
        {
            var module = NewModule();
            module.Execute("r = 1 == 1.0");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_StringInequality_Works()
        {
            var module = NewModule();
            module.Execute("r = \"abc\" != \"def\"");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        #endregion

        #region Execute - Logical Operators

        [Test]
        public void Execute_LogicalAndBothTrue_ProducesSecond()
        {
            var module = NewModule();
            module.Execute("r = True and True");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_LogicalAndFirstFalse_ShortCircuits()
        {
            var module = NewModule();
            module.Execute("r = False and True");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        [Test]
        public void Execute_LogicalOrFirstTrue_ShortCircuits()
        {
            var module = NewModule();
            module.Execute("r = True or False");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_LogicalOrBothFalse_ProducesFalse()
        {
            var module = NewModule();
            module.Execute("r = False or False");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        [Test]
        public void Execute_LogicalNotTrue_ProducesFalse()
        {
            var module = NewModule();
            module.Execute("r = not True");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        [Test]
        public void Execute_LogicalNotFalse_ProducesTrue()
        {
            var module = NewModule();
            module.Execute("r = not False");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        #endregion

        #region Execute - Control Flow

        [Test]
        public void Execute_IfTrueBranch_ExecutesBody()
        {
            var module = NewModule();
            module.Execute("r = 0\nif True:\n    r = 1");
            Assert.That(module["r"], Is.EqualTo(1L));
        }

        [Test]
        public void Execute_IfFalseBranch_SkipsBody()
        {
            var module = NewModule();
            module.Execute("r = 0\nif False:\n    r = 1");
            Assert.That(module["r"], Is.EqualTo(0L));
        }

        [Test]
        public void Execute_IfElse_TakesElseWhenFalse()
        {
            var module = NewModule();
            module.Execute("if False:\n    r = 1\nelse:\n    r = 2");
            Assert.That(module["r"], Is.EqualTo(2L));
        }

        [Test]
        public void Execute_IfElifElse_TakesElifWhenMatching()
        {
            var module = NewModule();
            module.Execute("x = 2\nif x == 1:\n    r = 10\nelif x == 2:\n    r = 20\nelse:\n    r = 30");
            Assert.That(module["r"], Is.EqualTo(20L));
        }

        [Test]
        public void Execute_WhileLoop_AccumulatesCount()
        {
            var module = NewModule();
            module.Execute("r = 0\nwhile r < 5:\n    r = r + 1");
            Assert.That(module["r"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_ForOverRange_IteratesExpectedTimes()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(4):\n    r = r + 1");
            Assert.That(module["r"], Is.EqualTo(4L));
        }

        [Test]
        public void Execute_ForOverRange_SumIndices()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(5):\n    r = r + i");
            Assert.That(module["r"], Is.EqualTo(10L));
        }

        [Test]
        public void Execute_BreakStopsLoop()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(10):\n    if i == 3:\n        break\n    r = r + 1");
            Assert.That(module["r"], Is.EqualTo(3L));
        }

        [Test]
        public void Execute_ContinueSkipsIteration()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(5):\n    if i == 2:\n        continue\n    r = r + 1");
            Assert.That(module["r"], Is.EqualTo(4L));
        }

        [Test]
        public void Execute_NestedIf_InnerBodyReached()
        {
            var module = NewModule();
            module.Execute("if True:\n    if True:\n        r = 99");
            Assert.That(module["r"], Is.EqualTo(99L));
        }

        [Test]
        public void Execute_NestedLoops_ProduceProduct()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(3):\n    for j in range(3):\n        r = r + 1");
            Assert.That(module["r"], Is.EqualTo(9L));
        }

        #endregion

        #region Execute - Functions

        [Test]
        public void Execute_DefAndCallSameBlock_ReturnsResult()
        {
            var module = NewModule();
            module.Execute("def add(a, b):\n    return a + b\nr = add(2, 3)");
            Assert.That(module["r"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_DefInOneCall_CalledInNextCall_ReturnsResult()
        {
            var module = NewModule();
            module.Execute("def sq(x):\n    return x * x");
            module.Execute("r = sq(7)");
            Assert.That(module["r"], Is.EqualTo(49L));
        }

        [Test]
        public void Execute_FunctionWithNoArgs_ReturnsValue()
        {
            var module = NewModule();
            module.Execute("def f():\n    return 42\nr = f()");
            Assert.That(module["r"], Is.EqualTo(42L));
        }

        [Test]
        public void Execute_FunctionWithEarlyReturn_StopsAtReturn()
        {
            var module = NewModule();
            module.Execute("def f(x):\n    if x > 0:\n        return 1\n    return 0\nr = f(5)");
            Assert.That(module["r"], Is.EqualTo(1L));
        }

        [Test]
        public void Execute_RecursiveFactorial_ProducesCorrectValue()
        {
            var module = NewModule();
            module.Execute("def fact(n):\n    if n <= 1:\n        return 1\n    return n * fact(n - 1)\nr = fact(6)");
            Assert.That(module["r"], Is.EqualTo(720L));
        }

        [Test]
        public void Execute_RecursiveFibonacci_ProducesCorrectValue()
        {
            var module = NewModule();
            module.Execute("def fib(n):\n    if n < 2:\n        return n\n    return fib(n - 1) + fib(n - 2)\nr = fib(10)");
            Assert.That(module["r"], Is.EqualTo(55L));
        }

        [Test]
        public void Execute_FunctionWithoutReturnValue_ReturnsNone()
        {
            var module = NewModule();
            module.Execute("def noop():\n    x = 1\nr = noop()");
            Assert.That(module["r"], Is.Null);
        }

        [Test]
        public void Execute_FunctionReadsGlobal_SeesHostSetValue()
        {
            var module = NewModule();
            module["g"] = 100;
            module.Execute("def get_g():\n    return g\nr = get_g()");
            Assert.That(module["r"], Is.EqualTo(100L));
        }

        [Test]
        public void Execute_NestedFunctionCalls_ChainsResults()
        {
            var module = NewModule();
            module.Execute("def double(x):\n    return x * 2\ndef triple(x):\n    return x * 3\nr = double(triple(4))");
            Assert.That(module["r"], Is.EqualTo(24L));
        }

        [Test]
        public void Execute_ClosureCapturesOuterVar()
        {
            var module = NewModule();
            module.Execute("def make_adder(n):\n    def adder(x):\n        return x + n\n    return adder\nadd5 = make_adder(5)");
            module.Execute("r = add5(10)");
            Assert.That(module["r"], Is.EqualTo(15L));
        }

        #endregion

        #region Execute - Lists & Dicts

        [Test]
        public void Execute_ListLiteral_AssignsList()
        {
            var module = NewModule();
            module.Execute("r = [1, 2, 3]");
            Assert.That(module["r"], Is.Not.Null);
        }

        [Test]
        public void Execute_ListIndexing_ReturnsElement()
        {
            var module = NewModule();
            module.Execute("xs = [10, 20, 30]\nr = xs[1]");
            Assert.That(module["r"], Is.EqualTo(20L));
        }

        [Test]
        public void Execute_ListAssignByIndex_MutatesList()
        {
            var module = NewModule();
            module.Execute("xs = [1, 2, 3]\nxs[0] = 99\nr = xs[0]");
            Assert.That(module["r"], Is.EqualTo(99L));
        }

        [Test]
        public void Execute_ListConcat_ProducesNewList()
        {
            var module = NewModule();
            module.Execute("xs = [1, 2] + [3, 4]\nr = xs[2]");
            Assert.That(module["r"], Is.EqualTo(3L));
        }

        [Test]
        public void Execute_ListLen_ReturnsCount()
        {
            var module = NewModule();
            module.Execute("r = len([1, 2, 3, 4])");
            Assert.That(module["r"], Is.EqualTo(4L));
        }

        [Test]
        public void Execute_DictLiteral_AssignsDict()
        {
            var module = NewModule();
            module.Execute("r = {\"a\": 1, \"b\": 2}");
            Assert.That(module["r"], Is.Not.Null);
        }

        [Test]
        public void Execute_DictLookup_ReturnsValue()
        {
            var module = NewModule();
            module.Execute("d = {\"a\": 1, \"b\": 2}\nr = d[\"b\"]");
            Assert.That(module["r"], Is.EqualTo(2L));
        }

        [Test]
        public void Execute_DictAssignByKey_AddsBinding()
        {
            var module = NewModule();
            module.Execute("d = {}\nd[\"key\"] = 42\nr = d[\"key\"]");
            Assert.That(module["r"], Is.EqualTo(42L));
        }

        [Test]
        public void Execute_DictLen_ReturnsCount()
        {
            var module = NewModule();
            module.Execute("r = len({\"a\": 1, \"b\": 2, \"c\": 3})");
            Assert.That(module["r"], Is.EqualTo(3L));
        }

        [Test]
        public void Execute_ForOverList_IteratesElements()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor x in [1, 2, 3, 4]:\n    r = r + x");
            Assert.That(module["r"], Is.EqualTo(10L));
        }

        #endregion

        #region Execute - Value Exchange

        [Test]
        public void Execute_AssignsVariable_AccessibleViaIndexer()
        {
            var module = NewModule();
            module.Execute("x = 5");
            Assert.That(module["x"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_TwoCalls_PreserveGlobalScope()
        {
            var module = NewModule();
            module.Execute("x = 5");
            module.Execute("y = x + 1");
            Assert.That(module["y"], Is.EqualTo(6L));
        }

        [Test]
        public void Indexer_SetThenExecuteReads_HostValueVisibleToChow()
        {
            var module = NewModule();
            module["x"] = 10;
            module.Execute("y = x");
            Assert.That(module["y"], Is.EqualTo(10L));
        }

        [Test]
        public void HostSet_ChowReassigns_HostReadsNewValue()
        {
            var module = NewModule();
            module["x"] = 1;
            module.Execute("x = x + 5");
            Assert.That(module["x"], Is.EqualTo(6L));
        }

        [Test]
        public void HostSetString_ChowConcats_HostReadsResult()
        {
            var module = NewModule();
            module["greeting"] = "Hello, ";
            module.Execute("greeting = greeting + \"world\"");
            Assert.That(module["greeting"], Is.EqualTo("Hello, world"));
        }

        [Test]
        public void ChowAssignsNone_HostReadsNull()
        {
            var module = NewModule();
            module.Execute("x = None");
            Assert.That(module["x"], Is.Null);
        }

        [Test]
        public void ChowDefinesList_HostReadsBackNonNull()
        {
            var module = NewModule();
            module.Execute("xs = [1, 2, 3]");
            Assert.That(module["xs"], Is.Not.Null);
        }

        [Test]
        public void ChowDefinesDict_HostReadsBackNonNull()
        {
            var module = NewModule();
            module.Execute("d = {\"k\": 1}");
            Assert.That(module["d"], Is.Not.Null);
        }

        [Test]
        public void ChowComputesFloat_HostReadsAsDouble()
        {
            var module = NewModule();
            module.Execute("r = 1.5 + 2.5");
            Assert.That(module["r"], Is.TypeOf<double>().And.EqualTo(4.0));
        }

        [Test]
        public void Execute_MultipleVariables_AllReadable()
        {
            var module = NewModule();
            module.Execute("a = 1\nb = 2\nc = 3");
            Assert.Multiple(() =>
            {
                Assert.That(module["a"], Is.EqualTo(1L));
                Assert.That(module["b"], Is.EqualTo(2L));
                Assert.That(module["c"], Is.EqualTo(3L));
            });
        }

        [Test]
        public void Execute_ReassignsExistingVariable_NewValueVisible()
        {
            var module = NewModule();
            module.Execute("x = 1");
            module.Execute("x = 99");
            Assert.That(module["x"], Is.EqualTo(99L));
        }

        #endregion

        #region Errors

        [Test]
        public void Indexer_GetUndefinedAfterFailedExecute_StillThrows()
        {
            var module = NewModule();

            try
            {
                module.Execute("syntax !! error !!");
            }
            catch
            {
                // ignore — verifying the failure didn't leak partial state
            }

            Assert.That(() => module["never_defined"], Throws.TypeOf<GlobalAccessException>());
        }

        [Test]
        public void Execute_AssignAfterFailedExecute_StillWorks()
        {
            var module = NewModule();

            try
            {
                module.Execute("definitely $$$ broken");
            }
            catch
            {
                // ignore
            }

            module.Execute("x = 7");
            Assert.That(module["x"], Is.EqualTo(7L));
        }

        [Test]
        public void Module_FreshInstance_HasNoUserVariables()
        {
            var module = NewModule();
            Assert.That(() => module["x"], Throws.InstanceOf<GlobalAccessException>());
        }

        #endregion

        #region Python Parity - Chained Comparison

        [Test]
        public void Execute_ChainedLessThan_TrueWhenIncreasing()
        {
            var module = NewModule();
            module.Execute("r = 1 < 2 < 3");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_ChainedLessThan_FalseWhenBreaks()
        {
            var module = NewModule();
            module.Execute("r = 1 < 2 < 2");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        [Test]
        public void Execute_ChainedMixed_Equality()
        {
            var module = NewModule();
            module.Execute("r = 1 == 1 < 2");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        #endregion

        #region Python Parity - and/or Operand Preservation

        [Test]
        public void Execute_AndBothTruthy_ReturnsRightOperand()
        {
            var module = NewModule();
            module.Execute("r = 1 and 2");
            Assert.That(module["r"], Is.EqualTo(2L));
        }

        [Test]
        public void Execute_AndFirstFalsy_ReturnsLeftOperand()
        {
            var module = NewModule();
            module.Execute("r = 0 and 2");
            Assert.That(module["r"], Is.EqualTo(0L));
        }

        [Test]
        public void Execute_OrFirstTruthy_ReturnsLeftOperand()
        {
            var module = NewModule();
            module.Execute("r = 1 or 2");
            Assert.That(module["r"], Is.EqualTo(1L));
        }

        [Test]
        public void Execute_OrBothFalsy_ReturnsRightOperand()
        {
            var module = NewModule();
            module.Execute("r = 0 or 0");
            Assert.That(module["r"], Is.EqualTo(0L));
        }

        [Test]
        public void Execute_OrFalsyString_ReturnsRightOperand()
        {
            var module = NewModule();
            module.Execute("r = \"\" or \"x\"");
            Assert.That(module["r"], Is.EqualTo("x"));
        }

        [Test]
        public void Execute_AndString_PreservesType()
        {
            var module = NewModule();
            module.Execute("r = \"a\" and \"b\"");
            Assert.That(module["r"], Is.EqualTo("b"));
        }

        #endregion

        #region Python Parity - Truthiness

        [Test]
        public void Execute_TruthinessZeroIsFalsy()
        {
            var module = NewModule();
            module.Execute("r = not 0");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_TruthinessEmptyStringIsFalsy()
        {
            var module = NewModule();
            module.Execute("r = not \"\"");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_TruthinessEmptyListIsFalsy()
        {
            var module = NewModule();
            module.Execute("r = not []");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_TruthinessEmptyDictIsFalsy()
        {
            var module = NewModule();
            module.Execute("r = not {}");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_TruthinessNoneIsFalsy()
        {
            var module = NewModule();
            module.Execute("r = not None");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_TruthinessNonzeroIntIsTruthy()
        {
            var module = NewModule();
            module.Execute("r = not 5");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        [Test]
        public void Execute_TruthinessNonEmptyStringIsTruthy()
        {
            var module = NewModule();
            module.Execute("r = not \"a\"");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        [Test]
        public void Execute_TruthinessZeroFloatIsFalsy()
        {
            var module = NewModule();
            module.Execute("r = not 0.0");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        #endregion

        #region Python Parity - in Operator

        [Test]
        public void Execute_InOperator_ListMembershipFound()
        {
            var module = NewModule();
            module.Execute("r = 3 in [1, 2, 3]");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        [Test]
        public void Execute_InOperator_ListMembershipMissing()
        {
            var module = NewModule();
            module.Execute("r = 9 in [1, 2, 3]");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        /* SUBSTRING IS NOT SUPPORTED YET
        [Test]
        public void Execute_InOperator_StringSubstring()
        {
            var module = NewModule();
            module.Execute("r = \"ll\" in \"hello\"");
            Assert.That(module["r"], Is.EqualTo(true));
        }
        */

        [Test]
        public void Execute_InOperator_DictKey()
        {
            var module = NewModule();
            module.Execute("r = \"k\" in {\"k\": 1}");
            Assert.That(module["r"], Is.EqualTo(true));
        }

        #endregion

        #region Python Parity - Indexing and Slicing

        [Test]
        public void Execute_ListPositiveIndex_ReturnsElement()
        {
            var module = NewModule();
            module.Execute("r = [10, 20, 30][1]");
            Assert.That(module["r"], Is.EqualTo(20L));
        }

        [Test]
        public void Execute_ListNegativeIndex_ReturnsFromEnd()
        {
            var module = NewModule();
            module.Execute("r = [10, 20, 30][-1]");
            Assert.That(module["r"], Is.EqualTo(30L));
        }

        // TODO: Re-enable when string subscripting is implemented.
        // [Test]
        public void Execute_StringIndex_ReturnsChar()
        {
            var module = NewModule();
            module.Execute("r = \"hello\"[0]");
            Assert.That(module["r"], Is.EqualTo("h"));
        }

        // TODO: Re-enable when string subscripting is implemented.
        // [Test]
        public void Execute_StringNegativeIndex_ReturnsFromEnd()
        {
            var module = NewModule();
            module.Execute("r = \"hello\"[-1]");
            Assert.That(module["r"], Is.EqualTo("o"));
        }

        [Test]
        public void Execute_ListSlice_ReturnsSubList()
        {
            var module = NewModule();
            module.Execute("r = len([1, 2, 3, 4][1:3])");
            Assert.That(module["r"], Is.EqualTo(2L));
        }

        // TODO: Re-enable when string slicing is implemented.
        // [Test]
        public void Execute_StringSlice_ReturnsSubString()
        {
            var module = NewModule();
            module.Execute("r = \"hello\"[1:4]");
            Assert.That(module["r"], Is.EqualTo("ell"));
        }

        // TODO: Re-enable when string slicing is implemented.
        // [Test]
        public void Execute_StringSliceStep_EveryOther()
        {
            var module = NewModule();
            module.Execute("r = \"abcdef\"[::2]");
            Assert.That(module["r"], Is.EqualTo("ace"));
        }

        [Test]
        public void Execute_ListSliceNegativeStep_Reverses()
        {
            var module = NewModule();
            module.Execute("r = len([1, 2, 3][::-1])");
            Assert.That(module["r"], Is.EqualTo(3L));
        }

        #endregion

        #region Python Parity - String Iteration

        [Test]
        public void Execute_ForOverString_VisitsEachChar()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor c in \"hello\":\n    r = r + 1");
            Assert.That(module["r"], Is.EqualTo(5L));
        }

        [Test]
        public void Execute_ForOverString_ConcatPreservesOrder()
        {
            var module = NewModule();
            module.Execute("r = \"\"\nfor c in \"ABC\":\n    r = r + c");
            Assert.That(module["r"], Is.EqualTo("ABC"));
        }

        #endregion

        #region Python Parity - range(start, stop, step)

        [Test]
        public void Execute_RangeTwoArg_StartStop()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(2, 5):\n    r = r + i");
            Assert.That(module["r"], Is.EqualTo(9L));
        }

        [Test]
        public void Execute_RangeThreeArg_PositiveStep()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(0, 10, 2):\n    r = r + i");
            Assert.That(module["r"], Is.EqualTo(20L));
        }

        [Test]
        public void Execute_RangeThreeArg_NegativeStep()
        {
            var module = NewModule();
            module.Execute("r = 0\nfor i in range(5, 0, -1):\n    r = r + i");
            Assert.That(module["r"], Is.EqualTo(15L));
        }

        // TODO: Update to be more specific to the actual exception type
        /*
        [Test]
        public void Execute_RangeStepZero_Throws()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("for i in range(0, 5, 0):\n    x = i"),
                Throws.InstanceOf<ChowRuntimeException>());
        }
        */

        #endregion

        #region Python Parity - f-strings

        [Test]
        public void Execute_FString_IntInterpolation()
        {
            var module = NewModule();
            module.Execute("x = 5\nr = f\"v={x}\"");
            Assert.That(module["r"], Is.EqualTo("v=5"));
        }

        [Test]
        public void Execute_FString_StringInterpolation()
        {
            var module = NewModule();
            module.Execute("x = \"hello\"\nr = f\"v={x}\"");
            Assert.That(module["r"], Is.EqualTo("v=hello"));
        }

        [Test]
        public void Execute_FString_Expression()
        {
            var module = NewModule();
            module.Execute("r = f\"sum={1 + 2}\"");
            Assert.That(module["r"], Is.EqualTo("sum=3"));
        }

        [Test]
        public void Execute_FString_MultiplePlaceholders()
        {
            var module = NewModule();
            module.Execute("a = 1\nb = 2\nr = f\"a={a} b={b}\"");
            Assert.That(module["r"], Is.EqualTo("a=1 b=2"));
        }

        [Test]
        public void Execute_FString_PlainPlaceholder()
        {
            var module = NewModule();
            module.Execute("x = 42\nr = f\"{x}\"");
            Assert.That(module["r"], Is.EqualTo("42"));
        }

        #endregion

        #region Python Parity - global Keyword

        [Test]
        public void Execute_GlobalAssign_InFunction_AffectsModuleScope()
        {
            var module = NewModule();
            module.Execute("x = 0\ndef setX():\n    global x\n    x = 99\nsetX()");
            Assert.That(module["x"], Is.EqualTo(99L));
        }

        [Test]
        public void Execute_NoGlobal_AssignIsLocal_ModuleUnchanged()
        {
            var module = NewModule();
            module.Execute("x = 0\ndef setX():\n    x = 99\nsetX()");
            Assert.That(module["x"], Is.EqualTo(0L));
        }

        [Test]
        public void Execute_GlobalRead_NoDeclarationNeeded()
        {
            var module = NewModule();
            module.Execute("x = 7\ndef readX():\n    return x\nr = readX()");
            Assert.That(module["r"], Is.EqualTo(7L));
        }

        #endregion

        #region Python Parity - Runtime Errors

        [Test]
        public void Execute_IntDivByZero_ThrowsZeroDivisionException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = 1 / 0"),
                Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Execute_FloorDivByZero_ThrowsZeroDivisionException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = 1 // 0"),
                Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Execute_ModByZero_ThrowsZeroDivisionException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = 1 % 0"),
                Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Execute_StringPlusInt_ThrowsTypeException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = \"a\" + 1"),
                Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Execute_ListPlusInt_ThrowsTypeException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = [1, 2] + 3"),
                Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Execute_DictMissingKey_ThrowsDictKeyException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("d = {\"a\": 1}\nr = d[\"b\"]"),
                Throws.TypeOf<DictKeyException>());
        }

        [Test]
        public void Execute_UndefinedNameRead_ThrowsUndefinedNameException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = nopeNeverDefined"),
                Throws.TypeOf<UndefinedNameException>());
        }

        /* DISABLING FOR NOW, BECAUSE MODULE EXCEPTIONS LIKE THIS ARE GOING TO CHANGE TO BE CLOSER TO PYTHON SOON, AND

        [Test]
        public void Execute_ListIndexOutOfRange_ThrowsChowRuntimeException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = [1, 2, 3][99]"),
                Throws.InstanceOf<ChowRuntimeException>());
        }
        */

        #endregion

        #region Python Parity - Float Edge Cases

        [Test]
        public void Execute_NegativeZeroFloat_RoundTrips()
        {
            var module = NewModule();
            module.Execute("r = -0.0");
            Assert.That((double)module["r"], Is.EqualTo(-0.0));
        }

        [Test]
        public void Execute_FloatDivByZero_ThrowsZeroDivisionException()
        {
            var module = NewModule();
            Assert.That(() => module.Execute("r = 1.0 / 0.0"),
                Throws.TypeOf<ZeroDivisionException>());
        }

        [Test]
        public void Execute_NaNNotEqualToItself()
        {
            var module = NewModule();
            module.Execute("x = float(\"nan\")\nr = x == x");
            Assert.That(module["r"], Is.EqualTo(false));
        }

        #endregion

        #region Call - Errors

        [Test]
        public void Call_UndefinedName_ThrowsGlobalAccessException()
        {
            var module = NewModule();
            Assert.That(() => module.InvokeGlobal("nope"), Throws.TypeOf<GlobalAccessException>()
                .With.Property(nameof(GlobalAccessException.Name)).EqualTo("nope"));
        }

        [Test]
        public void Call_UndefinedName_NamePropertyMatches()
        {
            var module = NewModule();

            try
            {
                module.InvokeGlobal("missing_func", 1, 2);
                Assert.Fail();
            }
            catch (GlobalAccessException ex)
            {
                Assert.That(ex.Name, Is.EqualTo("missing_func"));
            }
        }

        [Test]
        public void Call_IntGlobal_ThrowsTypeException()
        {
            var module = NewModule();
            module["x"] = 42;
            Assert.That(() => module.InvokeGlobal("x"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_StringGlobal_ThrowsTypeException()
        {
            var module = NewModule();
            module["s"] = "hello";
            Assert.That(() => module.InvokeGlobal("s"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_BoolGlobal_ThrowsTypeException()
        {
            var module = NewModule();
            module["b"] = true;
            Assert.That(() => module.InvokeGlobal("b"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_DoubleGlobal_ThrowsTypeException()
        {
            var module = NewModule();
            module["d"] = 1.5;
            Assert.That(() => module.InvokeGlobal("d"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_ListGlobalFromExecute_ThrowsTypeException()
        {
            var module = NewModule();
            module.Execute("xs = [1, 2, 3]");
            Assert.That(() => module.InvokeGlobal("xs"), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_NullObjectArg_ThrowsArgumentNullException()
        {
            var module = NewModule();
            module.Execute("def f(x):\n    return x");
            Assert.That(() => module.InvokeGlobal("f", new object?[] { null }!),
                Throws.TypeOf<ArgumentNullException>());
        }

        #endregion

        #region Call - Interop (host delegates)

        [Test]
        public void Call_HostDelegateNoArgs_ReturnsLiteral()
        {
            var module = NewModule();
            module["host"] = (Func<ChowValue[], ChowValue>)(_ => new ChowValue(123L));

            var result = module.InvokeGlobal("host");

            Assert.That(result.AsType<long>(), Is.EqualTo(123L));
        }

        [Test]
        public void Call_HostDelegate_ReceivesIntArg()
        {
            var module = NewModule();
            module["host"] = (Func<ChowValue[], ChowValue>)(args => new ChowValue(args[0].AsType<long>() * 2));

            var result = module.InvokeGlobal("host", 7);

            Assert.That(result.AsType<long>(), Is.EqualTo(14L));
        }

        [Test]
        public void Call_HostDelegate_ReceivesMultipleMixedArgs()
        {
            var module = NewModule();
            module["host"] = (Func<ChowValue[], ChowValue>)(args =>
            {
                var i = args[0].AsType<long>();
                var s = args[1].AsType<string>();
                var b = args[2].AsType<bool>();
                return new ChowValue($"{i}|{s}|{b}");
            });

            var result = module.InvokeGlobal("host", 5, "abc", true);

            Assert.That(result.AsType<string>(), Is.EqualTo("5|abc|True"));
        }

        [Test]
        public void Call_HostDelegate_LongArgPreserved()
        {
            var module = NewModule();
            module["host"] = (Func<ChowValue[], ChowValue>)(args => args[0]);

            var result = module.InvokeGlobal("host", 9_999_999_999L);

            Assert.That(result.AsType<long>(), Is.EqualTo(9_999_999_999L));
        }

        [Test]
        public void Call_HostDelegate_DoubleArgPreserved()
        {
            var module = NewModule();
            module["host"] = (Func<ChowValue[], ChowValue>)(args => args[0]);

            var result = module.InvokeGlobal("host", 2.5);

            Assert.That(result.AsType<double>(), Is.EqualTo(2.5));
        }

        [Test]
        public void Call_HostDelegate_InvocationFiresOnce()
        {
            var module = NewModule();
            var count = 0;
            module["host"] = (Func<ChowValue[], ChowValue>)(_ =>
            {
                count++;
                return ChowValue.None;
            });

            module.InvokeGlobal("host");
            module.InvokeGlobal("host");

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Call_HostDelegate_NoArgs_PassesEmptyArray()
        {
            var module = NewModule();
            int? receivedLength = null;
            module["host"] = (Func<ChowValue[], ChowValue>)(args =>
            {
                receivedLength = args.Length;
                return ChowValue.None;
            });

            module.InvokeGlobal("host");

            Assert.That(receivedLength, Is.EqualTo(0));
        }

        [Test]
        public void Call_BuiltinAbs_NegativeInt_ReturnsAbsolute()
        {
            var module = NewModule();
            var result = module.InvokeGlobal("abs", -42);
            Assert.That(result.AsType<long>(), Is.EqualTo(42L));
        }

        [Test]
        public void Call_BuiltinLen_String_ReturnsLength()
        {
            var module = NewModule();
            var result = module.InvokeGlobal("len", "hello");
            Assert.That(result.AsType<long>(), Is.EqualTo(5L));
        }

        [Test]
        public void Call_BuiltinStr_Int_ReturnsString()
        {
            var module = NewModule();
            var result = module.InvokeGlobal("str", 7);
            Assert.That(result.AsType<string>(), Is.EqualTo("7"));
        }

        [Test]
        public void Call_BuiltinMin_TwoInts_ReturnsLower()
        {
            var module = NewModule();
            var result = module.InvokeGlobal("min", 3, 1);
            Assert.That(result.AsType<long>(), Is.EqualTo(1L));
        }

        [Test]
        public void Call_BuiltinMax_TwoInts_ReturnsHigher()
        {
            var module = NewModule();
            var result = module.InvokeGlobal("max", 3, 1);
            Assert.That(result.AsType<long>(), Is.EqualTo(3L));
        }

        #endregion

        #region Call - Closures (Chow def)

        [Test]
        public void Call_DefinedClosureNoArgs_ReturnsLiteral()
        {
            var module = NewModule();
            module.Execute("def f():\n    return 42");

            var result = module.InvokeGlobal("f");

            Assert.That(result.AsType<long>(), Is.EqualTo(42L));
        }

        [Test]
        public void Call_DefinedClosure_AddsTwoInts()
        {
            var module = NewModule();
            module.Execute("def add(a, b):\n    return a + b");

            var result = module.InvokeGlobal("add", 2, 3);

            Assert.That(result.AsType<long>(), Is.EqualTo(5L));
        }

        [Test]
        public void Call_DefinedClosure_ArgOrderPreserved()
        {
            var module = NewModule();
            module.Execute("def sub(a, b):\n    return a - b");

            var result = module.InvokeGlobal("sub", 10, 3);

            Assert.That(result.AsType<long>(), Is.EqualTo(7L));
        }

        [Test]
        public void Call_DefinedClosure_StringConcat()
        {
            var module = NewModule();
            module.Execute("def join(a, b):\n    return a + b");

            var result = module.InvokeGlobal("join", "foo", "bar");

            Assert.That(result.AsType<string>(), Is.EqualTo("foobar"));
        }

        [Test]
        public void Call_DefinedClosureNoExplicitReturn_ReturnsNone()
        {
            var module = NewModule();
            module.Execute("def f():\n    x = 1");

            var result = module.InvokeGlobal("f");

            Assert.That(result.DataType, Is.EqualTo(DataType.None));
        }

        [Test]
        public void Call_DefinedClosure_TooFewArgs_ThrowsTypeException()
        {
            var module = NewModule();
            module.Execute("def add(a, b):\n    return a + b");

            Assert.That(() => module.InvokeGlobal("add", 1), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_DefinedClosure_TooManyArgs_ThrowsTypeException()
        {
            var module = NewModule();
            module.Execute("def add(a, b):\n    return a + b");

            Assert.That(() => module.InvokeGlobal("add", 1, 2, 3), Throws.TypeOf<TypeException>());
        }

        [Test]
        public void Call_DefinedClosure_CalledTwiceWithDifferentArgs_NoStateBleed()
        {
            var module = NewModule();
            module.Execute("def square(x):\n    return x * x");

            var first = module.InvokeGlobal("square", 3);
            var second = module.InvokeGlobal("square", 5);

            Assert.Multiple(() =>
            {
                Assert.That(first.AsType<long>(), Is.EqualTo(9L));
                Assert.That(second.AsType<long>(), Is.EqualTo(25L));
            });
        }

        [Test]
        public void Call_DefinedClosure_ReadsModuleGlobal()
        {
            var module = NewModule();
            module.Execute("base = 100\ndef addBase(x):\n    return base + x");

            var result = module.InvokeGlobal("addBase", 7);

            Assert.That(result.AsType<long>(), Is.EqualTo(107L));
        }

        [Test]
        public void Call_DefinedClosure_ReadsGlobalSetViaIndexer()
        {
            var module = NewModule();
            module["base"] = 50;
            module.Execute("def addBase(x):\n    return base + x");

            var result = module.InvokeGlobal("addBase", 4);

            Assert.That(result.AsType<long>(), Is.EqualTo(54L));
        }

        [Test]
        public void Call_DefinedClosure_CallsOtherClosure()
        {
            var module = NewModule();
            module.Execute("def inc(x):\n    return x + 1\ndef twice(x):\n    return inc(inc(x))");

            var result = module.InvokeGlobal("twice", 5);

            Assert.That(result.AsType<long>(), Is.EqualTo(7L));
        }

        [Test]
        public void Call_DefinedClosure_Recursive_Factorial()
        {
            var module = NewModule();
            module.Execute("def fact(n):\n    if n <= 1:\n        return 1\n    return n * fact(n - 1)");

            var result = module.InvokeGlobal("fact", 5);

            Assert.That(result.AsType<long>(), Is.EqualTo(120L));
        }

        [Test]
        public void Call_DefinedClosure_UsingBuiltinFromBody()
        {
            var module = NewModule();
            module.Execute("def absPlusOne(x):\n    return abs(x) + 1");

            var result = module.InvokeGlobal("absPlusOne", -10);

            Assert.That(result.AsType<long>(), Is.EqualTo(11L));
        }

        [Test]
        public void Call_DefinedClosure_BoolArg_PromotedToInt()
        {
            var module = NewModule();
            module.Execute("def add(a, b):\n    return a + b");

            var result = module.InvokeGlobal("add", true, 5);

            Assert.That(result.AsType<long>(), Is.EqualTo(6L));
        }

        [Test]
        public void Call_DefinedClosure_DoubleArg_ReturnsFloat()
        {
            var module = NewModule();
            module.Execute("def halve(x):\n    return x / 2");

            var result = module.InvokeGlobal("halve", 5.0);

            Assert.That(result.AsType<double>(), Is.EqualTo(2.5));
        }

        [Test]
        public void Call_DefinedClosure_ZeroArgsAfterMultipleExecutes()
        {
            var module = NewModule();
            module.Execute("a = 1");
            module.Execute("b = 2");
            module.Execute("def sum():\n    return a + b");

            var result = module.InvokeGlobal("sum");

            Assert.That(result.AsType<long>(), Is.EqualTo(3L));
        }

        [Test]
        public void Call_DefinedClosure_AfterIndexerReassign_SeesNewValue()
        {
            var module = NewModule();
            module.Execute("def get():\n    return v");
            module["v"] = 10;
            var first = module.InvokeGlobal("get");
            module["v"] = 20;
            var second = module.InvokeGlobal("get");

            Assert.Multiple(() =>
            {
                Assert.That(first.AsType<long>(), Is.EqualTo(10L));
                Assert.That(second.AsType<long>(), Is.EqualTo(20L));
            });
        }

        [Test]
        public void Call_FactoryClosureReturned_BoundViaExecute_Callable()
        {
            var module = NewModule();
            module.Execute("def makeAdder():\n    def add(a, b):\n        return a + b\n    return add\nadder = makeAdder()");

            var result = module.InvokeGlobal("adder", 4, 6);

            Assert.That(result.AsType<long>(), Is.EqualTo(10L));
        }

        [Test]
        public void Call_DefinedClosure_StringArgPreserved()
        {
            var module = NewModule();
            module.Execute("def identity(x):\n    return x");

            var result = module.InvokeGlobal("identity", "world");

            Assert.That(result.AsType<string>(), Is.EqualTo("world"));
        }

        #endregion

    }
}
