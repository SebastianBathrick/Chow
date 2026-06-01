namespace Chow.Interpreter.Tests;

[TestFixture]
public class ChowEngineTests
{

    #region Methods

    [SetUp]
    public void Setup()
    {
        ChowEngine.Reset();
    }
    
    [TestCaseSource(nameof(ExecuteLogicOperatorCases))]
    [TestCaseSource(nameof(ExecuteComparisonOperatorCases))]
    [TestCaseSource(nameof(ExecuteEmptyWhitespaceOrNullCases))]
    [TestCaseSource(nameof(ExecuteArithmeticOperatorCases))]
    public void Execute_ValidSourceCode_ReturnExpectedResult(CaseExecute caseExecute)
    {
        var returnValue = ChowEngine.Execute(caseExecute.SourceCode);
        
        Assert.That(returnValue, Is.EqualTo(caseExecute.ExpectedResult));
    }

    #endregion

    #region Static Readonly Fields
    
    static readonly ChowValue TrueChow = new(true);
    static readonly ChowValue FalseChow = new(false);


    static readonly IReadOnlyList<CaseExecute> ExecuteArithmeticOperatorCases =
    [
        // Note: Passing the ChowValue constructor a long or an int will result in a ChowValue
        // instance with DataType.Int. However, passing a float or a double will result in an
        // instance with DataType.Float. Always double-check the literal passed to the constructor.
        
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

        #region Float Positive Operands

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

        #region Float Negative Operands

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

        #region Float Positive and Negative Operands

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

        #region Left Larger Float Operand

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
        
        #region Right Larger Float Operand

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
            TRUE_STR + PLUS + TRUE_STR,
            new(2)
        ),

        new(
            TRUE_STR + PLUS + FALSE_STR,
            new(1)
        ),

        new(
            FALSE_STR + PLUS + FALSE_STR,
            new(0)
        ),

        new(
            TRUE_STR + MINUS + TRUE_STR,
            new(0)
        ),

        new(
            TRUE_STR + MINUS + FALSE_STR,
            new(1)
        ),

        new(
            FALSE_STR + MINUS + FALSE_STR,
            new(0)
        ),

        new(
            TRUE_STR + TIMES + TRUE_STR,
            new(1)
        ),

        new(
            TRUE_STR + TIMES + FALSE_STR,
            new(0)
        ),

        new(
            FALSE_STR + TIMES + FALSE_STR,
            new(0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            TRUE_STR + DIV + TRUE_STR,
            new(1.0)
        ),

        new(
            FALSE_STR + DIV + TRUE_STR,
            new(0.0)
        ),

        new(
            TRUE_STR + FLOOR + TRUE_STR,
            new(1)
        ),

        new(
            FALSE_STR + FLOOR + TRUE_STR,
            new(0)
        ),

        new(
            TRUE_STR + MOD + TRUE_STR,
            new(0)
        ),

        new(
            FALSE_STR + MOD + TRUE_STR,
            new(0)
        ),

        new(
            TRUE_STR + POW + TRUE_STR,
            new(1)
        ),

        new(
            TRUE_STR + POW + FALSE_STR,
            new(1)
        ),

        new(
            FALSE_STR + POW + TRUE_STR,
            new(0)
        ),

        new(
            FALSE_STR + POW + FALSE_STR,
            new(1)
        ),

        #endregion
        
        #region Integer/Float Mixed Operands

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
            "5" + PLUS + TRUE_STR,
            new(6)
        ),

        new(
            "5" + PLUS + FALSE_STR,
            new(5)
        ),

        new(
            TRUE_STR + PLUS + "5",
            new(6)
        ),

        new(
            "5" + MINUS + TRUE_STR,
            new(4)
        ),

        new(
            "5" + MINUS + FALSE_STR,
            new(5)
        ),

        new(
            TRUE_STR + MINUS + "5",
            new(-4)
        ),

        new(
            "5" + TIMES + TRUE_STR,
            new(5)
        ),

