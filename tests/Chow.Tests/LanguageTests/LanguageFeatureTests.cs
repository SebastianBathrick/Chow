using Chow;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;

namespace Chow.Tests;

[TestFixture]
public class LanguageFeatureTests
{
    #region Methods
    
    [TestCaseSource(nameof(ControlFlowCases))]
    [TestCaseSource(nameof(ExecuteStringConcatenationAndFStringCases))]
    [TestCaseSource(nameof(ExecuteBuiltInFunctionCases))]
    [TestCaseSource(nameof(ExecuteIterationCases))]
    [TestCaseSource(nameof(ExecuteDictionaryCases))]
    [TestCaseSource(nameof(ExecuteIteratorCases))]
    [TestCaseSource(nameof(ExecuteSetCases))]
    [TestCaseSource(nameof(ExecuteSetAlgebraCases))]
    [TestCaseSource(nameof(ExecuteCollectionSubscriptAndAssignmentCases))]
    [TestCaseSource(nameof(ExecuteFunctionScopeAndClosureCases))]
    [TestCaseSource(nameof(ExecuteClassCases))]
    [TestCaseSource(nameof(LiteralValueCases))]
    [TestCaseSource(nameof(ExecuteLogicOperatorCases))]
    [TestCaseSource(nameof(ExecuteComparisonOperatorCases))]
    [TestCaseSource(nameof(ExecuteEmptyWhitespaceOrNullCases))]
    [TestCaseSource(nameof(ExecuteArithmeticOperatorCases))]
    public void Execute_ValidSourceCode_ReturnExpectedResult(CaseExecute caseExecute)
    {
        var returnValue = ChowEngine.Run(caseExecute.SourceCode);

        Assert.That(returnValue, Is.EqualTo(caseExecute.ExpectedResult));
    }

    [TestCaseSource(nameof(ClassAttributeErrorCases))]
    public void Execute_InvalidClassAttributeUse_ThrowsAttributeException(string sourceCode)
    {
        Assert.Throws<AttributeException>(() => ChowEngine.Run(sourceCode));
    }

    [TestCaseSource(nameof(ClassArityErrorCases))]
    public void Execute_InvalidClassCallArity_ThrowsDataTypeException(string sourceCode)
    {
        Assert.Throws<DataTypeException>(() => ChowEngine.Run(sourceCode));
    }

    [TestCase("{'a': 1}['missing']")]
    [TestCase("{'a': 1}.pop('missing')")]
    public void Execute_MissingDictKey_ThrowsSubscriptException(string sourceCode)
    {
        var exception = Assert.Throws<SubscriptException>(() => ChowEngine.Run(sourceCode));

        // Python: KeyError: 'missing'
        Assert.That(exception!.Message, Does.StartWith("KeyError: 'missing'"));
    }

    [TestCase("set([1, 2]).remove(3)", "3")]
    [TestCase("set([1, 2]).remove('k')", "'k'")]
    [TestCase("set().pop()", "'pop from an empty set'")]
    public void Execute_MissingSetElement_ThrowsKeyErrorWithPythonMessage(string sourceCode, string expectedKey)
    {
        var exception = Assert.Throws<SubscriptException>(() => ChowEngine.Run(sourceCode));

        Assert.That(exception!.Message, Does.StartWith($"KeyError: {expectedKey} on line "));
    }

    [TestCase("frozenset().add(1)", "frozenset", "add")]
    [TestCase("frozenset([1]).discard(1)", "frozenset", "discard")]
    [TestCase("frozenset().intersection_update([1])", "frozenset", "intersection_update")]
    [TestCase("frozenset().difference_update()", "frozenset", "difference_update")]
    [TestCase("{1: 2}.values().isdisjoint([])", "dict_values", "isdisjoint")]
    public void Execute_MissingCollectionMethod_ThrowsAttributeErrorWithPythonMessage(
        string sourceCode, string typeName, string method)
    {
        var exception = Assert.Throws<AttributeException>(() => ChowEngine.Run(sourceCode));

        Assert.That(exception!.Message,
            Does.StartWith($"AttributeError: '{typeName}' object has no attribute '{method}' on line "));
    }

    [TestCase("{'a': 1}.keys()[0]")]
    [TestCase("{'a': 1}.values()[0]")]
    [TestCase("{[1]: 2}")]
    [TestCase("{'a': 1}.keys() in {}")]
    public void Execute_InvalidDictViewOrKeyUse_ThrowsDataTypeException(string sourceCode)
    {
        Assert.Throws<DataTypeException>(() => ChowEngine.Run(sourceCode));
    }

    [TestCaseSource(nameof(TypeErrorMessageCases))]
    public void Execute_UnsupportedOperation_ThrowsTypeErrorWithPythonMessage(string sourceCode, string expectedMessage)
    {
        var exception = Assert.Throws<DataTypeException>(() => ChowEngine.Run(sourceCode));

        Assert.That(exception!.Message, Does.StartWith($"TypeError: {expectedMessage} on line "));
    }

    [Test]
    public void Execute_NextOnExhaustedIteratorWithoutDefault_ThrowsBareStopIteration()
    {
        var exception = Assert.Throws<StopIterationException>(() => ChowEngine.Run("next(iter([]))"));

        // Python: StopIteration carries no description.
        Assert.That(exception!.Message, Is.EqualTo("StopIteration on line 1"));
    }

    [TestCaseSource(nameof(DictMutatedDuringIterationCases))]
    [TestCaseSource(nameof(SetMutatedDuringIterationCases))]
    public void Execute_CollectionMutatedDuringIteration_ThrowsRuntimeErrorWhereCPythonDoes(
        string sourceCode, string expectedLog, string expectedMessage)
    {
        var scope = new ChowScope();

        var exception = Assert.Throws<RuntimeErrorException>(() => ChowEngine.Run(sourceCode, scope));

        Assert.Multiple(() =>
        {
            // The keys yielded before the error must match CPython's too.
            Assert.That(scope["log"].ToString(), Is.EqualTo(expectedLog));
            Assert.That(exception!.Message, Does.StartWith($"RuntimeError: {expectedMessage}"));
        });
    }

    [TestCaseSource(nameof(ClassSyntaxErrorCases))]
    public void Execute_UnsupportedClassSyntax_ThrowsSyntaxException(string sourceCode)
    {
        Assert.Throws<SyntaxException>(() => ChowEngine.Run(sourceCode));
    }

    #endregion

    #region ProcessInstructions: Class Errors

    static readonly IReadOnlyList<string> ClassAttributeErrorCases =
    [
        // Attribute defined by neither the instance nor its class.
        """
        class Point:
            def __init__(self, x):
                self.x = x

        Point(1).y
        """,

        // Instance fields do not leak onto the class that produced them.
        """
        class Point:
            def __init__(self, x):
                self.x = x

        Point(1)
        Point.x
        """,

        // Types without an attribute table still reject assignment.
        """
        values = [1, 2]
        values.total = 3
        """
    ];

    static readonly IReadOnlyList<string> ClassArityErrorCases =
    [
        // Too few arguments for the constructor.
        """
        class Point:
            def __init__(self, x, y):
                self.x = x
                self.y = y

        Point(1)
        """,

        // A bound method's receiver is implicit, so passing self explicitly is one too many.
        """
        class Greeter:
            def greet(self):
                return "hello"

        Greeter().greet(1)
        """,

        // A class without a constructor takes no arguments.
        """
        class Empty:
            pass

        Empty(1)
        """
    ];

    static readonly IReadOnlyList<string> ClassSyntaxErrorCases =
    [
        // Inheritance is not supported.
        """
        class Animal:
            pass

        class Dog(Animal):
            pass
        """,

        // Class bodies hold methods and class variables only.
        """
        class Broken:
            print("side effect")
        """,

        // An assignment target with no name to bind on the class.
        """
        class Broken:
            values = [1]
            values[0] = 2
        """
    ];

    #endregion

    #region Static Readonly Fields
    
    static readonly ChowObject TrueChow = new(true);
    static readonly ChowObject FalseChow = new(false);
    
    #endregion

    #region ProcessInstructions: Expression Statements
    
