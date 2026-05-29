namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class ChowEngineTests
    {
        const string CHOW_ADD_MULTIPLY_PRECEDENCE = "2 + 3 * 4";
        const long CHOW_ADD_MULTIPLY_PRECEDENCE_RESULT = 14L;
        const string CHOW_ADD_MULTIPLY_PARENTHESIS_PRECEDENCE = "(2 + 3) * 4";
        const long CHOW_ADD_MULTIPLY_PARENTHESIS_PRECEDENCE_RESULT = 20L;
        const string CHOW_FLOAT_DIVISION = "10 / 4";
        const double CHOW_FLOAT_DIVISION_RESULT = 2.5;
        const string CHOW_FLOOR_DIVISION = "10 // 4";
        const long CHOW_FLOOR_DIVISION_RESULT = 2L;
        const string CHOW_MODULO = "10 % 4";
        const long CHOW_MODULO_RESULT = 2L;
        const string CHOW_EXPONENT_PRECEDENCE = "2 ** 3 ** 2";
        const long CHOW_EXPONENT_PRECEDENCE_RESULT = 512L;
        
        static void SetupChowEngine()
        {
            ChowEngine.Reset();
        }
        
        

        [TestCase()]
        static IEnumerable<TestCaseData> Execute_ExpressionStatement_ReturnsCorrectValue()
        {
            yield return new  TestCaseData
        }
    }
}