        new(
            "5" + TIMES + FALSE_STR,
            new(0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            "5" + DIV + TRUE_STR,
            new(5.0) // Division always converts operands to be Chow floats
        ),

        new(
            FALSE_STR + DIV + "5",
            new(0.0)
        ),

        new(
            "5" + FLOOR + TRUE_STR,
            new(5)
        ),

        new(
            TRUE_STR + FLOOR + "5",
            new(0)
        ),

        new(
            "5" + MOD + TRUE_STR,
            new(0)
        ),

        new(
            TRUE_STR + MOD + "5",
            new(1)
        ),

        new(
            "5" + POW + TRUE_STR,
            new(5)
        ),

        new(
            "5" + POW + FALSE_STR,
            new(1)
        ),

        new(
            TRUE_STR + POW + "5",
            new(1)
        ),

        new(
            FALSE_STR + POW + "5",
            new(0)
        ),

        #endregion

        #region Float/Boolean Mixed Operands

        // A boolean operand mixed with a float yields a Chow float result

        new(
            "2.5" + PLUS + TRUE_STR,
            new(3.5)
        ),

        new(
            "2.5" + PLUS + FALSE_STR,
            new(2.5)
        ),

        new(
            TRUE_STR + PLUS + "2.5",
            new(3.5)
        ),

        new(
            "2.5" + MINUS + TRUE_STR,
            new(1.5)
        ),

        new(
            "2.5" + MINUS + FALSE_STR,
            new(2.5)
        ),

        new(
            TRUE_STR + MINUS + "2.5",
            new(-1.5)
        ),

        new(
            "2.5" + TIMES + TRUE_STR,
            new(2.5)
        ),

        new(
            "2.5" + TIMES + FALSE_STR,
            new(0.0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            "2.5" + DIV + TRUE_STR,
            new(2.5)
        ),

        new(
            FALSE_STR + DIV + "2.5",
            new(0.0)
        ),

        new(
            "2.5" + FLOOR + TRUE_STR,
            new(2.0)
        ),

        new(
            TRUE_STR + FLOOR + "2.5",
            new(0.0)
        ),

        new(
            "2.5" + MOD + TRUE_STR,
            new(0.5)
        ),

        new(
            TRUE_STR + MOD + "2.5",
            new(1.0)
        ),

        new(
            "2.5" + POW + TRUE_STR,
            new(2.5)
        ),

        new(
            "2.5" + POW + FALSE_STR,
            new(1.0)
        ),

        new(
            TRUE_STR + POW + "2.5",
            new(1.0)
        ),

        new(
            FALSE_STR + POW + "2.5",
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

        #region Float Unary Minus

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
            "-" + TRUE_STR,
            new(-1)
        ),

        new(
            "-" + FALSE_STR,
            new(0)
        ),

        new(
            "-(" + TRUE_STR + ")",
            new(-1)
        ),

        new(
            "-(" + FALSE_STR + ")",
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
            "1" + NOT_EQUALS + "2",
            TrueChow
        ),

        new(
            "1" + NOT_EQUALS + "1",
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
            "1" + GREATER_EQUALS + "1",
            TrueChow
        ),

        new(
            "2" + GREATER_EQUALS + "1",
            TrueChow
        ),

        new(
            "1" + GREATER_EQUALS + "2",
            FalseChow
        ),

        new(
            "1" + LESS_OR_EQUALS + "1",
            TrueChow
        ),

        new(
            "1" + LESS_OR_EQUALS + "2",
            TrueChow
        ),

        new(
            "2" + LESS_OR_EQUALS + "1",
            FalseChow
        ),

        #endregion

        #region Float Operands

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
            "1.0" + NOT_EQUALS + "2.0",
            TrueChow
        ),

        new(
            "1.0" + NOT_EQUALS + "1.0",
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
            "1.0" + GREATER_EQUALS + "1.0",
            TrueChow
        ),

        new(
            "2.0" + GREATER_EQUALS + "1.0",
            TrueChow
        ),

        new(
            "1.0" + GREATER_EQUALS + "2.0",
            FalseChow
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "1.0",
            TrueChow
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "2.0",
            TrueChow
        ),

        new(
            "2.0" + LESS_OR_EQUALS + "1.0",
            FalseChow
        ),

        #endregion

        //--- Mixed-Type Operands ---
        
        #region Left Integer Right Float Mixed Operands

        // An integer and a float compare equal when they share the same numeric value
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
            "1" + NOT_EQUALS + "2.0",
            TrueChow
        ),

