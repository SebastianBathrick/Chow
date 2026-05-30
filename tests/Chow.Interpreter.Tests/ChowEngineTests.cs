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

    static readonly IReadOnlyList<TestCaseExecute> BinaryArithmeticOperatorCases =
    [
        // Note: Passing the ChowValue constructor a long or an int will result in a ChowValue
        // instance with DataType.Int. However, passing a float or a double will result in an
        // instance with DataType.Float. Always double-check the literal passed to the constructor.

        #region Basic Positive Integer Operands
        
        new(
            "1 + 2",
            new(3)
        ),

        new(
            "6 - 3",
            new(3)
        ),

        new(
            "7 * 8",
            new(56)
        ),

        new(
            "9 / 3",
            new(3.0) // Division always converts operands to be Chow floats
        ),

        new(
            "17 // 4",
            new(4)
        ),
        
        
        new(
            "3 % 2",
            new(1)
        ),
        
        new(
            "11 ** 3",
            new(1331)
        ),

        #endregion

        #region Basic Positive Float Operands

        new(
            "1.2 + 2.3",
            new(3.5)
        ),

        new(
            "6.2 - 3.0",
            new(3.2)
        ),

        new(
            "7.35 * 8.002",
            new(58.8147)
        ),

        new(
            "9.245 / 0.5",
            new(18.49)
        ),

        new(
            "17.8 // 4.2",
            new(4.0)
        ),
        
        // Expected: 0.94
        // But was: 0.9399999999999997
        new(
            "3.34 % 1.2",
            new(0.94)
        ),
        
        new(
            "11.5 ** 2.3",
            new(275.1725020936858)
        ),

        #endregion
        
        #region Basic Negative Integer Operands
        
        new(
            "-1 + -2",
            new(-3)
        ),

        new(
            "-6 - -3",
            new(-3)
        ),

        new(
            "-7 * -8",
            new(56)
        ),

        new(
            "-9 / -3",
            new(3.0) // Division always converts operands to be Chow floats
        ),

        new(
            "-17 // -4",
            new(4)
        ),
        
        
        new(
            "-3 % -2",
            new(-1)
        ),
        
        new(
            "-11 ** -3",
            new(-0.0007513148009015778)
        ),

        #endregion

    ];
    
    [TestCaseSource(nameof(BinaryArithmeticOperatorCases))]
    public void Execute_BinaryArithmeticOperators_ReturnExpectedResult(TestCaseExecute testCaseExecute)
    {
        var returnValue = ChowEngine.Execute(testCaseExecute.SourceCode);
        
        Assert.That(returnValue, Is.EqualTo(testCaseExecute.ExpectedResult));
    }
}
