namespace Chow.Interpreter.Tests;

[TestFixture]
public class ChowEngineTests
{
    [SetUp]
    public void Setup()
    {
        ChowEngine.Reset();
    }
    
    public record TestCaseExecute(string SourceCode, ChowValue ExpectedResult);

    static readonly IReadOnlyList<TestCaseExecute> ExecuteBinaryArithmeticCases =
    [
        // Note: Passing the ChowValue constructor a long or an int will result in a ChowValue
        // instance with DataType.Int. However, passing a float or a double will result in an
        // instance with DataType.Float. Always double-check the literal passed to the constructor.

        #region Positive Integer Operands

        new(
            "1" + BINARY_OP_PLUS + "2",
            new(3)
        ),

        new(
            "6" + BINARY_OP_MINUS + "3",
            new(3)
        ),

        new(
            "7" + BINARY_OP_TIMES + "8",
            new(56)
        ),

        new(
            "9" + BINARY_OP_DIVIDE + "3",
            new(3.0) // Division always converts operands to be Chow floats
        ),

        new(
            "17" + BINARY_OP_FLOOR + "4",
            new(4)
        ),

        new(
            "3" + BINARY_OP_MOD + "2",
            new(1)
        ),

        new(
            "11" + BINARY_OP_POW + "3",
            new(1331)
        ),

        #endregion

        #region Negative Integer Operands

        new(
            "-1" + BINARY_OP_PLUS + "-2",
            new(-3)
        ),

        new(
            "-6" + BINARY_OP_MINUS + "-3",
            new(-3)
        ),

        new(
            "-7" + BINARY_OP_TIMES + "-8",
            new(56)
        ),

        new(
            "-9" + BINARY_OP_DIVIDE + "-3",
            new(3.0) // Division always converts operands to be Chow floats
        ),

        new(
            "-17" + BINARY_OP_FLOOR + "-4",
            new(4)
        ),

        new(
            "-3" + BINARY_OP_MOD + "-2",
            new(-1)
        ),

        new(
            "-11" + BINARY_OP_POW + "-3",
            new(-0.0007513148009015778)
        ),

        #endregion

        #region Positive Float Operands

        new(
            "1.2" + BINARY_OP_PLUS + "2.3",
            new(3.5)
        ),

        new(
            "6.2" + BINARY_OP_MINUS + "3.0",
            new(3.2)
        ),

        new(
            "7.35" + BINARY_OP_TIMES + "8.002",
            new(58.8147)
        ),

        new(
            "9.245" + BINARY_OP_DIVIDE + "0.5",
            new(18.49)
        ),

        new(
            "17.8" + BINARY_OP_FLOOR + "4.2",
            new(4.0)
        ),

        // Expected: 0.94
        // But was: 0.9399999999999997
        new(
            "3.34" + BINARY_OP_MOD + "1.2",
            new(0.94)
        ),

        new(
            "11.5" + BINARY_OP_POW + "2.3",
            new(275.1725020936858)
        ),

        #endregion

        #region Negative Float Operands

        new(
            "-1.2" + BINARY_OP_PLUS + "-2.3",
            new(-3.5)
        ),

        new(
            "-6.2" + BINARY_OP_MINUS + "-3.0",
            new(-3.2)
        ),

        new(
            "-7.35" + BINARY_OP_TIMES + "-8.002",
            new(58.8147)
        ),

        new(
            "-9.245" + BINARY_OP_DIVIDE + "-0.5",
            new(18.49)
        ),

        new(
            "-17.8" + BINARY_OP_FLOOR + "-4.2",
            new(4.0)
        ),

        // Expected: 0.94
        // But was: 0.9399999999999997
        new(
            "-3.34" + BINARY_OP_MOD + "-1.2",
            new(-0.94)
        ),

        new(
            "-11.5" + BINARY_OP_POW + "-2.3",
            new(-0.0036340840468846625)
        ),

        #endregion

        #region Positive Boolean Operands

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_PLUS + LITERAL_BOOL_TRUE,
            new(2)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_PLUS + LITERAL_BOOL_FALSE,
            new(1)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_PLUS + LITERAL_BOOL_FALSE,
            new(0)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_MINUS + LITERAL_BOOL_TRUE,
            new(0)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_MINUS + LITERAL_BOOL_FALSE,
            new(1)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_MINUS + LITERAL_BOOL_FALSE,
            new(0)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_TIMES + LITERAL_BOOL_TRUE,
            new(1)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_TIMES + LITERAL_BOOL_FALSE,
            new(0)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_TIMES + LITERAL_BOOL_FALSE,
            new(0)
        ),

