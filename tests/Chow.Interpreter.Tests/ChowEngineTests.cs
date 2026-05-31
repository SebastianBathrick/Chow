namespace Chow.Interpreter.Tests;

[TestFixture]
public class ChowEngineTests
{
    [SetUp]
    public void Setup()
    {
        ChowEngine.Reset();
    }
    
    public record CaseExecute(string SourceCode, ChowValue ExpectedResult);

    static readonly IReadOnlyList<CaseExecute> ExecuteArithmeticOperatorCases =
    [
        // Note: Passing the ChowValue constructor a long or an int will result in a ChowValue
        // instance with DataType.Int. However, passing a float or a double will result in an
        // instance with DataType.Float. Always double-check the literal passed to the constructor.
        
        //--- Positive and Negative Operands ---
        
        #region Positive Integer Operands

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

        #region Negative Integer Operands

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

        #region Positive and Negative Integer Operands

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

        #region Positive Float Operands

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

        #region Negative Float Operands

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

        #region Positive and Negative Float Operands

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
        
        #region Integer Negation

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

        #region Repeated Negation

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

        #region Float Negation

        new(
            "-3.25",
            new(-3.25)
        ),

        new(
            "-(3.25)",
            new(-3.25)
        ),

        #endregion

        #region Boolean Negation

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

        #region Negative Zero

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
        // -- Numeric Operands --
        
        #region Integer Operands

        new(
            "1" + EQUALS + "1",
            new(true)
            ),

        new(
            "1" + EQUALS + "2",
            new(false)),

        new(
            "1" + NOT_EQUALS + "2",
            new(true)
        ),

        new(
            "1" + NOT_EQUALS + "1",
            new(false)
        ),

        new(
            "1" + LESS + "2",
            new(true)
        ),

        new(
            "2" + LESS + "1",
            new(false)
        ),

        new(
            "2" + GREATER + "1",
            new(true)
        ),

        new(
            "1" + GREATER + "2",
            new(false)
        ),

        new(
            "1" + GREATER_EQUALS + "1",
            new(true)
        ),

        new(
            "2" + GREATER_EQUALS + "1",
            new(true)
        ),

        new(
            "1" + GREATER_EQUALS + "2",
            new(false)
        ),

        new(
            "1" + LESS_OR_EQUALS + "1",
            new(true)
        ),

        new(
            "1" + LESS_OR_EQUALS + "2",
            new(true)
        ),

        new(
            "2" + LESS_OR_EQUALS + "1",
            new(false)
        ),

        #endregion

        #region Float Operands

        new(
            "1.0" + EQUALS + "1.0",
            new(true)
        ),

        new(
            "1.0" + EQUALS + "2.0",
            new(false)
        ),

        // Positive and negative zero compare equal (IEEE-754 / Python rule)
        new(
            "0.0" + EQUALS + "0.0",
            new(true)
        ),

        new(
            "-0.0" + EQUALS + "-0.0",
            new(true)
        ),

        new(
            "0.0" + EQUALS + "-0.0",
            new(true)
        ),

        new(
            "-0.0" + EQUALS + "0.0",
            new(true)
        ),

        new(
            "1.0" + NOT_EQUALS + "2.0",
            new(true)
        ),

        new(
            "1.0" + NOT_EQUALS + "1.0",
            new(false)
        ),

        new(
            "1.0" + LESS + "2.0",
            new(true)
        ),

        new(
            "2.0" + LESS + "1.0",
            new(false)
        ),

        new(
            "2.0" + GREATER + "1.0",
            new(true)
        ),

        new(
            "1.0" + GREATER + "2.0",
            new(false)
        ),

        new(
            "1.0" + GREATER_EQUALS + "1.0",
            new(true)
        ),

        new(
            "2.0" + GREATER_EQUALS + "1.0",
            new(true)
        ),

        new(
            "1.0" + GREATER_EQUALS + "2.0",
            new(false)
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "1.0",
            new(true)
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "2.0",
            new(true)
        ),

        new(
            "2.0" + LESS_OR_EQUALS + "1.0",
            new(false)
        ),

        #endregion

        // -- Mixed-Type Operands --
        
        #region Left Integer Right Float Mixed Operands

        // An integer and a float compare equal when they share the same numeric value
        new(
            "1" + EQUALS + "1.0",
            new(true)
        ),

        new(
            "1" + EQUALS + "2.0",
            new(false)
        ),

        // An integer zero compares equal to both positive and negative float zero
        new(
            "0" + EQUALS + "0.0",
            new(true)
        ),

        new(
            "0" + EQUALS + "-0.0",
            new(true)
        ),

        new(
            "1" + NOT_EQUALS + "2.0",
            new(true)
        ),

        new(
            "1" + NOT_EQUALS + "1.0",
            new(false)
        ),

        new(
            "1" + LESS + "2.0",
            new(true)
        ),

        new(
            "2" + LESS + "1.0",
            new(false)
        ),

        new(
            "2" + GREATER + "1.0",
            new(true)
        ),

        new(
            "1" + GREATER + "2.0",
            new(false)
        ),

        new(
            "1" + GREATER_EQUALS + "1.0",
            new(true)
        ),

        new(
            "2" + GREATER_EQUALS + "1.0",
            new(true)
        ),

        new(
            "1" + GREATER_EQUALS + "2.0",
            new(false)
        ),

        new(
            "1" + LESS_OR_EQUALS + "1.0",
            new(true)
        ),

        new(
            "1" + LESS_OR_EQUALS + "2.0",
            new(true)
        ),

        new(
            "2" + LESS_OR_EQUALS + "1.0",
            new(false)
        ),

        #endregion

        #region Right Integer Left Float Mixed Operands

        // A float and an integer compare equal when they share the same numeric value
        new(
            "1.0" + EQUALS + "1",
            new(true)
        ),

        new(
            "1.0" + EQUALS + "2",
            new(false)
        ),

        // A float zero (positive or negative) compares equal to integer zero
        new(
            "0.0" + EQUALS + "0",
            new(true)
        ),

        new(
            "-0.0" + EQUALS + "0",
            new(true)
        ),

        new(
            "1.0" + NOT_EQUALS + "2",
            new(true)
        ),

        new(
            "1.0" + NOT_EQUALS + "1",
            new(false)
        ),

        new(
            "1.0" + LESS + "2",
            new(true)
        ),

        new(
            "2.0" + LESS + "1",
            new(false)
        ),

        new(
            "2.0" + GREATER + "1",
            new(true)
        ),

        new(
            "1.0" + GREATER + "2",
            new(false)
        ),

        new(
            "1.0" + GREATER_EQUALS + "1",
            new(true)
        ),

        new(
            "2.0" + GREATER_EQUALS + "1",
            new(true)
        ),

        new(
            "1.0" + GREATER_EQUALS + "2",
            new(false)
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "1",
            new(true)
        ),

        new(
            "1.0" + LESS_OR_EQUALS + "2",
            new(true)
        ),

        new(
            "2.0" + LESS_OR_EQUALS + "1",
            new(false)
        ),

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
    
    [TestCaseSource(nameof(ExecuteComparisonOperatorCases))]
    [TestCaseSource(nameof(ExecuteEmptyWhitespaceOrNullCases))]
    [TestCaseSource(nameof(ExecuteArithmeticOperatorCases))]
    public void Execute_ValidSourceCode_ReturnExpectedResult(CaseExecute caseExecute)
    {
        var returnValue = ChowEngine.Execute(caseExecute.SourceCode);
        
        Assert.That(returnValue, Is.EqualTo(caseExecute.ExpectedResult));
    }

    #region Constants
    const string TRUE_STR = "True";
    const string FALSE_STR = "False";

    // Binary operator constants make it easier to scan and see where and what operator is being used.
    const string PLUS = " + ";
    const string MINUS = " - ";
    const string TIMES = " * ";
    const string DIV = " / ";
    const string MOD = " % ";
    const string FLOOR = " // ";
    const string POW = " ** ";
    
    const string EQUALS = " == ";
    const string NOT_EQUALS = " != ";
    const string LESS = " < ";
    const string GREATER = " > ";
    const string GREATER_EQUALS = " >= ";
    const string LESS_OR_EQUALS = " <= ";

    const string NEWLINE_LINUX_MAC = "\n";
    const string NEWLINE_WINDOWS = "\r\n";
    const string NEWLINE_OLD_MAC = "\r";

    const string SINGLE_INDENT_SPACES = "    "; // Indents used for blocks are 4 spaces
    const string SINGLE_INDENT_TAB = "\t";

    const string CODE_COMMENT = "# This is a comment";
    
    #endregion
}