    static readonly IReadOnlyList<CaseExecute> LiteralValueCases =
    [
        #region Integer Literals

        // Single-digit
        new(
            "1",
            new(1)
        ),
        
        // Multi-digit
        new(
            "123",
            new(123)
        ),

        // Negative Single-digit
        new(
            "-1",
            new(-1)
        ),
        
        // Negative Multi-digit
        new(
            "-123",
            new(-123)
        ),

        #endregion

        #region Double Literals

        // Single leading / Single trailing
        new(
            "1.2",
            new(1.2)
        ),

        // Multi leading / Single trailing
        new(
            "12.3",
            new(12.3)
        ),

        // Single leading / Multi trailing
        new(
            "1.23",
            new(1.23)
        ),

        // Multi leading / Multi trailing
        new(
            "12.34",
            new(12.34)
        ),

        // Negative Single leading / Single trailing
        new(
            "-1.2",
            new(-1.2)
        ),

        // Negative Multi leading / Single trailing
        new(
            "-12.3",
            new(-12.3)
        ),

        // Negative Single leading / Multi trailing
        new(
            "-1.23",
            new(-1.23)
        ),

        // Negative Multi leading / Multi trailing
        new(
            "-12.34",
            new(-12.34)
        ),

        // Leading decimal / Single trailing
        new(
            ".1",
            new(0.1)
        ),

        // Leading decimal / Multi trailing
        new(
            ".12",
            new(0.12)
        ),

        // Single leading / Trailing decimal
        new(
            "1.",
            new(1.0)
        ),

        // Multi leading / Trailing decimal
        new(
            "12.",
            new(12.0)
        ),

        // Negative leading decimal / Single trailing
        new(
            "-.1",
            new(-0.1)
        ),

        // Negative leading decimal / Multi trailing
        new(
            "-.12",
            new(-0.12)
        ),

        // Negative single leading / Trailing decimal
        new(
            "-1.",
            new(-1.0)
        ),

        // Negative multi leading / Trailing decimal
        new(
            "-12.",
            new(-12.0)
        ),

        #endregion

        #region String, List, and Dict Literals
        
        new(
            "\"Hello, Chow\"",
            new("Hello, Chow")
        ),

        new(
            "'Single quoted'",
            new("Single quoted")
        ),

        // TODO: Write tests when host language data type conversion is added for dicts and lists
        
        new(
            "[] == []",
            TrueChow
        ),

        new(
            "[1, 2, 3] == [1, 2, 3]",
            TrueChow
        ),

        new(
            "{} == {}",
            TrueChow
        ),

        new(
            "{'a': 1, 'b': 2} == {'a': 1, 'b': 2}",
            TrueChow
        ),

        // dict `|` merge (PEP 584): disjoint keys combine.
        new(
            "({'a': 1} | {'b': 2}) == {'a': 1, 'b': 2}",
            TrueChow
        ),

        // dict `|` merge: on key conflict the right operand wins.
        new(
            "({'a': 1} | {'a': 9}) == {'a': 9}",
            TrueChow
        ),

        // NOTE: Deferred niche literal syntax not yet confirmed in Chow:
        // bytes literals, set literals/comprehensions, and raw/triple-prefixed combinations.

        #endregion
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteArithmeticOperatorCases =
    [
        // Note: Passing the SourceValue constructor a long or an int will result in a SourceValue
        // instance with DataType.Long. However, passing a float or a double will result in an
        // instance with DataType.Double. Always double-check the literal passed to the constructor.
        
        //--- Positive and Negative Operands ---
        
        #region Integer Positive Operands

        new(
            "1" + PLUS + "2",
            new(3)
        ),

        new(
            "6" + MINUS + "3",
            new(3)
        ),

        new(
            "7" + TIMES + "8",
            new(56)
        ),

        new(
            "9" + DIV + "3",
            new(3.0) // Division always converts operands to be Chow floats
        ),

        new(
            "17" + FLOOR + "4",
            new(4)
        ),

        new(
            "3" + MOD + "2",
            new(1)
        ),

        new(
            "11" + POW + "3",
            new(1331)
        ),

        #endregion

        #region Integer Negative Operands

        new(
            "-1" + PLUS + "-2",
            new(-3)
        ),

        new(
            "-6" + MINUS + "-3",
            new(-3)
        ),

        new(
            "-7" + TIMES + "-8",
            new(56)
        ),

        new(
            "-9" + DIV + "-3",
            new(3.0) // Division always converts operands to be Chow floats
        ),

        new(
            "-17" + FLOOR + "-4",
            new(4)
        ),

        new(
            "-3" + MOD + "-2",
            new(-1)
        ),

        new(
            "-11" + POW + "-3",
            new(-0.0007513148009015778)
        ),

        #endregion

        #region Integer Positive and Negative Operands

        new(
            "1" + PLUS + "-2",
            new(-1)
        ),

        new(
            "-1" + PLUS + "2",
            new(1)
        ),

        new(
            "6" + MINUS + "-3",
            new(9)
        ),

        new(
            "-6" + MINUS + "3",
            new(-9)
        ),

        new(
            "7" + TIMES + "-8",
            new(-56)
        ),

        new(
            "-7" + TIMES + "8",
            new(-56)
        ),

        new(
            "9" + DIV + "-3",
            new(-3.0)
        ),

        new(
            "-9" + DIV + "3",
            new(-3.0) 
        ),

        new(
            "17" + FLOOR + "-4",
            new(-5)
        ),

        new(
            "-17" + FLOOR + "4",
            new(-5)
        ),

        // Modulus result takes the sign of the right operand (Python rule)
        new(
            "3" + MOD + "-2",
            new(-1)
        ),

        new(
            "-3" + MOD + "2",
            new(1)
        ),

        new(
            "11" + POW + "-3",
            new(0.0007513148009015778)
        ),

        // Parses as -(11 ** 3) because exponentiation binds tighter than unary minus
        new(
            "-11" + POW + "3",
            new(-1331)
        ),

        #endregion

        #region Double Positive Operands

        new(
            "1.2" + PLUS + "2.3",
            new(3.5)
        ),

        new(
            "6.2" + MINUS + "3.0",
            new(3.2)
        ),

        new(
            "7.35" + TIMES + "8.002",
            new(58.8147)
        ),

        new(
            "9.245" + DIV + "0.5",
            new(18.49)
        ),

        new(
            "17.8" + FLOOR + "4.2",
            new(4.0)
        ),

        new(
            "3.34" + MOD + "1.2",
            new(0.9399999999999997)
        ),

        new(
            "11.5" + POW + "2.3",
            new(275.1725020936858)
        ),

        #endregion

        #region Double Negative Operands

        new(
            "-1.2" + PLUS + "-2.3",
            new(-3.5)
        ),

        new(
            "-6.2" + MINUS + "-3.0",
            new(-3.2)
        ),

        new(
            "-7.35" + TIMES + "-8.002",
            new(58.8147)
        ),

        new(
            "-9.245" + DIV + "-0.5",
            new(18.49)
        ),

        new(
            "-17.8" + FLOOR + "-4.2",
            new(4.0)
        ),
        
        new(
            "-3.34" + MOD + "-1.2",
            new(-0.9399999999999997)
        ),

        new(
            "-11.5" + POW + "-2.3",
            new(-0.0036340840468846625)
        ),

        #endregion

        #region Double Positive and Negative Operands

        new(
            "1.2" + PLUS + "-2.3",
            new(-1.0999999999999999)
        ),

        new(
            "-1.2" + PLUS + "2.3",
            new(1.0999999999999999)
        ),

        new(
            "6.2" + MINUS + "-3.0",
            new(9.2)
        ),

        new(
            "-6.2" + MINUS + "3.0",
            new(-9.2)
        ),

        new(
            "7.35" + TIMES + "-8.002",
            new(-58.8147)
        ),

        new(
            "-7.35" + TIMES + "8.002",
            new(-58.8147)
        ),

        new(
            "9.245" + DIV + "-0.5",
            new(-18.49)
        ),

        new(
            "-9.245" + DIV + "0.5",
            new(-18.49)
        ),

        // Floor division rounds toward negative infinity (Python rule), not toward zero
        new(
            "17.8" + FLOOR + "-4.2",
            new(-5.0)
        ),

        new(
            "-17.8" + FLOOR + "4.2",
            new(-5.0)
        ),

        // Modulus result takes the sign of the right operand (Python rule)
        new(
            "3.34" + MOD + "-1.2",
            new(-0.26)
        ),

        new(
            "-3.34" + MOD + "1.2",
            new(0.26)
        ),

        new(
            "11.5" + POW + "-2.3",
            new(0.0036340840468846625)
        ),

        // Parses as -(11.5 ** 2.3) because exponentiation binds tighter than unary minus
        new(
            "-11.5" + POW + "2.3",
            new(-275.1725020936858)
        ),

        #endregion


        //--- Large/Small Operands ---

        #region Left Larger Integer Operand

        new(
            "5" + PLUS + "1",
            new(6)
            ),

        new(
            "8" + MINUS + "3",
            new(5)
        ),

        new(
            "7" + TIMES + "2",
            new(14)
        ),

        new(
            "10" + DIV + "4",
            new(2.5) // Division always converts operands to be Chow floats
        ),

        new(
            "17" + FLOOR + "4",
            new(4)
        ),

        new(
            "9" + MOD + "4",
            new(1)
        ),

        new(
            "5" + POW + "2",
            new(25)
        ),

        #endregion

        #region Right Larger Integer Operand

        new(
            "1" + PLUS + "5",
            new(6)
        ),

        new(
            "3" + MINUS + "8",
            new(-5)
        ),

        new(
            "2" + TIMES + "7",
            new(14)
        ),

        new(
            "4" + DIV + "10",
            new(0.4) // Division always converts operands to be Chow floats
        ),

        new(
            "4" + FLOOR + "17",
            new(0)
        ),

        new(
            "4" + MOD + "9",
            new(4)
        ),

        new(
            "2" + POW + "5",
            new(32)
        ),

        #endregion

        #region Left Larger Double Operand

        new(
            "8.75" + PLUS + "2.5",
            new(11.25)
        ),

        new(
            "9.5" + MINUS + "3.25",
            new(6.25)
        ),

        new(
            "6.5" + TIMES + "2.5",
            new(16.25)
        ),

        new(
            "9.75" + DIV + "2.5",
            new(3.9)
        ),

        new(
            "17.8" + FLOOR + "4.2",
            new(4.0)
        ),

        new(
            "9.75" + MOD + "2.5",
            new(2.25)
        ),

        new(
            "6.25" + POW + "2.5",
            new(97.65625)
        ),

        #endregion
        
        #region Right Larger Double Operand

        new(
            "2.25" + PLUS + "8.5",
            new(10.75)
        ),

        new(
            "3.25" + MINUS + "9.5",
            new(-6.25)
        ),

        new(
            "2.5" + TIMES + "6.5",
            new(16.25)
        ),

        new(
            "4.2" + DIV + "16.8",
            new(0.25)
        ),

        new(
            "4.2" + FLOOR + "17.8",
            new(0.0)
        ),

        new(
            "2.5" + MOD + "9.75",
            new(2.5)
        ),

        new(
            "2.5" + POW + "6.25",
            new(306.9905834186854)
        ),

        #endregion
        
        //--- Data Type Coercion ---
        
        #region Boolean Operands

        new(
            TrueStr + PLUS + TrueStr,
            new(2)
        ),

        new(
            TrueStr + PLUS + FalseStr,
            new(1)
        ),

        new(
            FalseStr + PLUS + FalseStr,
            new(0)
        ),

        new(
            TrueStr + MINUS + TrueStr,
            new(0)
        ),

        new(
            TrueStr + MINUS + FalseStr,
            new(1)
        ),

        new(
            FalseStr + MINUS + FalseStr,
            new(0)
        ),

        new(
            TrueStr + TIMES + TrueStr,
            new(1)
        ),

        new(
            TrueStr + TIMES + FalseStr,
            new(0)
        ),

        new(
            FalseStr + TIMES + FalseStr,
            new(0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            TrueStr + DIV + TrueStr,
            new(1.0)
        ),

        new(
            FalseStr + DIV + TrueStr,
            new(0.0)
        ),

        new(
            TrueStr + FLOOR + TrueStr,
            new(1)
        ),

        new(
            FalseStr + FLOOR + TrueStr,
            new(0)
        ),

        new(
            TrueStr + MOD + TrueStr,
            new(0)
        ),

        new(
            FalseStr + MOD + TrueStr,
            new(0)
        ),

        new(
            TrueStr + POW + TrueStr,
            new(1)
        ),

        new(
            TrueStr + POW + FalseStr,
            new(1)
        ),

        new(
            FalseStr + POW + TrueStr,
            new(0)
        ),

        new(
            FalseStr + POW + FalseStr,
            new(1)
        ),

        #endregion

        #region String and List Operands

        new(
            "\"ab\"" + TIMES + "3",
            "ababab"
        ),

        new(
            "3" + TIMES + "\"ab\"",
            "ababab"
        ),

        new(
            "\"ab\"" + TIMES + "0",
            string.Empty
        ),

        new(
            "[1]" + PLUS + "[2]",
            List(1, 2)
        ),

        new(
            "[1]" + TIMES + "3",
            List(1, 1, 1)
        ),

        new(
            "3" + TIMES + "[1]",
            List(1, 1, 1)
        ),

        #endregion

        #region Unary Negation

        new(
            "-" + TrueStr,
            -1
        ),

        new(
            "-" + FalseStr,
            0
        ),

        new(
            "-3.5",
            -3.5
        ),

        #endregion
        
        #region Integer/Double Mixed Operands

        new(
            "2.5" + PLUS + "7",
            new(9.5)
            ),
        
        new(
            "2.5" + MINUS + "7",
            new(-4.5)
            ),
        
        new(
            "2.5" + TIMES + "7",
            new(17.5)
            ),
        
        new(
            "2.5" + DIV + "7",
            new(0.35714285714285715)
            ),
        
        new(
            "14.5" + FLOOR + "7",
            new(2.0)
        ),
        
        new(
            "14" + FLOOR + "6.5",
            new(2.0)
        ),
        
        new(
            "8.5" + MOD + "7",
            new(1.5)
        ),

        new(
            "2.5" + POW + "-2",
            new(0.16)
            ),
        #endregion

        #region Integer/Boolean Mixed Operands

        // Booleans behave as integers in arithmetic (True is 1, False is 0)

        new(
            "5" + PLUS + TrueStr,
            new(6)
        ),

        new(
            "5" + PLUS + FalseStr,
            new(5)
        ),

        new(
            TrueStr + PLUS + "5",
            new(6)
        ),

        new(
            "5" + MINUS + TrueStr,
            new(4)
        ),

        new(
            "5" + MINUS + FalseStr,
            new(5)
        ),

        new(
            TrueStr + MINUS + "5",
            new(-4)
        ),

        new(
            "5" + TIMES + TrueStr,
            new(5)
        ),

        new(
            "5" + TIMES + FalseStr,
            new(0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            "5" + DIV + TrueStr,
            new(5.0) // Division always converts operands to be Chow floats
        ),

        new(
            FalseStr + DIV + "5",
            new(0.0)
        ),

        new(
            "5" + FLOOR + TrueStr,
            new(5)
        ),

        new(
            TrueStr + FLOOR + "5",
            new(0)
        ),

        new(
            "5" + MOD + TrueStr,
            new(0)
        ),

        new(
            TrueStr + MOD + "5",
            new(1)
        ),

        new(
            "5" + POW + TrueStr,
            new(5)
        ),

        new(
            "5" + POW + FalseStr,
            new(1)
        ),

        new(
            TrueStr + POW + "5",
            new(1)
        ),

        new(
            FalseStr + POW + "5",
            new(0)
        ),

        #endregion

        #region Double/Boolean Mixed Operands

        // A boolean operand mixed with a float yields a Chow float result

        new(
            "2.5" + PLUS + TrueStr,
            new(3.5)
        ),

        new(
            "2.5" + PLUS + FalseStr,
            new(2.5)
        ),

        new(
            TrueStr + PLUS + "2.5",
            new(3.5)
        ),

        new(
            "2.5" + MINUS + TrueStr,
            new(1.5)
        ),

        new(
            "2.5" + MINUS + FalseStr,
            new(2.5)
        ),

        new(
            TrueStr + MINUS + "2.5",
            new(-1.5)
        ),

        new(
            "2.5" + TIMES + TrueStr,
            new(2.5)
        ),

        new(
            "2.5" + TIMES + FalseStr,
            new(0.0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            "2.5" + DIV + TrueStr,
            new(2.5)
        ),

        new(
            FalseStr + DIV + "2.5",
            new(0.0)
        ),

        new(
            "2.5" + FLOOR + TrueStr,
            new(2.0)
        ),

        new(
            TrueStr + FLOOR + "2.5",
            new(0.0)
        ),

        new(
            "2.5" + MOD + TrueStr,
            new(0.5)
        ),

        new(
            TrueStr + MOD + "2.5",
            new(1.0)
        ),

        new(
            "2.5" + POW + TrueStr,
            new(2.5)
        ),

        new(
            "2.5" + POW + FalseStr,
            new(1.0)
        ),

        new(
            TrueStr + POW + "2.5",
            new(1.0)
        ),

        new(
            FalseStr + POW + "2.5",
            new(0.0)
        ),

        #endregion
        
        #region Precedence And Associativity

        // Multiplication binds tighter than addition
        new(
            "1" + PLUS + "2" + TIMES + "3",
            new(7)
        ),

        // Parentheses override precedence
        new(
            "(1" + PLUS + "2)" + TIMES + "3",
            new(9)
        ),

        // Subtraction is left-associative
        new(
            "10" + MINUS + "3" + MINUS + "2",
            new(5)
        ),

        // Division is left-associative (and always yields a Chow float)
        new(
            "100" + DIV + "10" + DIV + "2",
            new(5.0)
        ),

        // Exponentiation is right-associative: 2 ** (3 ** 2) = 2 ** 9
        new(
            "2" + POW + "3" + POW + "2",
            new(512)
        ),

        // Parentheses force left-associative exponentiation: (2 ** 3) ** 2
        new(
            "(2" + POW + "3)" + POW + "2",
            new(64)
        ),

        // Exponentiation binds tighter than multiplication: 2 * (3 ** 2)
        new(
            "2" + TIMES + "3" + POW + "2",
            new(18)
        ),

        // Parentheses override precedence: (2 * 3) ** 2
        new(
            "(2" + TIMES + "3)" + POW + "2",
            new(36)
        ),

        #endregion
    
        //--- Unary Minus ---
        
        #region Integer Unary Minus

        new(
            "-3",
            new(-3)
        ),

        new(
            "-(3)",
            new(-3)
        ),

        new(
            "-(-3)",
            new(3)
        ),

        new(
            "(-3)",
            new(-3)
        ),

        #endregion

        #region Repeated Unary Minus

        new(
            "--3",
            new(3)
        ),

        // Behaves the same as (-(-(-3)))
        new(
            "---3",
            new(-3)
        ),

        #endregion

        #region Double Unary Minus

        new(
            "-3.25",
            new(-3.25)
        ),

        new(
            "-(3.25)",
            new(-3.25)
        ),

        #endregion

        #region Boolean Unary Minus

        new(
            "-" + TrueStr,
            new(-1)
        ),

        new(
            "-" + FalseStr,
            new(0)
        ),

        new(
            "-(" + TrueStr + ")",
            new(-1)
        ),

        new(
            "-(" + FalseStr + ")",
            new(0)
        ),

        #endregion

        #region Negative Zero Unary Minus

        new(
            "-0",
            new(0)
        ),

        new(
            "-0.0",
            new(-0.0)
        ),

        #endregion
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteComparisonOperatorCases =
    [
        //--- Numeric Operands ---
        
        #region Integer Operands

        new(
            "1" + EQUALS + "1",
            TrueChow
            ),

        new(
            "1" + EQUALS + "2",
            FalseChow),

        new(
            "1" + NotEquals + "2",
            TrueChow
        ),

        new(
            "1" + NotEquals + "1",
            FalseChow
        ),

        new(
            "1" + LESS + "2",
            TrueChow
        ),

        new(
            "2" + LESS + "1",
            FalseChow
        ),

        new(
            "2" + GREATER + "1",
            TrueChow
        ),

        new(
            "1" + GREATER + "2",
            FalseChow
        ),

        new(
            "1" + GreaterEquals + "1",
            TrueChow
        ),

        new(
            "2" + GreaterEquals + "1",
            TrueChow
        ),

        new(
            "1" + GreaterEquals + "2",
            FalseChow
        ),

        new(
            "1" + LessOrEquals + "1",
            TrueChow
        ),

        new(
            "1" + LessOrEquals + "2",
            TrueChow
        ),

        new(
            "2" + LessOrEquals + "1",
            FalseChow
        ),

        #endregion

        #region Double Operands

        new(
            "1.0" + EQUALS + "1.0",
            TrueChow
        ),

        new(
            "1.0" + EQUALS + "2.0",
            FalseChow
        ),

        // Positive and negative zero compare equal (IEEE-754 / Python rule)
        new(
            "0.0" + EQUALS + "0.0",
            TrueChow
        ),

        new(
            "-0.0" + EQUALS + "-0.0",
            TrueChow
        ),

        new(
            "0.0" + EQUALS + "-0.0",
            TrueChow
        ),

        new(
            "-0.0" + EQUALS + "0.0",
            TrueChow
        ),

        new(
            "1.0" + NotEquals + "2.0",
            TrueChow
        ),

        new(
            "1.0" + NotEquals + "1.0",
            FalseChow
        ),

        new(
            "1.0" + LESS + "2.0",
            TrueChow
        ),

        new(
            "2.0" + LESS + "1.0",
            FalseChow
        ),

        new(
            "2.0" + GREATER + "1.0",
            TrueChow
        ),

        new(
            "1.0" + GREATER + "2.0",
            FalseChow
        ),

        new(
            "1.0" + GreaterEquals + "1.0",
            TrueChow
        ),

        new(
            "2.0" + GreaterEquals + "1.0",
            TrueChow
        ),

        new(
            "1.0" + GreaterEquals + "2.0",
            FalseChow
        ),

        new(
            "1.0" + LessOrEquals + "1.0",
            TrueChow
        ),

        new(
            "1.0" + LessOrEquals + "2.0",
            TrueChow
        ),

        new(
            "2.0" + LessOrEquals + "1.0",
            FalseChow
        ),

        #endregion

        //--- Mixed-Type Operands ---
        
        #region Left Integer Right Double Mixed Operands

        // An integer and a float compare equal when they share the same numeric @object
        new(
            "1" + EQUALS + "1.0",
            TrueChow
        ),

        new(
            "1" + EQUALS + "2.0",
            FalseChow
        ),

        // An integer zero compares equal to both positive and negative float zero
        new(
            "0" + EQUALS + "0.0",
            TrueChow
        ),

        new(
            "0" + EQUALS + "-0.0",
            TrueChow
        ),

        new(
            "1" + NotEquals + "2.0",
            TrueChow
        ),

        new(
            "1" + NotEquals + "1.0",
            FalseChow
        ),

        new(
            "1" + LESS + "2.0",
            TrueChow
        ),

        new(
            "2" + LESS + "1.0",
            FalseChow
        ),

        new(
            "2" + GREATER + "1.0",
            TrueChow
        ),

        new(
            "1" + GREATER + "2.0",
            FalseChow
        ),

        new(
            "1" + GreaterEquals + "1.0",
            TrueChow
        ),

        new(
            "2" + GreaterEquals + "1.0",
            TrueChow
        ),

        new(
            "1" + GreaterEquals + "2.0",
            FalseChow
        ),

        new(
            "1" + LessOrEquals + "1.0",
            TrueChow
        ),

        new(
            "1" + LessOrEquals + "2.0",
            TrueChow
        ),

        new(
            "2" + LessOrEquals + "1.0",
            FalseChow
        ),

        #endregion

        #region Right Integer Left Double Mixed Operands

        // A float and an integer compare equal when they share the same numeric @object
        new(
            "1.0" + EQUALS + "1",
            TrueChow
        ),

        new(
            "1.0" + EQUALS + "2",
            FalseChow
        ),

        // A float zero (positive or negative) compares equal to integer zero
        new(
            "0.0" + EQUALS + "0",
            TrueChow
        ),

        new(
            "-0.0" + EQUALS + "0",
            TrueChow
        ),

        new(
            "1.0" + NotEquals + "2",
            TrueChow
        ),

        new(
            "1.0" + NotEquals + "1",
            FalseChow
        ),

        new(
            "1.0" + LESS + "2",
            TrueChow
        ),

        new(
            "2.0" + LESS + "1",
            FalseChow
        ),

        new(
            "2.0" + GREATER + "1",
            TrueChow
        ),

        new(
            "1.0" + GREATER + "2",
            FalseChow
        ),

        new(
            "1.0" + GreaterEquals + "1",
            TrueChow
        ),

        new(
            "2.0" + GreaterEquals + "1",
            TrueChow
        ),

        new(
            "1.0" + GreaterEquals + "2",
            FalseChow
        ),

        new(
            "1.0" + LessOrEquals + "1",
            TrueChow
        ),

        new(
            "1.0" + LessOrEquals + "2",
            TrueChow
        ),

        new(
            "2.0" + LessOrEquals + "1",
            FalseChow
        ),

        #endregion

        #region Chained Operands

        // A chained comparison a OP b OP c is equivalent to (a OP b) and (b OP c)

        new(
            "1" + LESS + "2" + LESS + "3",
            TrueChow
        ),

        // The middle comparison fails, so the whole chain is false
        new(
            "1" + LESS + "3" + LESS + "2",
            FalseChow
        ),

        new(
            "3" + GREATER + "2" + GREATER + "1",
            TrueChow
        ),

        new(
            "3" + GREATER + "1" + GREATER + "2",
            FalseChow
        ),

        // Mixed operators within a single chain
        new(
            "1" + LESS + "2" + GREATER + "1",
            TrueChow
        ),

        new(
            "1" + LESS + "2" + LessOrEquals + "2",
            TrueChow
        ),

        new(
            "1" + LessOrEquals + "1" + LESS + "2",
            TrueChow
        ),

        new(
            "1" + LESS + "2" + EQUALS + "2",
            TrueChow
        ),

        // Equality chains
        new(
            "1" + EQUALS + "1" + EQUALS + "1",
            TrueChow
        ),

        new(
            "1" + NotEquals + "2" + NotEquals + "3",
            TrueChow
        ),

        // Three-operator chain
        new(
            "1" + LESS + "2" + LESS + "3" + LESS + "4",
            TrueChow
        ),

        // Mixed integer and float operands in a chain
        new(
            "1" + LESS + "2.0" + LessOrEquals + "2",
            TrueChow
        ),

        #endregion

        //--- Identity Operands ---
        // Int/str literals are bound to names first because CPython warns on 'is' with a literal.

        #region Identity Operands

        new(
            NoneStr + IS + NoneStr,
            TrueChow
        ),

        new(
            NoneStr + IsNot + NoneStr,
            FalseChow
        ),

        new(
            TrueStr + IS + TrueStr,
            TrueChow
        ),

        new(
            TrueStr + IS + FalseStr,
            FalseChow
        ),

        new(
            """
            x = None
            x is None
            """,
            TrueChow
        ),

        new(
            """
            x = None
            x is not None
            """,
            FalseChow
        ),

        // 'not' binds looser than 'is': not (x is None)
        new(
            """
            x = None
            not x is None
            """,
            FalseChow
        ),

        new(
            """
            a = [1]
            b = a
            a is b
            """,
            TrueChow
        ),

        new(
            """
            a = [1]
            c = [1]
            a is c
            """,
            FalseChow
        ),

        new(
            """
            a = [1]
            c = [1]
            a is not c
            """,
            TrueChow
        ),

        new(
            """
            d = {"a": 1}
            e = d
            d is e
            """,
            TrueChow
        ),

        // No numeric promotion, unlike ==
        new(
            """
            t = True
            one = 1
            t is one
            """,
            FalseChow
        ),

        new(
            """
            f = 1.5
            f is f
            """,
            TrueChow
        ),

        new(
            """
            s1 = "hi"
            s2 = "hi"
            s1 is s2
            """,
            TrueChow
        ),

        // Chains
        new(
            NoneStr + IS + NoneStr + IS + NoneStr,
            TrueChow
        ),

        new(
            """
            a = [1]
            b = a
            a is b is not None
            """,
            TrueChow
        ),

        #endregion
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteLogicOperatorCases =
    [
        //--- Binary Operators ---

        #region And Operator

        new(
            TrueStr + AND + TrueStr,
            TrueChow
        ),

        new(
            TrueStr + AND + FalseStr,
            FalseChow
        ),

        new(
            FalseStr + AND + TrueStr,
            FalseChow
        ),

        new(
            FalseStr + AND + FalseStr,
            FalseChow
        ),

        #endregion

        #region Or Operator

        new(
            TrueStr + OR + TrueStr,
            TrueChow
        ),

        new(
            TrueStr + OR + FalseStr,
            TrueChow
        ),

        new(
            FalseStr + OR + TrueStr,
            TrueChow
        ),

        new(
            FalseStr + OR + FalseStr,
            FalseChow
        ),

        #endregion

        //--- Short-Circuiting ---

        #region Value-Returning And Operator

        new(
            FalseyInt64 + AND + "\"rhs\"",
            0L
        ),

        new(
            TruthyInt64 + AND + "\"rhs\"",
            "rhs"
        ),

        new(
            FalseyStr + AND + "3",
            string.Empty
        ),

        new(
            TruthyStr + AND + "3",
            3
        ),

        new(
            NoneStr + AND + "3",
            ChowObject.None
        ),

        new(
            TruthyList + AND + "\"rhs\"",
            "rhs"
        ),

        #endregion

        #region Value-Returning Or Operator

        new(
            FalseyInt64 + OR + "\"rhs\"",
            "rhs"
        ),

        new(
            TruthyInt64 + OR + "\"rhs\"",
            1
        ),

        new(
            FalseyStr + OR + "3",
            3
        ),

        new(
            TruthyStr + OR + "3",
            "Truthy string"
        ),

        new(
            NoneStr + OR + "3",
            3
        ),

        new(
            FalseyList + OR + "\"rhs\"",
            "rhs"
        ),

        #endregion

        #region Short-Circuiting

        new(
            FalseyInt64 + AND + "(" + "1" + DIV + "0" + ")",
            0
        ),

        new(
            TruthyInt64 + OR + "(" + "1" + DIV + "0" + ")",
            1
        ),

        new(
            FalseStr + AND + "(" + "1" + DIV + "0" + ")",
            FalseChow
        ),

        new(
            TrueStr + OR + "(" + "1" + DIV + "0" + ")",
            TrueChow
        ),

        #endregion

        //--- Unary Not ---

        #region Boolean Unary Not

        new(
            NOT + TrueStr,
            FalseChow
            ),
        
        new(
            NOT + FalseStr,
            TrueChow
        ),

        // Behaves like not (not (not True))
        new(
            NOT + NOT + NOT + TrueStr,
            FalseChow
        ),

        new(
            NOT + NOT + NOT + FalseStr,
            TrueChow
        ),

        new(
            NOT + "(" + TrueStr + ")",
            FalseChow
        ),

        new(
            NOT + "(" + FalseStr + ")",
            TrueChow
        ),

        #endregion
 
        #region Integer Unary Not

        new(
            NOT + "0",
            TrueChow
        ),

        new(
            NOT + "3",
            FalseChow
        ),

        new(
            NOT + "-5",
            FalseChow
        ),

        #endregion

        #region Double Unary Not

        new(
            NOT + "0.0",
            TrueChow
        ),

        new(
            NOT + "3.5",
            FalseChow
        ),

        new(
            NOT + "-10.5",
            FalseChow
        ),

        #endregion

        #region Other Unary Not Operand Types
        
        // None
        new(
            NOT + NoneStr,
            TrueChow
            ),

        // Strings
        new(
            NOT + TruthyStr,
            FalseChow
        ),

        new(
            NOT + FalseyStr,
           TrueChow
        ),

        // Integers
        new(
            NOT + TruthyInt64,
            FalseChow
        ),

        new(
            NOT + FalseyInt64,
            TrueChow
        ),

        // Floats
        new(
            NOT + TruthyFloat64,
            FalseChow
        ),

        new(
            NOT + FalseyFloat64,
            TrueChow
        ),

        // Lists
        new(
            NOT + TruthyList,
            FalseChow
        ),

        new(
            NOT + FalseyList,
            TrueChow
        ),
        
        // Dictionaries
        new(
            NOT + TruthyDict,
            FalseChow
        ),

        new(
            NOT + FalseyDict,
            TrueChow
        ),
        
        // Range
        // TODO: Uncomment after making range something other than a built-in function
        /*
        new(
            NOT + TruthyRange,
            FalseChow
        ),

        new(
            NOT + FalseyRange,
            TrueChow
        ),
        */
        #endregion
   ];

    static readonly IReadOnlyList<CaseExecute> ExecuteFunctionScopeAndClosureCases =
    [
        //--- Function Calls and Scope ---

        new(
            """
            def add(a, b):
                return a + b
            add(2, 3)
            """,
            5
        ),

        new(
            """
            x = 10
            def read_local():
                x = 99
                return x
            read_local() + x
            """,
            109
        ),

        new(
            """
            x = 2
            def set_global():
                global x
                x = 9
            set_global()
            x
            """,
            9
        ),

        //--- Closures ---

        new(
            """
            def make_closure():
                x = 5
                def closure():
                    return x
                return closure
                
            test_closure = make_closure()
            test_closure()
            """,
            5
        ),

        new(
            """
            def outer():
                x = 1000
                def inner():
                    nonlocal x
                    x = 100
                    return x
                return inner
            
            test_closure =  outer()
            test_closure()
            """,
            100
        ),

        // NOTE: Deferred niche function syntax not yet confirmed in Chow:
        // lambda, decorators, variadic/keyword-only parameters, and advanced nonlocal edge patterns.
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteClassCases =
    [
        //--- Declaration and Instantiation ---

        new(
            """
            class Empty:
                pass

            instance = Empty()
            1
            """,
            1
        ),

        new(
            """
            class Empty:
                pass

            bool(Empty())
            """,
            TrueChow
        ),

        //--- Constructor and Instance Fields ---

        new(
            """
            class Point:
                def __init__(self, x):
                    self.x = x

            p = Point(5)
            p.x
            """,
            5
        ),

        new(
            """
            class Point:
                def __init__(self, x, y):
                    self.x = x
                    self.y = y

            p = Point(3, 4)
            p.x + p.y
            """,
            7
        ),

        // Each instance owns its fields; rebinding one must not disturb another.
        new(
            """
            class Box:
                def __init__(self, value):
                    self.value = value

            first = Box(1)
            second = Box(2)
            second.value = 10
            first.value
            """,
            1
        ),

        //--- Methods ---

        new(
            """
            class Greeter:
                def greet(self):
                    return "hello"

            Greeter().greet()
            """,
            new("hello")
        ),

        new(
            """
            class Counter:
                def __init__(self, start):
                    self.value = start

                def bump(self, amount):
                    self.value = self.value + amount
                    return self.value

            counter = Counter(5)
            counter.bump(2)
            """,
            7
        ),

        // A method reached through self, so binding has to work from inside a method body too.
        new(
            """
            class Doubler:
                def double(self, n):
                    return n * 2

                def quadruple(self, n):
                    return self.double(self.double(n))

            Doubler().quadruple(3)
            """,
            12
        ),

        //--- Class Variables ---

        new(
            """
            class Config:
                limit = 10

            Config.limit
            """,
            10
        ),

        // Reading through an instance falls through to the class.
        new(
            """
            class Config:
                limit = 10

            Config().limit
            """,
            10
        ),

        // Assigning through an instance shadows the class variable and leaves the class unchanged.
        new(
            """
            class Config:
                limit = 10

            config = Config()
            config.limit = 99
            config.limit + Config.limit
            """,
            109
        ),

        new(
            """
            class Config:
                limit = 10

            Config.limit = 20
            Config().limit
            """,
            20
        ),

        new(
            """
            class Config:
                total = 2 * 3

            Config.total
            """,
            6
        ),

        new(
            """
            class Tally:
                count = 0

                def __init__(self, start):
                    self.count = start

                def total(self):
                    return self.count + Tally.count

            Tally(4).total()
            """,
            4
        ),

        //--- Interaction With Other Features ---

        // A class declared inside a function: its methods must capture the defining scope.
        new(
            """
            def make_holder():
                hidden = 42
                class Holder:
                    def reveal(self):
                        return hidden
                return Holder()

            make_holder().reveal()
            """,
            42
        ),

        new(
            """
            class Person:
                def __init__(self, name):
                    self.name = name

            person = Person("Ada")
            f"hi {person.name}"
            """,
            new("hi Ada")
        ),

        new(
            """
            class Box:
                def __init__(self, value):
                    self.value = value

            def total(boxes):
                running = 0
                for box in boxes:
                    running = running + box.value
                return running

            total([Box(1), Box(2), Box(3)])
            """,
            6
        ),

        // NOTE: Deferred class features: inheritance, polymorphism, dunder dispatch
        // (__str__/__eq__/__len__), @classmethod/@staticmethod, and super().
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteCollectionSubscriptAndAssignmentCases =
    [
        //--- List Index and Assignment ---

        new(
            "[10, 20, 30][1]",
            20
        ),

        new(
            """
            values = [1, 2, 3]
            values[1] = 7
            values[1]
            """,
            7
        ),

        //--- List Slices ---

        new(
            """
            values = [1, 2, 3, 4]
            values[1:3] == [2, 3]
            """,
            TrueChow
        ),

        new(
            """
            values = [1, 2, 3, 4, 5]
            values[::2] == [1, 3, 5]
            """,
            TrueChow
        ),

        //--- Dict Index and Assignment ---

        new(
            "{'a': 1, 'b': 2}['b']",
            2
        ),

        new(
            """
            values = {'a': 1}
            values['b'] = 9
            values['b']
            """,
            9
        ),

        // NOTE: Deferred niche collection syntax not yet confirmed in Chow:
        // string indexing/slicing, extended slicing edge cases, and complex destructuring assignment targets.
    ];

    // Expected values verified against Python 3.11.0. The mutation cases depend on CPython's dict
    // layout (presizing, holes, resize compaction), which SourceDict reproduces.
    static readonly IReadOnlyList<CaseExecute> ExecuteDictionaryCases =
    [
        #region Iteration

        new(
            """
            d = {'a': 1, 'b': 2}
            out = []
            for k in d:
                out.append(k)
            str(out)
            """,
            "['a', 'b']"
        ),

        new(
            """
            d = {'a': 1, 'b': 2}
            out = []
            for k in d.keys():
                out.append(k)
            for v in d.values():
                out.append(v)
            str(out)
            """,
            "['a', 'b', 1, 2]"
        ),

        new(
            """
            d = {'b': 1, 'a': 2}
            str([list(d), min(d), max(d), list(d.values())])
            """,
            "[['b', 'a'], 'a', 'b', [1, 2]]"
        ),

        #endregion

        #region Views

        // Views are live: they see keys added after they were created.
        new(
            """
            d = {'a': 1}
            k = d.keys()
            d['b'] = 2
            str([len(k), 'b' in k, 2 in d.values(), bool({}.keys())])
            """,
            "[2, True, True, False]"
        ),

        // Keys views compare like sets; values views compare by identity.
        new(
            "str([{'a': 1}.keys() == {'a': 2}.keys(), {'a': 1}.keys() == {'b': 1}.keys(), {'a': 1}.values() == {'a': 1}.values()])",
            "[True, False, False]"
        ),

        new(
            """
            d = {'a': 1, 'b': 'x'}
            str(d.keys()) + ' ' + str(d.values())
            """,
            "dict_keys(['a', 'b']) dict_values([1, 'x'])"
        ),

        #endregion

        #region Keys and Repr

        // Equal numbers are the same key; the first key object is kept.
        new(
            "str({1: 'a', 1.0: 'b', True: 'c'})",
            "{1: 'c'}"
        ),

        new(
            "str({'k': 'v', 'n': [\"it's\"]})",
            "{'k': 'v', 'n': [\"it's\"]}"
        ),

        // Int/float keys match exactly: 2**53 + 1 is not 2.0**53.
        new(
            "str({9007199254740993: 'a'}.get(9007199254740992.0, 'missing'))",
            "missing"
        ),

        #endregion

        #region Mutation During Iteration

        // Changing values is allowed.
        new(
            """
            d = {'a': 1}
            for k in d:
                d[k] = 5
            str(d)
            """,
            "{'a': 5}"
        ),

        // Same size and the new key lands where iteration hasn't reached yet: no error.
        new(
            """
            log = []
            d = {'a': 1, 'b': 2, 'c': 3}
            for x in d:
                log.append(x)
                if x == 'a':
                    d.pop('c')
                    d['q'] = 9
            str(log)
            """,
            "['a', 'b', 'q']"
        ),

        // clear() swaps in a fresh table, so re-adding the same keys keeps iterating.
        new(
            """
            log = []
            d = {'a': 1, 'b': 2}
            for x in d:
                log.append(x)
                d.clear()
                d['a'] = 1
                d['b'] = 2
            str(log)
            """,
            "['a', 'b']"
        ),

        // The insert triggers a resize that compacts the hole, so 'b' is skipped.
        new(
            """
            log = []
            d = {'a': 1, 'b': 2, 'c': 3, 'd': 4, 'e': 5}
            for x in d:
                log.append(x)
                if x == 'a':
                    d.pop('a')
                    d['f'] = 6
            str(log)
            """,
            "['a', 'c', 'd', 'e', 'f']"
        ),

        new(
            """
            log = []
            d = {}
            for ch in 'abcde':
                d[ch] = 0
            for x in d:
                log.append(x)
                if x == 'a':
                    d.pop('a')
                    d['f'] = 6
            str(log)
            """,
            "['a', 'c', 'd', 'e', 'f']"
        ),

        // A str-only table is rebuilt (compacting the hole) before it takes its first non-str key.
        new(
            """
            log = []
            d = {'k0': 0}
            for x in d:
                log.append(x)
                d.pop('k0', 0)
                d[5] = 0
            str(log) + ' ' + str(d)
            """,
            "['k0'] {5: 0}"
        ),

        new(
            """
            log = []
            d = {'a': 1, 'b': 2, 'c': 3}
            for x in d:
                log.append(x)
                if x == 'a':
                    d.pop('a')
                    d[5] = 0
            str(log)
            """,
            "['a', 'c', 5]"
        ),

        // Literals of 16+ pairs are built one key at a time, so repeated keys leave a small table
        // that resizes (and compacts) during the loop. The 15-pair version raises instead.
        new(
            """
            log = []
            d = {'k0': 0, 'k1': 1, 'k2': 2, 'k0': 3, 'k1': 4, 'k2': 5, 'k0': 6, 'k1': 7, 'k2': 8, 'k0': 9, 'k1': 10, 'k2': 11, 'k0': 12, 'k1': 13, 'k2': 14, 'k0': 15}
            step = 0
            for x in d:
                log.append(x)
                d.pop(x, 0)
                d['n' + str(step)] = 0
                step = step + 1
            str(log)
            """,
            "['k0', 'k1', 'k2']"
        ),

        // 20 pairs: a 17-pair chunk built key by key, then the last 3 merged in.
        new(
            """
            log = []
            d = {'k0': 0, 'k1': 1, 'k2': 2, 'k0': 3, 'k1': 4, 'k2': 5, 'k0': 6, 'k1': 7, 'k2': 8, 'k0': 9, 'k1': 10, 'k2': 11, 'k0': 12, 'k1': 13, 'k2': 14, 'k0': 15, 'k1': 16, 'k2': 17, 'k0': 18, 'k1': 19}
            step = 0
            for x in d:
                log.append(x)
                d.pop(x, 0)
                d['n' + str(step)] = 0
                step = step + 1
            str(log)
            """,
            "['k0', 'k1', 'k2']"
        ),

        #endregion
    ];

    // Expected values verified against Python 3.11.0. Iterator reprs omit Python's memory address,
    // following Chow's <Point object> / <function f> convention.
    static readonly IReadOnlyList<CaseExecute> ExecuteIteratorCases =
    [
        #region reversed()

        new(
            "str([list(reversed([1, 2, 3])), list(reversed(range(0, 10, 3))), list(reversed(range(5, -6, -4)))])",
            "[[3, 2, 1], [9, 6, 3, 0], [-3, 1, 5]]"
        ),

        new(
            "str([list(reversed(range(0))), list(reversed(range(3, 3))), list(reversed(range(-1, -10, -3)))])",
            "[[], [], [-7, -4, -1]]"
        ),

        new(
            "str([list(reversed('abc')), list(reversed('')), list(reversed([]))])",
            "[['c', 'b', 'a'], [], []]"
        ),

        new(
            """
            d = {'a': 1, 'b': 2, 'c': 3}
            str([list(reversed(d)), list(reversed(d.keys())), list(reversed(d.values()))])
            """,
            "[['c', 'b', 'a'], ['c', 'b', 'a'], [3, 2, 1]]"
        ),

        #endregion

        #region iter() and next()

        // A for loop over a partly used iterator carries on from where it is.
        new(
            """
            it = iter([1, 2, 3])
            first = next(it)
            rest = []
            for x in it:
                rest.append(x)
            str([first, rest, next(it, 'done')])
            """,
            "[1, [2, 3], 'done']"
        ),

        new(
            """
            it = iter([1])
            str([iter(it) is it, it == it, bool(it), next(it), next(it, None)])
            """,
            "[True, True, True, 1, None]"
        ),

        // Iterators hash by identity, so they work as dict keys.
        new(
            """
            it = iter(range(3))
            d = {it: 'v'}
            str([d[it], next(it), list(it), list(it)])
            """,
            "['v', 0, [1, 2], []]"
        ),

        new(
            "str([next(iter('ab')), next(reversed('ab')), next(iter({'k': 1})), next(iter({'k': 1}.values()))])",
            "['a', 'b', 'k', 1]"
        ),

        new(
            """
            it = iter({'a': 1, 'b': 2}.keys())
            str([next(it), list(it)])
            """,
            "['a', ['b']]"
        ),

        #endregion

        #region Single Use

        // Exhausted stays exhausted, even after the list grows.
        new(
            """
            l = [1]
            it = iter(l)
            a = list(it)
            l.append(2)
            str([a, next(it, 'done')])
            """,
            "[[1], 'done']"
        ),

        // A reverse iterator stops if the list shrinks past it, and never sees appended items.
        new(
            """
            l = [1, 2, 3]
            r = reversed(l)
            a = next(r)
            l.pop()
            l.pop()
            str([a, next(r, 'done')])
            """,
            "[3, 'done']"
        ),

        new(
            """
            l = [1, 2, 3]
            r = reversed(l)
            a = next(r)
            l.append(9)
            str([a, list(r)])
            """,
            "[3, [2, 1]]"
        ),

        // `in` consumes the iterator up to the match.
        new(
            """
            it = iter([1, 2, 3])
            str([2 in it, list(it), 5 in it])
            """,
            "[True, [3], False]"
        ),

        new(
            "str([3 in reversed([1, 2, 3]), 'b' in iter('abc'), 1 not in iter([])])",
            "[True, True, True]"
        ),

        #endregion

        #region Repr

        new(
            "str([iter([]), reversed([]), iter(range(1)), reversed(range(1)), iter('a'), reversed('a')])",
            "[<list_iterator object>, <list_reverseiterator object>, <range_iterator object>, <range_iterator object>, <str_ascii_iterator object>, <reversed object>]"
        ),

        new(
            """
            d = {'a': 1}
            str([iter(d), reversed(d), iter(d.values()), reversed(d.values()), iter(d.keys())])
            """,
            "[<dict_keyiterator object>, <dict_reversekeyiterator object>, <dict_valueiterator object>, <dict_reversevalueiterator object>, <dict_keyiterator object>]"
        ),

        #endregion

        #region Reverse Dict Iteration With Mutation

        // CPython's reverse dict iterator checks only the size, never for swapped keys.
        new(
            """
            d = {'a': 1, 'b': 2, 'c': 3}
            r = reversed(d)
            a = next(r)
            d.pop('a')
            d['z'] = 0
            str([a, list(r)])
            """,
            "['c', ['b']]"
        ),

        new(
            """
            d = {'a': 1, 'b': 2, 'c': 3}
            r = reversed(d)
            a = next(r)
            d['c'] = 9
            str([a, list(r)])
            """,
            "['c', ['b', 'a']]"
        ),

        new(
            """
            d = {'a': 1, 'b': 2, 'c': 3}
            r = reversed(d)
            a = next(r)
            d.pop('b')
            d['q'] = 1
            str([a, list(r)])
            """,
            "['c', ['a']]"
        ),

        #endregion
    ];

    // Expected orders come from CPython 3.11 run with PYTHONHASHSEED=0, which Chow's sets reproduce.
    static readonly IReadOnlyList<CaseExecute> ExecuteSetCases =
    [
        #region Construction and order

        new(
            "str([set(), frozenset(), set([3, 1, 2]), frozenset('abc')])",
            "[set(), frozenset(), {1, 2, 3}, frozenset({'c', 'a', 'b'})]"
        ),

        // Colliding hashes: -1 hashes to -2, and 2**61 - 1 to 0.
        new(
            "str([list(set([8, 16, 1, 9, 0, 24])), set([2**61 - 1, 2**61, -2, -1, 2**62])])",
            "[[0, 1, 8, 9, 16, 24], {2305843009213693951, 2305843009213693952, 4611686018427387904, -1, -2}]"
        ),

        // Equal numbers are one element, and the first one added is kept.
        new(
            "str(set([1.5, True, 0, 2.25, -1, 'x', 1, 1.0]))",
            "{0, 1.5, 2.25, True, 'x', -1}"
        ),

        // String order comes from SipHash-1-3, CPython's string hash.
        new(
            "str(set(['apple', 'banana', 'cherry', 'date', 'elderberry', 'fig', 'grape']))",
            "{'banana', 'apple', 'cherry', 'fig', 'date', 'grape', 'elderberry'}"
        ),

        new(
            "str([set({'b': 1, 'a': 2}), set('hello'), set(range(20, 0, -3))])",
            "[{'a', 'b'}, {'e', 'h', 'l', 'o'}, {2, 5, 8, 11, 14, 17, 20}]"
        ),

        new(
            "str([set(frozenset(range(100, 0, -7))), frozenset(set([5, 13, 21]))])",
            "[{65, 2, 100, 37, 72, 9, 44, 79, 16, 51, 86, 23, 58, 93, 30}, frozenset({13, 21, 5})]"
        ),

        // frozenset() of a frozenset, and its copy(), return it unchanged.
        new(
            """
            fs = frozenset([1, 2])
            str([frozenset(fs) is fs, set(fs) is fs, fs.copy() is fs])
            """,
            "[True, False, True]"
        ),

        new(
            "str([set([frozenset([1, 2]), frozenset(), 3]), frozenset([frozenset('ab')])])",
            "[{frozenset(), 3, frozenset({1, 2})}, frozenset({frozenset({'a', 'b'})})]"
        ),

        #endregion

        #region Methods

        new(
            """
            s = set()
            log = []
            for i in range(20):
                s.add(i * 7 % 13)
            s.discard(3)
            s.discard(99)
            s.remove(5)
            log.append(s.pop())
            log.append(s.pop())
            s.add('k')
            str([log, s, len(s)])
            """,
            "[[0, 1], {2, 4, 6, 7, 8, 9, 10, 11, 12, 'k'}, 10]"
        ),

        // New elements reuse the slots of removed ones.
        new(
            """
            s = set(range(10))
            for i in range(0, 10, 2):
                s.remove(i)
            s.update([40, 32, 8, 24])
            str(s)
            """,
            "{1, 3, 5, 32, 7, 40, 9, 8, 24}"
        ),

        // pop() carries on from where the last pop stopped, even across clear().
        new(
            """
            s = set([1, 2, 3])
            a = s.pop()
            s.clear()
            s.update([1, 2, 3, 4])
            str([a, s.pop(), s])
            """,
            "[1, 2, {1, 3, 4}]"
        ),

        new(
            """
            s = set(range(5, 50, 5))
            log = []
            while s:
                log.append(s.pop())
            str(log)
            """,
            "[35, 5, 40, 10, 45, 15, 20, 25, 30]"
        ),

        new(
            """
            s = set([1])
            s.update([3, 2], 'ab', {'k': 0}, frozenset([9]))
            s.update()
            str(s)
            """,
            "{1, 2, 3, 9, 'k', 'b', 'a'}"
        ),

        new(
            """
            s = set([1, 2])
            t = s.copy()
            t.add(3)
            str([s, t, s.copy() is s, s.copy() == s])
            """,
            "[{1, 2}, {1, 2, 3}, False, True]"
        ),

        new(
            "str([set([1, 2]).isdisjoint([3, 4]), set([1, 2]).isdisjoint({2: 0}), set('ab').issubset('abc'), set([1, 2]).issubset([1]), set([1, 2, 3]).issuperset([1, 3]), frozenset([1]).issuperset(set()), set().issubset([])])",
            "[True, False, True, False, True, True, True]"
        ),

        new(
            "str([set([1, 2]).add(3), set([1]).discard(1), set().clear(), set().update([1])])",
            "[None, None, None, None]"
        ),

        // Only a size change is detected, so a discard followed by an add goes unnoticed, as in CPython.
        new(
            """
            log = []
            s = set([1, 2, 3])
            for x in s:
                log.append(x)
                if x == 1:
                    s.discard(3)
                    s.add(4)
            str(log)
            """,
            "[1, 2, 4]"
        ),

        #endregion

        #region Equality, membership, and hashing

        new(
            "str([set([1, 2]) == frozenset([2, 1]), set() == frozenset(), set([1]) == [1], set([1]) != set([1.0]), frozenset([True]) == set([1]), set([1]) == set([2])])",
            "[True, True, False, False, True, False]"
        ),

        // A set needle is looked up as the equal frozenset.
        new(
            """
            s = set([frozenset([1, 2]), 3])
            str([set([2, 1]) in s, frozenset([1]) in s, 3.0 in s, 'x' not in s])
            """,
            "[True, False, True, True]"
        ),

        new(
            """
            s = set([frozenset([1, 2]), frozenset([3]), 5])
            s.discard(set([1, 2]))
            s.remove(set([3]))
            str(s)
            """,
            "{5}"
        ),

        new(
            """
            d = {frozenset([1, 2]): 'a'}
            str([d[frozenset([2, 1])], frozenset() in {frozenset(): 0}, {frozenset('ab'): 1}])
            """,
            "['a', True, {frozenset({'a', 'b'}): 1}]"
        ),

        new(
            "str([bool(set()), bool(frozenset([0])), len(set('aab')), len(frozenset()), set() is set()])",
            "[False, True, 2, 0, False]"
        ),

        #endregion

        #region Literals

        // {} is an empty dict, not a set.
        new(
            "str([{1, 2, 3}, {'a'}, {1, True, 1.0}, {0.5, -0.0, 0}, {1, 2,}, {}, set()])",
            "[{1, 2, 3}, {'a'}, {1}, {0.5, -0.0}, {1, 2}, {}, set()]"
        ),

        // Three or more constants compile to a frozenset constant that is rebuilt from its own order,
        // so they can be ordered differently than a literal with a variable, or with two elements.
        new(
            """
            x = 7
            str([list({7, 15, 0}), list({x, 15, 0}), list({7, 15}), list({15, 7})])
            """,
            "[[0, 7, 15], [0, 15, 7], [15, 7], [7, 15]]"
        ),

        // A for loop iterates a constant literal's frozenset directly, whatever its size.
        new(
            """
            log = []
            for v in {7, 15, 0}:
                log.append(v)
            for v in {7, 15}:
                log.append(v)
            str(log)
            """,
            "[0, 7, 15, 7, 15]"
        ),

        // Literals with the same constants share the first one's frozenset, across functions too.
        new("str(list({0, 15, 7}))", "[0, 15, 7]"),
        new("str([list({7, 15, 0}), list({0, 15, 7})])", "[[0, 7, 15], [0, 7, 15]]"),
        new(
            """
            def f():
                return list({0, 15, 7})
            str([list({7, 15, 0}), f()])
            """,
            "[[0, 15, 7], [0, 15, 7]]"
        ),

        // Constant expressions are folded first, as CPython's AST optimizer does.
        new(
            "str([{-1, 2 * 3, 7, 10 // 3, 2 ** 3}, {'ab' * 2, 'x' + 'y', 'z'}, {-7, 1 - 8, 2.0 * 3}])",
            "[{3, 6, 7, 8, -1}, {'xy', 'abab', 'z'}, {-7, 6.0}]"
        ),

        // A string CPython had already interned makes it rebuild the frozenset once more. That includes
        // any name in the program, even one after the literal...
        new("str(list({'red', 'green', 'blue', 'cyan'}))", "['red', 'green', 'blue', 'cyan']"),
        new(
            """
            red = 1
            str(list({'red', 'green', 'blue', 'cyan'}))
            """,
            "['red', 'blue', 'green', 'cyan']"
        ),
        new(
            """
            s = str(list({'red', 'green', 'blue', 'cyan'}))
            red = 1
            s
            """,
            "['red', 'blue', 'green', 'cyan']"
        ),

        // ...and some single characters, interned at CPython's startup. A one-character name doesn't
        // count, because it is the same object as the literal.
        new("str(list({'a', 'b', 'f'}))", "['f', 'a', 'b']"),
        new(
            """
            g = 1
            D = 2
            str(list({'g', 'D', 'G'}))
            """,
            "['g', 'D', 'G']"
        ),

        #endregion
    ];

    // Expected values come from CPython 3.11 run with PYTHONHASHSEED=0.
    static readonly IReadOnlyList<CaseExecute> ExecuteSetAlgebraCases =
    [
        #region Bitwise operators on ints and bools

        // & binds tighter than ^, ^ tighter than |, and all three looser than + and -.
        new(
            "str([1 | 2 ^ 3 & 4, 1 + 2 & 3, 5 & 3 == 1, not 1 & 0, -1 & 3, -8 | 3, -5 ^ 3])",
            "[3, 3, True, True, 3, -5, -8]"
        ),

        // Two bools give a bool; a bool with an int gives an int.
        new(
            "str([True & False, True | False, True ^ True, True & 1, True | 2, 1 ^ True, False | False])",
            "[False, True, False, 1, 3, 0, False]"
        ),

        new(
            "str([9223372036854775807 & -1, -9223372036854775807 - 1 | 1, 2 ** 40 ^ 2 ** 40 - 1])",
            "[9223372036854775807, -9223372036854775807, 2199023255551]"
        ),

        // Constant bitwise expressions are folded, so this literal compiles to a frozenset constant.
        new(
            """
            x = 7
            str([list({3 | 4, 15 & 15, 0 ^ 0}), list({x, 15, 0})])
            """,
            "[[0, 7, 15], [0, 15, 7]]"
        ),

        #endregion

        #region Set operators

        // The result takes the left operand's type.
        new(
            """
            a = set([1, 2, 3])
            b = frozenset([2, 3, 4])
            str([a | b, a & b, a - b, a ^ b, b | a, b & a, b - a, b ^ a])
            """,
            "[{1, 2, 3, 4}, {2, 3}, {1}, {1, 4}, frozenset({1, 2, 3, 4}), frozenset({2, 3}), frozenset({4}), frozenset({1, 4})]"
        ),

        // Which of two equal elements survives depends on the operand iterated: & walks the smaller
        // set (the right one on a tie), and | keeps the left operand's elements.
        new(
            "str([{1, 2} & {1.0, 3}, {1} & {1.0, 2}, set([1, 2]).intersection([True, 5]), {1, 2} | {True}, {True} | {1, 2}])",
            "[{1.0}, {1}, {True}, {1, 2}, {True, 2}]"
        ),

        // Ordering compares as subsets.
        new(
            "str([{1} < {1, 2}, {1, 2} < {1, 2}, {1, 2} <= {1, 2}, {1, 2} > {1}, frozenset([1]) >= set(), {1, 3} <= {1, 2}, {1} != frozenset([1])])",
            "[True, False, True, True, True, False, False]"
        ),

        // The same set on both sides. s ^ s leaves a table of dummies, which decides where 9 goes.
        new(
            """
            s = {1, 2}
            t = s ^ s
            t.add(9)
            str([t, s | s, s - s, s & s, s.union(s), s.intersection(s, s)])
            """,
            "[{9}, {1, 2}, set(), {1, 2}, {1, 2}, {1, 2}]"
        ),

        #endregion

        #region Methods

        // Methods take any number of iterables of any kind.
        new(
            """
            s = set(range(10))
            str([s.union([20], 'ab', {30: 0}), s.intersection(range(5), [3, 4, 99]), s.difference([1, 2], {3: 0}, 'ab'), s.symmetric_difference({5: 0, 50: 0}), s.union(), s.intersection(), s.difference()])
            """,
            "[{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 'b', 'a', 20, 30}, {3, 4}, {0, 4, 5, 6, 7, 8, 9}, {0, 1, 2, 3, 4, 6, 7, 8, 9, 50}, {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}]"
        ),

        // A frozenset's methods return new frozensets; only copy() returns it unchanged.
        new(
            """
            fs = frozenset([1, 2])
            str([fs.union([3]), fs.intersection([2]), fs.difference([1]), fs.symmetric_difference([9]), fs.union() is fs, fs.copy() is fs])
            """,
            "[frozenset({1, 2, 3}), frozenset({2}), frozenset({2}), frozenset({9, 2, 1}), False, True]"
        ),

        // intersection_update takes over a new table but keeps pop()'s place.
        new(
            """
            s = set(range(5))
            a = s.pop()
            s.intersection_update(range(1, 4))
            str([a, s, s.pop(), s])
            """,
            "[0, {2, 3}, 1, {2, 3}]"
        ),

        // Once more than a quarter of its slots are dummies, difference_update rebuilds the table at
        // four times the size of what's left, which decides where 100 goes.
        new(
            """
            s = set(range(40))
            s.difference_update(range(0, 40, 2))
            s.add(100)
            str(s)
            """,
            "{1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 100}"
        ),

        // Past 50,000 elements it rebuilds at only twice the size, where 131073 lands in slot 1.
        new(
            """
            s = set(range(120000))
            s.difference_update(range(66000))
            s.add(131073)
            l = list(s)
            str([len(s), l[:2], l[-2:]])
            """,
            "[54001, [131073, 66000], [119998, 119999]]"
        ),

        // Against a set over 8 times larger, only the common elements are looked for.
        new(
            """
            s = set(range(3))
            s.difference_update(set(range(100)))
            s.add(7)
            str(s)
            """,
            "{7}"
        ),

        new(
            """
            s = set([1, 2, 3])
            s.symmetric_difference_update([3, 4])
            t = str(s)
            s.symmetric_difference_update(s)
            str([t, s])
            """,
            "['{1, 2, 4}', set()]"
        ),

        new(
            """
            s = set([1, 2])
            str([s.intersection_update(), s.difference_update(), s.symmetric_difference_update([]), s])
            """,
            "[None, None, None, {1, 2}]"
        ),

        #endregion

        #region dict_keys

        // A keys view combines with any iterable on either side, and the result is always a set.
        new(
            """
            d = {1: 'a', 2: 'b', 3: 'c'}
            str([d.keys() | {4}, d.keys() & {2, 9}, d.keys() - {1}, d.keys() ^ {3, 4}, {4} | d.keys(), [1, 2] - d.keys(), 'ab' | d.keys(), d.keys() | d.values(), d.values() | d.keys()])
            """,
            "[{1, 2, 3, 4}, {2}, {2, 3}, {1, 2, 4}, {1, 2, 3, 4}, set(), {1, 2, 3, 'b', 'a'}, {1, 2, 3, 'b', 'c', 'a'}, {1, 2, 3, 'b', 'c', 'a'}]"
        ),

        new(
            """
            d = {1: 'a', 2: 'b', 3: 'c'}
            str([d.keys() & {1: 0}.keys(), d.keys() & frozenset([1, 9]), frozenset([1]) | d.keys(), {1: 0} | {2: 0}.keys()])
            """,
            "[{1}, {1}, {1, 2, 3}, {1, 2}]"
        ),

        // A keys view compares with sets and other keys views from either side, but not with lists.
        new(
            """
            d = {1: 'a', 2: 'b', 3: 'c'}
            str([{1, 2, 3} == d.keys(), d.keys() == {1, 2, 3}, d.keys() != {1}, {1} < d.keys(), d.keys() >= {1}, d.keys() > d.keys(), d.keys() <= {1: 0, 2: 0, 3: 0, 4: 0}.keys(), d.keys() == [1, 2, 3]])
            """,
            "[True, True, True, True, True, False, True, False]"
        ),

        new(
            """
            d = {1: 'a', 2: 'b', 3: 'c'}
            str([d.keys().isdisjoint([4, 5]), d.keys().isdisjoint({1}), d.keys().isdisjoint(d.keys()), {}.keys().isdisjoint({}.keys())])
            """,
            "[True, False, False, True]"
        ),

        // The view reads the dict as it is now.
        new(
            """
            d = {1: 'a', 2: 'b', 3: 'c'}
            k = d.keys()
            d[9] = 0
            str(k & {9, 1})
            """,
            "{9, 1}"
        ),

        #endregion
    ];

    static IEnumerable<TestCaseData> TypeErrorMessageCases()
    {
        const string pointClass = "class Point:\n    pass\n";

        yield return new TestCaseData("len(iter([]))", "object of type 'list_iterator' has no len()");
        yield return new TestCaseData("iter([])[0]", "'list_iterator' object is not subscriptable");
        yield return new TestCaseData("reversed(5)", "'int' object is not reversible");
        yield return new TestCaseData("reversed(iter([]))", "'list_iterator' object is not reversible");
        yield return new TestCaseData("reversed(3.5)", "'float' object is not reversible");
        yield return new TestCaseData("next([1])", "'list' object is not an iterator");
        yield return new TestCaseData("next([1], 0)", "'list' object is not an iterator");
        yield return new TestCaseData("iter(5)", "'int' object is not iterable");
        yield return new TestCaseData("iter([]) + 1", "unsupported operand type(s) for +: 'list_iterator' and 'int'");
        yield return new TestCaseData("-iter([])", "bad operand type for unary -: 'list_iterator'");

        yield return new TestCaseData("set([[1]])", "unhashable type: 'list'");
        yield return new TestCaseData("{[1], 2}", "unhashable type: 'list'");
        yield return new TestCaseData("{set(), 1, 2}", "unhashable type: 'set'");
        yield return new TestCaseData("set().add(set())", "unhashable type: 'set'");
        yield return new TestCaseData("[1] in set()", "unhashable type: 'list'");
        yield return new TestCaseData("set([1]).remove([1])", "unhashable type: 'list'");
        yield return new TestCaseData("{1: 2}[set()]", "unhashable type: 'set'");
        yield return new TestCaseData("set()[0]", "'set' object is not subscriptable");
        yield return new TestCaseData("reversed(set())", "'set' object is not reversible");
        yield return new TestCaseData("set(5)", "'int' object is not iterable");
        yield return new TestCaseData("set() + set()", "unsupported operand type(s) for +: 'set' and 'set'");
        yield return new TestCaseData("-frozenset()", "bad operand type for unary -: 'frozenset'");
        yield return new TestCaseData("next(set())", "'set' object is not an iterator");
        yield return new TestCaseData("set().isdisjoint(5)", "'int' object is not iterable");
        yield return new TestCaseData("set([1]).issubset(5)", "'int' object is not iterable");
        yield return new TestCaseData("set([1]).update(5)", "'int' object is not iterable");
        yield return new TestCaseData("1.5 & 1", "unsupported operand type(s) for &: 'float' and 'int'");
        yield return new TestCaseData("'a' ^ 'b'", "unsupported operand type(s) for ^: 'str' and 'str'");
        yield return new TestCaseData("{1} & [1]", "unsupported operand type(s) for &: 'set' and 'list'");
        yield return new TestCaseData("{1: 2}.values() | {1}", "unsupported operand type(s) for |: 'dict_values' and 'set'");
        yield return new TestCaseData("{} | set()", "unsupported operand type(s) for |: 'dict' and 'set'");
        yield return new TestCaseData("5 | {1: 2}.keys()", "'int' object is not iterable");
        yield return new TestCaseData("{1: 2}.keys() & 5", "'int' object is not iterable");
        yield return new TestCaseData("{1}.union(5)", "'int' object is not iterable");
        yield return new TestCaseData("{1}.difference_update([set()])", "unhashable type: 'set'");
        yield return new TestCaseData("{1}.intersection([[1]])", "unhashable type: 'list'");

        // Instances report their class's name; a class itself is a 'type'.
        yield return new TestCaseData(pointClass + "iter(Point())", "'Point' object is not iterable");
        yield return new TestCaseData(pointClass + "len(Point())", "object of type 'Point' has no len()");
        yield return new TestCaseData(pointClass + "Point()[0]", "'Point' object is not subscriptable");
        yield return new TestCaseData(pointClass + "Point() + 1", "unsupported operand type(s) for +: 'Point' and 'int'");
        yield return new TestCaseData(pointClass + "Point + 1", "unsupported operand type(s) for +: 'type' and 'int'");
        yield return new TestCaseData(pointClass + "for x in Point():\n    pass", "'Point' object is not iterable");
    }

    static IEnumerable<TestCaseData> DictMutatedDuringIterationCases()
    {
        yield return new TestCaseData(
            """
            log = []
            d = {'a': 1, 'b': 2}
            for x in d:
                log.append(x)
                d.pop(x)
                d['z' + x] = 0
            """,
            "['a', 'b']",
            "dictionary keys changed during iteration").SetName("PopAndInsertEachStep");

        yield return new TestCaseData(
            """
            log = []
            d = {'a': 1}
            for x in d:
                log.append(x)
                d['b'] = 2
            """,
            "['a']",
            "dictionary changed size during iteration").SetName("InsertChangesSize");

        // A 4-entry literal still has a free slot, so no resize: the swapped-in key is reached.
        yield return new TestCaseData(
            """
            log = []
            d = {'a': 1, 'b': 2, 'c': 3, 'd': 4}
            for x in d:
                log.append(x)
                if x == 'a':
                    d.pop('a')
                    d['f'] = 6
            """,
            "['a', 'b', 'c', 'd']",
            "dictionary keys changed during iteration").SetName("SwapInFourEntryLiteral");

        // A 6-entry literal is presized to 16 slots, so again no resize.
        yield return new TestCaseData(
            """
            log = []
            d = {'a': 1, 'b': 2, 'c': 3, 'd': 4, 'e': 5, 'g': 7}
            for x in d:
                log.append(x)
                if x == 'a':
                    d.pop('a')
                    d['f'] = 6
            """,
            "['a', 'b', 'c', 'd', 'e', 'g']",
            "dictionary keys changed during iteration").SetName("SwapInSixEntryLiteral");

        // 15 pairs is the largest literal CPython presizes, so the repeated keys leave a roomy table
        // that never resizes. Compare the 16-pair case in ExecuteDictionaryCases.
        yield return new TestCaseData(
            """
            log = []
            d = {'k0': 0, 'k1': 1, 'k2': 2, 'k0': 3, 'k1': 4, 'k2': 5, 'k0': 6, 'k1': 7, 'k2': 8, 'k0': 9, 'k1': 10, 'k2': 11, 'k0': 12, 'k1': 13, 'k2': 14}
            step = 0
            for x in d:
                log.append(x)
                d.pop(x, 0)
                d['n' + str(step)] = 0
                step = step + 1
            """,
            "['k0', 'k1', 'k2']",
            "dictionary keys changed during iteration").SetName("SwapInPresizedFifteenPairLiteral");

        // CPython sizes tables with bit_length((n | 8) - 1), so copying this 4-key dict gives 16
        // slots, not 8. With 8, the setdefault would resize and compact the table instead.
        yield return new TestCaseData(
            """
            log = []
            d = dict({5: 38, 1: 32, 21: 92, True: 72, 1: 8, 3: 42, True: 75})
            d.pop(5, 0)
            d[8] = 41
            step = 0
            for x in d:
                log.append(x)
                if step == 0:
                    d.pop(1, 0)
                    d.setdefault(2.5, 7)
                if step == 3:
                    d.clear()
                step = step + 1
            """,
            "[1, 21, 3, 8]",
            "dictionary changed size during iteration").SetName("CopyUsesCPythonTableSizeFormula");

        yield return new TestCaseData(
            """
            d = {'a': 1, 'b': 2}
            r = reversed(d)
            log = [next(r)]
            d['c'] = 3
            log.append(next(r))
            """,
            "['b']",
            "dictionary changed size during iteration").SetName("ReverseIteratorChecksSize");
    }

    // A set iterator only checks the size, so each of these is caught on the next step.
    static IEnumerable<TestCaseData> SetMutatedDuringIterationCases()
    {
        const string setup = "log = []\ns = set([1, 2, 3])\n";
        const string message = "Set changed size during iteration";

        yield return new TestCaseData(setup + "for x in s:\n    log.append(x)\n    s.add(x + 10)", "[1]", message)
            .SetName("SetAddChangesSize");
        yield return new TestCaseData(setup + "for x in s:\n    log.append(x)\n    s.discard(x)", "[1]", message)
            .SetName("SetDiscardChangesSize");
        yield return new TestCaseData(setup + "for x in s:\n    log.append(x)\n    s.pop()", "[1]", message)
            .SetName("SetPopChangesSize");
        yield return new TestCaseData(setup + "for x in s:\n    log.append(x)\n    s.clear()", "[1]", message)
            .SetName("SetClearChangesSize");
        yield return new TestCaseData(setup + "it = iter(s)\nlog.append(next(it))\ns.add(9)\nlog.append(next(it))",
            "[1]", message).SetName("SetIteratorChecksSize");
    }

    static readonly IReadOnlyList<CaseExecute> ExecuteIterationCases =
    [
        //--- For Loops Over Built-in Iterables ---

        new(
            """
            total = 0
            for x in [1, 2, 3]:
                total = total + x
            total
            """,
            6
        ),

        new(
            """
            result = ""
            for char in "abc":
                result = result + char
            result
            """,
            "abc"
        ),

        new(
            """
            total = 0
            for i in [1, 2]:
                for j in [10, 20]:
                    total = total + i + j
            total
            """,
            66
        ),

        //--- Iterable Semantics ---

        new(
            "2 in [1, 2, 3]",
            TrueChow
        ),

        new(
            "'b' in {'a': 1, 'b': 2}",
            TrueChow
        ),

        // NOTE: Deferred niche iteration syntax not yet confirmed in Chow:
        // for-else, generator expressions, and comprehension variants.
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteBuiltInFunctionCases =
    [
        //--- Core Built-ins ---

        new(
            "abs(-5)",
            5
        ),

        new(
            "len([1, 2, 3])",
            3
        ),

        new(
            "round(2.5)",
            2
        ),

        new(
            "min(4, -1, 8)",
            -1
        ),

        new(
            "max([4, -1, 8])",
            8
        ),

        new(
            "int(\"12\")",
            12
        ),

        new(
            "float(\"2.5\")",
            2.5
        ),

        new(
            "str(123)",
            "123"
        ),

        new(
            "bool(0)",
            FalseChow
        ),

        new(
            "list(\"ab\") == ['a', 'b']",
            TrueChow
        ),

        // NOTE: Deferred niche built-in syntax/behavior not yet confirmed in Chow:
        // clear, print/input output assertions, dict kwargs/mapping-protocol variants, and ValueError-specific parity checks.
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteStringConcatenationAndFStringCases =
    [
        //--- String Concatenation ---

        new(
            "\"hello\" + \" \" + \"world\"",
            "hello world"
        ),

        new(
            """
            prefix = "Chow"
            prefix + " V2"
            """,
            "Chow V2"
        ),

        //--- f-Strings ---

        new(
            """
            name = "Chow"
            f"Hello, {name}!"
            """,
            "Hello, Chow!"
        ),

        new(
            "f\"{1 + 2}\"",
            "3"
        ),

        new(
            """
            n = 2
            f"Value: {n * 5}"
            """,
            "Value: 10"
        ),

        // NOTE: Deferred niche f-string syntax not yet confirmed in Chow:
        // conversion flags (!r, !s, !a), format spec mini-language, and deeply nested expression forms.
    ];
    
    static readonly IReadOnlyList<CaseExecute> ExecuteEmptyWhitespaceOrNullCases =
    [
        new(
            string.Empty,
            ChowObject.None
        ),

        new(
            null!,
            ChowObject.None
        ),

        // The supported sequences of characters that make up a single newline are as follows:
        // - '\n' (Unix/Linux/macOS)
        // - '\r\n' (Windows/MS-DOS)
        // - '\r' (Older Mac)
        //
        // Sequences '\n\r', '\r\r', and '\n\n' would be two lines

        new(
            NewlineLinuxMac,
            ChowObject.None
        ),

        new(
            NewlineWindows,
            ChowObject.None
        ),

        new(
            NewlineOldMac,
            ChowObject.None
        ),

        #region Pure Spaces

        new(
            " ",
            ChowObject.None
        ),

        new(
            SingleIndentSpaces,
            ChowObject.None
        ),

        new(
            SingleIndentSpaces + SingleIndentSpaces,
            ChowObject.None
        ),

        #endregion

        #region Pure Tabs

        new(
            SingleIndentTab,
            ChowObject.None
        ),

        new(
            SingleIndentTab + SingleIndentTab,
            ChowObject.None
        ),

        #endregion

        #region Mixed Spaces and Tabs

        new(
            SingleIndentSpaces + SingleIndentTab,
            ChowObject.None
        ),

        new(
            SingleIndentTab + SingleIndentSpaces,
            ChowObject.None
        ),

        #endregion

        #region Multiple Newlines (Same Style)

        new(
            NewlineLinuxMac + NewlineLinuxMac,
            ChowObject.None
        ),

        new(
            NewlineWindows + NewlineWindows,
            ChowObject.None
        ),

        new(
            NewlineOldMac + NewlineOldMac,
            ChowObject.None
        ),

        #endregion

        #region Multiple Newlines (Mixed Styles)

        new(
            NewlineLinuxMac + NewlineWindows,
            ChowObject.None
        ),

        new(
            NewlineWindows + NewlineOldMac,
            ChowObject.None
        ),

        new(
            NewlineOldMac + NewlineLinuxMac,
            ChowObject.None
        ),

        new(
            NewlineLinuxMac + NewlineWindows + NewlineOldMac,
            ChowObject.None
        ),

        #endregion

        #region Whitespace Combined With Newlines

        new(
            SingleIndentSpaces + NewlineLinuxMac,
            ChowObject.None
        ),

        new(
            NewlineLinuxMac + SingleIndentSpaces,
            ChowObject.None
        ),

        new(
            SingleIndentTab + NewlineWindows + SingleIndentTab,
            ChowObject.None
        ),

        new(
            NewlineLinuxMac + SingleIndentSpaces + NewlineOldMac,
            ChowObject.None
        ),

        new(
            SingleIndentSpaces + NewlineWindows + SingleIndentTab + NewlineLinuxMac,
            ChowObject.None
        ),

        #endregion

        #region Form Feed

        // '\f' is not handled by SkipToFirstLexeme (only by ScanIndentColumn), so it returns
        // early from SkipToFirstLexeme and is consumed by ScanIndentColumn instead
        new(
            "\f",
            ChowObject.None
        ),

        new(
            "\f" + NewlineLinuxMac + SingleIndentSpaces,
            ChowObject.None
        ),

        #endregion

        #region Comment-Only Source

        // Comment with no trailing newline — SkipRemainingLineChars consumes it and reaches EOF
        new(
            CodeComment,
            ChowObject.None
        ),

        new(
            CodeComment + NewlineLinuxMac,
            ChowObject.None
        ),

        // Multiple comment lines separated by different newline styles
        new(
            CodeComment + NewlineLinuxMac + CodeComment,
            ChowObject.None
        ),

        new(
            CodeComment + NewlineWindows + CodeComment + NewlineOldMac + CodeComment,
            ChowObject.None
        ),

        // Leading whitespace before a comment (spaces/tabs are consumed as indent chars first)
        new(
            SingleIndentSpaces + CodeComment,
            ChowObject.None
        ),

        new(
            SingleIndentTab + CodeComment + NewlineLinuxMac,
            ChowObject.None
        ),

        #endregion
    ];

    #endregion

    #region Process Instructions: Control Flow Statements

    static readonly IReadOnlyList<CaseExecute> ControlFlowCases =
    [
        #region If/Else-If/Else Statements

        new(
            """
            if True:
               True
            """,
            true
        ),
        
        new(
            """
            if False:
                False
            """,
            ChowObject.None
        ),
        
        new(
            """
            if True:
                True
            else:
                False
            """,
            true
        ),
        
        new(
            """
            if False:
                False
            else:
                True
            """,
            true
        ),
        
        new(
            """
            if True:
                True
            elif False:
                False
            else:
                False
            """,
            true
        ),
        
        new(
            """
            if False:
                False
            elif True:
                True
            else:
                False
            """,
            true
        ),
        
        new(
            """
            if False:
                False
            elif False:
                False
            else:
                True
            """,
            true
        ),

        // Test case where a block has more than one statement
        new(
            """
            if True:
               False
               True
            """,
            true
        ),

        new(
            """
            x = None
            if x is None:
                1
            else:
                2
            """,
            1
        ),

        new(
            """
            x = [1]
            if x is not None:
                1
            else:
                2
            """,
            1
        ),

        #endregion

        #region While Loops

        // Condition false from the start, so the body never executes and there is
        // no expression statement result to return
        new(
            """
            while False:
                True
            """,
            ChowObject.None
        ),

        // A counter drives the loop; after it exits, the trailing expression statement
        // reports the final @object of the counter
        new(
            """
            i = 0
            while i < 3:
                i = i + 1
            i
            """,
            3
        ),

        // A loop that runs exactly once
        new(
            """
            i = 0
            while i < 1:
                i = i + 1
            i
            """,
            1
        ),

        // A decrementing counter loops down to zero
        new(
            """
            i = 5
            while i > 0:
                i = i - 1
            i
            """,
            0
        ),

        // A boolean flag is cleared inside the body to exit after a single pass
        new(
            """
            run = True
            while run:
                run = False
            run
            """,
            false
        ),

        // The expression statement inside the body returns the last @object evaluated
        // before the condition became false
        new(
            """
            i = 0
            while i < 3:
                i = i + 1
                i
            """,
            3
        ),

        // A multi-statement body accumulates a running total across iterations
        new(
            """
            total = 0
            i = 1
            while i <= 3:
                total = total + i
                i = i + 1
            total
            """,
            6
        ),

        #endregion

        #region Break Statements

        // break exits the loop immediately, freezing the counter at its current @object
        new(
            """
            i = 0
            while True:
                i = i + 1
                if i == 3:
                    break
            i
            """,
            3
        ),

        // Statements after break in the same body are skipped on the breaking iteration
        new(
            """
            reached = False
            while True:
                break
                reached = True
            reached
            """,
            false
        ),

        // A break under an always-true condition exits after a single pass
        new(
            """
            ran = False
            while True:
                ran = True
                break
            ran
            """,
            true
        ),

        //--- Nested Loops ---

        // break only exits the innermost loop, so the outer loop runs to completion
        new(
            """
            i = 0
            total = 0
            while i < 3:
                i = i + 1
                j = 0
                while j < 3:
                    j = j + 1
                    if j == 2:
                        break
                    total = total + 1
            total
            """,
            3
        ),

        // A break in the outer loop ends both loops once its condition is met
        new(
            """
            i = 0
            total = 0
            while i < 5:
                i = i + 1
                j = 0
                while j < 5:
                    j = j + 1
                    total = total + 1
                if i == 2:
                    break
            total
            """,
            10
        ),

        #endregion

        #region Continue Statements

        // continue skips the rest of the body and re-tests the condition
        new(
            """
            reached = False
            done = False
            while not done:
                done = True
                continue
                reached = True
            reached
            """,
            false
        ),

        // continue can skip multiple iterations while the loop still runs to completion
        new(
            """
            i = 0
            count = 0
            while i < 6:
                i = i + 1
                if i <= 3:
                    continue
                count = count + 1
            count
            """,
            3
        ),

        //--- Nested Loops ---

        // continue affects only the innermost loop
        new(
            """
            i = 0
            total = 0
            while i < 2:
                i = i + 1
                j = 0
                while j < 3:
                    j = j + 1
                    if j == 2:
                        continue
                    total = total + j
            total
            """,
            8
        ),

        #endregion
    ];

    #endregion
    
    #region Source Code Constants
    
    #region Data Type Literals

    const string TrueStr = "True";
    const string FalseStr = "False";
    const string NoneStr = "None";

    #endregion
    
    #region Operators
    
    // Binary operator constants make it easier to scan and see where and what operator is being used.

    //--- Arithmetic Operators ---
    const string PLUS = " + ";
    const string MINUS = " - ";
    const string TIMES = " * ";
    const string DIV = " / ";
    const string MOD = " % ";
    const string FLOOR = " // ";
    const string POW = " ** ";
    
    //--- Comparison Operators ---
    const string EQUALS = " == ";
    const string NotEquals = " != ";
    const string LESS = " < ";
    const string GREATER = " > ";
    const string GreaterEquals = " >= ";
    const string LessOrEquals = " <= ";
    const string IS = " is ";
    const string IsNot = " is not ";

    //--- Logic Operators ---
    const string AND = " and ";
    const string OR = " or ";
    const string NOT = "not ";

    #endregion

    #region Newlines, Whitespace, and Comments

    //--- Newlines ---
    const string NewlineLinuxMac = "\n";
    const string NewlineWindows = "\r\n";
    const string NewlineOldMac = "\r";

    //--- Whitespace ---
    const string SingleIndentSpaces = "    "; // Indents used for blocks are 4 spaces
    const string SingleIndentTab = "\t";
    const string CodeComment = "# This is a comment";

    #endregion

    #region Truthy and Falsey Values

    //--- Truthy Values ---
    const string TruthyStr = "\"Truthy string\""; // Anything with more than one character
    const int TruthyInt64 = 1; // Non-zero integer
    const double TruthyFloat64 = 1.0; // Non-zero Chow float
    const string TruthyList = "[1, 2, 3]"; // Non-empty list
    const string TruthyDict = "{'a': 1, 'b': 2, 'c': 3}"; // Non-empty dict
    // const string TruthyRange = "range(1, 10)"; // Non-empty range

    //--- Falsey Values ---
    const string FalseyStr = "\"\""; // Empty string
    const string FalseyInt64 = "0"; // Zero integer
    const string FalseyFloat64 = "0.0"; // Zero Chow float
    const string FalseyList = "[]"; // Empty list
    const string FalseyDict = "{}"; // Empty dict
    // const string FalseyRange = "range(0)"; // Empty range

    #endregion

    #endregion

    #region Helper Types

    static ChowObject List(params SourceValue[] values)
    {
        var list = SourceObjectFactory.CreateNewObject(DataType.List);

        foreach (var value in values)
        {
            list.AppendItem(value);
        }

        return new ChowObject(new SourceValue(list));
    }

    public record CaseExecute(string SourceCode, ChowObject ExpectedResult);

    #endregion
}