        // Note: Exclude False as right operand for division, floor, & modulus tests because False
        // as a Chow float is 0.0, and it would result in a zero-division exception.

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_DIVIDE + LITERAL_BOOL_TRUE,
            new(1.0)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_DIVIDE + LITERAL_BOOL_TRUE,
            new(0.0)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_FLOOR + LITERAL_BOOL_TRUE,
            new(1)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_FLOOR + LITERAL_BOOL_TRUE,
            new(0)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_MOD + LITERAL_BOOL_TRUE,
            new(0)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_MOD + LITERAL_BOOL_TRUE,
            new(0)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_POW + LITERAL_BOOL_TRUE,
            new(1)
        ),

        new(
            LITERAL_BOOL_TRUE + BINARY_OP_POW + LITERAL_BOOL_FALSE,
            new(1)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_POW + LITERAL_BOOL_TRUE,
            new(0)
        ),

        new(
            LITERAL_BOOL_FALSE + BINARY_OP_POW + LITERAL_BOOL_FALSE,
            new(1)
        ),

        #endregion

        #region Precedence And Associativity

        // Multiplication binds tighter than addition
        new(
            "1" + BINARY_OP_PLUS + "2" + BINARY_OP_TIMES + "3",
            new(7)
        ),

        // Parentheses override precedence
        new(
            "(1" + BINARY_OP_PLUS + "2)" + BINARY_OP_TIMES + "3",
            new(9)
        ),

        // Subtraction is left-associative
        new(
            "10" + BINARY_OP_MINUS + "3" + BINARY_OP_MINUS + "2",
            new(5)
        ),

        // Division is left-associative (and always yields a Chow float)
        new(
            "100" + BINARY_OP_DIVIDE + "10" + BINARY_OP_DIVIDE + "2",
            new(5.0)
        ),

        // Exponentiation is right-associative: 2 ** (3 ** 2) = 2 ** 9
        new(
            "2" + BINARY_OP_POW + "3" + BINARY_OP_POW + "2",
            new(512)
        ),

        // Parentheses force left-associative exponentiation: (2 ** 3) ** 2
        new(
            "(2" + BINARY_OP_POW + "3)" + BINARY_OP_POW + "2",
            new(64)
        ),

        // Exponentiation binds tighter than multiplication: 2 * (3 ** 2)
        new(
            "2" + BINARY_OP_TIMES + "3" + BINARY_OP_POW + "2",
            new(18)
        ),

        // Parentheses override precedence: (2 * 3) ** 2
        new(
            "(2" + BINARY_OP_TIMES + "3)" + BINARY_OP_POW + "2",
            new(36)
        ),

        #endregion
    ];

    static readonly IReadOnlyList<TestCaseExecute> ExecuteUnaryCases =
    [
        // Note: Passing the ChowValue constructor a long or an int will result in a ChowValue
        // instance with DataType.Int. However, passing a float or a double will result in an
        // instance with DataType.Float. Always double-check the literal passed to the constructor.

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

        new(
            "---3",
            new(3)
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
            "-True",
            new(-1)
        ),

        new(
            "-False",
            new(0)
        ),

        new(
            "-(True)",
            new(-1)
        ),

        new(
            "-(False)",
            new(0)
        ),

        #endregion

        #region Negation With Exponentiation

        new(
            "-2" + BINARY_OP_POW + "2",
            new(-4)
        ),

        new(
            "-(2)" + BINARY_OP_POW + "2",
            new(-4)
        ),

        new(
            "2" + BINARY_OP_POW + "-2",
            new(0.25)
        ),

        new(
            "-2" + BINARY_OP_POW + "-2",
            new(-0.25)
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

        #region Negation With Floor Division And Modulus

        new(
            "-3" + BINARY_OP_FLOOR + "2",
            new(-2)
        ),

        new(
            "3" + BINARY_OP_FLOOR + "-2",
            new(-2)
        ),

        new(
            "-3" + BINARY_OP_MOD + "2",
            new(1)
        ),

        new(
            "3" + BINARY_OP_MOD + "-2",
            new(-1)
        ),

        #endregion
    ];
    
    static readonly IReadOnlyList<TestCaseExecute> ExecuteEmptyWhitespaceOrNullCases =
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
    
    [TestCaseSource(nameof(ExecuteUnaryCases))]
    [TestCaseSource(nameof(ExecuteEmptyWhitespaceOrNullCases))]
    [TestCaseSource(nameof(ExecuteBinaryArithmeticCases))]
    public void Execute_ValidSourceCode_ReturnExpectedResult(TestCaseExecute testCaseExecute)
    {
        var returnValue = ChowEngine.Execute(testCaseExecute.SourceCode);
        
        Assert.That(returnValue, Is.EqualTo(testCaseExecute.ExpectedResult));
    }

    #region Constants

    const string LITERAL_BOOL_TRUE = "True";
    const string LITERAL_BOOL_FALSE = "False";

    // Binary operator constants make it easier to scan and see where and what operator is being used.
    const string BINARY_OP_PLUS = " + ";
    const string BINARY_OP_MINUS = " - ";
    const string BINARY_OP_TIMES = " * ";
    const string BINARY_OP_DIVIDE = " / ";
    const string BINARY_OP_MOD = " % ";
    const string BINARY_OP_FLOOR = " // ";
    const string BINARY_OP_POW = " ** ";
    

    const string NEWLINE_LINUX_MAC = "\n";
    const string NEWLINE_WINDOWS = "\r\n";
    const string NEWLINE_OLD_MAC = "\r";

    const string SINGLE_INDENT_SPACES = "    "; // Indents used for blocks are 4 spaces
    const string SINGLE_INDENT_TAB = "\t";

    const string CODE_COMMENT = "# This is a comment";
    
    #endregion
}