        new(
            "1" + NOT_EQUALS + "1.0",
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
            "1" + GREATER_EQUALS + "1.0",
            TrueChow
        ),

        new(
            "2" + GREATER_EQUALS + "1.0",
            TrueChow
        ),

        new(
            "1" + GREATER_EQUALS + "2.0",
            FalseChow
        ),

        new(
            "1" + LESS_OR_EQUALS + "1.0",
            TrueChow
        ),

        new(
            "1" + LESS_OR_EQUALS + "2.0",
            TrueChow
        ),

        new(
            "2" + LESS_OR_EQUALS + "1.0",
            FalseChow
        ),

        #endregion

        #region Right Integer Left Float Mixed Operands

        // A float and an integer compare equal when they share the same numeric value
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
            "1.0" + NOT_EQUALS + "2",
            TrueChow
        ),

        new(
            "1.0" + NOT_EQUALS + "1",
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
            "1.0" + GREATER_EQUALS + "1",
            TrueChow
        ),

        new(
            "2.0" + GREATER_EQUALS + "1",
            TrueChow
        ),

        new(
            "1.0" + GREATER_EQUALS + "2",
            FalseChow
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "1",
            TrueChow
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "2",
            TrueChow
        ),

        new(
            "2.0" + LESS_OR_EQUALS + "1",
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
            "1" + LESS + "2" + LESS_OR_EQUALS + "2",
            TrueChow
        ),

        new(
            "1" + LESS_OR_EQUALS + "1" + LESS + "2",
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
            "1" + NOT_EQUALS + "2" + NOT_EQUALS + "3",
            TrueChow
        ),

        // Three-operator chain
        new(
            "1" + LESS + "2" + LESS + "3" + LESS + "4",
            TrueChow
        ),

        // Mixed integer and float operands in a chain
        new(
            "1" + LESS + "2.0" + LESS_OR_EQUALS + "2",
            TrueChow
        ),

        #endregion
    ];

    static readonly IReadOnlyList<CaseExecute> ExecuteLogicOperatorCases =
    [
        #region And Operator

        new(
            TRUE_STR + AND + TRUE_STR,
            TrueChow
        ),

        new(
            TRUE_STR + AND + FALSE_STR,
            FalseChow
        ),

        new(
            FALSE_STR + AND + TRUE_STR,
            FalseChow
        ),

        new(
            FALSE_STR + AND + FALSE_STR,
            FalseChow
        ),

        #endregion

        #region Or Operator

        new(
            TRUE_STR + OR + TRUE_STR,
            TrueChow
        ),

        new(
            TRUE_STR + OR + FALSE_STR,
            TrueChow
        ),

        new(
            FALSE_STR + OR + TRUE_STR,
            TrueChow
        ),

        new(
            FALSE_STR + OR + FALSE_STR,
            FalseChow
        ),

        #endregion

        //--- Unary Not ---

        #region Boolean Unary Not

        new(
            NOT + TRUE_STR,
            FalseChow
            ),
        
        new(
            NOT + FALSE_STR,
            TrueChow
        ),

        // Behaves like not (not (not True))
        new(
            NOT + NOT + NOT + TRUE_STR,
            FalseChow
        ),

        new(
            NOT + NOT + NOT + FALSE_STR,
            TrueChow
        ),

        new(
            NOT + "(" + TRUE_STR + ")",
            FalseChow
        ),

        new(
            NOT + "(" + FALSE_STR + ")",
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

        #region Float Unary Not

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
            NOT + NONE_STR,
            TrueChow
            ),

        // Strings
        new(
            NOT + TRUTHY_STR,
            FalseChow
        ),

        new(
            NOT + FALSEY_STR,
           TrueChow
        ),

        // Integers
        new(
            NOT + TRUTHY_INT64,
            FalseChow
        ),

        new(
            NOT + FALSEY_INT64,
            TrueChow
        ),

        // Floats
        new(
            NOT + TRUTHY_FLOAT64,
            FalseChow
        ),

        new(
            NOT + FALSEY_FLOAT64,
            TrueChow
        ),

        // Lists
        new(
            NOT + TRUTHY_LIST,
            FalseChow
        ),

        new(
            NOT + FALSEY_LIST,
            TrueChow
        ),
        
        // Dictionaries
        new(
            NOT + TRUTHY_DICT,
            FalseChow
        ),

        new(
            NOT + FALSEY_DICT,
            TrueChow
        ),
        
        // Range
        // TODO: Uncomment after making range something other than a built-in function
        /*
        new(
            NOT + TRUTHY_RANGE,
            FalseChow
        ),

        new(
            NOT + FALSEY_RANGE,
            TrueChow
        ),
        */
        #endregion
   ];
    
    static readonly IReadOnlyList<CaseExecute> ExecuteEmptyWhitespaceOrNullCases =
    [
        new(
            string.Empty,
            ChowValue.None
        ),

        new(
            null!,
            ChowValue.None
        ),

        // The supported sequences of characters that make up a single newline are as follows:
        // - '\n' (Unix/Linux/macOS)
        // - '\r\n' (Windows/MS-DOS)
        // - '\r' (Older Mac)
        //
        // Sequences '\n\r', '\r\r', and '\n\n' would be two lines

        new(
            NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        new(
            NEWLINE_WINDOWS,
            ChowValue.None
        ),

        new(
            NEWLINE_OLD_MAC,
            ChowValue.None
        ),

        #region Pure Spaces

        new(
            " ",
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_SPACES,
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_SPACES + SINGLE_INDENT_SPACES,
            ChowValue.None
        ),

        #endregion

        #region Pure Tabs

        new(
            SINGLE_INDENT_TAB,
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_TAB + SINGLE_INDENT_TAB,
            ChowValue.None
        ),

        #endregion

        #region Mixed Spaces and Tabs

        new(
            SINGLE_INDENT_SPACES + SINGLE_INDENT_TAB,
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_TAB + SINGLE_INDENT_SPACES,
            ChowValue.None
        ),

        #endregion

        #region Multiple Newlines (Same Style)

        new(
            NEWLINE_LINUX_MAC + NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        new(
            NEWLINE_WINDOWS + NEWLINE_WINDOWS,
            ChowValue.None
        ),

        new(
            NEWLINE_OLD_MAC + NEWLINE_OLD_MAC,
            ChowValue.None
        ),

        #endregion

        #region Multiple Newlines (Mixed Styles)

        new(
            NEWLINE_LINUX_MAC + NEWLINE_WINDOWS,
            ChowValue.None
        ),

        new(
            NEWLINE_WINDOWS + NEWLINE_OLD_MAC,
            ChowValue.None
        ),

        new(
            NEWLINE_OLD_MAC + NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        new(
            NEWLINE_LINUX_MAC + NEWLINE_WINDOWS + NEWLINE_OLD_MAC,
            ChowValue.None
        ),

        #endregion

        #region Whitespace Combined With Newlines

        new(
            SINGLE_INDENT_SPACES + NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        new(
            NEWLINE_LINUX_MAC + SINGLE_INDENT_SPACES,
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_TAB + NEWLINE_WINDOWS + SINGLE_INDENT_TAB,
            ChowValue.None
        ),

        new(
            NEWLINE_LINUX_MAC + SINGLE_INDENT_SPACES + NEWLINE_OLD_MAC,
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_SPACES + NEWLINE_WINDOWS + SINGLE_INDENT_TAB + NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        #endregion

        #region Form Feed

        // '\f' is not handled by SkipToFirstLexeme (only by ScanIndentColumn), so it returns
        // early from SkipToFirstLexeme and is consumed by ScanIndentColumn instead
        new(
            "\f",
            ChowValue.None
        ),

        new(
            "\f" + NEWLINE_LINUX_MAC + SINGLE_INDENT_SPACES,
            ChowValue.None
        ),

        #endregion

        #region Comment-Only Source

        // Comment with no trailing newline — SkipRemainingLineChars consumes it and reaches EOF
        new(
            CODE_COMMENT,
            ChowValue.None
        ),

        new(
            CODE_COMMENT + NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        // Multiple comment lines separated by different newline styles
        new(
            CODE_COMMENT + NEWLINE_LINUX_MAC + CODE_COMMENT,
            ChowValue.None
        ),

        new(
            CODE_COMMENT + NEWLINE_WINDOWS + CODE_COMMENT + NEWLINE_OLD_MAC + CODE_COMMENT,
            ChowValue.None
        ),

        // Leading whitespace before a comment (spaces/tabs are consumed as indent chars first)
        new(
            SINGLE_INDENT_SPACES + CODE_COMMENT,
            ChowValue.None
        ),

        new(
            SINGLE_INDENT_TAB + CODE_COMMENT + NEWLINE_LINUX_MAC,
            ChowValue.None
        ),

        #endregion
    ];

    #endregion

    #region Source Code Constants
    
    #region Data Type Literals

    const string TRUE_STR = "True";
    const string FALSE_STR = "False";
    const string NONE_STR = "None";

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
    const string NOT_EQUALS = " != ";
    const string LESS = " < ";
    const string GREATER = " > ";
    const string GREATER_EQUALS = " >= ";
    const string LESS_OR_EQUALS = " <= ";

    //--- Logic Operators ---
    const string AND = " and ";
    const string OR = " or ";
    const string NOT = "not ";

    #endregion

    #region Newlines, Whitespace, and Comments

    //--- Newlines ---
    const string NEWLINE_LINUX_MAC = "\n";
    const string NEWLINE_WINDOWS = "\r\n";
    const string NEWLINE_OLD_MAC = "\r";

    //--- Whitespace ---
    const string SINGLE_INDENT_SPACES = "    "; // Indents used for blocks are 4 spaces
    const string SINGLE_INDENT_TAB = "\t";
    const string CODE_COMMENT = "# This is a comment";

    #endregion

    #region Truthy and Falsey Values

    //--- Truthy Values ---
    const string TRUTHY_STR = "\"Truthy string\""; // Anything with more than one character
    const int TRUTHY_INT64 = 1; // Non-zero integer
    const double TRUTHY_FLOAT64 = 1.0; // Non-zero Chow float
    const string TRUTHY_LIST = "[1, 2, 3]"; // Non-empty list
    const string TRUTHY_DICT = "{'a': 1, 'b': 2, 'c': 3}"; // Non-empty dictionary
    // const string TRUTHY_RANGE = "range(1, 10)"; // Non-empty range

    //--- Falsey Values ---
    const string FALSEY_STR = "\"\""; // Empty string
    const string FALSEY_INT64 = "0"; // Zero integer
    const string FALSEY_FLOAT64 = "0.0"; // Zero Chow float
    const string FALSEY_LIST = "[]"; // Empty list
    const string FALSEY_DICT = "{}"; // Empty dictionary
    // const string FALSEY_RANGE = "range(0)"; // Empty range

    #endregion

    #endregion

    #region Helper Types

    public record CaseExecute(string SourceCode, ChowValue ExpectedResult);

    #endregion
}
