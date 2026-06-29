using Chow;
using Chow.SourceData;
using Chow.VM;

namespace Chow.Tests;

[TestFixture]
public class LanguageFeatureTests
{
    #region Methods
    
    [TestCaseSource(nameof(ControlFlowCases))]
    [TestCaseSource(nameof(ExecuteStringConcatenationAndFStringCases))]
    [TestCaseSource(nameof(ExecuteBuiltInFunctionCases))]
    [TestCaseSource(nameof(ExecuteIterationCases))]
    [TestCaseSource(nameof(ExecuteCollectionSubscriptAndAssignmentCases))]
    [TestCaseSource(nameof(ExecuteFunctionScopeAndClosureCases))]
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
